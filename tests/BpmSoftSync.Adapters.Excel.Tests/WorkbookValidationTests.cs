using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using System.Security.AccessControl;
using BpmSoftSync.Adapters.Excel;
using BpmSoftSync.Adapters.FileSystem;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.Excel.Tests;

public static class WorkbookValidationTests
{
    public static async Task GeneratedPairIsClosedDeterministicAndBoundOneToOneAsync()
    {
        var qualification = SyntheticSnapshot.CreateQualification();
        var first = NewRoot();
        var second = NewRoot();
        try
        {
            var materializer = new WorkbookPairMaterializer();
            var one = await materializer.StageValidatedPairAsync(qualification, first);
            var two = await materializer.StageValidatedPairAsync(qualification, second);
            WorkbookContractTests.Assert(one.ModelSha256 == two.ModelSha256 && one.LookupSha256 == two.LookupSha256 && one.PairDigest == two.PairDigest, "Same snapshot did not produce deterministic workbook hashes.");
            WorkbookContractTests.Assert(one.RunId == qualification.RunId && one.PairId == qualification.Snapshot!.PairId, "Pair identity differs from qualified snapshot.");
            if (OperatingSystem.IsWindows())
            {
                WorkbookContractTests.Assert(new DirectoryInfo(first).GetAccessControl().AreAccessRulesProtected, "Raw-value staging directory inherited broad Windows permissions.");
                WorkbookContractTests.Assert(new FileInfo(one.LookupPath).GetAccessControl().AreAccessRulesProtected, "Raw-value lookup workbook inherited broad Windows permissions.");
            }
            else
            {
                WorkbookContractTests.Assert((File.GetUnixFileMode(first) & (UnixFileMode.GroupRead | UnixFileMode.OtherRead)) == 0, "Raw-value staging directory is group/world-readable.");
                WorkbookContractTests.Assert((File.GetUnixFileMode(one.LookupPath) & (UnixFileMode.GroupRead | UnixFileMode.OtherRead)) == 0, "Raw-value lookup workbook is group/world-readable.");
            }

            foreach (var path in new[] { one.ModelPath, one.LookupPath })
            {
                var report = WorkbookPackageInspector.Validate(path);
                WorkbookContractTests.Assert(report.IsValid && report.FormulaCellCount == 0 && report.ExternalRelationshipCount == 0 && report.ForbiddenPartCount == 0, $"OOXML closure/security failed for {path}: {string.Join(",", report.Errors)}");
                using var archive = ZipFile.OpenRead(path);
                var workbookXml = Read(archive, "xl/workbook.xml");
                WorkbookContractTests.Assert(workbookXml.Contains("state=\"hidden\"", StringComparison.Ordinal), "ValidationLists is not hidden.");
                WorkbookContractTests.Assert(!archive.Entries.Any(entry => entry.FullName.Contains("externalLinks", StringComparison.OrdinalIgnoreCase) || entry.FullName.Contains("vbaProject", StringComparison.OrdinalIgnoreCase) || entry.FullName.Contains("connections", StringComparison.OrdinalIgnoreCase)), "Forbidden OOXML part exists.");
            }

            var readBack = WorkbookPairReader.Read(one.ModelPath, one.LookupPath);
            WorkbookContractTests.Assert(readBack.PairId == qualification.Snapshot!.PairId && readBack.RunId == qualification.RunId, "Read-back pair binding failed.");
            WorkbookContractTests.Assert(readBack.Model.Sheet("Columns").Rows.Count == qualification.Snapshot!.Counts["columns"] && readBack.Lookup.Sheet("LookupValues").Rows.Count == qualification.Snapshot.Counts["lookupValues"], "Read-back changed projected row counts.");
        }
        finally { Delete(first); Delete(second); }
    }

    public static async Task PublicationFaultsNeverExposePartialPairAsync()
    {
        foreach (var checkpoint in Enum.GetValues<WorkbookPublicationCheckpoint>())
        {
                var root = NewRoot();
            try
            {
                var store = new AppendOnlyRunStore(root);
                var qualification = await SyntheticSnapshot.CreateAcceptedQualificationAsync(store);
                var publisher = new AtomicWorkbookPairPublisher(store, new WorkbookPairMaterializer(), new ThrowAt(checkpoint));
                var result = await publisher.PublishAsync(qualification);
                var run = store.GetRunRoot(qualification.RunId);
                WorkbookContractTests.Assert(!result.IsSuccess, $"Fault {checkpoint} unexpectedly succeeded.");
                WorkbookContractTests.Assert(!Directory.EnumerateFileSystemEntries(run.OutputPath).Any(), $"Fault {checkpoint} exposed a partial or successful pair.");
                WorkbookContractTests.Assert(!File.Exists(Path.Combine(run.RootPath, ".sealed")), $"Fault {checkpoint} sealed the run.");
                WorkbookContractTests.Assert(!File.Exists(Path.Combine(run.EvidencePath, "workbook-pair.json")) && !File.Exists(Path.Combine(run.EvidencePath, "review-only-seal.json")), $"Fault {checkpoint} left S05 evidence without output.");
                WorkbookContractTests.Assert(!File.ReadAllText(run.JournalPath).Contains("\"stableKey\":\"workbook-pair\"", StringComparison.Ordinal), $"Fault {checkpoint} left S05 journal records.");
            }
            finally { Delete(root); }
        }

        foreach (var checkpoint in new[] { WorkbookPublicationCheckpoint.AfterPublication, WorkbookPublicationCheckpoint.AfterPairEvidence, WorkbookPublicationCheckpoint.AfterSeal })
        {
            var root = NewRoot();
            try
            {
                var store = new AppendOnlyRunStore(root);
                var qualification = await SyntheticSnapshot.CreateAcceptedQualificationAsync(store);
                var result = await new AtomicWorkbookPairPublisher(store, new WorkbookPairMaterializer(), new CancelAt(checkpoint)).PublishAsync(qualification);
                var run = store.GetRunRoot(qualification.RunId);
                WorkbookContractTests.Assert(!result.IsSuccess && !Directory.EnumerateFileSystemEntries(run.OutputPath).Any(), $"Cancellation at {checkpoint} left visible output.");
                WorkbookContractTests.Assert(!File.Exists(Path.Combine(run.RootPath, ".sealed")) && !File.Exists(Path.Combine(run.EvidencePath, "workbook-pair.json")) && !File.Exists(Path.Combine(run.EvidencePath, "review-only-seal.json")), $"Cancellation at {checkpoint} left evidence/seal state.");
            }
            finally { Delete(root); }
        }

        foreach (var checkpoint in new[] { WorkbookPublicationCheckpoint.AfterPublication, WorkbookPublicationCheckpoint.AfterPairEvidence, WorkbookPublicationCheckpoint.AfterSeal })
        {
            var root = NewRoot();
            try
            {
                var store = new AppendOnlyRunStore(root);
                var qualification = await SyntheticSnapshot.CreateAcceptedQualificationAsync(store);
                var result = await new AtomicWorkbookPairPublisher(store, new WorkbookPairMaterializer(), new ThrowInvalidAt(checkpoint)).PublishAsync(qualification);
                var run = store.GetRunRoot(qualification.RunId);
                WorkbookContractTests.Assert(!result.IsSuccess && !Directory.EnumerateFileSystemEntries(run.OutputPath).Any(), $"InvalidOperationException at {checkpoint} left visible output.");
                WorkbookContractTests.Assert(!File.Exists(Path.Combine(run.RootPath, ".sealed")) && !File.Exists(Path.Combine(run.EvidencePath, "workbook-pair.json")) && !File.Exists(Path.Combine(run.EvidencePath, "review-only-seal.json")), $"InvalidOperationException at {checkpoint} left evidence/seal state.");
            }
            finally { Delete(root); }
        }

        var mutatedGraphRoot = NewRoot();
        try
        {
            var store = new AppendOnlyRunStore(mutatedGraphRoot);
            var qualification = await SyntheticSnapshot.CreateAcceptedQualificationAsync(store);
            var values = (NormalizedLookupValue[])qualification.PassB!.Content.Lookups.Collections[0].Rows[0].Values;
            values[1] = values[1] with { TypedValue = new LookupTextValue("post-acceptance-mutation"), CanonicalValue = "post-acceptance-mutation" };
            var result = await new AtomicWorkbookPairPublisher(store, new WorkbookPairMaterializer()).PublishAsync(qualification);
            var run = store.GetRunRoot(qualification.RunId);
            WorkbookContractTests.Assert(!result.IsSuccess && !Directory.EnumerateFileSystemEntries(run.OutputPath).Any() && !Directory.EnumerateDirectories(run.RootPath, ".pair-staging-*", SearchOption.TopDirectoryOnly).Any(), "Post-acceptance nested Pass-B mutation crossed the persisted snapshot binding gate.");
        }
        finally { Delete(mutatedGraphRoot); }

        var recoveryRoot = NewRoot();
        try
        {
            var store = new AppendOnlyRunStore(recoveryRoot);
            var qualification = await SyntheticSnapshot.CreateAcceptedQualificationAsync(store);
            var run = store.GetRunRoot(qualification.RunId);
            var crashStaging = Path.Combine(run.RootPath, ".pair-staging-crash-window");
            await new WorkbookPairMaterializer().StageValidatedPairAsync(qualification, crashStaging);
            Directory.Delete(run.OutputPath, false);
            Directory.Move(crashStaging, run.OutputPath);
            File.Copy(Path.Combine(run.EvidencePath, "qualified-snapshot.json"), Path.Combine(run.EvidencePath, "workbook-pair.json"));
            File.Copy(Path.Combine(run.EvidencePath, "qualified-snapshot.json"), Path.Combine(run.EvidencePath, "review-only-seal.json"));
            File.WriteAllText(Path.Combine(run.RootPath, ".sealed"), "review-only-seal");
            File.WriteAllText(Path.Combine(run.RootPath, ".workbook-publication.pending.json"), "{\"schema\":\"WorkbookPublicationPending/v1\"}");
            File.AppendAllText(run.JournalPath, "{\"stableKey\":\"workbook-pair\"");
            var orphanStaging = Path.Combine(run.RootPath, ".pair-staging-orphan");
            Directory.CreateDirectory(orphanStaging);
            File.WriteAllText(Path.Combine(orphanStaging, "raw.tmp"), SyntheticSnapshot.RawLookupValue);
            var result = await new AtomicWorkbookPairPublisher(store, new WorkbookPairMaterializer()).PublishAsync(qualification);
            WorkbookContractTests.Assert(result.IsSuccess && Directory.EnumerateFiles(run.OutputPath, "*.xlsx").Count() == 2, "Subsequent invocation did not recover and replace the crash-window orphan output.");
            WorkbookContractTests.Assert(!Directory.Exists(orphanStaging) && !Directory.EnumerateDirectories(run.RootPath, ".pair-staging-*", SearchOption.TopDirectoryOnly).Any(), "Startup recovery retained a raw-value staging orphan.");
        }
        finally { Delete(recoveryRoot); }

        var unacceptedRoot = NewRoot();
        try
        {
            var store = new AppendOnlyRunStore(unacceptedRoot);
            var runId = await store.BeginRunAsync("fixture");
            var qualification = SyntheticSnapshot.CreateQualification() with { RunId = runId, Snapshot = SyntheticSnapshot.CreateQualification().Snapshot! with { RunId = runId } };
            var result = await new AtomicWorkbookPairPublisher(store, new WorkbookPairMaterializer()).PublishAsync(qualification);
            var run = store.GetRunRoot(runId);
            WorkbookContractTests.Assert(!result.IsSuccess && !Directory.EnumerateFileSystemEntries(run.OutputPath).Any() && !Directory.EnumerateDirectories(run.RootPath, ".pair-staging-*", SearchOption.TopDirectoryOnly).Any(), "BeginRun-only qualification crossed the persisted S04 gate or created staging.");
        }
        finally { Delete(unacceptedRoot); }

        var mismatchedRoot = NewRoot();
        try
        {
            var store = new AppendOnlyRunStore(mismatchedRoot);
            var qualification = await SyntheticSnapshot.CreateAcceptedQualificationAsync(store);
            var mismatched = qualification with { Snapshot = qualification.Snapshot! with { PairId = Guid.NewGuid() } };
            var result = await new AtomicWorkbookPairPublisher(store, new WorkbookPairMaterializer()).PublishAsync(mismatched);
            var run = store.GetRunRoot(qualification.RunId);
            WorkbookContractTests.Assert(!result.IsSuccess && !Directory.EnumerateFileSystemEntries(run.OutputPath).Any() && !Directory.EnumerateDirectories(run.RootPath, ".pair-staging-*", SearchOption.TopDirectoryOnly).Any(), "Persisted qualified-snapshot binding did not reject a changed PairId before staging.");
        }
        finally { Delete(mismatchedRoot); }

        var successRoot = NewRoot();
        try
        {
            var store = new AppendOnlyRunStore(successRoot);
            var qualification = await SyntheticSnapshot.CreateAcceptedQualificationAsync(store);
            var result = await new AtomicWorkbookPairPublisher(store, new WorkbookPairMaterializer()).PublishAsync(qualification);
            var run = store.GetRunRoot(qualification.RunId);
            WorkbookContractTests.Assert(result.IsSuccess && result.Pair is not null && Directory.EnumerateDirectories(run.OutputPath).Count() == 0, "Canonical output contains an unapproved pair subdirectory.");
            WorkbookContractTests.Assert(Directory.EnumerateFiles(run.OutputPath, "*.xlsx").Select(Path.GetFileName).OrderBy(value => value).SequenceEqual(new[] { WorkbookContract.LookupFileName, WorkbookContract.ModelFileName }), "Validated pair was not published as canonical output/*.xlsx.");
            WorkbookContractTests.Assert(File.Exists(Path.Combine(run.EvidencePath, "workbook-pair.json")) && File.Exists(Path.Combine(run.EvidencePath, "review-only-seal.json")), "Pair evidence/final seal missing.");
            var safeText = string.Join("\n", Directory.EnumerateFiles(run.RootPath, "*", SearchOption.AllDirectories).Where(path => !path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)).Select(File.ReadAllText));
            WorkbookContractTests.Assert(!safeText.Contains(SyntheticSnapshot.RawLookupValue, StringComparison.Ordinal), "Raw lookup value escaped workbook output.");
        }
        finally { Delete(successRoot); }
    }

    public static async Task FormulaExternalAndVbaTamperingIsRejectedAsync()
    {
        var root = NewRoot();
        try
        {
            var pair = await new WorkbookPairMaterializer().StageValidatedPairAsync(SyntheticSnapshot.CreateQualification(), Path.Combine(root, "clean"));
            var formula = Path.Combine(root, "formula.xlsx");
            var external = Path.Combine(root, "external.xlsx");
            var vba = Path.Combine(root, "vba.xlsx");
            File.Copy(pair.ModelPath, formula);
            File.Copy(pair.ModelPath, external);
            File.Copy(pair.ModelPath, vba);
            AddFormula(formula);
            AddExternalRelationship(external);
            AddPart(vba, "xl/vbaProject.bin", [1, 2, 3]);
            var formulaReport = WorkbookPackageInspector.Validate(formula);
            var externalReport = WorkbookPackageInspector.Validate(external);
            var vbaReport = WorkbookPackageInspector.Validate(vba);
            WorkbookContractTests.Assert(!formulaReport.IsValid && formulaReport.FormulaCellCount == 1, "Worksheet formula tampering was not rejected.");
            WorkbookContractTests.Assert(!externalReport.IsValid && externalReport.ExternalRelationshipCount == 1, "External relationship tampering was not rejected.");
            WorkbookContractTests.Assert(!vbaReport.IsValid && vbaReport.ForbiddenPartCount == 1, "VBA part tampering was not rejected.");

            foreach (var tamper in new Action<string>[] { TamperDefinedName, TamperValidationSqref, TamperValidationFormula, TamperProtection, TamperCellXfProtection, TamperAutoFilter, TamperWorkbookRelationship, TamperContentType })
            {
                var target = Path.Combine(root, Guid.NewGuid().ToString("N") + ".xlsx");
                File.Copy(pair.ModelPath, target);
                tamper(target);
                WorkbookContractTests.Assert(!WorkbookPackageInspector.Validate(target).IsValid, $"Deep OOXML tamper {tamper.Method.Name} was not rejected.");
            }
        }
        finally { Delete(root); }
    }

    public static async Task NonQualifiedInputsAreRejectedWithoutOutputAsync()
    {
        var root = NewRoot();
        try
        {
            var qualification = SyntheticSnapshot.CreateQualification();
            foreach (var invalid in new[]
            {
                qualification with { IsQualified = false },
                qualification with { Snapshot = null },
                qualification with { Snapshot = qualification.Snapshot! with { Schema = "QualifiedCatalogSnapshot/v2" } },
                qualification with { Snapshot = qualification.Snapshot! with { Scale = new WorkbookScaleForecast(WorkbookScaleForecast.SchemaVersion, WorkbookScaleForecastStatus.DecisionRequired, "0-100", "WORKBOOK_SCALE_DECISION_REQUIRED") } }
            })
            {
                var target = Path.Combine(root, Guid.NewGuid().ToString("N"));
                var rejected = false;
                try { await new WorkbookPairMaterializer().StageValidatedPairAsync(invalid, target); }
                catch (InvalidDataException) { rejected = true; }
                WorkbookContractTests.Assert(rejected && !Directory.Exists(target), "Invalid qualification created output.");
            }
        }
        finally { Delete(root); }
    }

    public static async Task ManifestScaleOverflowIsRejectedBeforeWorkbookGenerationAsync()
    {
        var qualification = SyntheticSnapshot.CreateQualification();
        var snapshot = qualification.Snapshot!;
        var projection = WorkbookPairProjection.Create(snapshot);
        var businessLimit = projection.Model.Sheets.Concat(projection.Lookup.Sheets)
            .Where(sheet => sheet.Name is not "Manifest" and not "Readme" and not "ValidationLists" and not "PullConflicts")
            .Max(sheet => sheet.Rows.Count + 1);
        var manifestRows = projection.Model.Sheet("Manifest").Rows.Count + 1;
        var forecastRows = WorkbookScaleForecast.ProjectCatalogPairWorksheets(snapshot.Counts, snapshot.ComponentDigests.Count);
        var actualRows = projection.Model.Sheets.Select(sheet => new KeyValuePair<string, int>("Model." + sheet.Name, sheet.Rows.Count + 1))
            .Concat(projection.Lookup.Sheets.Select(sheet => new KeyValuePair<string, int>("Lookup." + sheet.Name, sheet.Rows.Count + 1)))
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        WorkbookContractTests.Assert(forecastRows.Count == 14 && forecastRows.OrderBy(item => item.Key).SequenceEqual(actualRows.OrderBy(item => item.Key)), "Scale forecast does not cover every actual projected worksheet including its header.");
        WorkbookContractTests.Assert(businessLimit < manifestRows, "Synthetic boundary does not isolate a Manifest-only scale overflow.");

        var passA = qualification.PassA! with { DeclaredWorkbookRowLimit = businessLimit };
        var passB = qualification.PassB! with { DeclaredWorkbookRowLimit = businessLimit };
        var reconciled = CatalogQualification.Reconcile(passA, passB, qualification.RunId, snapshot.PairId, snapshot.PullStartedUtc, snapshot.PullCompletedUtc);
        WorkbookContractTests.Assert(!reconciled.IsQualified && reconciled.Result.Reason == "WORKBOOK_SCALE_DECISION_REQUIRED", "Qualification ignored the Manifest/header scale overflow while all business sheets fit.");

        var forged = qualification with { PassA = passA, PassB = passB };
        var target = NewRoot();
        var rejected = false;
        try { await new WorkbookPairMaterializer().StageValidatedPairAsync(forged, target); }
        catch (InvalidDataException error) when (error.Message == "WORKBOOK_SCALE_DECISION_REQUIRED") { rejected = true; }
        WorkbookContractTests.Assert(rejected && !Directory.Exists(target), "Writer guard generated an XLSX/staging directory before rejecting Manifest overflow.");
    }

    private sealed class ThrowAt(WorkbookPublicationCheckpoint target) : IWorkbookPublicationFaultInjector
    {
        public void Check(WorkbookPublicationCheckpoint checkpoint)
        {
            if (checkpoint == target) throw new IOException("INJECTED_" + checkpoint);
        }
    }

    private sealed class CancelAt(WorkbookPublicationCheckpoint target) : IWorkbookPublicationFaultInjector
    {
        public void Check(WorkbookPublicationCheckpoint checkpoint)
        {
            if (checkpoint == target) throw new OperationCanceledException("INJECTED_CANCEL_" + checkpoint);
        }
    }

    private sealed class ThrowInvalidAt(WorkbookPublicationCheckpoint target) : IWorkbookPublicationFaultInjector
    {
        public void Check(WorkbookPublicationCheckpoint checkpoint)
        {
            if (checkpoint == target) throw new InvalidOperationException("INJECTED_INVALID_" + checkpoint);
        }
    }

    private static string Read(ZipArchive archive, string name)
    {
        using var reader = new StreamReader(archive.GetEntry(name)!.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }
    private static void AddFormula(string path)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Update);
        var entry = archive.GetEntry("xl/worksheets/sheet1.xml")!;
        XDocument document;
        using (var stream = entry.Open()) document = XDocument.Load(stream);
        entry.Delete();
        XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        document.Descendants(main + "c").First().Add(new XElement(main + "f", "1+1"));
        var replacement = archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Optimal);
        using var output = replacement.Open();
        document.Save(output);
    }
    private static void AddExternalRelationship(string path)
    {
        const string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/externalLink\" Target=\"https://example.invalid/book.xlsx\" TargetMode=\"External\"/></Relationships>";
        AddPart(path, "_rels/external.rels", Encoding.UTF8.GetBytes(xml));
    }
    private static void TamperDefinedName(string path) => Rewrite(path, "xl/workbook.xml", document => document.Descendants(XName.Get("definedName", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")).First().Value = "'ValidationLists'!$A$2:$A$999");
    private static void TamperValidationSqref(string path) => Rewrite(path, "xl/worksheets/sheet4.xml", document => document.Descendants(XName.Get("dataValidation", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")).First().SetAttributeValue("sqref", "A1:XFD1048576"));
    private static void TamperValidationFormula(string path) => Rewrite(path, "xl/worksheets/sheet4.xml", document => document.Descendants(XName.Get("formula1", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")).First().Value = "MissingName");
    private static void TamperProtection(string path) => Rewrite(path, "xl/worksheets/sheet1.xml", document => document.Descendants(XName.Get("sheetProtection", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")).Remove());
    private static void TamperCellXfProtection(string path) => Rewrite(path, "xl/styles.xml", document => document.Descendants(XName.Get("cellXfs", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")).Elements().ElementAt(3).Descendants(XName.Get("protection", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")).Single().SetAttributeValue("locked", "1"));
    private static void TamperAutoFilter(string path) => Rewrite(path, "xl/worksheets/sheet1.xml", document => document.Descendants(XName.Get("autoFilter", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")).Single().SetAttributeValue("ref", "A1:XFD1048576"));
    private static void TamperWorkbookRelationship(string path) => Rewrite(path, "xl/_rels/workbook.xml.rels", document => document.Descendants(XName.Get("Relationship", "http://schemas.openxmlformats.org/package/2006/relationships")).First().SetAttributeValue("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/chartsheet"));
    private static void TamperContentType(string path) => Rewrite(path, "[Content_Types].xml", document => document.Descendants(XName.Get("Override", "http://schemas.openxmlformats.org/package/2006/content-types")).First().SetAttributeValue("ContentType", "application/xml"));
    private static void Rewrite(string path, string entryName, Action<XDocument> mutation)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Update);
        var entry = archive.GetEntry(entryName)!;
        XDocument document;
        using (var stream = entry.Open()) document = XDocument.Load(stream);
        entry.Delete();
        mutation(document);
        var replacement = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var output = replacement.Open();
        document.Save(output);
    }
    private static void AddPart(string path, string name, byte[] content)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Update);
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(content);
    }
    private static string NewRoot() => Path.Combine(Path.GetTempPath(), "BpmSoftSync-S05-" + Guid.NewGuid().ToString("N"));
    private static void Delete(string path) { if (Directory.Exists(path)) Directory.Delete(path, true); }
}
