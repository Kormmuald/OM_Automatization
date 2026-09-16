using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BpmSoftSync.Application;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.BpmSoft;

public sealed class LookupCatalogSource(BpmSoftReadTransport transport) : ILookupCatalogSource
{
    private static readonly IReadOnlyList<string> RegistryColumns = ["Id", "SysEntitySchemaUId"];
    private readonly BpmSoftReadTransport _transport = transport ?? throw new ArgumentNullException(nameof(transport));

    public ValueTask<LookupCatalog> ReadFullAsync(WorkspaceObjectModel objectModel, LookupReadLimits limits, CancellationToken cancellationToken = default) =>
        new(ReadCoreAsync(objectModel, limits, bestEffort: false, cancellationToken));

    /// <summary>Single-read export mode: unknown lookup types are retained as text rather than rejected.</summary>
    public ValueTask<LookupCatalog> ReadBestEffortAsync(WorkspaceObjectModel objectModel, LookupReadLimits limits, CancellationToken cancellationToken = default) =>
        new(ReadCoreAsync(objectModel, limits, bestEffort: true, cancellationToken));

    public static Task<LookupCatalog> ReadFullAsync(BpmSoftReadTransport transport, WorkspaceObjectModel objectModel, LookupReadLimits limits, CancellationToken cancellationToken = default) =>
        new LookupCatalogSource(transport).ReadCoreAsync(objectModel, limits, bestEffort: false, cancellationToken);

    private async Task<LookupCatalog> ReadCoreAsync(WorkspaceObjectModel objectModel, LookupReadLimits limits, bool bestEffort, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(objectModel);
        ArgumentNullException.ThrowIfNull(limits);
        if (!objectModel.IsQualified || objectModel.Blocker is not null)
            return LookupCatalog.Blocked([], null, [], ShapeBlocker("lookup-inventory", "LOOKUP_INVENTORY_NOT_QUALIFIED"));
        if (!limits.IsValid)
            return LookupCatalog.Blocked([], null, [], PagingBlocker("lookup-limits", "LOOKUP_READ_LIMITS_INVALID"));

        var registryRead = await ReadOrderedAsync(
            "Lookup",
            RegistryColumns,
            limits,
            row => ParseRegistryRow(row, objectModel),
            cancellationToken);
        if (!registryRead.IsQualified)
            return LookupCatalog.Blocked(registryRead.Rows, registryRead.Manifest, [], registryRead.Blocker!);

        // The system Lookup registry also contains templates, profiles and other operational
        // entities. Export only the approved classical business lookups; do not infer scope
        // from a schema's shape or from a value's size.
        var registry = registryRead.Rows.Where(record => ClassicLookupAllowlist.Contains(record.SchemaIdentity.SchemaName)).ToArray();
        var duplicateSchema = registry.GroupBy(item => item.SysEntitySchemaUId).FirstOrDefault(group => group.Count() != 1);
        if (duplicateSchema is not null)
            return LookupCatalog.Blocked(registry, registryRead.Manifest, [], ShapeBlocker($"lookup-registry/schema:{duplicateSchema.Key:D}", "LOOKUP_SCHEMA_BINDING_AMBIGUOUS"));

        var collections = new List<LookupCollection>();
        foreach (var registryRecord in registry)
        {
            var schema = objectModel.Schemas.Single(item => item.Identity.SchemaUId == registryRecord.SysEntitySchemaUId);
            var columns = schema.Columns.OrderBy(column => column.Ordinal).ToArray();
            if (columns.Length == 0 || columns.Select(column => column.ColumnName).Distinct(StringComparer.Ordinal).Count() != columns.Length ||
                columns.Count(column => string.Equals(column.ColumnName, "Id", StringComparison.Ordinal)) != 1)
                return LookupCatalog.Blocked(registry, registryRead.Manifest, collections, ShapeBlocker($"lookup-schema:{schema.Identity.SchemaUId:D}", "LOOKUP_COLUMN_SET_UNQUALIFIED"));
            var idColumn = columns.Single(column => string.Equals(column.ColumnName, "Id", StringComparison.Ordinal));
            if (idColumn.TypeCode != 0 || idColumn.Reference is not null)
                return LookupCatalog.Blocked(registry, registryRead.Manifest, collections, ShapeBlocker($"lookup-schema:{schema.Identity.SchemaUId:D}/column:{idColumn.ColumnUId:D}", "LOOKUP_ID_COLUMN_UNQUALIFIED"));

            foreach (var column in columns)
            {
                if (!TryKind(column, bestEffort, out _))
                    return LookupCatalog.Blocked(registry, registryRead.Manifest, collections, ShapeBlocker($"lookup-schema:{schema.Identity.SchemaUId:D}/column:{column.ColumnUId:D}", $"LOOKUP_COLUMN_TYPE_UNSUPPORTED:{column.TypeCode.ToString(CultureInfo.InvariantCulture)}"));
            }

            var orderedNames = columns.Select(column => column.ColumnName).ToArray();
            var collectionRead = await ReadOrderedAsync(
                schema.Identity.SchemaName,
                orderedNames,
                limits,
                row => ParseLookupRow(row, schema, columns, bestEffort),
                cancellationToken);
            if (!collectionRead.IsQualified)
            {
                // A server-side SelectQuery refusal has no rows to recover. Best-effort output
                // retains every other successfully read lookup instead of discarding the whole pull.
                if (bestEffort) continue;
                return LookupCatalog.Blocked(registry, registryRead.Manifest, collections, collectionRead.Blocker!);
            }

            var collectionFingerprint = Digest(string.Join("\n", collectionRead.Rows.Select(row => row.SourceFingerprint)));
            collections.Add(new LookupCollection(registryRecord, schema, collectionRead.Rows, collectionRead.Manifest!, collectionFingerprint));
        }

        return new LookupCatalog(true, registry, registryRead.Manifest, collections, null);
    }

    // TEMPORARY LEGACY MODE: the single SelectQuery request mirrors SyncOM behaviour.
    // It deliberately does not prove that the server returned every row; the disabled
    // paging implementation below is retained for restoration after BPMSoft paging is qualified.
    private async Task<OrderedReadResult<T>> ReadOrderedAsync<T>(
        string schemaName,
        IReadOnlyList<string> columns,
        LookupReadLimits limits,
        Func<JsonElement, ParseResult<T>> parse,
        CancellationToken cancellationToken)
        where T : notnull
    {
        var started = Stopwatch.StartNew();
        JsonElement root;
        int responseBytes;
        try
        {
            using var body = CreateLegacySingleSelectBody(schemaName, columns, limits.MaxRows);
            using var response = await _transport.SelectQueryAsync(body, cancellationToken);
            root = response.Root.Clone();
            responseBytes = response.ResponseBytes;
        }
        catch (BpmSoftTransportException error)
        {
            return OrderedReadResult<T>.Blocked([], CreateManifest(schemaName, [], [], 0, started.Elapsed), PagingBlocker($"lookup-collection:{SafeIdentity(schemaName)}", "LOOKUP_READ_UNAVAILABLE:" + error.Code));
        }

        if (responseBytes > limits.MaxResponseBytes)
            return OrderedReadResult<T>.Blocked([], CreateManifest(schemaName, [], [], responseBytes, started.Elapsed), PagingBlocker($"lookup-collection:{SafeIdentity(schemaName)}", "LOOKUP_MAX_RESPONSE_BYTES_EXCEEDED"));
        if (!TryGetLegacyRows(root, columns, out var sourceRows, out var shapeReason))
            return OrderedReadResult<T>.Blocked([], CreateManifest(schemaName, [], [], responseBytes, started.Elapsed), ShapeBlocker($"lookup-collection:{SafeIdentity(schemaName)}", shapeReason));
        if (sourceRows.Count > limits.MaxRows)
            return OrderedReadResult<T>.Blocked([], CreateManifest(schemaName, [], [], responseBytes, started.Elapsed), PagingBlocker($"lookup-collection:{SafeIdentity(schemaName)}", "LOOKUP_MAX_ROWS_EXCEEDED"));

        var rows = new List<T>(sourceRows.Count);
        var identities = new List<string>(sourceRows.Count);
        foreach (var row in sourceRows)
        {
            var parsed = parse(row);
            if (!parsed.IsQualified)
                return OrderedReadResult<T>.Blocked(rows, CreateManifest(schemaName, [], identities, responseBytes, started.Elapsed), parsed.Blocker!);
            rows.Add(parsed.Value!);
            identities.Add(parsed.RecordId!.Value.ToString("D").ToLowerInvariant());
        }
        if (identities.Distinct(StringComparer.Ordinal).Count() != identities.Count)
            return OrderedReadResult<T>.Blocked(rows, CreateManifest(schemaName, [], identities, responseBytes, started.Elapsed), PagingBlocker($"lookup-collection:{SafeIdentity(schemaName)}", "LOOKUP_DUPLICATE_IDENTITY"));

        var pages = new[] { PageManifest.Create(0, "legacy-single-query", identities, responseBytes) };
        return OrderedReadResult<T>.Qualified(rows, CreateManifest(schemaName, pages, identities, responseBytes, started.Elapsed));
    }

#if false
    // Retained paging implementation. Re-enable only after rowsOffset semantics are qualified.
    private async Task<OrderedReadResult<T>> ReadPagedAsync<T>(
        string schemaName,
        IReadOnlyList<string> columns,
        LookupReadLimits limits,
        Func<JsonElement, ParseResult<T>> parse,
        CancellationToken cancellationToken)
        where T : notnull
    {
        var started = Stopwatch.StartNew();
        var rows = new List<T>();
        var identities = new List<string>();
        var manifests = new List<PageManifest>();
        var seen = new HashSet<Guid>();
        var nonEmptyPageDigests = new HashSet<string>(StringComparer.Ordinal);
        long responseBytes = 0;
        var offset = 0;
        var terminalSeen = false;

        for (var requestOrdinal = 0; requestOrdinal < limits.MaxPages; requestOrdinal++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            JsonElement root;
            int currentBytes;
            try
            {
                using var body = CreateSelectBody(schemaName, columns, limits.PageSize, offset);
                using var response = await _transport.SelectQueryAsync(body, cancellationToken);
                root = response.Root.Clone();
                currentBytes = response.ResponseBytes;
            }
            catch (BpmSoftTransportException error)
            {
                return OrderedReadResult<T>.Blocked(rows, CreateManifest(schemaName, manifests, identities, responseBytes, started.Elapsed), PagingBlocker($"lookup-collection:{SafeIdentity(schemaName)}", "LOOKUP_READ_UNAVAILABLE:" + error.Code));
            }

            responseBytes += currentBytes;
            if (responseBytes > limits.MaxResponseBytes)
                return OrderedReadResult<T>.Blocked(rows, CreateManifest(schemaName, manifests, identities, responseBytes, started.Elapsed), PagingBlocker($"lookup-collection:{SafeIdentity(schemaName)}", "LOOKUP_MAX_RESPONSE_BYTES_EXCEEDED"));
            if (!TryGetRows(root, offset, columns, out var pageRows, out var shapeReason))
            {
                var blocker = string.Equals(shapeReason, "LOOKUP_PAGE_OFFSET_UNQUALIFIED", StringComparison.Ordinal)
                    ? PagingBlocker($"lookup-collection:{SafeIdentity(schemaName)}", shapeReason)
                    : ShapeBlocker($"lookup-collection:{SafeIdentity(schemaName)}", shapeReason);
                return OrderedReadResult<T>.Blocked(rows, CreateManifest(schemaName, manifests, identities, responseBytes, started.Elapsed), blocker);
            }
            if (pageRows.Count > limits.PageSize)
                return OrderedReadResult<T>.Blocked(rows, CreateManifest(schemaName, manifests, identities, responseBytes, started.Elapsed), PagingBlocker($"lookup-collection:{SafeIdentity(schemaName)}", "LOOKUP_PAGE_SIZE_EXCEEDED"));

            var pageIds = new List<string>(pageRows.Count);
            var pageValues = new List<T>(pageRows.Count);
            foreach (var row in pageRows)
            {
                var parsed = parse(row);
                if (!parsed.IsQualified)
                    return OrderedReadResult<T>.Blocked(rows, CreateManifest(schemaName, manifests, identities, responseBytes, started.Elapsed), parsed.Blocker!);
                pageIds.Add(parsed.RecordId!.Value.ToString("D").ToLowerInvariant());
                pageValues.Add(parsed.Value!);
            }

            var pageDigest = Digest(string.Join("\n", pageIds));
            if (pageIds.Count > 0 && !nonEmptyPageDigests.Add(pageDigest))
                return OrderedReadResult<T>.Blocked(rows, CreateManifest(schemaName, manifests, identities, responseBytes, started.Elapsed), PagingBlocker($"lookup-collection:{SafeIdentity(schemaName)}", "LOOKUP_PAGE_LOOP_DETECTED"));
            if (pageIds.Count != pageIds.Distinct(StringComparer.Ordinal).Count())
                return OrderedReadResult<T>.Blocked(rows, CreateManifest(schemaName, manifests, identities, responseBytes, started.Elapsed), PagingBlocker($"lookup-collection:{SafeIdentity(schemaName)}", "LOOKUP_DUPLICATE_IDENTITY"));
            if (pageIds.Any(id => seen.Contains(Guid.Parse(id))))
                return OrderedReadResult<T>.Blocked(rows, CreateManifest(schemaName, manifests, identities, responseBytes, started.Elapsed), PagingBlocker($"lookup-collection:{SafeIdentity(schemaName)}", "LOOKUP_PAGE_OVERLAP"));
            if (pageIds.Zip(pageIds.Skip(1), (left, right) => string.CompareOrdinal(left, right) < 0).Any(isAscending => !isAscending) ||
                identities.Count > 0 && pageIds.Count > 0 && string.CompareOrdinal(identities[^1], pageIds[0]) >= 0)
                return OrderedReadResult<T>.Blocked(rows, CreateManifest(schemaName, manifests, identities, responseBytes, started.Elapsed), PagingBlocker($"lookup-collection:{SafeIdentity(schemaName)}", "LOOKUP_ID_ORDER_UNQUALIFIED"));

            manifests.Add(PageManifest.Create(requestOrdinal, $"{offset.ToString(CultureInfo.InvariantCulture)}:{(terminalSeen ? "confirm" : "read")}", pageIds, currentBytes));
            if (terminalSeen)
            {
                if (pageIds.Count != 0)
                    return OrderedReadResult<T>.Blocked(rows, CreateManifest(schemaName, manifests, identities, responseBytes, started.Elapsed), PagingBlocker($"lookup-collection:{SafeIdentity(schemaName)}", "LOOKUP_NONEMPTY_AFTER_TERMINAL"));
                return OrderedReadResult<T>.Qualified(rows, CreateManifest(schemaName, manifests, identities, responseBytes, started.Elapsed));
            }

            if (pageIds.Count == 0)
            {
                terminalSeen = true;
                continue;
            }

            if (rows.Count + pageValues.Count > limits.MaxRows)
                return OrderedReadResult<T>.Blocked(rows, CreateManifest(schemaName, manifests, identities, responseBytes, started.Elapsed), PagingBlocker($"lookup-collection:{SafeIdentity(schemaName)}", "LOOKUP_MAX_ROWS_EXCEEDED"));
            foreach (var id in pageIds) seen.Add(Guid.Parse(id));
            identities.AddRange(pageIds);
            rows.AddRange(pageValues);
            offset += pageValues.Count;
        }

        return OrderedReadResult<T>.Blocked(rows, CreateManifest(schemaName, manifests, identities, responseBytes, started.Elapsed), PagingBlocker($"lookup-collection:{SafeIdentity(schemaName)}", "LOOKUP_MAX_PAGES_EXCEEDED"));
    }
#endif

    private static ParseResult<LookupRegistryRecord> ParseRegistryRow(JsonElement row, WorkspaceObjectModel objectModel)
    {
        if (!TryGuid(row.GetProperty("Id"), out var lookupRecordId) || !TryReferenceGuid(row.GetProperty("SysEntitySchemaUId"), out var schemaUId, out _))
            return ParseResult<LookupRegistryRecord>.Blocked(ShapeBlocker("lookup-registry", "LOOKUP_REGISTRY_IDENTITY_UNQUALIFIED"));
        var schemas = objectModel.Schemas.Where(item => item.Identity.SchemaUId == schemaUId).ToArray();
        if (schemas.Length != 1)
            return ParseResult<LookupRegistryRecord>.Blocked(ShapeBlocker($"lookup-registry/record:{lookupRecordId:D}/schema:{schemaUId:D}", schemas.Length == 0 ? "LOOKUP_SCHEMA_NOT_IN_INVENTORY" : "LOOKUP_SCHEMA_LAYER_AMBIGUOUS"));
        var schema = schemas[0];
        var baseIdentity = schema.Identity.ParentSchemaUId is { } parentUId && schema.Identity.ParentSchemaName is { } parentName
            ? new SchemaReference(parentName, parentUId)
            : null;
        var fingerprint = Digest($"registry|{lookupRecordId:D}|{schemaUId:D}|{PackageKey(schema.Identity.PackageLayer)}|{CanonicalJson(row)}");
        return ParseResult<LookupRegistryRecord>.Qualified(lookupRecordId, new LookupRegistryRecord(lookupRecordId, schemaUId, schema.Identity, baseIdentity, fingerprint));
    }

    private static ParseResult<LookupRow> ParseLookupRow(JsonElement row, EntitySchemaModel schema, IReadOnlyList<EntityColumnModel> columns, bool bestEffort)
    {
        if (!TryGuid(row.GetProperty("Id"), out var recordId))
            return ParseResult<LookupRow>.Blocked(ShapeBlocker($"lookup-schema:{schema.Identity.SchemaUId:D}", "LOOKUP_RECORD_ID_UNQUALIFIED"));
        var values = new List<NormalizedLookupValue>();
        foreach (var column in columns.Where(column => !string.Equals(column.ColumnName, "Id", StringComparison.Ordinal)))
        {
            var parsed = ParseValue(schema, recordId, column, row.GetProperty(column.ColumnName), bestEffort);
            if (!parsed.IsQualified) return ParseResult<LookupRow>.Blocked(parsed.Blocker!);
            values.Add(parsed.Value!);
        }
        var rowFingerprint = Digest(string.Join("\n", values.Select(value => value.ValueFingerprint)));
        var sourceFingerprint = Digest($"row|{schema.Identity.SchemaUId:D}|{PackageKey(schema.Identity.PackageLayer)}|{recordId:D}|{string.Join("\n", values.Select(value => value.SourceFingerprint))}");
        return ParseResult<LookupRow>.Qualified(recordId, new LookupRow(recordId, values, rowFingerprint, sourceFingerprint));
    }

    private static ParseResult<NormalizedLookupValue> ParseValue(EntitySchemaModel schema, Guid recordId, EntityColumnModel column, JsonElement value, bool bestEffort)
    {
        if (!TryKind(column, bestEffort, out var kind))
            return ParseResult<NormalizedLookupValue>.Blocked(ShapeBlocker($"lookup-schema:{schema.Identity.SchemaUId:D}/column:{column.ColumnUId:D}", $"LOOKUP_COLUMN_TYPE_UNSUPPORTED:{column.TypeCode.ToString(CultureInfo.InvariantCulture)}"));

        LookupValueState state;
        LookupTypedValue? typed = null;
        string? canonical = null;
        Guid? referenceId = null;
        if (value.ValueKind == JsonValueKind.Null)
        {
            state = LookupValueState.Null;
        }
        else if (bestEffort && !TryKind(column, false, out _))
        {
            // The raw JSON value remains in the Lookup workbook only. It has no typed semantic
            // meaning in this mode and must never be used by Compare or Apply.
            state = LookupValueState.Value;
            canonical = CanonicalJson(value);
            typed = new LookupTextValue(canonical);
        }
        else
        {
            state = LookupValueState.Value;
            switch (kind)
            {
                case LookupValueKind.Text when value.ValueKind == JsonValueKind.String:
                    canonical = (value.GetString() ?? string.Empty).Normalize(NormalizationForm.FormC);
                    state = canonical.Length == 0 ? LookupValueState.EmptyString : LookupValueState.Value;
                    typed = new LookupTextValue(canonical);
                    break;
                case LookupValueKind.Guid when TryGuid(value, out var guid):
                    canonical = guid.ToString("D").ToLowerInvariant();
                    typed = new LookupGuidValue(guid);
                    break;
                case LookupValueKind.Integer when value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var integer):
                    canonical = integer.ToString(CultureInfo.InvariantCulture);
                    typed = new LookupIntegerValue(integer);
                    break;
                case LookupValueKind.Decimal when value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number):
                    canonical = number.ToString("G29", CultureInfo.InvariantCulture);
                    typed = new LookupDecimalValue(number);
                    break;
                case LookupValueKind.Boolean when value.ValueKind is JsonValueKind.True or JsonValueKind.False:
                    canonical = value.GetBoolean() ? "true" : "false";
                    typed = new LookupBooleanValue(value.GetBoolean());
                    break;
                case LookupValueKind.DateTime when value.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var dateTime):
                    var utc = dateTime.ToUniversalTime();
                    canonical = utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
                    typed = new LookupDateTimeValue(utc);
                    break;
                case LookupValueKind.Date when value.ValueKind == JsonValueKind.String && DateOnly.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var date):
                    canonical = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    typed = new LookupDateValue(date);
                    break;
                case LookupValueKind.Time when value.ValueKind == JsonValueKind.String && TimeOnly.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var time):
                    canonical = time.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
                    typed = new LookupTimeValue(time);
                    break;
                case LookupValueKind.Reference when TryReferenceGuid(value, out var reference, out var display):
                    referenceId = reference;
                    canonical = reference.ToString("D").ToLowerInvariant();
                    typed = new LookupReferenceValue(reference, display?.Normalize(NormalizationForm.FormC));
                    break;
                default:
                    if (bestEffort)
                    {
                        // A declared BPMSoft type and its actual JSON value disagree. Preserve the
                        // value verbatim-as-canonical-JSON and make its loss of type semantics visible.
                        kind = LookupValueKind.Text;
                        canonical = CanonicalJson(value);
                        typed = new LookupTextValue(canonical);
                        break;
                    }
                    return ParseResult<NormalizedLookupValue>.Blocked(ShapeBlocker($"lookup-schema:{schema.Identity.SchemaUId:D}/record:{recordId:D}/column:{column.ColumnUId:D}", "LOOKUP_VALUE_UNQUALIFIED"));
            }
        }

        var valueFingerprint = Digest($"{state}|{kind}|{canonical ?? "<null>"}|{referenceId?.ToString("D") ?? "<null>"}");
        var sourceFingerprint = Digest($"value|{schema.Identity.SchemaUId:D}|{PackageKey(schema.Identity.PackageLayer)}|{recordId:D}|{column.ColumnUId:D}|{CanonicalJson(value)}");
        return ParseResult<NormalizedLookupValue>.Qualified(recordId, new NormalizedLookupValue(recordId, column.ColumnUId, column.ColumnName, state, kind, typed, canonical, referenceId, valueFingerprint, sourceFingerprint));
    }

    private static bool TryGetRows(JsonElement root, int requestedOffset, IReadOnlyList<string> columns, out IReadOnlyList<JsonElement> rows, out string reason)
    {
        rows = [];
        reason = "SELECT_QUERY_SHAPE_UNQUALIFIED";
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("success", out var success) || success.ValueKind != JsonValueKind.True ||
            !root.TryGetProperty("notFoundColumns", out var notFound) || notFound.ValueKind != JsonValueKind.Array || notFound.GetArrayLength() != 0 ||
            !root.TryGetProperty("rows", out var sourceRows) || sourceRows.ValueKind != JsonValueKind.Array)
            return false;
        if (!root.TryGetProperty("rowsOffset", out var reportedOffset) || !reportedOffset.TryGetInt32(out var parsedOffset) || parsedOffset != requestedOffset)
        {
            reason = "LOOKUP_PAGE_OFFSET_UNQUALIFIED";
            return false;
        }

        var expected = columns.ToHashSet(StringComparer.Ordinal);
        var result = new List<JsonElement>();
        foreach (var row in sourceRows.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object || !row.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal).SetEquals(expected))
                return false;
            result.Add(row.Clone());
        }
        rows = result;
        return true;
    }

    private static bool TryGetLegacyRows(JsonElement root, IReadOnlyList<string> columns, out IReadOnlyList<JsonElement> rows, out string reason)
    {
        rows = [];
        reason = "SELECT_QUERY_SHAPE_UNQUALIFIED";
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("success", out var success) || success.ValueKind != JsonValueKind.True ||
            !root.TryGetProperty("notFoundColumns", out var notFound) || notFound.ValueKind != JsonValueKind.Array || notFound.GetArrayLength() != 0 ||
            !root.TryGetProperty("rows", out var sourceRows) || sourceRows.ValueKind != JsonValueKind.Array)
            return false;
        var required = columns.ToHashSet(StringComparer.Ordinal);
        var result = new List<JsonElement>();
        foreach (var row in sourceRows.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object || !required.All(column => row.TryGetProperty(column, out _))) return false;
            result.Add(row.Clone());
        }
        rows = result;
        return true;
    }

    private static CanonicalSelectQueryBody CreateSelectBody(string schemaName, IReadOnlyList<string> columns, int pageSize, int offset)
    {
        if (string.IsNullOrWhiteSpace(schemaName) || columns.Count == 0 || columns.Any(string.IsNullOrWhiteSpace) || columns.Distinct(StringComparer.Ordinal).Count() != columns.Count || !columns.Contains("Id", StringComparer.Ordinal))
            throw new InvalidOperationException("LOOKUP_QUERY_CONTRACT_INVALID");
        var items = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var column in columns)
        {
            items[column] = new
            {
                caption = column,
                orderDirection = string.Equals(column, "Id", StringComparison.Ordinal) ? 1 : 0,
                orderPosition = string.Equals(column, "Id", StringComparison.Ordinal) ? 0 : -1,
                isVisible = true,
                expression = new { expressionType = 0, columnPath = column }
            };
        }
        var json = JsonSerializer.Serialize(new { rootSchemaName = schemaName, rowCount = pageSize, rowsOffset = offset, isPageable = true, allColumns = false, useLocalization = true, columns = new { items } });
        return CanonicalSelectQueryBody.Parse(json);
    }

    private static CanonicalSelectQueryBody CreateLegacySingleSelectBody(string schemaName, IReadOnlyList<string> columns, int maxRows)
    {
        if (string.IsNullOrWhiteSpace(schemaName) || columns.Count == 0 || maxRows <= 0) throw new InvalidOperationException("LOOKUP_QUERY_CONTRACT_INVALID");
        // 3,000 is the historical SyncOM request size. MaxRows remains the local hard ceiling.
        var json = JsonSerializer.Serialize(new { rootSchemaName = schemaName, rowCount = Math.Min(maxRows, 3000), allColumns = true, useLocalization = true });
        return CanonicalSelectQueryBody.Parse(json);
    }

    private static bool TryKind(EntityColumnModel column, bool bestEffort, out LookupValueKind kind)
    {
        if (column.Reference is not null || column.TypeCode == 10)
        {
            kind = LookupValueKind.Reference;
            return column.Reference is not null;
        }
        kind = column.TypeCode switch
        {
            0 => LookupValueKind.Guid,
            1 or 29 or 30 or 31 or 32 or 33 => LookupValueKind.Text,
            4 => LookupValueKind.Integer,
            5 or 6 or 26 => LookupValueKind.Decimal,
            7 => LookupValueKind.DateTime,
            8 => LookupValueKind.Date,
            9 => LookupValueKind.Time,
            12 => LookupValueKind.Boolean,
            _ => default
        };
        if (column.TypeCode is 0 or 1 or 4 or 5 or 6 or 7 or 8 or 9 or 12 or 26 or 29 or 30 or 31 or 32 or 33) return true;
        if (bestEffort) { kind = LookupValueKind.Text; return true; }
        return false;
    }

    private static bool TryGuid(JsonElement value, out Guid guid)
    {
        guid = default;
        return value.ValueKind == JsonValueKind.String && Guid.TryParseExact(value.GetString(), "D", out guid) && guid != Guid.Empty;
    }

    private static bool TryReferenceGuid(JsonElement value, out Guid guid, out string? display)
    {
        display = null;
        if (TryGuid(value, out guid)) return true;
        guid = default;
        if (value.ValueKind != JsonValueKind.Object) return false;
        var properties = value.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
        if (!properties.IsSubsetOf(new[] { "value", "displayValue" }) || !properties.Contains("value") || !TryGuid(value.GetProperty("value"), out guid)) return false;
        if (value.TryGetProperty("displayValue", out var displayValue))
        {
            if (displayValue.ValueKind == JsonValueKind.String) display = displayValue.GetString();
            else if (displayValue.ValueKind != JsonValueKind.Null) return false;
        }
        return true;
    }

    private static OrderedCollectionManifest CreateManifest(string schemaName, IReadOnlyList<PageManifest> pages, IReadOnlyList<string> identities, long responseBytes, TimeSpan duration)
    {
        var identityDigest = Digest(string.Join("\n", identities));
        var telemetry = new OrderedCollectionTelemetry(pages.Count, identities.Count, responseBytes, SizeBucket(responseBytes), duration, identityDigest);
        var manifestFingerprint = Digest($"{schemaName}|Id|{identityDigest}|{string.Join("\n", pages.Select(page => page.IdentityDigest))}|{identities.Count.ToString(CultureInfo.InvariantCulture)}");
        return new OrderedCollectionManifest(schemaName, "Id:Ascending", pages.ToArray(), telemetry, manifestFingerprint);
    }

    private static string SizeBucket(long bytes) => bytes switch { 0 => "empty", <= 16 * 1024 => "0-16KiB", <= 1024 * 1024 => "16KiB-1MiB", <= 16 * 1024 * 1024 => "1-16MiB", _ => "16MiB+" };
    private static string CanonicalJson(JsonElement value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) WriteCanonical(writer, value);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in value.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name.Normalize(NormalizationForm.FormC));
                    WriteCanonical(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var element in value.EnumerateArray()) WriteCanonical(writer, element);
                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue((value.GetString() ?? string.Empty).Normalize(NormalizationForm.FormC));
                break;
            case JsonValueKind.Number when value.TryGetDecimal(out var number):
                writer.WriteNumberValue(number);
                break;
            case JsonValueKind.Number:
                writer.WriteRawValue(value.GetRawText(), skipInputValidation: false);
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
            default:
                throw new InvalidOperationException("LOOKUP_CANONICAL_JSON_UNSUPPORTED");
        }
    }
    // The GUID companion is the only package identity. The opaque source id
    // contributes solely through its one-way provenance digest so it cannot
    // become a raw value or a collision-prone join key.
    private static string PackageKey(PackageLayerIdentity package) => $"{package.PackageUId?.ToString("D") ?? "none"}|{package.LayerKind ?? "none"}|{package.PackageName ?? "none"}|{package.OpaquePackageIdDigest ?? "none"}";
    private static string SafeIdentity(string value) => Digest(value)[..16];
    private static string Digest(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static Blocker PagingBlocker(string scope, string reason) => new(BlockerCode.CatalogOrderOrPagingUnqualified, scope, reason, "Inspect the declared Id order, bounded page contract and sanitized fake response.", "Stop this qualification run; do not retry automatically.");
    private static Blocker ShapeBlocker(string scope, string reason) => new(BlockerCode.UnknownShapeUnqualified, scope, reason, "Resolve the exact schema/column/value contract without dropping source data.", "Stop this qualification run.");

    private sealed record ParseResult<T>(bool IsQualified, Guid? RecordId, T? Value, Blocker? Blocker)
    {
        public static ParseResult<T> Qualified(Guid recordId, T value) => new(true, recordId, value, null);
        public static ParseResult<T> Blocked(Blocker blocker) => new(false, null, default, blocker);
    }

    private sealed record OrderedReadResult<T>(bool IsQualified, IReadOnlyList<T> Rows, OrderedCollectionManifest? Manifest, Blocker? Blocker)
    {
        public static OrderedReadResult<T> Qualified(IReadOnlyList<T> rows, OrderedCollectionManifest manifest) => new(true, rows, manifest, null);
        public static OrderedReadResult<T> Blocked(IReadOnlyList<T> rows, OrderedCollectionManifest manifest, Blocker blocker) => new(false, rows, manifest, blocker);
    }
}
