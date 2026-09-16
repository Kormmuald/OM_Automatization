using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.Excel;

public sealed record StagedWorkbookPair(Guid RunId, Guid PairId, string ModelPath, string LookupPath, string ModelSha256, string LookupSha256, string PairDigest, string PairBaselineHash, IReadOnlyDictionary<string, int> ProjectionCounts);

public sealed class WorkbookPairMaterializer
{
    public ValueTask<StagedWorkbookPair> StageValidatedPairAsync(CatalogQualification qualification, string stagingDirectory, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateInput(qualification);
        return StagePairAsync(qualification.Snapshot!, qualification.PassB, stagingDirectory, cancellationToken);
    }

    /// <summary>Writes a single-read, explicitly unverified pair without crossing qualification gates.</summary>
    public ValueTask<StagedWorkbookPair> StageBestEffortPairAsync(QualifiedCatalogSnapshot snapshot, string stagingDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!snapshot.ExportNotice.StartsWith("UNVERIFIED_SINGLE_READ", StringComparison.Ordinal)) throw new InvalidDataException("BEST_EFFORT_NOTICE_REQUIRED");
        return StagePairAsync(snapshot, null, stagingDirectory, cancellationToken);
    }

    private static ValueTask<StagedWorkbookPair> StagePairAsync(QualifiedCatalogSnapshot snapshot, CatalogPass? qualifiedPass, string stagingDirectory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fullStaging = Path.GetFullPath(stagingDirectory);
        if (Directory.Exists(fullStaging) || File.Exists(fullStaging)) throw new IOException("WORKBOOK_STAGING_ALREADY_EXISTS");
        var projection = WorkbookPairProjection.Create(snapshot);
        if (qualifiedPass is not null) ValidateScaleBeforeWriting(snapshot, qualifiedPass, projection);
        try
        {
            PrivateWorkbookStaging.CreateDirectory(fullStaging);
            var modelPath = Path.Combine(fullStaging, WorkbookContract.ModelFileName);
            var lookupPath = Path.Combine(fullStaging, WorkbookContract.LookupFileName);
            WorkbookPackageWriter.Write(modelPath, projection.Model);
            WorkbookPackageWriter.Write(lookupPath, projection.Lookup);
            PrivateWorkbookStaging.RestrictFile(modelPath);
            PrivateWorkbookStaging.RestrictFile(lookupPath);
            ValidateWorkbook(modelPath, projection.Model, WorkbookContract.ModelSheetOrder, WorkbookContract.ModelHeaders);
            ValidateWorkbook(lookupPath, projection.Lookup, WorkbookContract.LookupSheetOrder, WorkbookContract.LookupHeaders);
            var pairReadBack = WorkbookPairReader.Read(modelPath, lookupPath);
            if (pairReadBack.RunId != snapshot.RunId || pairReadBack.PairId != snapshot.PairId ||
                qualifiedPass is not null && (pairReadBack.Model.CanonicalDigest() != projection.Model.CanonicalDigest() || pairReadBack.Lookup.CanonicalDigest() != projection.Lookup.CanonicalDigest()))
                throw new InvalidDataException("WORKBOOK_READBACK_PROJECTION_INVALID");
            var modelHash = WorkbookHash.File(modelPath);
            var lookupHash = WorkbookHash.File(lookupPath);
            var pairDigest = WorkbookHash.Json(new { runId = snapshot.RunId.ToString("D"), pairId = snapshot.PairId.ToString("D"), modelHash, lookupHash, projection.PairBaselineHash });
            var counts = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["workspaceItems"] = projection.Model.Sheet("WorkspaceInventory").Rows.Count,
                ["schemas"] = projection.Model.Sheet("Schemas").Rows.Count,
                ["columns"] = projection.Model.Sheet("Columns").Rows.Count,
                ["indexMembers"] = projection.Model.Sheet("Indexes").Rows.Count,
                ["lookupRegistry"] = projection.Lookup.Sheet("LookupRegistry").Rows.Count,
                ["lookupValues"] = projection.Lookup.Sheet("LookupValues").Rows.Count
            };
            return ValueTask.FromResult(new StagedWorkbookPair(snapshot.RunId, snapshot.PairId, modelPath, lookupPath, modelHash, lookupHash, pairDigest, projection.PairBaselineHash, counts));
        }
        catch
        {
            if (Directory.Exists(fullStaging)) Directory.Delete(fullStaging, true);
            throw;
        }
    }

    private static void ValidateScaleBeforeWriting(QualifiedCatalogSnapshot snapshot, CatalogPass passB, WorkbookPairProjection projection)
    {
        var actualRows = projection.Model.Sheets.Select(sheet => new KeyValuePair<string, int>("Model." + sheet.Name, checked(sheet.Rows.Count + 1)))
            .Concat(projection.Lookup.Sheets.Select(sheet => new KeyValuePair<string, int>("Lookup." + sheet.Name, checked(sheet.Rows.Count + 1))))
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        var expectedRows = WorkbookScaleForecast.ProjectCatalogPairWorksheets(snapshot.Counts, snapshot.ComponentDigests.Count);
        if (!actualRows.OrderBy(item => item.Key, StringComparer.Ordinal).SequenceEqual(expectedRows.OrderBy(item => item.Key, StringComparer.Ordinal)))
            throw new InvalidDataException("WORKBOOK_SCALE_PROJECTION_MISMATCH");
        var actualScale = WorkbookScaleForecast.Create(actualRows, passB.DeclaredWorkbookRowLimit);
        if (actualScale.Status != WorkbookScaleForecastStatus.DiagnosticOnly || actualScale != snapshot.Scale)
            throw new InvalidDataException("WORKBOOK_SCALE_DECISION_REQUIRED");
    }

    private static void ValidateInput(CatalogQualification qualification)
    {
        ArgumentNullException.ThrowIfNull(qualification);
        var snapshot = qualification.Snapshot;
        var passA = qualification.PassA;
        var passB = qualification.PassB;
        if (!qualification.IsQualified || !qualification.Result.IsSuccess || qualification.Result.Reason != "HUMAN_REVIEW_REQUIRED" || qualification.RetryCount != 0 || snapshot is null || passA is null || passB is null ||
            snapshot.Schema != QualifiedCatalogSnapshot.SchemaVersion || snapshot.RunId == Guid.Empty || snapshot.RunId != qualification.RunId || snapshot.PairId == Guid.Empty || !snapshot.Scope.IsSealed ||
            snapshot.Scale.Status != WorkbookScaleForecastStatus.DiagnosticOnly || snapshot.Scale.Schema != WorkbookScaleForecast.SchemaVersion || snapshot.PullStartedUtc == default || snapshot.PullCompletedUtc < snapshot.PullStartedUtc ||
            passA.Ordinal != CatalogPassOrdinal.A || passB.Ordinal != CatalogPassOrdinal.B || passA.IndependentReadId == passB.IndependentReadId ||
            snapshot.PassADigest != passA.ReconciliationDigest || snapshot.PassBDigest != passB.ReconciliationDigest || snapshot.Scope.Digest != passB.Scope.Digest || snapshot.SourceIdentity != passB.SourceIdentity ||
            snapshot.TargetFingerprint.Digest != passB.Fingerprint.Digest || !ReferenceEquals(snapshot.Workspace, passB.Content.Workspace) || !ReferenceEquals(snapshot.Lookups, passB.Content.Lookups) ||
            !CatalogPassBuilder.IsCurrentContentBindingValid(passB) ||
            !snapshot.ComponentDigests.SequenceEqual(passB.ComponentDigests) || !snapshot.OrderedIdentities.SequenceEqual(passB.OrderedIdentities, StringComparer.Ordinal) ||
            !snapshot.Counts.OrderBy(item => item.Key, StringComparer.Ordinal).SequenceEqual(passB.Counts.OrderBy(item => item.Key, StringComparer.Ordinal)) ||
            !snapshot.Workspace.IsQualified || snapshot.Workspace.Blocker is not null || !snapshot.Lookups.IsQualified || snapshot.Lookups.Blocker is not null)
            throw new InvalidDataException("QUALIFIED_SNAPSHOT_REQUIRED");
    }

    private static void ValidateWorkbook(string path, WorkbookProjection expected, IReadOnlyList<string> sheetOrder, IReadOnlyDictionary<string, string[]> headers)
    {
        var package = WorkbookPackageInspector.Validate(path);
        if (!package.IsValid) throw new InvalidDataException("WORKBOOK_OOXML_INVALID:" + string.Join(',', package.Errors));
        var styleErrors = WorkbookPackageInspector.ValidateProjectionStyles(path, expected);
        if (styleErrors.Count != 0) throw new InvalidDataException("WORKBOOK_STYLE_INVALID:" + string.Join(',', styleErrors));
        var actual = WorkbookPairReader.ReadWorkbook(path, expected.Kind);
        if (!actual.Sheets.Select(sheet => sheet.Name).SequenceEqual(sheetOrder, StringComparer.Ordinal)) throw new InvalidDataException("WORKBOOK_SHEET_ORDER_INVALID");
        foreach (var sheet in actual.Sheets)
        {
            if (!sheet.Headers.SequenceEqual(headers[sheet.Name], StringComparer.Ordinal) || sheet.ReadOnly != WorkbookContract.IsReadOnly(sheet.Name) || sheet.Hidden != (sheet.Name == "ValidationLists")) throw new InvalidDataException("WORKBOOK_CONTRACT_INVALID:" + sheet.Name);
        }
    }
}
