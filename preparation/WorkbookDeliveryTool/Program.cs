using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace BpmSoftWorkbookDelivery;

internal static class Program
{
    private const string DefaultTarget = "http://localhost:8002";
    private const string ScopeMode = "VerifiedBoundedBaseline";
    private const string ContractVersion = "1";
    private const string TemplateVersion = "3-bounded-research";
    private const string SelectQueryPath = "DataService/json/SyncReply/SelectQuery";
    private const int Ascending = 1;
    private const int Descending = 2;
    private const int MaximumPageCount = 1_000;
    private const string ExpectedAssemblyVersion = "1.8.0.14107";
    private const string CommonAssemblyPath = @"C:\Creatio\BpmSoftStandSetup\Constructor_1.8.0.14107_Net8_PostgreSQL\BPMSoft.Common.dll";
    private const string CommonAssemblyHash = "42bed5e551e01e63c35d5439ed71b6335b34feec740185f6a09af8a11228ae20";
    private const string NuiAssemblyPath = @"C:\Creatio\BpmSoftStandSetup\Constructor_1.8.0.14107_Net8_PostgreSQL\BPMSoft.Nui.ServiceModel.dll";
    private const string NuiAssemblyHash = "3c147aaa90d16a6b4d3fbb3b7773876a741ca2e73f41bb690997c241e057d948";
    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null
    };

    private static readonly ExpectedSchema[] ExpectedSchemas =
    [
        new("ActivityPriority", "Base", "b934f48c-5dea-49b9-bde3-697cb4be5d8b"),
        new("Lookup", "Base", "2aecdb97-990e-4c17-96f4-240ca6531c84"),
        new("Account", "Base", "25d7c1ab-1de0-4501-b402-02e0e5a72d6e"),
        new("Account", "Completeness", "cc642965-191f-4b55-b1ea-fb8336e623b9"),
        new("Account", "Test1", "05c46595-729a-4668-94bc-56061dad4fb9")
    ];

    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 2;
            }

            return args[0].ToLowerInvariant() switch
            {
                "capture" => await CaptureAsync(),
                "generate" => await GenerateCommandAsync(args[1..]),
                "verify" => VerifyCommand(args[1..]),
                "self-test" => await SelfTestAsync(),
                _ => throw new InvalidOperationException($"Unknown command '{args[0]}'.")
            };
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"FAILED: {exception.Message}");
            return 1;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("BPMSoft bounded workbook delivery tool (read-only capture; no write API)");
        Console.WriteLine("  capture");
        Console.WriteLine("  generate --source <json> --model <xlsx> --lookup <xlsx>");
        Console.WriteLine("  verify --source <json> --model <xlsx> --lookup <xlsx>");
        Console.WriteLine("  self-test");
    }

    private static async Task<int> CaptureAsync()
    {
        Console.WriteLine("BPMSoft bounded read-only workbook capture");
        Console.WriteLine("Только Login/GetWorkspaceItems/GetSchema/SelectQuery. Write/Manage/delete/compile/save/create/update запрещены.");
        var startedUtc = DateTimeOffset.UtcNow;
        var assemblies = VerifyAssemblies();
        var target = ReadLocalTarget(ReadValue("URL стенда", DefaultTarget));
        var login = ReadValue("Логин", "Supervisor");
        var password = ReadPassword();

        var cookies = new CookieContainer();
        using var handler = new HttpClientHandler { CookieContainer = cookies, UseCookies = true };
        using var client = new HttpClient(handler) { BaseAddress = target };
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            var offset = -(int)TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.Now).TotalMinutes;
            using var loginDocument = await PostJsonAsync(client, "ServiceModel/AuthService.svc/Login",
                new { UserName = login, UserPassword = password, TimeZoneOffset = offset });
            password = string.Empty;
            if (!IsSuccessfulLogin(loginDocument.RootElement))
            {
                throw new InvalidOperationException("Стенд не подтвердил вход. Пароль не сохранён.");
            }

            var csrfCookie = cookies.GetCookies(target)["BPMCSRF"];
            if (csrfCookie is null || string.IsNullOrWhiteSpace(csrfCookie.Value))
            {
                throw new InvalidOperationException("Login подтверждён, но BPMCSRF отсутствует; session values не сохранены.");
            }

            var csrf = csrfCookie.Value;
            client.DefaultRequestHeaders.Add("BPMCSRF", csrf);

            using var workspaceDocument = await PostEmptyJsonAsync(client,
                "ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems");
            EnsureSuccessfulEnvelope(workspaceDocument.RootElement, "GetWorkspaceItems");
            var workspaceItems = SelectExpectedWorkspaceItems(workspaceDocument.RootElement);

            var schemas = new List<SchemaSource>();
            foreach (var item in workspaceItems)
            {
                using var schemaDocument = await PostJsonAsync(client,
                    "ServiceModel/EntitySchemaDesignerService.svc/GetSchema", new { schemaUId = item.WorkspaceItemUId });
                schemas.Add(ParseSchema(item, schemaDocument.RootElement));
            }

            ValidateBoundedIndexShape(schemas);

            var lookupPass1 = await ReadOrderedPassAsync(client, "Lookup", 50, Ascending,
                ["Id", "SysEntitySchemaUId"]);
            var lookupPass2 = await ReadOrderedPassAsync(client, "Lookup", 50, Ascending,
                ["Id", "SysEntitySchemaUId"]);
            var lookupDesc = await ReadOrderedPassAsync(client, "Lookup", 50, Descending,
                ["Id", "SysEntitySchemaUId"]);
            ValidateOrderedProof(lookupPass1, lookupPass2, lookupDesc);

            var activityPass1 = await ReadOrderedPassAsync(client, "ActivityPriority", 2, Ascending,
                ["Id", "Name", "Description"]);
            var activityPass2 = await ReadOrderedPassAsync(client, "ActivityPriority", 2, Ascending,
                ["Id", "Name", "Description"]);
            var activityDesc = await ReadOrderedPassAsync(client, "ActivityPriority", 2, Descending,
                ["Id", "Name", "Description"]);
            ValidateOrderedProof(activityPass1, activityPass2, activityDesc);

            var activitySchema = schemas.Single(item => item.SchemaName == "ActivityPriority" && item.ActualPackageName == "Base");
            var matchingRegistry = lookupPass1.Rows
                .Where(row => string.Equals(GetGuidValue(row.Values["SysEntitySchemaUId"]), activitySchema.SchemaUId,
                    StringComparison.Ordinal))
                .ToArray();
            if (matchingRegistry.Length != 1)
            {
                throw new InvalidOperationException($"Lookup registry must contain exactly one ActivityPriority relation; found {matchingRegistry.Length}.");
            }

            var lookupRecordId = matchingRegistry[0].RecordId;
            RequireRegistryStable(lookupPass2, lookupDesc, activitySchema.SchemaUId, lookupRecordId);

            var rowSources = new List<LookupRowSource>();
            var valueSources = new List<LookupValueSource>();
            foreach (var row in activityPass1.Rows.OrderBy(item => item.RecordId, StringComparer.Ordinal))
            {
                var values = new[]
                {
                    CreateTextValue("ActivityPriority", row.RecordId, "Name", row.Values["Name"]),
                    CreateTextValue("ActivityPriority", row.RecordId, "Description", row.Values["Description"])
                };
                var fingerprint = HashCanonical(values.Select(value => new
                {
                    value.ColumnName, value.ValueState, value.CanonicalValue
                }));
                rowSources.Add(new LookupRowSource("ActivityPriority", activitySchema.SchemaUId, row.RecordId, fingerprint));
                valueSources.AddRange(values.Select(value => value with { SourceFingerprint = fingerprint }));
            }

            client.DefaultRequestHeaders.Remove("BPMCSRF");
            csrf = string.Empty;

            var completedUtc = DateTimeOffset.UtcNow;
            var source = new BaselineSource(
                ContractVersion,
                Guid.NewGuid().ToString("D"),
                Guid.NewGuid().ToString("D"),
                target.GetLeftPart(UriPartial.Authority),
                ScopeMode,
                startedUtc,
                completedUtc,
                assemblies,
                workspaceItems.ToArray(),
                schemas.ToArray(),
                new LookupRegistrySource("ActivityPriority", activitySchema.SchemaUId, lookupRecordId,
                    activitySchema.ParentSchemaName, activitySchema.ParentSchemaUId),
                rowSources.ToArray(),
                valueSources.ToArray(),
                new[] { CreateProof(lookupPass1, lookupPass2, lookupDesc), CreateProof(activityPass1, activityPass2, activityDesc) });

            var indexCheck = BuildActualIndexedCheck(source);
            var root = FindWorkspaceRoot();
            var runDirectory = Path.Combine(root, "WorkbookDeliveryTool", "runs",
                completedUtc.ToString("yyyy", CultureInfo.InvariantCulture),
                completedUtc.ToString("MM", CultureInfo.InvariantCulture),
                completedUtc.ToString("dd", CultureInfo.InvariantCulture), source.PullRunId);
            if (Directory.Exists(runDirectory))
            {
                throw new InvalidOperationException("Append-only run directory already exists.");
            }

            var evidenceDirectory = Path.Combine(runDirectory, "evidence");
            var auditDirectory = Path.Combine(runDirectory, "audit");
            Directory.CreateDirectory(evidenceDirectory);
            Directory.CreateDirectory(auditDirectory);
            var stamp = completedUtc.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
            var sourcePath = Path.Combine(evidenceDirectory, $"{stamp}-bounded-baseline.json");
            var checkPath = Path.Combine(evidenceDirectory, $"{stamp}-actualindexed-indexes-check.json");
            await File.WriteAllTextAsync(sourcePath, Serialize(source), Utf8NoBom);
            await File.WriteAllTextAsync(checkPath, Serialize(indexCheck), Utf8NoBom);

            var manifest = CreateFileManifest([sourcePath, checkPath]);
            var manifestPath = Path.Combine(evidenceDirectory, $"{stamp}-source-manifest.sha256.json");
            await File.WriteAllTextAsync(manifestPath, Serialize(manifest), Utf8NoBom);
            var journal = new
            {
                Status = "CAPTURE_COMPLETE",
                source.PairId,
                source.PullRunId,
                source.TargetAlias,
                source.ScopeMode,
                SourcePath = sourcePath,
                SourceManifestPath = manifestPath,
                EndpointBoundary = new[] { "Login", "GetWorkspaceItems", "GetSchema", "SelectQuery" },
                NotRun = new[] { "full catalog", "BPMSoft Write/Manage", "compile/save/create/update/delete", "Google input", "Excel generation" }
            };
            await File.WriteAllTextAsync(Path.Combine(runDirectory, $"{stamp}-run-journal.json"), Serialize(journal), Utf8NoBom);

            Console.WriteLine($"CAPTURE_COMPLETE: {sourcePath}");
            Console.WriteLine($"SOURCE_MANIFEST: {manifestPath}");
            Console.WriteLine($"RUN_DIRECTORY: {runDirectory}");
            return 0;
        }
        finally
        {
            password = string.Empty;
            client.DefaultRequestHeaders.Remove("BPMCSRF");
        }
    }

    private static async Task<int> GenerateCommandAsync(string[] args)
    {
        var options = ParseOptions(args, "source", "model", "lookup");
        var sourcePath = Path.GetFullPath(options["source"]);
        var source = ReadSource(sourcePath);
        ValidateSource(source);
        var pairHash = ComputePairBaselineHash(source);
        var targetFingerprint = ComputeTargetFingerprint(source, HashFile(sourcePath));
        var modelTables = BuildModelTables(source, pairHash, targetFingerprint);
        var lookupTables = BuildLookupTables(source, pairHash, targetFingerprint);
        await CreateWorkbookAsync(Path.GetFullPath(options["model"]), modelTables);
        await CreateWorkbookAsync(Path.GetFullPath(options["lookup"]), lookupTables);
        Console.WriteLine($"GENERATED_MODEL: {Path.GetFullPath(options["model"])}");
        Console.WriteLine($"GENERATED_LOOKUP: {Path.GetFullPath(options["lookup"])}");
        Console.WriteLine($"PAIR_BASELINE_HASH: {pairHash}");
        return 0;
    }

    private static int VerifyCommand(string[] args)
    {
        var options = ParseOptions(args, "source", "model", "lookup");
        var sourcePath = Path.GetFullPath(options["source"]);
        var source = ReadSource(sourcePath);
        ValidateSource(source);
        var pairHash = ComputePairBaselineHash(source);
        var targetFingerprint = ComputeTargetFingerprint(source, HashFile(sourcePath));
        var modelExpected = BuildModelTables(source, pairHash, targetFingerprint);
        var lookupExpected = BuildLookupTables(source, pairHash, targetFingerprint);
        var modelResult = VerifyWorkbook(Path.GetFullPath(options["model"]), modelExpected);
        var lookupResult = VerifyWorkbook(Path.GetFullPath(options["lookup"]), lookupExpected);
        VerifyPair(modelResult, lookupResult, source, pairHash);
        var indexResult = VerifyExportedIndexes(source, modelResult);
        var output = new
        {
            Status = "PASS",
            Model = modelResult.Summary,
            Lookup = lookupResult.Summary,
            PairBaselineHash = pairHash,
            ActualIndexedVersusIndexes = indexResult,
            FutureIndexLoadGate = "INDEX_SYNC_UNRESOLVED"
        };
        Console.WriteLine(Serialize(output));
        return 0;
    }

    private static async Task<int> SelfTestAsync()
    {
        var temp = Path.Combine(Path.GetTempPath(), "bpmsoft-workbook-selftest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var source = CreateSyntheticSource();
            ValidateSource(source);
            var sourcePath = Path.Combine(temp, "source.json");
            await File.WriteAllTextAsync(sourcePath, Serialize(source), Utf8NoBom);
            var model = Path.Combine(temp, "model.xlsx");
            var lookup = Path.Combine(temp, "lookup.xlsx");
            var pairHash = ComputePairBaselineHash(source);
            var fingerprint = ComputeTargetFingerprint(source, HashFile(sourcePath));
            var modelTables = BuildModelTables(source, pairHash, fingerprint);
            var lookupTables = BuildLookupTables(source, pairHash, fingerprint);
            await CreateWorkbookAsync(model, modelTables);
            await CreateWorkbookAsync(lookup, lookupTables);
            var modelResult = VerifyWorkbook(model, modelTables);
            var lookupResult = VerifyWorkbook(lookup, lookupTables);
            VerifyPair(modelResult, lookupResult, source, pairHash);
            _ = VerifyExportedIndexes(source, modelResult);
            if (lookupResult.Tables.ContainsKey("LookupRows") ||
                lookupResult.Tables["LookupValues"].Rows.Length != source.LookupValues.Count)
            {
                throw new InvalidOperationException("LookupRows must be merged into LookupValues without changing value-row count.");
            }

            var model2 = Path.Combine(temp, "model2.xlsx");
            var lookup2 = Path.Combine(temp, "lookup2.xlsx");
            await CreateWorkbookAsync(model2, modelTables);
            await CreateWorkbookAsync(lookup2, lookupTables);
            if (ComputeCanonicalWorksheetHash(model) != ComputeCanonicalWorksheetHash(model2)
                || ComputeCanonicalWorksheetHash(lookup) != ComputeCanonicalWorksheetHash(lookup2))
                throw new InvalidOperationException("Canonical worksheet reproducibility fixture failed.");

            var separated = source with
            {
                Schemas = source.Schemas.Select(schema => schema.SchemaName == "Account"
                    ? schema with
                    {
                        Columns = schema.Columns.Select(column => column.ColumnName == "Code"
                            ? column with { ActualIndexed = false }
                            : column).ToArray()
                    }
                    : schema).ToArray()
            };
            var check = BuildActualIndexedCheck(separated);
            if (check.SourceMemberCount != 1 || check.Matrix.Single().ActualIndexed || check.Matrix.Single().IndexMembershipCount != 1)
            {
                throw new InvalidOperationException("Negative fixture ActualIndexed=false/membership-present failed.");
            }

            var fabricated = separated with
            {
                Schemas = separated.Schemas.Select(schema => schema.SchemaName == "Account"
                    ? schema with
                    {
                        Indexes = Array.Empty<IndexSource>(),
                        Columns = schema.Columns.Select(column => column with { ActualIndexed = true }).ToArray()
                    }
                    : schema).ToArray()
            };
            var fabricatedCheck = BuildActualIndexedCheck(fabricated);
            if (fabricatedCheck.SourceMemberCount != 0 || fabricatedCheck.Matrix.Single().IndexMembershipCount != 0)
            {
                throw new InvalidOperationException("Negative fixture ActualIndexed=true/membership-absent fabricated an index.");
            }

            var wrongRelation = source with
            {
                Schemas = source.Schemas.Select(schema => schema.SchemaName == "Account"
                    ? schema with
                    {
                        Indexes = schema.Indexes.Select(index => index with
                        {
                            Members = index.Members.Select(member => member with { ColumnUId = member.MemberUId }).ToArray()
                        }).ToArray()
                    }
                    : schema).ToArray()
            };
            ExpectFailure(() => ValidateSource(wrongRelation), "member uId substitute");

            ExpectFailure(() => ValidateSource(source with { PairId = "not-a-guid" }), "invalid GUID");
            ExpectFailure(() => ValidateSource(source with { Schemas = source.Schemas.Concat([source.Schemas[0]]).ToArray() }),
                "duplicate schema package layer");

            var mismatchedTables = lookupTables.Select(table => table.Name != "Manifest" ? table : table with
            {
                Rows = table.Rows.Select(row => string.Equals(Convert.ToString(row[0], CultureInfo.InvariantCulture), "PairId", StringComparison.Ordinal)
                    ? Row("PairId", "abababab-abab-abab-abab-abababababab") : row).ToArray()
            }).ToArray();
            var mismatchPath = Path.Combine(temp, "mismatch.xlsx");
            await CreateWorkbookAsync(mismatchPath, mismatchedTables);
            var mismatchResult = VerifyWorkbook(mismatchPath, mismatchedTables);
            ExpectFailure(() => VerifyPair(modelResult, mismatchResult, source, pairHash), "pair mismatch");

            var unknownHeader = modelTables.Select(table => table.Name != "Indexes" ? table : table with
            { Headers = table.Headers.Concat(["UnknownColumn"]).ToArray() }).ToArray();
            ExpectFailure(() => VerifyWorkbook(model, unknownHeader), "unknown column");

            var external = Path.Combine(temp, "external.xlsx");
            CreateTamperedWorkbook(model, external, addFormula: false, addExternalPart: true);
            ExpectFailure(() => VerifyWorkbook(external, modelTables), "external link blocker");
            var formula = Path.Combine(temp, "formula.xlsx");
            CreateTamperedWorkbook(model, formula, addFormula: true, addExternalPart: false);
            ExpectFailure(() => VerifyWorkbook(formula, modelTables), "formula blocker");

            var permuted = source with
            {
                WorkspaceItems = source.WorkspaceItems.Reverse().ToArray(),
                Schemas = source.Schemas.Reverse().ToArray(),
                LookupRows = source.LookupRows.Reverse().ToArray(),
                LookupValues = source.LookupValues.Reverse().ToArray()
            };
            if (!string.Equals(ComputePairBaselineHash(source), ComputePairBaselineHash(permuted), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Canonical ordering fixture failed.");
            }

            Console.WriteLine("SELF_TEST_PASS: generation/read-back, reproducibility, pair and schema guards, unknown-column/external/formula blockers, ordering, inherited index join and ActualIndexed separation.");
            return 0;
        }
        finally
        {
            if (Directory.Exists(temp))
            {
                Directory.Delete(temp, recursive: true);
            }
        }
    }

    private static void ExpectFailure(Action action, string name)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected failure did not occur: {name}.");
    }

    private static AssemblyEvidence VerifyAssemblies()
    {
        return new AssemblyEvidence(
            VerifyAssembly(CommonAssemblyPath, CommonAssemblyHash),
            VerifyAssembly(NuiAssemblyPath, NuiAssemblyHash),
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["None"] = 0,
                ["Ascending"] = 1,
                ["Descending"] = 2
            });
    }

    private static AssemblyFileEvidence VerifyAssembly(string path, string expectedHash)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Required local assembly is missing: {path}");
        }

        var version = FileVersionInfo.GetVersionInfo(path).FileVersion;
        if (!string.Equals(version, ExpectedAssemblyVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Assembly version mismatch: {Path.GetFileName(path)}.");
        }

        var hash = HashFile(path);
        if (!string.Equals(hash, expectedHash, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Assembly SHA-256 mismatch: {Path.GetFileName(path)}.");
        }

        return new AssemblyFileEvidence(path, version!, hash);
    }

    private static List<WorkspaceItemSource> SelectExpectedWorkspaceItems(JsonElement root)
    {
        if (!root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("GetWorkspaceItems did not return items array.");
        }

        var selected = new List<WorkspaceItemSource>();
        foreach (var expected in ExpectedSchemas)
        {
            var matches = items.EnumerateArray().Where(item =>
                GetOptionalInt32(item, "type") == 3 &&
                string.Equals(GetOptionalString(item, "name"), expected.SchemaName, StringComparison.Ordinal) &&
                string.Equals(GetOptionalString(item, "packageName"), expected.PackageName, StringComparison.Ordinal) &&
                string.Equals(NormalizeGuid(GetOptionalString(item, "uId")), expected.SchemaUId, StringComparison.Ordinal)).ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException($"Expected exactly one {expected.SchemaName}/{expected.PackageName}/{expected.SchemaUId}; found {matches.Length}.");
            }

            var item = matches[0];
            selected.Add(new WorkspaceItemSource(
                expected.SchemaUId,
                expected.SchemaName,
                3,
                expected.PackageName,
                GetRequiredGuid(item, "packageUId", $"{expected.SchemaName}/{expected.PackageName}")));
        }

        return selected.OrderBy(item => item.WorkspaceItemUId, StringComparer.Ordinal).ToList();
    }

    private static SchemaSource ParseSchema(WorkspaceItemSource requested, JsonElement response)
    {
        EnsureSuccessfulEnvelope(response, $"GetSchema {requested.Name}/{requested.PackageName}");
        if (!response.TryGetProperty("schema", out var schema) || schema.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException($"GetSchema {requested.Name}/{requested.PackageName} did not return schema object.");
        }

        var schemaName = GetRequiredString(schema, "name", "schema");
        var schemaUId = GetRequiredGuid(schema, "uId", schemaName);
        if (!string.Equals(schemaName, requested.Name, StringComparison.Ordinal) ||
            !string.Equals(schemaUId, requested.WorkspaceItemUId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"GetSchema identity mismatch for {requested.Name}/{requested.PackageName}.");
        }

        var idCandidate = GetRequiredGuid(schema, "id", schemaName);
        string? parentName = null;
        string? parentUId = null;
        if (schema.TryGetProperty("parentSchema", out var parent) && parent.ValueKind != JsonValueKind.Null)
        {
            if (parent.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException($"Unknown parentSchema form for {schemaName}.");
            }
            parentName = GetRequiredString(parent, "name", $"{schemaName}.parentSchema");
            parentUId = GetRequiredGuid(parent, "uId", $"{schemaName}.parentSchema");
        }

        var columns = new List<ColumnSource>();
        AppendColumns(schema, "columns", "Own", columns, schemaUId);
        AppendColumns(schema, "inheritedColumns", "Inherited", columns, schemaUId);
        var indexes = ParseIndexes(schema, schemaUId);
        return new SchemaSource(schemaName, schemaUId, idCandidate, parentName, parentUId,
            requested.PackageName, requested.PackageUId, columns.ToArray(), indexes.ToArray());
    }

    private static void AppendColumns(JsonElement schema, string propertyName, string ownership,
        List<ColumnSource> destination, string schemaUId)
    {
        if (!schema.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"schema.{propertyName} is not an array.");
        }

        foreach (var column in array.EnumerateArray())
        {
            var name = GetRequiredString(column, "name", $"{schemaUId}.{propertyName}");
            var uId = GetRequiredGuid(column, "uId", $"{schemaUId}.{name}");
            var typeCode = GetRequiredInt32(column, "type", $"{schemaUId}.{name}");
            var requirementType = GetRequiredInt32(column, "requirementType", $"{schemaUId}.{name}");
            if (requirementType is not 0 and not 1)
            {
                throw new InvalidOperationException($"REQUIREMENT_MAPPING_UNRESOLVED: {schemaUId}.{name}={requirementType}.");
            }
            var indexed = GetRequiredBoolean(column, "indexed", $"{schemaUId}.{name}");
            string? referenceName = null;
            string? referenceUId = null;
            if (column.TryGetProperty("referenceSchema", out var reference) && reference.ValueKind != JsonValueKind.Null)
            {
                if (reference.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException($"Unknown referenceSchema form for {schemaUId}.{name}.");
                }
                referenceName = GetRequiredString(reference, "name", $"{schemaUId}.{name}.referenceSchema");
                referenceUId = GetRequiredGuid(reference, "uId", $"{schemaUId}.{name}.referenceSchema");
            }

            destination.Add(new ColumnSource(schemaUId, name, uId, ownership, typeCode,
                requirementType == 1, indexed, referenceName, referenceUId));
        }
    }

    private static IReadOnlyList<IndexSource> ParseIndexes(JsonElement schema, string schemaUId)
    {
        if (!schema.TryGetProperty("indexes", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("schema.indexes is not an array.");
        }

        var result = new List<IndexSource>();
        foreach (var index in array.EnumerateArray())
        {
            var indexUId = GetRequiredGuid(index, "uId", $"{schemaUId}.index");
            var name = GetRequiredString(index, "name", $"{schemaUId}.index");
            var isUnique = GetRequiredBoolean(index, "isUnique", $"{schemaUId}.{name}");
            var isAutoName = GetRequiredBoolean(index, "isAutoName", $"{schemaUId}.{name}");
            if (!index.TryGetProperty("columns", out var members) || members.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException($"{schemaUId}.{name}.columns is not an array.");
            }

            var parsedMembers = new List<IndexMemberSource>();
            var ordinal = 0;
            foreach (var member in members.EnumerateArray())
            {
                parsedMembers.Add(new IndexMemberSource(
                    GetRequiredGuid(member, "uId", $"{schemaUId}.{name}.member"),
                    GetRequiredString(member, "name", $"{schemaUId}.{name}.member"),
                    GetRequiredGuid(member, "columnUId", $"{schemaUId}.{name}.member"),
                    GetRequiredInt32(member, "orderDirection", $"{schemaUId}.{name}.member"),
                    ordinal++));
            }
            result.Add(new IndexSource(schemaUId, indexUId, name, isUnique, isAutoName, parsedMembers.ToArray()));
        }
        return result;
    }

    private static void ValidateBoundedIndexShape(IReadOnlyList<SchemaSource> schemas)
    {
        var test1 = schemas.Single(schema => schema.SchemaName == "Account" && schema.ActualPackageName == "Test1");
        var ownCount = test1.Columns.Count(column => column.Ownership == "Own");
        var inheritedCount = test1.Columns.Count(column => column.Ownership == "Inherited");
        if (ownCount != 0 || inheritedCount != 34)
        {
            throw new InvalidOperationException($"Account/Test1 expected 0 own and 34 inherited columns; found {ownCount}/{inheritedCount}.");
        }
        if (test1.Indexes.Count != 2 || test1.Indexes.Any(index => index.IsAutoName || index.Members.Count != 1))
        {
            throw new InvalidOperationException("UNSUPPORTED_INDEX_SHAPE: Account/Test1 must have exactly two simple non-auto-named indexes.");
        }

        var code = test1.Columns.SingleOrDefault(column => column.ColumnName == "Code" && column.Ownership == "Inherited");
        var name = test1.Columns.SingleOrDefault(column => column.ColumnName == "Name" && column.Ownership == "Inherited");
        if (code is null || name is null)
        {
            throw new InvalidOperationException("Account/Test1 inherited Code/Name relation is missing.");
        }
        var codeIndexes = test1.Indexes.Where(index => index.Members.Single().ColumnUId == code.ColumnUId && index.IsUnique).ToArray();
        var nameIndexes = test1.Indexes.Where(index => index.Members.Single().ColumnUId == name.ColumnUId && !index.IsUnique).ToArray();
        if (codeIndexes.Length != 1 || nameIndexes.Length != 1)
        {
            throw new InvalidOperationException("Account/Test1 owner oracle does not match one UNIQUE Code and one NON-UNIQUE Name index.");
        }
    }

    private static async Task<OrderedPass> ReadOrderedPassAsync(HttpClient client, string schemaName,
        int pageSize, int orderDirection, IReadOnlyList<string> visibleColumns)
    {
        var rows = new List<QueryRow>();
        var pages = new List<PageEvidence>();
        var offset = 0;
        var terminalReached = false;
        for (var page = 0; page < MaximumPageCount; page++)
        {
            using var document = await PostJsonAsync(client, SelectQueryPath,
                CreateSelectPayload(schemaName, pageSize, offset, orderDirection, visibleColumns));
            var pageRows = ParseSelectRows(document.RootElement, schemaName, offset, visibleColumns);
            pages.Add(new PageEvidence(page, offset, pageRows.Count,
                pageRows.FirstOrDefault()?.RecordId, pageRows.LastOrDefault()?.RecordId));
            rows.AddRange(pageRows);
            if (pageRows.Count < pageSize)
            {
                terminalReached = true;
                break;
            }
            offset += pageSize;
        }
        if (!terminalReached)
        {
            throw new InvalidOperationException($"{schemaName} exceeded bounded page limit.");
        }
        if (rows.Select(row => row.RecordId).Distinct(StringComparer.Ordinal).Count() != rows.Count)
        {
            throw new InvalidOperationException($"{schemaName} ordered pass contains duplicate IDs.");
        }

        using var terminal = await PostJsonAsync(client, SelectQueryPath,
            CreateSelectPayload(schemaName, pageSize, rows.Count, orderDirection, visibleColumns));
        var terminalRows = ParseSelectRows(terminal.RootElement, schemaName, rows.Count, visibleColumns);
        if (terminalRows.Count != 0)
        {
            throw new InvalidOperationException($"{schemaName} terminal confirmation is not empty.");
        }
        return new OrderedPass(schemaName, pageSize, orderDirection, rows.ToArray(), pages.ToArray(), rows.Count, true);
    }

    private static object CreateSelectPayload(string schemaName, int pageSize, int offset,
        int orderDirection, IReadOnlyList<string> columns)
    {
        var items = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var column in columns)
        {
            items[column] = new
            {
                caption = column,
                orderDirection = column == "Id" ? orderDirection : 0,
                orderPosition = column == "Id" ? 0 : -1,
                isVisible = true,
                expression = new { expressionType = 0, columnPath = column }
            };
        }
        return new
        {
            rootSchemaName = schemaName,
            rowCount = pageSize,
            rowsOffset = offset,
            isPageable = true,
            allColumns = false,
            useLocalization = true,
            columns = new { items }
        };
    }

    private static List<QueryRow> ParseSelectRows(JsonElement root, string schemaName, int offset,
        IReadOnlyList<string> expectedColumns)
    {
        EnsureSuccessfulEnvelope(root, $"SelectQuery {schemaName} offset {offset}");
        if (!root.TryGetProperty("notFoundColumns", out var notFound) ||
            notFound.ValueKind != JsonValueKind.Array || notFound.GetArrayLength() != 0)
        {
            throw new InvalidOperationException($"SelectQuery {schemaName} did not confirm empty notFoundColumns.");
        }
        if (!root.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"SelectQuery {schemaName} did not return rows array.");
        }
        var expected = expectedColumns.ToHashSet(StringComparer.Ordinal);
        var result = new List<QueryRow>();
        foreach (var row in rows.EnumerateArray())
        {
            var actual = row.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
            if (!actual.SetEquals(expected))
            {
                throw new InvalidOperationException($"SelectQuery {schemaName} returned an unexpected row shape.");
            }
            var id = GetRequiredGuid(row, "Id", $"{schemaName}.row");
            var values = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (var column in expectedColumns.Where(column => column != "Id"))
            {
                values[column] = row.GetProperty(column).Clone();
            }
            result.Add(new QueryRow(id, values));
        }
        return result;
    }

    private static void ValidateOrderedProof(OrderedPass pass1, OrderedPass pass2, OrderedPass descending)
    {
        var first = pass1.Rows.Select(CanonicalQueryRow).ToArray();
        var second = pass2.Rows.Select(CanonicalQueryRow).ToArray();
        var reverse = descending.Rows.Select(CanonicalQueryRow).Reverse().ToArray();
        if (!first.SequenceEqual(second, StringComparer.Ordinal) || !first.SequenceEqual(reverse, StringComparer.Ordinal))
        {
            throw new InvalidOperationException($"{pass1.SchemaName} ordering/pagination proof failed.");
        }
    }

    private static string CanonicalQueryRow(QueryRow row)
    {
        var values = row.Values.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new { pair.Key, Value = CanonicalJsonValue(pair.Value) });
        return row.RecordId + "|" + JsonSerializer.Serialize(values);
    }

    private static void RequireRegistryStable(OrderedPass second, OrderedPass descending,
        string schemaUId, string lookupRecordId)
    {
        foreach (var pass in new[] { second, descending })
        {
            var matches = pass.Rows.Where(row =>
                string.Equals(GetGuidValue(row.Values["SysEntitySchemaUId"]), schemaUId, StringComparison.Ordinal) &&
                string.Equals(row.RecordId, lookupRecordId, StringComparison.Ordinal)).ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException("Lookup registry relation changed between ordered passes.");
            }
        }
    }

    private static LookupValueSource CreateTextValue(string schemaName, string recordId,
        string columnName, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            return new LookupValueSource(schemaName, recordId, columnName, "Null", null, "Text", null, "");
        }
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"DATATYPE_MAPPING_UNRESOLVED: {schemaName}.{columnName} is not text/null.");
        }
        var text = value.GetString()!.Normalize(NormalizationForm.FormC);
        return new LookupValueSource(schemaName, recordId, columnName,
            text.Length == 0 ? "EmptyString" : "Value", text, "Text", text, "");
    }

    private static OrderedProof CreateProof(OrderedPass first, OrderedPass second, OrderedPass descending)
    {
        return new OrderedProof(first.SchemaName, first.PageSize, first.Rows.Count,
            HashCanonical(first.Rows.Select(CanonicalQueryRow)),
            HashCanonical(second.Rows.Select(CanonicalQueryRow)),
            HashCanonical(descending.Rows.Select(CanonicalQueryRow)),
            first.TerminalEmpty && second.TerminalEmpty && descending.TerminalEmpty,
            first.Pages, second.Pages, descending.Pages);
    }

    private static BaselineSource ReadSource(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Bounded baseline source not found.", path);
        }
        return JsonSerializer.Deserialize<BaselineSource>(File.ReadAllText(path, Encoding.UTF8), JsonOptions)
            ?? throw new InvalidOperationException("Bounded baseline JSON is empty.");
    }

    private static void ValidateSource(BaselineSource source)
    {
        if (source.ContractVersion != ContractVersion || source.ScopeMode != ScopeMode)
        {
            throw new InvalidOperationException("Unsupported contract or scope mode.");
        }
        RequireGuid(source.PairId, "PairId");
        RequireGuid(source.PullRunId, "PullRunId");
        _ = ReadLocalTarget(source.TargetAlias);
        if (source.WorkspaceItems.Count != ExpectedSchemas.Length || source.Schemas.Count != ExpectedSchemas.Length)
        {
            throw new InvalidOperationException("Bounded source must contain exactly five selected schemas.");
        }
        foreach (var expected in ExpectedSchemas)
        {
            var itemMatches = source.WorkspaceItems.Where(item => item.Name == expected.SchemaName &&
                item.PackageName == expected.PackageName && item.WorkspaceItemUId == expected.SchemaUId).ToArray();
            var schemaMatches = source.Schemas.Where(item => item.SchemaName == expected.SchemaName &&
                item.ActualPackageName == expected.PackageName && item.SchemaUId == expected.SchemaUId).ToArray();
            if (itemMatches.Length != 1 || schemaMatches.Length != 1)
            {
                throw new InvalidOperationException($"Missing or ambiguous bounded identity {expected.SchemaName}/{expected.PackageName}.");
            }
        }
        foreach (var schema in source.Schemas)
        {
            RequireGuid(schema.SchemaUId, "SchemaUId");
            RequireGuid(schema.GetSchemaSchemaIdCandidate, "GetSchemaSchemaIdCandidate");
            RequireGuid(schema.ActualPackageUId, "ActualPackageUId");
            if (schema.ParentSchemaUId is not null) RequireGuid(schema.ParentSchemaUId, "ParentSchemaUId");
            var duplicateColumns = schema.Columns.GroupBy(column => column.ColumnUId, StringComparer.Ordinal)
                .Where(group => group.Count() != 1).ToArray();
            if (duplicateColumns.Length != 0)
            {
                throw new InvalidOperationException($"Duplicate ColumnUId in {schema.SchemaUId}.");
            }
            foreach (var column in schema.Columns)
            {
                RequireGuid(column.ColumnUId, "ColumnUId");
                if (column.ReferenceSchemaUId is not null) RequireGuid(column.ReferenceSchemaUId, "ReferenceSchemaUId");
            }
            foreach (var index in schema.Indexes)
            {
                RequireGuid(index.IndexUId, "IndexUId");
                if (index.IsAutoName || index.Members.Count != 1)
                {
                    throw new InvalidOperationException("UNSUPPORTED_INDEX_SHAPE.");
                }
                foreach (var member in index.Members)
                {
                    RequireGuid(member.MemberUId, "IndexMemberUId");
                    RequireGuid(member.ColumnUId, "IndexColumnUId");
                    var targets = schema.Columns.Where(column => column.ColumnUId == member.ColumnUId).ToArray();
                    if (targets.Length != 1 || targets[0].Ownership is not ("Own" or "Inherited"))
                    {
                        throw new InvalidOperationException("Index member does not resolve exactly once to Own/Inherited ColumnUId.");
                    }
                    if (member.MemberUId == member.ColumnUId)
                    {
                        throw new InvalidOperationException("Index member uId must not substitute ColumnUId.");
                    }
                }
            }
        }
        if (source.LookupRows.Count != 3 || source.LookupValues.Count != 6)
        {
            throw new InvalidOperationException("Bounded lookup source must contain 3 rows and 6 Name/Description values.");
        }
        if (source.LookupValues.Any(value => value.ValueKind != "Text" || value.ColumnName is not ("Name" or "Description")))
        {
            throw new InvalidOperationException("Bounded lookup values contain an unsupported kind/column.");
        }
        var lookupRowKeys = source.LookupRows
            .GroupBy(row => (row.SchemaName, row.RecordId))
            .ToDictionary(group => group.Key, group => group.ToArray());
        if (lookupRowKeys.Values.Any(rows => rows.Length != 1))
        {
            throw new InvalidOperationException("Bounded lookup row metadata contains duplicate identities.");
        }
        foreach (var (key, metadataRows) in lookupRowKeys)
        {
            var values = source.LookupValues
                .Where(value => value.SchemaName == key.SchemaName && value.RecordId == key.RecordId)
                .OrderBy(value => value.ColumnName, StringComparer.Ordinal)
                .ToArray();
            if (!values.Select(value => value.ColumnName).SequenceEqual(new[] { "Description", "Name" }) ||
                values.Any(value => !string.Equals(value.SourceFingerprint, metadataRows[0].SourceFingerprint, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException($"LookupValues metadata/value group is incomplete or inconsistent: {key.SchemaName}/{key.RecordId}.");
            }
        }
        if (source.LookupValues.Any(value => !lookupRowKeys.ContainsKey((value.SchemaName, value.RecordId))))
        {
            throw new InvalidOperationException("Bounded lookup value has no matching row metadata.");
        }
        _ = BuildActualIndexedCheck(source);
    }

    private static ActualIndexedCheck BuildActualIndexedCheck(BaselineSource source)
    {
        var rows = new List<ActualIndexedMatrixRow>();
        foreach (var schema in source.Schemas)
        {
            var membership = schema.Indexes.SelectMany(index => index.Members)
                .GroupBy(member => member.ColumnUId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            foreach (var column in schema.Columns)
            {
                membership.TryGetValue(column.ColumnUId, out var count);
                if (count > 0 || column.ActualIndexed)
                {
                    rows.Add(new ActualIndexedMatrixRow(schema.SchemaUId, schema.SchemaName,
                        schema.ActualPackageName, column.ColumnName, column.ColumnUId,
                        column.ActualIndexed, count,
                        count > 0 == column.ActualIndexed ? "AgreementIsInformativeOnly" : "SeparationRequired"));
                }
            }
        }
        return new ActualIndexedCheck(
            "SOURCE_READY",
            source.Schemas.Sum(schema => schema.Columns.Count(column => column.ActualIndexed)),
            source.Schemas.Sum(schema => schema.Indexes.Sum(index => index.Members.Count)),
            rows.OrderBy(row => row.SchemaUId, StringComparer.Ordinal).ThenBy(row => row.ColumnUId, StringComparer.Ordinal).ToArray(),
            "ActualIndexed and schema.indexes[] membership are independent source fields; neither is inferred from the other.",
            "INDEX_SYNC_UNRESOLVED");
    }

    private static IReadOnlyList<TableData> BuildModelTables(BaselineSource source, string pairHash,
        string targetFingerprint)
    {
        var manifest = ManifestRows(source, pairHash, targetFingerprint);
        var workspaceRows = source.WorkspaceItems.OrderBy(item => item.WorkspaceItemUId, StringComparer.Ordinal)
            .Select(item => Row(item.WorkspaceItemUId, item.Name, "EntitySchema", item.PackageName,
                item.PackageUId, "Structured", "Verified bounded baseline")).ToArray();
        var schemaRows = source.Schemas.OrderBy(schema => schema.SchemaUId, StringComparer.Ordinal)
            .Select(schema => Row(schema.SchemaName, schema.SchemaUId, null,
                schema.SchemaName == "ActivityPriority" ? "lookup" : "entity",
                schema.ParentSchemaName, schema.ParentSchemaUId, null, schema.ActualPackageName,
                schema.ActualPackageUId, "Active", "Present", FingerprintSchema(schema))).ToArray();
        var columnRows = source.Schemas.SelectMany(schema => schema.Columns.Select(column => new { schema, column }))
            .OrderBy(item => item.schema.SchemaUId, StringComparer.Ordinal)
            .ThenBy(item => OwnershipRank(item.column.Ownership))
            .ThenBy(item => item.column.ColumnUId, StringComparer.Ordinal)
            .Select(item => Row(item.schema.SchemaName, item.schema.SchemaUId, item.column.ColumnName,
                item.column.ColumnUId, item.column.Ownership, $"BPMSoftTypeCode:{item.column.TypeCode}",
                item.column.ReferenceSchemaName, item.column.ReferenceSchemaUId,
                item.column.Ownership == "Own" ? item.column.RequirementTypeMapped : null,
                item.column.RequirementTypeMapped,
                item.column.Ownership == "Own" ? item.column.ActualIndexed : null,
                item.column.ActualIndexed,
                "Active", "Present", FingerprintColumn(item.column))).ToArray();
        var indexRows = source.Schemas.SelectMany(schema => schema.Indexes.SelectMany(index =>
                index.Members.Select(member => new { schema, index, member })))
            .OrderBy(item => item.schema.SchemaUId, StringComparer.Ordinal)
            .ThenBy(item => item.index.IndexUId, StringComparer.Ordinal)
            .ThenBy(item => item.member.Ordinal)
            .Select(item =>
            {
                var column = item.schema.Columns.Single(value => value.ColumnUId == item.member.ColumnUId);
                return Row(item.schema.SchemaName, item.schema.SchemaUId, item.index.IndexUId,
                    item.index.IndexName, item.index.IsUnique, column.ColumnName,
                    item.member.ColumnUId, item.member.Ordinal, FingerprintIndex(item.index, item.member));
            }).ToArray();

        return
        [
            ReadmeTable("Model", source),
            new TableData("Manifest", ["Key", "Value"], manifest, _ => Array.Empty<int>(), true),
            new TableData("WorkspaceInventory", ["WorkspaceItemUId", "Name", "ItemType", "PackageName", "PackageUId", "SupportStatus", "SupportReason"], workspaceRows, _ => Array.Empty<int>(), true),
            new TableData("Schemas", ["SchemaName", "SchemaUId", "SysSchemaId", "SchemaKind", "ParentSchemaName", "ParentSchemaUId", "DesiredPackageName", "ActualPackageName", "ActualPackageUId", "DesiredState", "ServerPresence", "ActualFingerprint"], schemaRows, _ => [9], false),
            new TableData("Columns", ["SchemaName", "ParentSchemaUId", "ColumnName", "ColumnUId", "Ownership", "DataType", "ReferenceSchemaName", "ReferenceSchemaUId", "DesiredRequired", "ActualRequired", "DesiredIndexed", "ActualIndexed", "DesiredState", "ServerPresence", "ActualFingerprint"], columnRows,
                row => string.Equals(Convert.ToString(row[4], CultureInfo.InvariantCulture), "Own", StringComparison.Ordinal) ? [8, 10, 12] : Array.Empty<int>(), false),
            new TableData("Indexes", ["SchemaName", "SchemaUId", "IndexUId", "IndexName", "IsUnique", "ColumnName", "ColumnUId", "Ordinal", "ActualFingerprint"], indexRows, _ => Array.Empty<int>(), true),
            ValidationListsTable(),
            PullConflictsTable()
        ];
    }

    private static IReadOnlyList<TableData> BuildLookupTables(BaselineSource source, string pairHash,
        string targetFingerprint)
    {
        var registry = source.LookupRegistry;
        var registryFingerprint = HashCanonical(new { registry.SchemaName, registry.SysEntitySchemaUId,
            registry.LookupRecordId, registry.BaseSchemaName, registry.BaseSchemaUId });
        var registryRows = new[] { Row(registry.SchemaName, registry.SysEntitySchemaUId,
            registry.LookupRecordId, registry.BaseSchemaName, registry.BaseSchemaUId,
            "Active", "Present", registryFingerprint) };
        var rowsByKey = source.LookupRows.ToDictionary(
            row => (row.SchemaName, row.RecordId), row => row);
        var valueRows = source.LookupValues.OrderBy(value => value.SchemaName, StringComparer.Ordinal)
            .ThenBy(value => value.RecordId, StringComparer.Ordinal)
            .ThenBy(value => value.ColumnName, StringComparer.Ordinal)
            .Select(value =>
            {
                if (!rowsByKey.TryGetValue((value.SchemaName, value.RecordId), out var row))
                {
                    throw new InvalidOperationException($"Lookup value has no row metadata: {value.SchemaName}/{value.RecordId}.");
                }
                if (!string.Equals(row.SourceFingerprint, value.SourceFingerprint, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Lookup row/value fingerprint mismatch: {value.SchemaName}/{value.RecordId}.");
                }
                return Row(value.SchemaName, row.SysEntitySchemaUId, value.RecordId, null,
                    "Active", "Present", value.SourceFingerprint, null, value.ColumnName,
                    value.ValueState, value.Value, value.ValueKind, null, null,
                    value.CanonicalValue);
            }).ToArray();

        return
        [
            ReadmeTable("Lookup", source),
            new TableData("Manifest", ["Key", "Value"], ManifestRows(source, pairHash, targetFingerprint), _ => Array.Empty<int>(), true),
            new TableData("LookupRegistry", ["SchemaName", "SysEntitySchemaUId", "LookupRecordId", "BaseSchemaName", "BaseSchemaUId", "DesiredState", "ServerPresence", "ActualFingerprint"], registryRows, _ => [5], false),
            new TableData("LookupValues", ["SchemaName", "SysEntitySchemaUId", "RecordId", "DraftRowToken", "DesiredState", "ServerPresence", "SourceFingerprint", "Comment", "ColumnName", "ValueState", "Value", "ValueKind", "ReferenceRecordId", "ReferenceDraftRowToken", "CanonicalValue"], valueRows, _ => [4, 7, 9, 10, 12, 13], false),
            ValidationListsTable(),
            PullConflictsTable()
        ];
    }

    private static TableData ReadmeTable(string kind, BaselineSource source)
    {
        return new TableData("Readme", ["Topic", "Value"],
        [
            Row("Workbook", kind + " Catalog"),
            Row("Scope", "Verified bounded baseline; not a full catalog"),
            Row("Source", "Local BPMSoft read-only capture; Google input is prohibited"),
            Row("Identity", "Metadata relations use UId; record relations use Id"),
            Row("Indexes", "Read-only membership from schema.indexes[].columns[].columnUId only"),
            Row("ActualIndexed", "Separate compatibility flag; never inferred from index membership"),
            Row("Future index load", "INDEX_SYNC_UNRESOLVED until a separate owner gate"),
            Row("Schema id candidate", "Preserved only in local source evidence; SysSchemaId workbook field is blank"),
            Row("PullRunId", source.PullRunId)
        ], _ => Array.Empty<int>(), true);
    }

    private static object?[][] ManifestRows(BaselineSource source, string pairHash, string targetFingerprint)
    {
        return
        [
            Row("ContractVersion", source.ContractVersion),
            Row("PairId", source.PairId),
            Row("PullRunId", source.PullRunId),
            Row("TargetAlias", source.TargetAlias),
            Row("ScopeMode", source.ScopeMode),
            Row("PullStartedUtc", source.PullStartedUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)),
            Row("PullCompletedUtc", source.PullCompletedUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)),
            Row("PairBaselineHash", pairHash),
            Row("BaselineTargetFingerprint", targetFingerprint),
            Row("TemplateVersion", TemplateVersion)
        ];
    }

    private static TableData ValidationListsTable()
    {
        var columns = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["DesiredState"] = ["Active", "Proposed", "Removed"],
            ["ServerPresence"] = ["Present", "PotentiallyDeleted", "NotRead"],
            ["Ownership"] = ["Own", "Inherited", "System"],
            ["SchemaKind"] = ["entity", "lookup", "other"],
            ["ValueState"] = ["Null", "EmptyString", "Value"],
            ["ValueKind"] = ["Text", "Integer", "Decimal", "Boolean", "Date", "DateTime", "Guid", "LookupReference"],
            ["BooleanChoice"] = ["TRUE", "FALSE"],
            ["SupportStatus"] = ["Structured", "InventoryOnly", "Unreadable", "Unsupported"],
            ["ScopeMode"] = [ScopeMode, "AllReadableCatalog"],
            ["DataType"] = ["BPMSoftTypeCode:0", "BPMSoftTypeCode:4", "BPMSoftTypeCode:7", "BPMSoftTypeCode:10", "BPMSoftTypeCode:12", "BPMSoftTypeCode:14", "BPMSoftTypeCode:16", "BPMSoftTypeCode:27", "BPMSoftTypeCode:28", "BPMSoftTypeCode:29"]
        };
        var max = columns.Values.Max(values => values.Length);
        var rows = Enumerable.Range(0, max).Select(row => columns.Values
            .Select(values => row < values.Length ? (object?)values[row] : null).ToArray()).ToArray();
        return new TableData("ValidationLists", columns.Keys.ToArray(), rows, _ => Array.Empty<int>(), true, true);
    }

    private static TableData PullConflictsTable()
    {
        return new TableData("PullConflicts",
            ["ConflictCode", "Workbook", "Sheet", "StableKey", "ColumnName", "BeforeValue", "ServerValue", "ResolutionStatus", "Message"],
            Array.Empty<object?[]>(), _ => Array.Empty<int>(), true);
    }

    private static object?[] Row(params object?[] values) => values;
    private static int OwnershipRank(string ownership) => ownership switch
    {
        "Own" => 0,
        "Inherited" => 1,
        "System" => 2,
        _ => 99
    };

    private static async Task CreateWorkbookAsync(string outputPath, IReadOnlyList<TableData> tables)
    {
        if (File.Exists(outputPath))
        {
            throw new InvalidOperationException($"Refusing to overwrite workbook: {outputPath}");
        }

        var template = @"C:\Users\Evgenii_2\.codex\skills\minimax-xlsx\templates\minimal_xlsx";
        var packer = @"C:\Users\Evgenii_2\.codex\skills\minimax-xlsx\scripts\xlsx_pack.py";
        if (!Directory.Exists(template) || !File.Exists(packer))
        {
            throw new InvalidOperationException("minimax-xlsx template or xlsx_pack.py is unavailable.");
        }

        var staging = Path.Combine(Path.GetTempPath(), "bpmsoft-xlsx-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            CopyDirectory(template, staging);
            AppendUnlockedStyle(Path.Combine(staging, "xl", "styles.xml"));
            WriteWorkbookParts(staging, tables);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            await RunProcessAsync("python", [$"{packer}", staging, outputPath]);
            if (!File.Exists(outputPath))
            {
                throw new InvalidOperationException("xlsx_pack.py did not create the workbook.");
            }
        }
        finally
        {
            if (Directory.Exists(staging))
            {
                Directory.Delete(staging, recursive: true);
            }
        }
    }

    private static void AppendUnlockedStyle(string path)
    {
        var document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
        document.DescendantNodes().OfType<XComment>().Remove();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var cellXfs = document.Root?.Element(ns + "cellXfs")
            ?? throw new InvalidOperationException("Template styles.xml has no cellXfs.");
        var count = cellXfs.Elements(ns + "xf").Count();
        if (count != 13)
        {
            throw new InvalidOperationException($"Unexpected minimax template cellXfs count: {count}.");
        }

        cellXfs.Add(new XElement(ns + "xf",
            new XAttribute("numFmtId", "0"), new XAttribute("fontId", "1"),
            new XAttribute("fillId", "0"), new XAttribute("borderId", "0"),
            new XAttribute("xfId", "0"), new XAttribute("applyFont", "1"),
            new XAttribute("applyProtection", "1"),
            new XElement(ns + "protection", new XAttribute("locked", "0"))));
        cellXfs.SetAttributeValue("count", count + 1);
        document.Save(path);
    }

    private static void WriteWorkbookParts(string staging, IReadOnlyList<TableData> tables)
    {
        XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XNamespace packageRel = "http://schemas.openxmlformats.org/package/2006/relationships";
        XNamespace types = "http://schemas.openxmlformats.org/package/2006/content-types";

        var sheets = tables.Select((table, index) => new XElement(main + "sheet",
            new XAttribute("name", table.Name), new XAttribute("sheetId", index + 1),
            new XAttribute(rel + "id", "rId" + (index + 1)),
            table.Hidden ? new XAttribute("state", "veryHidden") : null)).ToArray();
        var names = ValidationDefinedNames(tables);
        var workbook = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(main + "workbook", new XAttribute(XNamespace.Xmlns + "r", rel),
                new XElement(main + "fileVersion", new XAttribute("appName", "xl"),
                    new XAttribute("lastEdited", "7"), new XAttribute("lowestEdited", "7")),
                new XElement(main + "workbookPr", new XAttribute("defaultThemeVersion", "166925")),
                new XElement(main + "bookViews", new XElement(main + "workbookView",
                    new XAttribute("xWindow", "0"), new XAttribute("yWindow", "0"),
                    new XAttribute("windowWidth", "20140"), new XAttribute("windowHeight", "10960"))),
                new XElement(main + "sheets", sheets),
                new XElement(main + "definedNames", names.Select(pair =>
                    new XElement(main + "definedName", new XAttribute("name", pair.Key), pair.Value))),
                new XElement(main + "calcPr", new XAttribute("calcId", "191029"))));
        workbook.Save(Path.Combine(staging, "xl", "workbook.xml"));

        var relationships = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(packageRel + "Relationships",
                tables.Select((_, index) => new XElement(packageRel + "Relationship",
                    new XAttribute("Id", "rId" + (index + 1)),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                    new XAttribute("Target", "worksheets/sheet" + (index + 1) + ".xml"))),
                new XElement(packageRel + "Relationship",
                    new XAttribute("Id", "rId" + (tables.Count + 1)),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"),
                    new XAttribute("Target", "styles.xml"))));
        relationships.Save(Path.Combine(staging, "xl", "_rels", "workbook.xml.rels"));

        var contentTypes = XDocument.Load(Path.Combine(staging, "[Content_Types].xml"));
        var root = contentTypes.Root ?? throw new InvalidOperationException("Invalid content types template.");
        root.Elements(types + "Override")
            .Where(element =>
            {
                var part = (string?)element.Attribute("PartName");
                return part?.StartsWith("/xl/worksheets/", StringComparison.Ordinal) == true
                    || string.Equals(part, "/xl/sharedStrings.xml", StringComparison.Ordinal);
            })
            .Remove();
        foreach (var index in Enumerable.Range(1, tables.Count))
        {
            root.Add(new XElement(types + "Override", new XAttribute("PartName", $"/xl/worksheets/sheet{index}.xml"),
                new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")));
        }
        contentTypes.Save(Path.Combine(staging, "[Content_Types].xml"));
        var oldSheet = Path.Combine(staging, "xl", "worksheets", "sheet1.xml");
        if (File.Exists(oldSheet)) File.Delete(oldSheet);
        for (var index = 0; index < tables.Count; index++)
        {
            WriteSheet(Path.Combine(staging, "xl", "worksheets", $"sheet{index + 1}.xml"), tables[index]);
        }
        var sharedStrings = Path.Combine(staging, "xl", "sharedStrings.xml");
        if (File.Exists(sharedStrings)) File.Delete(sharedStrings);
    }

    private static Dictionary<string, string> ValidationDefinedNames(IReadOnlyList<TableData> tables)
    {
        var validation = tables.Single(table => table.Name == "ValidationLists");
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var column = 0; column < validation.Headers.Length; column++)
        {
            var count = validation.Rows.Count(row => row[column] is not null);
            result[validation.Headers[column]] = $"'ValidationLists'!${ColumnName(column + 1)}$2:${ColumnName(column + 1)}${count + 1}";
        }
        return result;
    }

    private static void WriteSheet(string path, TableData table)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var allRows = new List<object?[]> { table.Headers.Cast<object?>().ToArray() };
        allRows.AddRange(table.Rows);
        var rows = allRows.Select((values, rowIndex) =>
        {
            var editable = rowIndex == 0 ? Array.Empty<int>() : table.EditableColumns(values);
            return new XElement(ns + "row", new XAttribute("r", rowIndex + 1),
                values.Select((value, columnIndex) => CreateCell(ns, rowIndex + 1, columnIndex + 1,
                    value, rowIndex == 0 ? 4 : editable.Contains(columnIndex) ? 13 : 0)));
        }).ToArray();
        var lastColumn = ColumnName(table.Headers.Length);
        var lastRow = Math.Max(1, allRows.Count);
        var validations = CreateValidations(ns, table, lastRow);
        var document = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(ns + "worksheet",
                new XElement(ns + "sheetViews", new XElement(ns + "sheetView", new XAttribute("workbookViewId", "0"),
                    new XElement(ns + "pane", new XAttribute("ySplit", "1"), new XAttribute("topLeftCell", "A2"),
                        new XAttribute("activePane", "bottomLeft"), new XAttribute("state", "frozen")))),
                new XElement(ns + "cols", Enumerable.Range(1, table.Headers.Length).Select(column =>
                    new XElement(ns + "col", new XAttribute("min", column), new XAttribute("max", column),
                        new XAttribute("width", Math.Clamp(table.Headers[column - 1].Length + 4, 12, 34)),
                        new XAttribute("customWidth", "1")))),
                new XElement(ns + "sheetData", rows),
                table.ReadOnly ? new XElement(ns + "sheetProtection", new XAttribute("sheet", "1"),
                    new XAttribute("objects", "1"), new XAttribute("scenarios", "1"),
                    new XAttribute("formatColumns", "0"), new XAttribute("autoFilter", "0"),
                    new XAttribute("sort", "0"), new XAttribute("selectLockedCells", "0"),
                    new XAttribute("selectUnlockedCells", "0")) : null,
                new XElement(ns + "autoFilter", new XAttribute("ref", $"A1:{lastColumn}{lastRow}")),
                validations));
        document.Save(path);
    }

    private static XElement? CreateValidations(XNamespace ns, TableData table, int lastRow)
    {
        if (table.ReadOnly || lastRow < 2) return null;
        var map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["DesiredState"] = "DesiredState", ["ServerPresence"] = "ServerPresence",
            ["Ownership"] = "Ownership", ["SchemaKind"] = "SchemaKind",
            ["ValueState"] = "ValueState", ["ValueKind"] = "ValueKind",
            ["DesiredRequired"] = "BooleanChoice", ["DesiredIndexed"] = "BooleanChoice"
        };
        var elements = new List<XElement>();
        for (var index = 0; index < table.Headers.Length; index++)
        {
            if (!map.TryGetValue(table.Headers[index], out var list)) continue;
            var column = ColumnName(index + 1);
            elements.Add(new XElement(ns + "dataValidation", new XAttribute("type", "list"),
                new XAttribute("allowBlank", "1"), new XAttribute("showErrorMessage", "1"),
                new XAttribute("sqref", $"{column}2:{column}{Math.Max(lastRow, 500)}"),
                new XElement(ns + "formula1", list)));
        }
        return elements.Count == 0 ? null : new XElement(ns + "dataValidations",
            new XAttribute("count", elements.Count), elements);
    }

    private static XElement CreateCell(XNamespace ns, int row, int column, object? value, int style)
    {
        var cell = new XElement(ns + "c", new XAttribute("r", ColumnName(column) + row), new XAttribute("s", style));
        if (value is null) return cell;
        if (value is bool boolean)
        {
            cell.SetAttributeValue("t", "b");
            cell.Add(new XElement(ns + "v", boolean ? "1" : "0"));
        }
        else if (value is int or long)
        {
            cell.Add(new XElement(ns + "v", Convert.ToString(value, CultureInfo.InvariantCulture)));
        }
        else
        {
            cell.SetAttributeValue("t", "inlineStr");
            cell.Add(new XElement(ns + "is", new XElement(ns + "t",
                new XAttribute(XNamespace.Xml + "space", "preserve"), Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)));
        }
        return cell;
    }

    private static string ColumnName(int column)
    {
        var result = string.Empty;
        while (column > 0)
        {
            column--;
            result = (char)('A' + column % 26) + result;
            column /= 26;
        }
        return result;
    }

    private static WorkbookVerification VerifyWorkbook(string path, IReadOnlyList<TableData> expected)
    {
        if (!File.Exists(path)) throw new InvalidOperationException($"Workbook missing: {path}");
        using var archive = ZipFile.OpenRead(path);
        VerifyPackageClosure(archive);
        var forbidden = archive.Entries.Where(entry => entry.FullName.Contains("externalLinks", StringComparison.OrdinalIgnoreCase)
            || entry.FullName.Contains("connections", StringComparison.OrdinalIgnoreCase)
            || entry.FullName.EndsWith("vbaProject.bin", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (forbidden.Length != 0) throw new InvalidOperationException("Forbidden external/VBA parts found.");
        if (archive.Entries.Where(entry => entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .Any(entry => LoadEntry(archive, entry.FullName).Descendants().Any(element => element.Name.LocalName == "f")))
            throw new InvalidOperationException("Formula cells are prohibited.");

        var workbookDoc = LoadEntry(archive, "xl/workbook.xml");
        XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        var relDoc = LoadEntry(archive, "xl/_rels/workbook.xml.rels");
        XNamespace packageRel = "http://schemas.openxmlformats.org/package/2006/relationships";
        var relationMap = relDoc.Root!.Elements(packageRel + "Relationship")
            .ToDictionary(e => (string)e.Attribute("Id")!, e => (string)e.Attribute("Target")!, StringComparer.Ordinal);
        var sheetElements = workbookDoc.Root!.Element(main + "sheets")!.Elements(main + "sheet").ToArray();
        if (!sheetElements.Select(e => (string)e.Attribute("name")!).SequenceEqual(expected.Select(t => t.Name)))
            throw new InvalidOperationException("Workbook sheet order/name mismatch.");
        var workbookView = workbookDoc.Root.Element(main + "bookViews")?.Element(main + "workbookView");
        if (workbookView is null || workbookView.Attribute("windowWidth") is null || workbookView.Attribute("windowHeight") is null)
            throw new InvalidOperationException("Excel-compatible workbook view properties are missing.");
        var tables = new Dictionary<string, ParsedTable>(StringComparer.Ordinal);
        for (var i = 0; i < expected.Count; i++)
        {
            var sheet = sheetElements[i];
            var target = relationMap[(string)sheet.Attribute(rel + "id")!].Replace('\\', '/');
            var sheetDoc = LoadEntry(archive, "xl/" + target);
            var protection = sheetDoc.Descendants(main + "sheetProtection").SingleOrDefault();
            if (expected[i].ReadOnly)
            {
                if (protection is null)
                    throw new InvalidOperationException($"Read-only sheet protection missing: {expected[i].Name}");
                // Excel cannot actually reorder locked cells on a protected sheet even when sort="0".
                // Sorting is best-effort metadata, not a required interaction contract.
                foreach (var permission in new[] { "formatColumns", "autoFilter", "selectLockedCells", "selectUnlockedCells" })
                    if (!string.Equals((string?)protection.Attribute(permission), "0", StringComparison.Ordinal))
                        throw new InvalidOperationException($"Required interactive permission is missing: {expected[i].Name}/{permission}");
            }
            else if (protection is not null)
            {
                throw new InvalidOperationException($"Mixed/editable sheet must not be protected: {expected[i].Name}");
            }
            var parsed = ParseTable(sheetDoc, main);
            CompareTable(expected[i], parsed);
            VerifySheetGuards(expected[i], parsed, sheetDoc, main);
            var actualState = (string?)sheet.Attribute("state");
            if (expected[i].Hidden != string.Equals(actualState, "veryHidden", StringComparison.Ordinal))
                throw new InvalidOperationException($"Sheet hidden-state mismatch: {expected[i].Name}");
            tables[expected[i].Name] = parsed;
        }
        return new WorkbookVerification(path, tables,
            new WorkbookSummary("PASS", expected.Count, expected.Sum(t => t.Rows.Length), HashFile(path),
                ComputeCanonicalWorksheetHash(path), false, forbidden.Length));
    }

    private static void VerifyPackageClosure(ZipArchive archive)
    {
        var entries = archive.Entries.Select(entry => "/" + entry.FullName.Replace('\\', '/')).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var content = LoadEntry(archive, "[Content_Types].xml");
        XNamespace types = "http://schemas.openxmlformats.org/package/2006/content-types";
        foreach (var part in content.Root!.Elements(types + "Override").Select(element => (string?)element.Attribute("PartName")).Where(part => part is not null))
            if (!entries.Contains(part!)) throw new InvalidOperationException($"Content type points to missing part: {part}");
        XNamespace relationships = "http://schemas.openxmlformats.org/package/2006/relationships";
        foreach (var entry in archive.Entries.Where(entry => entry.FullName.EndsWith(".rels", StringComparison.OrdinalIgnoreCase)))
        {
            var document = LoadEntry(archive, entry.FullName);
            if (document.Root!.Elements(relationships + "Relationship").Any(element =>
                string.Equals((string?)element.Attribute("TargetMode"), "External", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("External relationship is prohibited.");
        }
    }

    private static void VerifySheetGuards(TableData expected, ParsedTable parsed, XDocument document, XNamespace ns)
    {
        if (document.Descendants(ns + "autoFilter").Count() != 1)
            throw new InvalidOperationException($"AutoFilter missing: {expected.Name}");
        var worksheetChildren = document.Root!.Elements().ToArray();
        var protectionIndex = Array.FindIndex(worksheetChildren, element => element.Name == ns + "sheetProtection");
        var filterIndex = Array.FindIndex(worksheetChildren, element => element.Name == ns + "autoFilter");
        var validationsIndex = Array.FindIndex(worksheetChildren, element => element.Name == ns + "dataValidations");
        if (filterIndex < 0 ||
            (expected.ReadOnly && (protectionIndex < 0 || protectionIndex > filterIndex)) ||
            (!expected.ReadOnly && protectionIndex >= 0) ||
            (validationsIndex >= 0 && filterIndex > validationsIndex))
            throw new InvalidOperationException($"Excel worksheet element order is invalid: {expected.Name}");
        var rowElements = document.Descendants(ns + "row").ToArray();
        for (var row = 0; row < rowElements.Length; row++)
        {
            var editable = row == 0 ? Array.Empty<int>() : expected.EditableColumns(parsed.Rows[row - 1]);
            var cells = rowElements[row].Elements(ns + "c").ToArray();
            for (var column = 0; column < cells.Length; column++)
            {
                var actualStyle = (int?)cells[column].Attribute("s") ?? 0;
                var expectedStyle = row == 0 ? 4 : editable.Contains(column) ? 13 : 0;
                if (actualStyle != expectedStyle)
                    throw new InvalidOperationException($"Lock/style map mismatch: {expected.Name}!{ColumnName(column + 1)}{row + 1}");
            }
        }
        var validationHeaders = new[] { "DesiredState", "ServerPresence", "Ownership", "SchemaKind", "ValueState", "ValueKind", "DesiredRequired", "DesiredIndexed" };
        if (!expected.ReadOnly && expected.Headers.Any(validationHeaders.Contains)
            && !document.Descendants(ns + "dataValidation").Any())
            throw new InvalidOperationException($"Data validation missing: {expected.Name}");
    }

    private static string ComputeCanonicalWorksheetHash(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var parts = archive.Entries.Where(entry => entry.FullName.StartsWith("xl/worksheets/", StringComparison.Ordinal)
            && entry.FullName.EndsWith(".xml", StringComparison.Ordinal)).OrderBy(entry => entry.FullName, StringComparer.Ordinal)
            .Select(entry => entry.FullName + "\n" + LoadEntry(archive, entry.FullName).ToString(SaveOptions.DisableFormatting));
        return HashCanonical(parts);
    }

    private static void CreateTamperedWorkbook(string source, string destination, bool addFormula, bool addExternalPart)
    {
        using (var input = ZipFile.OpenRead(source))
        using (var output = ZipFile.Open(destination, ZipArchiveMode.Create))
        {
            foreach (var entry in input.Entries)
            {
                var created = output.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                using var from = entry.Open();
                using var to = created.Open();
                if (addFormula && entry.FullName == "xl/worksheets/sheet1.xml")
                {
                    var document = XDocument.Load(from);
                    XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                    document.Descendants(ns + "c").First().Add(new XElement(ns + "f", "1+1"));
                    document.Save(to);
                }
                else from.CopyTo(to);
            }
            if (addExternalPart)
            {
                var extra = output.CreateEntry("xl/externalLinks/externalLink1.xml");
                using var writer = new StreamWriter(extra.Open(), Utf8NoBom);
                writer.Write("<?xml version=\"1.0\"?><externalLink xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"/>");
            }
        }
    }

    private static ParsedTable ParseTable(XDocument sheet, XNamespace ns)
    {
        var rows = sheet.Descendants(ns + "row").Select(row => row.Elements(ns + "c").Select(cell =>
        {
            var type = (string?)cell.Attribute("t");
            if (type == "inlineStr") return (object?)(cell.Element(ns + "is")?.Element(ns + "t")?.Value ?? string.Empty);
            if (type == "b") return cell.Element(ns + "v")?.Value == "1";
            var raw = cell.Element(ns + "v")?.Value;
            return raw is null ? null : int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer) ? integer : raw;
        }).ToArray()).ToArray();
        if (rows.Length == 0) throw new InvalidOperationException("Worksheet has no header row.");
        return new ParsedTable(rows[0].Select(value => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty).ToArray(), rows[1..]);
    }

    private static void CompareTable(TableData expected, ParsedTable actual)
    {
        if (!expected.Headers.SequenceEqual(actual.Headers))
            throw new InvalidOperationException($"Header mismatch: {expected.Name}");
        if (expected.Rows.Length != actual.Rows.Length)
            throw new InvalidOperationException($"Row count mismatch: {expected.Name}");
        for (var row = 0; row < expected.Rows.Length; row++)
        for (var column = 0; column < expected.Headers.Length; column++)
        {
            var left = expected.Rows[row][column];
            var right = column < actual.Rows[row].Length ? actual.Rows[row][column] : null;
            if (!CellEquals(left, right))
                throw new InvalidOperationException($"Cell mismatch: {expected.Name}!{ColumnName(column + 1)}{row + 2}");
        }
    }

    private static bool CellEquals(object? left, object? right)
    {
        if (left is null && right is null) return true;
        if (left?.GetType() == right?.GetType()) return Equals(left, right);
        return string.Equals(Convert.ToString(left, CultureInfo.InvariantCulture), Convert.ToString(right, CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    private static void VerifyPair(WorkbookVerification model, WorkbookVerification lookup, BaselineSource source, string pairHash)
    {
        var modelManifest = ManifestDictionary(model.Tables["Manifest"]);
        var lookupManifest = ManifestDictionary(lookup.Tables["Manifest"]);
        foreach (var key in new[] { "ContractVersion", "PairId", "PullRunId", "PairBaselineHash", "BaselineTargetFingerprint", "TemplateVersion" })
        {
            if (!modelManifest.TryGetValue(key, out var left) || !lookupManifest.TryGetValue(key, out var right)
                || !string.Equals(left, right, StringComparison.Ordinal))
                throw new InvalidOperationException($"Workbook pair identity mismatch: {key}");
        }
        if (modelManifest["PairId"] != source.PairId || modelManifest["PullRunId"] != source.PullRunId
            || modelManifest["PairBaselineHash"] != pairHash)
            throw new InvalidOperationException("Workbook pair does not bind to source evidence.");
    }

    private static Dictionary<string, string> ManifestDictionary(ParsedTable table) => table.Rows
        .ToDictionary(row => Convert.ToString(row[0], CultureInfo.InvariantCulture)!,
            row => Convert.ToString(row[1], CultureInfo.InvariantCulture) ?? string.Empty, StringComparer.Ordinal);

    private static object VerifyExportedIndexes(BaselineSource source, WorkbookVerification model)
    {
        var sourceRows = source.Schemas.SelectMany(schema => schema.Indexes.SelectMany(index => index.Members.Select(member =>
            string.Join("|", schema.SchemaName, schema.SchemaUId, index.IndexUId, index.IndexName,
                index.IsUnique, member.ColumnUId, member.Ordinal)))).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var sheet = model.Tables["Indexes"];
        var exported = sheet.Rows.Select(row => string.Join("|", row[0], row[1], row[2], row[3], row[4], row[6], row[7]))
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (!sourceRows.SequenceEqual(exported)) throw new InvalidOperationException("Indexes export is not an exact schema.indexes[] membership projection.");
        var check = BuildActualIndexedCheck(source);
        return new { Status = "PASS", IndexRows = exported.Length, check.SourceActualIndexedTrueCount,
            check.SourceMemberCount, SeparationPreserved = true, Source = "schema.indexes[].columns[].columnUId",
            NotInferredFromActualIndexed = true };
    }

    private static XDocument LoadEntry(ZipArchive archive, string name)
    {
        var entry = archive.GetEntry(name) ?? throw new InvalidOperationException($"XLSX part missing: {name}");
        using var stream = entry.Open();
        return XDocument.Load(stream);
    }

    private static string ReadEntry(ZipArchiveEntry entry)
    {
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string ComputePairBaselineHash(BaselineSource source) => HashCanonical(new
    {
        source.ContractVersion, source.ScopeMode,
        WorkspaceItems = source.WorkspaceItems.OrderBy(item => item.WorkspaceItemUId, StringComparer.Ordinal),
        Schemas = source.Schemas.OrderBy(schema => schema.SchemaUId, StringComparer.Ordinal).Select(schema => new
        {
            schema.SchemaName, schema.SchemaUId, schema.ParentSchemaName, schema.ParentSchemaUId,
            schema.ActualPackageName, schema.ActualPackageUId,
            Columns = schema.Columns.OrderBy(column => column.ColumnUId, StringComparer.Ordinal),
            Indexes = schema.Indexes.OrderBy(index => index.IndexUId, StringComparer.Ordinal).Select(index => new
            { index.SchemaUId, index.IndexUId, index.IndexName, index.IsUnique, index.IsAutoName,
                Members = index.Members.OrderBy(member => member.Ordinal).ThenBy(member => member.ColumnUId, StringComparer.Ordinal) })
        }),
        source.LookupRegistry,
        LookupRows = source.LookupRows.OrderBy(row => row.RecordId, StringComparer.Ordinal),
        LookupValues = source.LookupValues.OrderBy(value => value.RecordId, StringComparer.Ordinal).ThenBy(value => value.ColumnName, StringComparer.Ordinal)
    });

    private static string ComputeTargetFingerprint(BaselineSource source, string sourceHash) => HashCanonical(new
    { source.TargetAlias, source.PullStartedUtc, source.PullCompletedUtc, sourceHash, source.Assemblies });
    private static string FingerprintSchema(SchemaSource schema) => HashCanonical(new
    { schema.SchemaName, schema.SchemaUId, schema.ParentSchemaName, schema.ParentSchemaUId, schema.ActualPackageName, schema.ActualPackageUId });
    private static string FingerprintColumn(ColumnSource column) => HashCanonical(column);
    private static string FingerprintIndex(IndexSource index, IndexMemberSource member) => HashCanonical(new
    { index.SchemaUId, index.IndexUId, index.IndexName, index.IsUnique, member.ColumnUId, member.Ordinal });
    private static string HashCanonical(object value) => Convert.ToHexString(SHA256.HashData(Utf8NoBom.GetBytes(JsonSerializer.Serialize(value)))).ToLowerInvariant();
    private static string HashFile(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static string Serialize(object value) => JsonSerializer.Serialize(value, JsonOptions);

    private static FileManifest CreateFileManifest(IEnumerable<string> paths) => new("SHA-256", paths.Select(path =>
        new FileManifestEntry(Path.GetFullPath(path), new FileInfo(path).Length, HashFile(path))).ToArray());

    private static async Task<JsonDocument> PostJsonAsync(HttpClient client, string path, object body)
    {
        using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(path, content);
        return await EnsureSuccessfulResponse(response, path);
    }

    private static Task<JsonDocument> PostEmptyJsonAsync(HttpClient client, string path) => PostJsonAsync(client, path, new { });

    private static async Task<JsonDocument> EnsureSuccessfulResponse(HttpResponseMessage response, string path)
    {
        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"{path} returned HTTP {(int)response.StatusCode}.");
        try { return JsonDocument.Parse(text); }
        catch (JsonException) { throw new InvalidOperationException($"{path} returned malformed JSON."); }
    }

    private static bool IsSuccessfulLogin(JsonElement root) =>
        (!root.TryGetProperty("Code", out var code) || code.ValueKind == JsonValueKind.Number && code.GetInt32() == 0)
        && (!root.TryGetProperty("code", out var lowerCode) || lowerCode.ValueKind == JsonValueKind.Number && lowerCode.GetInt32() == 0);

    private static void EnsureSuccessfulEnvelope(JsonElement root, string operation)
    {
        if (root.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.False)
            throw new InvalidOperationException($"{operation} returned success=false.");
    }

    private static string GetRequiredString(JsonElement element, string property, string context)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new InvalidOperationException($"{context}.{property} must be a non-empty string.");
        return value.GetString()!;
    }

    private static string? GetOptionalString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static int GetRequiredInt32(JsonElement element, string property, string context)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var result))
            throw new InvalidOperationException($"{context}.{property} must be an Int32.");
        return result;
    }

    private static int? GetOptionalInt32(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result) ? result : null;

    private static bool GetRequiredBoolean(JsonElement element, string property, string context)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new InvalidOperationException($"{context}.{property} must be Boolean.");
        return value.GetBoolean();
    }

    private static string GetRequiredGuid(JsonElement element, string property, string context)
    {
        var value = GetRequiredString(element, property, context);
        return NormalizeGuid(value) ?? throw new InvalidOperationException($"{context}.{property} must be a GUID.");
    }

    private static string? NormalizeGuid(string? value) => Guid.TryParse(value, out var guid) ? guid.ToString("D") : null;

    private static void RequireGuid(string? value, string name)
    {
        if (NormalizeGuid(value) is null) throw new InvalidOperationException($"{name} must be a GUID.");
    }

    private static string GetGuidValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String) return NormalizeGuid(value.GetString()) ?? string.Empty;
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var name in new[] { "value", "Value", "primaryValue" })
                if (value.TryGetProperty(name, out var nested) && nested.ValueKind == JsonValueKind.String)
                    return NormalizeGuid(nested.GetString()) ?? string.Empty;
        }
        return string.Empty;
    }

    private static object? CanonicalJsonValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.String => value.GetString()?.Normalize(NormalizationForm.FormC),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.Object => value.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToDictionary(property => property.Name, property => CanonicalJsonValue(property.Value), StringComparer.Ordinal),
        JsonValueKind.Array => value.EnumerateArray().Select(CanonicalJsonValue).ToArray(),
        _ => value.GetRawText()
    };

    private static Uri ReadLocalTarget(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttp
            || !(uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || IPAddress.TryParse(uri.Host, out var ip) && IPAddress.IsLoopback(ip)))
            throw new InvalidOperationException("Only an explicit local http://localhost/loopback BPMSoft target is allowed.");
        return uri.AbsoluteUri.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri + "/");
    }

    private static string ReadValue(string label, string defaultValue)
    {
        Console.Write($"{label} [{defaultValue}]: ");
        var value = Console.ReadLine();
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
    }

    private static string ReadPassword()
    {
        Console.Write("Пароль: ");
        var builder = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return builder.ToString(); }
            if (key.Key == ConsoleKey.Backspace && builder.Length > 0) builder.Length--;
            else if (!char.IsControl(key.KeyChar)) builder.Append(key.KeyChar);
        }
    }

    private static Dictionary<string, string> ParseOptions(string[] args, params string[] required)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
                throw new InvalidOperationException("Options must be --name value pairs.");
            result[args[index][2..]] = args[index + 1];
        }
        foreach (var name in required) if (!result.ContainsKey(name)) throw new InvalidOperationException($"Missing --{name}.");
        return result;
    }

    private static string FindWorkspaceRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "docs")) && Directory.Exists(Path.Combine(directory.FullName, "WorkbookDeliveryTool"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new InvalidOperationException("Preparation workspace root not found.");
    }

    private static void CopyDirectory(string source, string destination)
    {
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: false);
        }
    }

    private static async Task RunProcessAsync(string executable, IEnumerable<string> arguments)
    {
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.Environment["PYTHONIOENCODING"] = "utf-8";
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Unable to start {executable}.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) throw new InvalidOperationException($"{executable} failed: {await stderr}");
        _ = await stdout;
    }

    private static BaselineSource CreateSyntheticSource()
    {
        var accountUId = "05c46595-729a-4668-94bc-56061dad4fb9";
        var codeUId = "11111111-1111-1111-1111-111111111111";
        var index = new IndexSource(accountUId, "22222222-2222-2222-2222-222222222222", "IX_Account_Code", true, false,
            [new IndexMemberSource("33333333-3333-3333-3333-333333333333", "Code", codeUId, 1, 0)]);
        var account = new SchemaSource("Account", accountUId, "10101010-1010-1010-1010-101010101010", "BaseEntity", "44444444-4444-4444-4444-444444444444",
            "Test1", "55555555-5555-5555-5555-555555555555",
            [new ColumnSource(accountUId, "Code", codeUId, "Own", 28, true, true, null, null)], [index]);
        var activity = new SchemaSource("ActivityPriority", "b934f48c-5dea-49b9-bde3-697cb4be5d8b", "20202020-2020-2020-2020-202020202020",
            "BaseLookup", "66666666-6666-6666-6666-666666666666", "Base", "77777777-7777-7777-7777-777777777777",
            Array.Empty<ColumnSource>(), Array.Empty<IndexSource>());
        var lookup = new SchemaSource("Lookup", "2aecdb97-990e-4c17-96f4-240ca6531c84", "30303030-3030-3030-3030-303030303030",
            "BaseLookup", "66666666-6666-6666-6666-666666666666", "Base", "12121212-1212-1212-1212-121212121212",
            Array.Empty<ColumnSource>(), Array.Empty<IndexSource>());
        var accountBase = new SchemaSource("Account", "25d7c1ab-1de0-4501-b402-02e0e5a72d6e", "40404040-4040-4040-4040-404040404040",
            "BaseEntity", "44444444-4444-4444-4444-444444444444", "Base", "13131313-1313-1313-1313-131313131313",
            Array.Empty<ColumnSource>(), Array.Empty<IndexSource>());
        var completeness = new SchemaSource("Account", "cc642965-191f-4b55-b1ea-fb8336e623b9", "50505050-5050-5050-5050-505050505050",
            "BaseEntity", "44444444-4444-4444-4444-444444444444", "Completeness", "14141414-1414-1414-1414-141414141414",
            Array.Empty<ColumnSource>(), Array.Empty<IndexSource>());
        var now = DateTimeOffset.Parse("2026-09-04T20:00:00Z", CultureInfo.InvariantCulture);
        var records = new[]
        {
            "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1", "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2", "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3"
        };
        var rows = records.Select(record => new LookupRowSource("ActivityPriority", activity.SchemaUId, record, "row-" + record[^1])).ToArray();
        var values = records.SelectMany((record, ordinal) => new[]
        {
            new LookupValueSource("ActivityPriority", record, "Name", "Value", "Priority " + ordinal, "Text", "Priority " + ordinal, "row-" + record[^1]),
            new LookupValueSource("ActivityPriority", record, "Description", ordinal == 0 ? "Null" : "Value", ordinal == 0 ? null : "Description " + ordinal,
                "Text", ordinal == 0 ? null : "Description " + ordinal, "row-" + record[^1])
        }).ToArray();
        return new BaselineSource(ContractVersion, "88888888-8888-8888-8888-888888888888", "99999999-9999-9999-9999-999999999999",
            "http://localhost:8002", ScopeMode, now, now.AddMinutes(1),
            new AssemblyEvidence(new AssemblyFileEvidence("common", ExpectedAssemblyVersion, CommonAssemblyHash),
                new AssemblyFileEvidence("nui", ExpectedAssemblyVersion, NuiAssemblyHash), new Dictionary<string, int>{{"Ascending",1},{"Descending",2},{"None",0}}),
            [new WorkspaceItemSource(accountUId, "Account", 3, "Test1", "55555555-5555-5555-5555-555555555555"),
             new WorkspaceItemSource(activity.SchemaUId, "ActivityPriority", 3, "Base", activity.ActualPackageUId),
             new WorkspaceItemSource(lookup.SchemaUId, "Lookup", 3, "Base", lookup.ActualPackageUId),
             new WorkspaceItemSource(accountBase.SchemaUId, "Account", 3, "Base", accountBase.ActualPackageUId),
             new WorkspaceItemSource(completeness.SchemaUId, "Account", 3, "Completeness", completeness.ActualPackageUId)],
            [account, activity, lookup, accountBase, completeness],
            new LookupRegistrySource("ActivityPriority", activity.SchemaUId, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", activity.ParentSchemaName, activity.ParentSchemaUId),
            rows, values,
            Array.Empty<OrderedProof>());
    }

    private sealed record ExpectedSchema(string SchemaName, string PackageName, string SchemaUId);
    private sealed record AssemblyFileEvidence(string Path, string Version, string Sha256);
    private sealed record AssemblyEvidence(AssemblyFileEvidence Common, AssemblyFileEvidence Nui, Dictionary<string, int> OrderDirectionValues);
    private sealed record WorkspaceItemSource(string WorkspaceItemUId, string Name, int ItemType, string PackageName, string PackageUId)
    {
        public string SupportStatus => "Structured";
        public string SupportReason => "Verified bounded baseline";
    }
    private sealed record ColumnSource(string ParentSchemaUId, string ColumnName, string ColumnUId, string Ownership, int TypeCode,
        bool RequirementTypeMapped, bool ActualIndexed, string? ReferenceSchemaName, string? ReferenceSchemaUId);
    private sealed record IndexMemberSource(string MemberUId, string MemberName, string ColumnUId, int OrderDirection, int Ordinal);
    private sealed record IndexSource(string SchemaUId, string IndexUId, string IndexName, bool IsUnique, bool IsAutoName, IReadOnlyList<IndexMemberSource> Members);
    private sealed record SchemaSource(string SchemaName, string SchemaUId, string? GetSchemaSchemaIdCandidate, string? ParentSchemaName,
        string? ParentSchemaUId, string ActualPackageName, string ActualPackageUId, IReadOnlyList<ColumnSource> Columns, IReadOnlyList<IndexSource> Indexes);
    private sealed record QueryRow(string RecordId, Dictionary<string, JsonElement> Values);
    private sealed record PageEvidence(int PageNumber, int Offset, int RowCount, string? FirstRecordId, string? LastRecordId);
    private sealed record OrderedPass(string SchemaName, int PageSize, int Direction, IReadOnlyList<QueryRow> Rows,
        IReadOnlyList<PageEvidence> Pages, int RowCount, bool TerminalEmpty);
    private sealed record OrderedProof(string SchemaName, int PageSize, int RowCount, string AscendingDigest, string RepeatedAscendingDigest,
        string DescendingDigest, bool TerminalEmptyPage, IReadOnlyList<PageEvidence> AscendingPages,
        IReadOnlyList<PageEvidence> RepeatedAscendingPages, IReadOnlyList<PageEvidence> DescendingPages);
    private sealed record LookupRegistrySource(string SchemaName, string SysEntitySchemaUId, string LookupRecordId, string? BaseSchemaName, string? BaseSchemaUId);
    private sealed record LookupRowSource(string SchemaName, string SysEntitySchemaUId, string RecordId, string SourceFingerprint);
    private sealed record LookupValueSource(string SchemaName, string RecordId, string ColumnName, string ValueState, string? Value,
        string ValueKind, string? CanonicalValue, string SourceFingerprint);
    private sealed record BaselineSource(string ContractVersion, string PairId, string PullRunId, string TargetAlias, string ScopeMode,
        DateTimeOffset PullStartedUtc, DateTimeOffset PullCompletedUtc, AssemblyEvidence Assemblies,
        IReadOnlyList<WorkspaceItemSource> WorkspaceItems, IReadOnlyList<SchemaSource> Schemas, LookupRegistrySource LookupRegistry,
        IReadOnlyList<LookupRowSource> LookupRows, IReadOnlyList<LookupValueSource> LookupValues, IReadOnlyList<OrderedProof> OrderedProofs);
    private sealed record ActualIndexedMatrixRow(string SchemaUId, string SchemaName, string PackageName, string ColumnName, string ColumnUId,
        bool ActualIndexed, int IndexMembershipCount, string Assessment);
    private sealed record ActualIndexedCheck(string Status, int SourceActualIndexedTrueCount, int SourceMemberCount,
        ActualIndexedMatrixRow[] Matrix, string Rule, string FutureIndexLoadGate);
    private sealed record FileManifest(string Algorithm, FileManifestEntry[] Files);
    private sealed record FileManifestEntry(string Path, long Bytes, string Sha256);
    private sealed record TableData(string Name, string[] Headers, object?[][] Rows,
        Func<object?[], int[]> EditableColumns, bool ReadOnly, bool Hidden = false);
    private sealed record ParsedTable(string[] Headers, object?[][] Rows);
    private sealed record WorkbookSummary(string Status, int SheetCount, int DataRowCount, string Sha256,
        string CanonicalWorksheetHash, bool HasFormulas, int ForbiddenPartCount);
    private sealed record WorkbookVerification(string Path, Dictionary<string, ParsedTable> Tables, WorkbookSummary Summary);
}
