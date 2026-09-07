using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

const string DefaultTarget = "http://localhost:8002";
const string SelectQueryPath = "DataService/json/SyncReply/SelectQuery";
const int AscendingOrder = 1;
const int DescendingOrder = 2;
const int MaximumPageCount = 1_000;
const string ExpectedAssemblyVersion = "1.8.0.14107";
const string BpmSoftCommonAssemblyPath =
    @"C:\Creatio\BpmSoftStandSetup\Constructor_1.8.0.14107_Net8_PostgreSQL\BPMSoft.Common.dll";
const string BpmSoftCommonAssemblySha256 =
    "42bed5e551e01e63c35d5439ed71b6335b34feec740185f6a09af8a11228ae20";
const string BpmSoftNuiServiceModelAssemblyPath =
    @"C:\Creatio\BpmSoftStandSetup\Constructor_1.8.0.14107_Net8_PostgreSQL\BPMSoft.Nui.ServiceModel.dll";
const string BpmSoftNuiServiceModelAssemblySha256 =
    "3c147aaa90d16a6b4d3fbb3b7773876a741ca2e73f41bb690997c241e057d948";

Console.WriteLine("BPMSoft read-only contract probe");
Console.WriteLine("Этот прототип вызывает только login/read endpoints, не вызывает write API и не создаёт Excel.");

ProbeMode probeMode;
try
{
    probeMode = ParseProbeMode(args);
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Mode selection failed before login: {exception.Message}");
    Environment.ExitCode = 1;
    return;
}

AssemblyContractEvidence assemblyContract;
try
{
    assemblyContract = VerifyAssemblyContract();
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Assembly contract check failed before login: {exception.Message}");
    Environment.ExitCode = 1;
    return;
}

var targetUri = ReadLocalTarget(ReadValue("URL стенда", DefaultTarget));
var login = ReadValue("Логин", "Supervisor");
var password = ReadPassword();

try
{
    var cookies = new CookieContainer();
    using var handler = new HttpClientHandler { CookieContainer = cookies, UseCookies = true };
    using var client = new HttpClient(handler) { BaseAddress = targetUri };
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    var timeZoneOffset = -(int)TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.Now).TotalMinutes;
    using var loginDocument = await PostJsonAsync(
        client,
        "ServiceModel/AuthService.svc/Login",
        new { UserName = login, UserPassword = password, TimeZoneOffset = timeZoneOffset });
    password = string.Empty;

    if (!IsSuccessfulLogin(loginDocument.RootElement))
    {
        throw new InvalidOperationException("Стенд не подтвердил вход. Пароль не сохранён.");
    }

    var csrfCookie = cookies.GetCookies(targetUri)["BPMCSRF"];
    if (csrfCookie is null || string.IsNullOrWhiteSpace(csrfCookie.Value))
    {
        throw new InvalidOperationException(
            "Login подтверждён, но session CSRF-cookie отсутствует. Ответ login и session values не сохранены.");
    }

    var csrfValue = csrfCookie.Value;
    client.DefaultRequestHeaders.Add("BPMCSRF", csrfValue);

    if (probeMode == ProbeMode.IndexProbe)
    {
        var confirmed = await RunIndexProbeAsync(
            client,
            targetUri,
            assemblyContract);
        client.DefaultRequestHeaders.Remove("BPMCSRF");
        csrfValue = string.Empty;
        if (!confirmed)
        {
            Environment.ExitCode = 1;
        }

        return;
    }

    using var workspaceDocument = await PostEmptyJsonAsync(
        client, "ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems");
    EnsureSuccessfulEnvelope(workspaceDocument.RootElement, "GetWorkspaceItems");

    var activityPriority = FindUniqueWorkspaceSchema(
        workspaceDocument.RootElement, "ActivityPriority", "Base");
    var lookupRegistry = FindUniqueWorkspaceSchema(
        workspaceDocument.RootElement, "Lookup", "Base");
    var accountBase = FindUniqueWorkspaceSchema(
        workspaceDocument.RootElement, "Account", "Base");
    var accountExtension = FindDeterministicExtensionSchema(
        workspaceDocument.RootElement, "Account", "Base");

    using var activityPrioritySchemaDocument = await ReadSchemaAsync(client, activityPriority);
    using var lookupSchemaDocument = await ReadSchemaAsync(client, lookupRegistry);
    using var accountBaseSchemaDocument = await ReadSchemaAsync(client, accountBase);
    using var accountExtensionSchemaDocument = await ReadSchemaAsync(client, accountExtension);

    var activityPriorityMapping = CreateSchemaMapping(
        activityPriority, activityPrioritySchemaDocument.RootElement);
    var lookupMapping = CreateSchemaMapping(
        lookupRegistry, lookupSchemaDocument.RootElement);
    var accountBaseMapping = CreateSchemaMapping(
        accountBase, accountBaseSchemaDocument.RootElement);
    var accountExtensionMapping = CreateSchemaMapping(
        accountExtension, accountExtensionSchemaDocument.RootElement);

    var referenceMappingCount = new[]
        {
            activityPriorityMapping,
            lookupMapping,
            accountBaseMapping,
            accountExtensionMapping
        }
        .SelectMany(mapping => mapping.Columns)
        .Count(column => column.ReferenceSchemaUId is not null);
    if (referenceMappingCount == 0)
    {
        throw new InvalidOperationException(
            "Bounded schema sample не содержит ни одной доказуемой referenceSchema.uId mapping.");
    }

    var requestContract = CreateRequestContract(assemblyContract);

    var activityAscPass1 = await ReadOrderedPassAsync(
        client, "ActivityPriority", pageSize: 2, AscendingOrder, captureRegistryIdentity: false);
    var activityAscPass2 = await ReadOrderedPassAsync(
        client, "ActivityPriority", pageSize: 2, AscendingOrder, captureRegistryIdentity: false);
    var activityDescControl = await ReadOrderedPassAsync(
        client, "ActivityPriority", pageSize: 2, DescendingOrder, captureRegistryIdentity: false);
    PaginationProofEvidence activityProof;
    try
    {
        activityProof = VerifyOrderedProof(
            "ActivityPriority", activityAscPass1, activityAscPass2, activityDescControl);
    }
    catch (InvalidOperationException exception)
        when (exception.Message.StartsWith("ActivityPriority:", StringComparison.Ordinal))
    {
        client.DefaultRequestHeaders.Remove("BPMCSRF");
        csrfValue = string.Empty;

        var failedUtc = DateTimeOffset.UtcNow;
        var failedArtefacts = CreateFailedActivityOrderingArtefacts(
            requestContract,
            failedUtc,
            targetUri.GetLeftPart(UriPartial.Authority),
            activityAscPass1,
            activityAscPass2,
            activityDescControl,
            exception.Message);
        var failedOutputDirectory = await WriteSafeArtefactsAsync(failedArtefacts, failedUtc);

        Console.Error.WriteLine($"Проверка не завершена: {exception.Message}");
        Console.Error.WriteLine($"Safe NOT_CONFIRMED diagnostics: {failedOutputDirectory}");
        Environment.ExitCode = 1;
        return;
    }

    var lookupAscPass1 = await ReadOrderedPassAsync(
        client, "Lookup", pageSize: 50, AscendingOrder, captureRegistryIdentity: true);
    var lookupAscPass2 = await ReadOrderedPassAsync(
        client, "Lookup", pageSize: 50, AscendingOrder, captureRegistryIdentity: true);
    var lookupDescControl = await ReadOrderedPassAsync(
        client, "Lookup", pageSize: 50, DescendingOrder, captureRegistryIdentity: true);
    var lookupProof = VerifyOrderedProof(
        "Lookup", lookupAscPass1, lookupAscPass2, lookupDescControl);

    var registryMapping = CreateRegistryMapping(
        activityPriorityMapping,
        lookupAscPass1.RegistryIdentities,
        lookupAscPass2.RegistryIdentities,
        lookupDescControl.RegistryIdentities,
        activityProof.AscendingRecordIds);

    client.DefaultRequestHeaders.Remove("BPMCSRF");
    csrfValue = string.Empty;

    var pulledUtc = DateTimeOffset.UtcNow;

    var summary = new
    {
        Target = targetUri.GetLeftPart(UriPartial.Authority),
        PulledUtc = pulledUtc,
        WorkspaceItemCount = CountArray(workspaceDocument.RootElement, "items"),
        SelectedSchemas = new[] { activityPriority, lookupRegistry, accountBase, accountExtension },
        Pagination = new[]
        {
            new { activityProof.SchemaName, activityProof.RecordCount, activityProof.PageSize, activityProof.PassDigestsMatch, activityProof.DescendingIsExactReverse },
            new { lookupProof.SchemaName, lookupProof.RecordCount, lookupProof.PageSize, lookupProof.PassDigestsMatch, lookupProof.DescendingIsExactReverse }
        },
        Mapping = new
        {
            SchemaUId = "GetSchema.schema.uId",
            SysSchemaIdCandidate = "GetSchema.schema.id",
            ColumnUId = "GetSchema.schema.columns[].uId / inheritedColumns[].uId",
            ReferenceSchemaUId = "GetSchema column.referenceSchema.uId",
            LookupRecordId = "SelectQuery Lookup row.Id",
            SysEntitySchemaUId = "SelectQuery Lookup row.SysEntitySchemaUId",
            RecordId = "SelectQuery lookup-data row.Id"
        },
        Safety = "Only login/read responses were used. Raw rows, captions, descriptions, source code, session values and login response were not persisted."
    };

    var runResult = new
    {
        Status = "CONFIRMED",
        Reason = (string?)null,
        Target = targetUri.GetLeftPart(UriPartial.Authority),
        CompletedUtc = pulledUtc,
        ActivityPriority = CreateDiagnosticPassSummary(
            activityAscPass1, activityAscPass2, activityDescControl),
        Lookup = CreateDiagnosticPassSummary(
            lookupAscPass1, lookupAscPass2, lookupDescControl),
        Completed = new[]
        {
            "ActivityPriority two ascending passes",
            "ActivityPriority descending reverse control",
            "Lookup two ascending passes",
            "Lookup descending reverse control",
            "bounded schema and lookup-registry mapping"
        },
        NotRun = new[]
        {
            "full-catalog pull",
            "write/manage operations",
            "Excel generation"
        }
    };

    var artefacts = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["ActivityPriority.asc-pass-1.ids.txt"] = FormatIdFile(activityAscPass1.RecordIds),
        ["ActivityPriority.asc-pass-2.ids.txt"] = FormatIdFile(activityAscPass2.RecordIds),
        ["ActivityPriority.desc-control.ids.txt"] = FormatIdFile(activityDescControl.RecordIds),
        ["Lookup.asc-pass-1.ids.txt"] = FormatIdFile(lookupAscPass1.RecordIds),
        ["Lookup.asc-pass-2.ids.txt"] = FormatIdFile(lookupAscPass2.RecordIds),
        ["Lookup.desc-control.ids.txt"] = FormatIdFile(lookupDescControl.RecordIds),
        ["request-contract.json"] = SerializeJson(requestContract),
        ["run-result.json"] = SerializeJson(runResult),
        ["summary.json"] = SerializeJson(summary),
        ["pagination-ActivityPriority-proof.json"] = SerializeJson(activityProof),
        ["pagination-Lookup-proof.json"] = SerializeJson(lookupProof),
        ["schema-ActivityPriority.mapping.json"] = SerializeJson(activityPriorityMapping),
        ["schema-Lookup.mapping.json"] = SerializeJson(lookupMapping),
        ["schema-Account-Base.mapping.json"] = SerializeJson(accountBaseMapping),
        ["schema-Account-extension.mapping.json"] = SerializeJson(accountExtensionMapping),
        ["lookup-registry-ActivityPriority.mapping.json"] = SerializeJson(registryMapping),
        ["schema-ActivityPriority.response-shape.json"] = SerializeJson(CreateResponseShape(activityPrioritySchemaDocument.RootElement)),
        ["schema-Lookup.response-shape.json"] = SerializeJson(CreateResponseShape(lookupSchemaDocument.RootElement)),
        ["schema-Account-Base.response-shape.json"] = SerializeJson(CreateResponseShape(accountBaseSchemaDocument.RootElement)),
        ["schema-Account-extension.response-shape.json"] = SerializeJson(CreateResponseShape(accountExtensionSchemaDocument.RootElement)),
        ["lookup-ActivityPriority-ordered.response-shape.json"] = SerializeJson(activityAscPass1.FirstPageShape),
        ["lookup-Lookup-ordered.response-shape.json"] = SerializeJson(lookupAscPass1.FirstPageShape)
    };

    var outputDirectory = await WriteSafeArtefactsAsync(artefacts, pulledUtc);

    Console.WriteLine($"Read-only evidence сохранён: {outputDirectory}");
    Console.WriteLine(
        $"ActivityPriority: {activityProof.RecordCount} records; Lookup: {lookupProof.RecordCount} records; " +
        "two-pass/reverse controls passed.");
    Console.WriteLine("Raw lookup values, login response и session values не сохранялись; Excel не создавался.");
}
catch (Exception exception)
{
    password = string.Empty;
    Console.Error.WriteLine($"Проверка не завершена: {exception.Message}");
    Environment.ExitCode = 1;
}

static ProbeMode ParseProbeMode(string[] arguments)
{
    if (arguments.Length == 0)
    {
        return ProbeMode.Ordering;
    }

    if (arguments.Length == 1 &&
        string.Equals(arguments[0], "--index-probe", StringComparison.Ordinal))
    {
        return ProbeMode.IndexProbe;
    }

    throw new InvalidOperationException(
        "Разрешён только отдельный режим --index-probe либо default ordering mode без arguments.");
}

static async Task<bool> RunIndexProbeAsync(
    HttpClient client,
    Uri targetUri,
    AssemblyContractEvidence assemblyContract)
{
    const string schemaName = "Account";
    const string packageName = "Test1";

    using var workspaceDocument = await PostEmptyJsonAsync(
        client, "ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems");
    EnsureSuccessfulEnvelope(workspaceDocument.RootElement, "GetWorkspaceItems index probe");
    var selectedSchema = FindUniqueWorkspaceSchema(
        workspaceDocument.RootElement, schemaName, packageName);

    using var schemaDocument = await ReadSchemaAsync(client, selectedSchema);
    EnsureSuccessfulEnvelope(schemaDocument.RootElement, "GetSchema Account/Test1 index probe");
    if (!schemaDocument.RootElement.TryGetProperty("schema", out var schema) ||
        schema.ValueKind != JsonValueKind.Object)
    {
        throw new InvalidOperationException("GetSchema Account/Test1 не вернул object schema.");
    }

    var returnedName = GetRequiredString(schema, "name", "GetSchema.schema");
    var returnedUId = GetRequiredGuid(schema, "uId", "GetSchema.schema Account/Test1");
    if (!string.Equals(returnedName, schemaName, StringComparison.Ordinal) ||
        !string.Equals(returnedUId, selectedSchema.UId, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            "GetSchema Account/Test1 name/uId не совпали с exact workspace selection.");
    }

    var fullResponseShape = CreateStructuralLedger(
        schemaDocument.RootElement,
        string.Empty,
        includeSafeScalarValues: false);
    var analysis = AnalyzeIndexProbeSchema(schema);
    var completedUtc = DateTimeOffset.UtcNow;
    var requestContract = CreateIndexProbeRequestContract(assemblyContract);
    var workspaceSelection = new
    {
        MatchCount = 1,
        Type = 3,
        selectedSchema.Name,
        selectedSchema.PackageName,
        WorkspaceItemUId = selectedSchema.UId,
        SourcePaths = new
        {
            Type = "items[].type",
            Name = "items[].name",
            PackageName = "items[].packageName",
            WorkspaceItemUId = "items[].uId"
        }
    };
    var runResult = new
    {
        Status = analysis.Confirmed ? "CONFIRMED" : "NOT_CONFIRMED",
        Reason = analysis.Reason,
        Target = targetUri.GetLeftPart(UriPartial.Authority),
        CompletedUtc = completedUtc,
        Completed = new[]
        {
            "assembly preflight before login",
            "one GetWorkspaceItems read",
            "exact Account/Test1/type=3 selection",
            "one GetSchema read",
            "redacted full response shape",
            "bounded own-column and index candidate analysis"
        },
        NotRun = new[]
        {
            "SelectQuery and ordering flow",
            "full-catalog schema reads",
            "write/manage/compile/save/create/update/delete operations",
            "Excel generation",
            "alternative payload or automatic retry"
        }
    };

    var artefacts = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["index-probe-request-contract.json"] = SerializeJson(requestContract),
        ["workspace-selection-Account-Test1.mapping.json"] = SerializeJson(workspaceSelection),
        ["schema-Account-Test1.response-shape.json"] = SerializeJson(new
        {
            RootPath = "/",
            ScalarValuesPersisted = false,
            fullResponseShape.Nodes
        }),
        ["schema-Account-Test1.own-columns.mapping.json"] = SerializeJson(new
        {
            SchemaName = schemaName,
            SchemaUId = returnedUId,
            analysis.GetSchemaSchemaIdCandidate,
            Columns = analysis.OwnColumns,
            SourcePaths = new
            {
                OwnColumns = "schema.columns[]",
                InheritedColumns = "schema.inheritedColumns[]",
                GetSchemaSchemaIdCandidate = "schema.id"
            }
        }),
        ["schema-Account-Test1.indexes.shape.json"] = SerializeJson(new
        {
            RootPath = "/schema/indexes",
            analysis.IndexStructure.Nodes,
            analysis.IndexStructure.UnsafeScalarPaths
        }),
        ["schema-Account-Test1.index-candidates.mapping.json"] = SerializeJson(new
        {
            ExpectedSource = "owner-provided BPMSoft UI observation",
            Expected = new[]
            {
                new { ColumnName = "Code", Unique = true },
                new { ColumnName = "Name", Unique = false }
            },
            analysis.Candidates,
            analysis.ValidationErrors,
            Confirmed = analysis.Confirmed,
            SourcePaths = new
            {
                Indexes = "schema.indexes[]",
                OwnColumns = "schema.columns[]",
                CandidateRule = "exact technical name/UId leaf matches plus direct one-item columns array",
                UniqueRule = "shared exact relative boolean path named isUnique or unique"
            }
        }),
        ["run-result.json"] = SerializeJson(runResult)
    };

    var outputDirectory = await WriteIndexProbeArtefactsAsync(artefacts, completedUtc);
    if (analysis.Confirmed)
    {
        Console.WriteLine($"Index-probe evidence сохранён: {outputDirectory}");
        Console.WriteLine("Account/Test1 Code unique + Name non-unique mapping confirmed against owner oracle.");
    }
    else
    {
        Console.Error.WriteLine($"Index probe NOT_CONFIRMED: {analysis.Reason}");
        Console.Error.WriteLine($"Safe bounded diagnostics: {outputDirectory}");
    }

    return analysis.Confirmed;
}

static object CreateIndexProbeRequestContract(AssemblyContractEvidence assemblyContract)
{
    return new
    {
        Mode = "--index-probe",
        AssemblyContract = assemblyContract,
        Authentication = new
        {
            Endpoint = "/ServiceModel/AuthService.svc/Login",
            Input = "interactive-only; response and session material are not persisted"
        },
        Reads = new object[]
        {
            new
            {
                Endpoint = "/ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems",
                Body = "zero-length UTF-8 application/json body",
                Count = 1,
                Selection = new { Type = 3, Name = "Account", PackageName = "Test1", MatchCount = 1 }
            },
            new
            {
                Endpoint = "/ServiceModel/EntitySchemaDesignerService.svc/GetSchema",
                Payload = new { schemaUId = "<exact selected workspace item uId>" },
                Count = 1
            }
        },
        OwnerOracle = new[]
        {
            new { ColumnName = "Code", Unique = true, Ownership = "Own", SimpleColumnCount = 1 },
            new { ColumnName = "Name", Unique = false, Ownership = "Own", SimpleColumnCount = 1 }
        },
        CandidateSemantics = new
        {
            GetSchemaSchemaIdCandidate = "schema.id",
            IndexFields = "candidate until exact response paths and owner oracle agree"
        },
        TerminologyOnlyReference = new
        {
            Path = "docs/REFERENCES/creatio-terrasoft-manual-section-registration/README.md",
            Use = "Id versus UId terminology only; historical SQL is not executed and is not BPMSoft 1.8 API proof"
        },
        Artefacts = IndexProbeExpectedFileNames(),
        ExplicitlyNotRun = new[]
        {
            "SelectQuery",
            "full-catalog schema reads",
            "write/manage/compile/save/create/update/delete",
            "Excel"
        }
    };
}

static IndexProbeAnalysis AnalyzeIndexProbeSchema(JsonElement schema)
{
    var validationErrors = new List<string>();
    string? schemaIdCandidate = null;
    if (schema.TryGetProperty("id", out var schemaId) &&
        schemaId.ValueKind == JsonValueKind.String &&
        Guid.TryParse(schemaId.GetString(), out var parsedSchemaId))
    {
        schemaIdCandidate = parsedSchemaId.ToString("D");
    }
    else
    {
        validationErrors.Add("GET_SCHEMA_SCHEMA_ID_CANDIDATE_MISSING_OR_INVALID");
    }

    var ownColumns = new[]
    {
        FindIndexProbeOwnColumn(schema, "Code"),
        FindIndexProbeOwnColumn(schema, "Name")
    };
    foreach (var column in ownColumns)
    {
        if (column.OwnMatchCount != 1)
        {
            validationErrors.Add($"OWN_COLUMN_{column.ColumnName.ToUpperInvariant()}_MATCH_COUNT_NOT_ONE");
        }
        if (column.InheritedMatchCount != 0)
        {
            validationErrors.Add($"OWN_COLUMN_{column.ColumnName.ToUpperInvariant()}_ALSO_INHERITED");
        }
        if (column.ColumnUId is null)
        {
            validationErrors.Add($"OWN_COLUMN_{column.ColumnName.ToUpperInvariant()}_UID_MISSING_OR_INVALID");
        }
        if (column.Indexed != true)
        {
            validationErrors.Add($"OWN_COLUMN_{column.ColumnName.ToUpperInvariant()}_INDEXED_NOT_TRUE");
        }
    }

    StructuralLedgerResult indexStructure;
    var candidates = new List<IndexCandidateEvidence>();
    if (!schema.TryGetProperty("indexes", out var indexes) ||
        indexes.ValueKind != JsonValueKind.Array ||
        indexes.GetArrayLength() == 0)
    {
        validationErrors.Add("SCHEMA_INDEXES_MISSING_EMPTY_OR_NOT_ARRAY");
        indexStructure = new StructuralLedgerResult(
            Array.Empty<StructuralNodeEvidence>(),
            Array.Empty<string>());
    }
    else
    {
        indexStructure = CreateStructuralLedger(
            indexes,
            "/schema/indexes",
            includeSafeScalarValues: true);
        if (indexStructure.UnsafeScalarPaths.Count != 0)
        {
            validationErrors.Add("INDEX_STRUCTURE_CONTAINS_UNSAFE_OR_DATA_LIKE_SCALARS");
        }

        for (var indexOrdinal = 0; indexOrdinal < indexes.GetArrayLength(); indexOrdinal++)
        {
            candidates.Add(CreateIndexCandidate(
                indexes[indexOrdinal],
                indexOrdinal,
                indexStructure.Nodes,
                ownColumns));
        }
    }

    var codeMatches = candidates
        .Where(candidate => candidate.MatchedColumnNames.Contains("Code", StringComparer.Ordinal))
        .ToArray();
    var nameMatches = candidates
        .Where(candidate => candidate.MatchedColumnNames.Contains("Name", StringComparer.Ordinal))
        .ToArray();
    if (codeMatches.Length != 1)
    {
        validationErrors.Add("CODE_INDEX_CANDIDATE_COUNT_NOT_ONE");
    }
    if (nameMatches.Length != 1)
    {
        validationErrors.Add("NAME_INDEX_CANDIDATE_COUNT_NOT_ONE");
    }

    if (codeMatches.Length == 1 && nameMatches.Length == 1)
    {
        var code = codeMatches[0];
        var name = nameMatches[0];
        if (code.IndexOrdinal == name.IndexOrdinal)
        {
            validationErrors.Add("CODE_AND_NAME_MAP_TO_SAME_INDEX");
        }
        if (!code.IsUnambiguousSimpleOneColumn || !name.IsUnambiguousSimpleOneColumn)
        {
            validationErrors.Add("EXPECTED_INDEX_NOT_UNAMBIGUOUS_SIMPLE_ONE_COLUMN");
        }
        if (code.UniqueFlagCandidate != true || name.UniqueFlagCandidate != false)
        {
            validationErrors.Add("OWNER_UNIQUENESS_ORACLE_MISMATCH");
        }
        if (code.UniqueFlagRelativePath is null ||
            !string.Equals(code.UniqueFlagRelativePath, name.UniqueFlagRelativePath, StringComparison.Ordinal))
        {
            validationErrors.Add("UNIQUENESS_PROPERTY_PATH_NOT_SHARED_AND_EXACT");
        }
    }

    var distinctErrors = validationErrors.Distinct(StringComparer.Ordinal).ToArray();
    var confirmed = distinctErrors.Length == 0;
    return new IndexProbeAnalysis(
        schemaIdCandidate,
        ownColumns,
        indexStructure,
        candidates,
        distinctErrors,
        confirmed,
        confirmed
            ? "Exact response paths match owner oracle for Code unique and Name non-unique."
            : string.Join(";", distinctErrors));
}

static IndexProbeOwnColumnEvidence FindIndexProbeOwnColumn(JsonElement schema, string expectedName)
{
    var ownMatches = FindColumnLocations(schema, "columns", expectedName);
    var inheritedMatches = FindColumnLocations(schema, "inheritedColumns", expectedName);
    if (ownMatches.Count != 1)
    {
        return new IndexProbeOwnColumnEvidence(
            expectedName,
            null,
            "Own",
            null,
            ownMatches.Count,
            inheritedMatches.Count,
            null,
            null,
            null);
    }

    var match = ownMatches[0];
    string? columnUId = null;
    if (match.Element.TryGetProperty("uId", out var uId) &&
        uId.ValueKind == JsonValueKind.String &&
        Guid.TryParse(uId.GetString(), out var parsedUId))
    {
        columnUId = parsedUId.ToString("D");
    }

    bool? indexed = null;
    if (match.Element.TryGetProperty("indexed", out var indexedElement) &&
        indexedElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
    {
        indexed = indexedElement.GetBoolean();
    }

    return new IndexProbeOwnColumnEvidence(
        expectedName,
        columnUId,
        "Own",
        indexed,
        ownMatches.Count,
        inheritedMatches.Count,
        match.Path,
        match.Path + "/uId",
        match.Path + "/indexed");
}

static List<ColumnLocation> FindColumnLocations(
    JsonElement schema,
    string collectionName,
    string expectedName)
{
    var matches = new List<ColumnLocation>();
    if (!schema.TryGetProperty(collectionName, out var columns) ||
        columns.ValueKind != JsonValueKind.Array)
    {
        return matches;
    }

    for (var ordinal = 0; ordinal < columns.GetArrayLength(); ordinal++)
    {
        var column = columns[ordinal];
        if (column.ValueKind == JsonValueKind.Object &&
            column.TryGetProperty("name", out var name) &&
            name.ValueKind == JsonValueKind.String &&
            string.Equals(name.GetString(), expectedName, StringComparison.Ordinal))
        {
            matches.Add(new ColumnLocation(
                column,
                $"/schema/{EscapeJsonPointerToken(collectionName)}/{ordinal}"));
        }
    }

    return matches;
}

static IndexCandidateEvidence CreateIndexCandidate(
    JsonElement index,
    int ordinal,
    IReadOnlyList<StructuralNodeEvidence> structuralNodes,
    IReadOnlyList<IndexProbeOwnColumnEvidence> ownColumns)
{
    var basePath = $"/schema/indexes/{ordinal}";
    var issues = new List<string>();
    if (index.ValueKind != JsonValueKind.Object)
    {
        return new IndexCandidateEvidence(
            ordinal,
            null,
            null,
            Array.Empty<string>(),
            Array.Empty<IndexColumnMatchEvidence>(),
            null,
            null,
            null,
            false,
            new[] { "INDEX_ITEM_NOT_OBJECT" },
            null,
            null);
    }

    string? indexNameCandidate = null;
    string? indexNamePath = null;
    if (index.TryGetProperty("name", out var indexName) &&
        indexName.ValueKind == JsonValueKind.String &&
        IsSafeTechnicalIdentifier(indexName.GetString()))
    {
        indexNameCandidate = indexName.GetString();
        indexNamePath = basePath + "/name";
    }
    else
    {
        issues.Add("INDEX_NAME_CANDIDATE_MISSING_OR_UNSAFE");
    }

    var matches = structuralNodes
        .Where(node => node.Path.StartsWith(basePath + "/", StringComparison.Ordinal) &&
            node.SafeValue is not null)
        .SelectMany(node => ownColumns
            .Where(column =>
                string.Equals(node.SafeValue, column.ColumnName, StringComparison.Ordinal) ||
                (column.ColumnUId is not null &&
                 string.Equals(node.SafeValue, column.ColumnUId, StringComparison.OrdinalIgnoreCase)))
            .Select(column => new IndexColumnMatchEvidence(
                column.ColumnName,
                column.ColumnUId,
                node.Path,
                node.SafeValueClass ?? "technical")))
        .Distinct()
        .ToArray();
    var matchedColumnNames = matches
        .Select(match => match.ColumnName)
        .Distinct(StringComparer.Ordinal)
        .ToArray();
    if (matchedColumnNames.Length != 1)
    {
        issues.Add("INDEX_COLUMN_RELATION_NOT_UNIQUE");
    }

    string? memberArrayPath = null;
    int? memberCount = null;
    if (index.TryGetProperty("columns", out var members) &&
        members.ValueKind == JsonValueKind.Array)
    {
        memberArrayPath = basePath + "/columns";
        memberCount = members.GetArrayLength();
    }
    else
    {
        issues.Add("DIRECT_COLUMNS_ARRAY_MISSING_OR_NOT_ARRAY");
    }
    if (memberCount != 1)
    {
        issues.Add("INDEX_MEMBER_COUNT_NOT_ONE");
    }
    if (memberArrayPath is not null &&
        matches.Any(match => !match.Path.StartsWith(memberArrayPath + "/0/", StringComparison.Ordinal)))
    {
        issues.Add("COLUMN_MATCH_OUTSIDE_SINGLE_MEMBER");
    }

    var uniqueNodes = structuralNodes
        .Where(node =>
            node.Path.StartsWith(basePath + "/", StringComparison.Ordinal) &&
            string.Equals(node.Kind, "boolean", StringComparison.Ordinal) &&
            (string.Equals(node.PropertyName, "isUnique", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(node.PropertyName, "unique", StringComparison.OrdinalIgnoreCase)))
        .ToArray();
    bool? uniqueFlag = null;
    string? uniquePath = null;
    string? uniqueRelativePath = null;
    if (uniqueNodes.Length == 1 && bool.TryParse(uniqueNodes[0].SafeValue, out var parsedUnique))
    {
        uniqueFlag = parsedUnique;
        uniquePath = uniqueNodes[0].Path;
        uniqueRelativePath = uniquePath[basePath.Length..];
    }
    else
    {
        issues.Add("CLEAR_UNIQUENESS_BOOLEAN_COUNT_NOT_ONE");
    }

    var isSimple = issues.Count == 0;
    return new IndexCandidateEvidence(
        ordinal,
        indexNameCandidate,
        indexNamePath,
        matchedColumnNames,
        matches,
        memberArrayPath,
        memberCount,
        uniqueFlag,
        isSimple,
        issues,
        uniquePath,
        uniqueRelativePath);
}

static StructuralLedgerResult CreateStructuralLedger(
    JsonElement element,
    string rootPath,
    bool includeSafeScalarValues)
{
    var nodes = new List<StructuralNodeEvidence>();
    var unsafeScalarPaths = new List<string>();
    AppendStructuralNodes(
        element,
        rootPath,
        propertyName: null,
        includeSafeScalarValues,
        prohibitedValueContext: false,
        nodes,
        unsafeScalarPaths);
    return new StructuralLedgerResult(nodes, unsafeScalarPaths);
}

static void AppendStructuralNodes(
    JsonElement element,
    string path,
    string? propertyName,
    bool includeSafeScalarValues,
    bool prohibitedValueContext,
    List<StructuralNodeEvidence> nodes,
    List<string> unsafeScalarPaths)
{
    var kind = GetStructuralKind(element.ValueKind);
    int? arrayLength = element.ValueKind == JsonValueKind.Array
        ? element.GetArrayLength()
        : null;
    string? safeValue = null;
    string? safeValueClass = null;
    var valueOmitted = false;

    if (element.ValueKind is not JsonValueKind.Object and not JsonValueKind.Array)
    {
        if (!includeSafeScalarValues)
        {
            valueOmitted = true;
        }
        else if (prohibitedValueContext)
        {
            valueOmitted = true;
            unsafeScalarPaths.Add(NormalizeRootPath(path));
        }
        else
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    var text = element.GetString();
                    if (Guid.TryParse(text, out var guid))
                    {
                        safeValue = guid.ToString("D");
                        safeValueClass = "Guid";
                    }
                    else if (IsSafeTechnicalIdentifier(text))
                    {
                        safeValue = text;
                        safeValueClass = "AsciiTechnicalIdentifier";
                    }
                    else
                    {
                        valueOmitted = true;
                        unsafeScalarPaths.Add(NormalizeRootPath(path));
                    }
                    break;
                case JsonValueKind.Number:
                    safeValue = element.GetRawText();
                    safeValueClass = "Number";
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    safeValue = element.GetBoolean() ? "true" : "false";
                    safeValueClass = "Boolean";
                    break;
                case JsonValueKind.Null:
                    safeValueClass = "Null";
                    break;
                default:
                    valueOmitted = true;
                    unsafeScalarPaths.Add(NormalizeRootPath(path));
                    break;
            }
        }
    }

    nodes.Add(new StructuralNodeEvidence(
        NormalizeRootPath(path),
        propertyName,
        kind,
        arrayLength,
        safeValue,
        safeValueClass,
        valueOmitted));

    if (element.ValueKind == JsonValueKind.Object)
    {
        foreach (var property in element.EnumerateObject())
        {
            AppendStructuralNodes(
                property.Value,
                path + "/" + EscapeJsonPointerToken(property.Name),
                property.Name,
                includeSafeScalarValues,
                prohibitedValueContext || IsProhibitedValueProperty(property.Name),
                nodes,
                unsafeScalarPaths);
        }
    }
    else if (element.ValueKind == JsonValueKind.Array)
    {
        var ordinal = 0;
        foreach (var item in element.EnumerateArray())
        {
            AppendStructuralNodes(
                item,
                path + "/" + ordinal,
                propertyName: null,
                includeSafeScalarValues,
                prohibitedValueContext,
                nodes,
                unsafeScalarPaths);
            ordinal++;
        }
    }
}

static bool IsSafeTechnicalIdentifier(string? value)
{
    return !string.IsNullOrEmpty(value) &&
        value.Length <= 128 &&
        System.Text.RegularExpressions.Regex.IsMatch(
            value,
            "^[A-Za-z_][A-Za-z0-9_.]*$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
}

static bool IsProhibitedValueProperty(string propertyName)
{
    var lowered = propertyName.ToLowerInvariant();
    return lowered.Contains("caption", StringComparison.Ordinal) ||
        lowered.Contains("description", StringComparison.Ordinal) ||
        lowered.Contains("display", StringComparison.Ordinal) ||
        lowered.Contains("localizable", StringComparison.Ordinal) ||
        lowered.Contains("sourcecode", StringComparison.Ordinal) ||
        string.Equals(lowered, "body", StringComparison.Ordinal) ||
        string.Equals(lowered, "value", StringComparison.Ordinal) ||
        lowered.Contains("password", StringComparison.Ordinal) ||
        lowered.Contains("cookie", StringComparison.Ordinal) ||
        lowered.Contains("authorization", StringComparison.Ordinal) ||
        lowered.Contains("csrf", StringComparison.Ordinal);
}

static string EscapeJsonPointerToken(string token)
{
    return token.Replace("~", "~0", StringComparison.Ordinal)
        .Replace("/", "~1", StringComparison.Ordinal);
}

static string NormalizeRootPath(string path)
{
    return string.IsNullOrEmpty(path) ? "/" : path;
}

static string GetStructuralKind(JsonValueKind kind)
{
    return kind switch
    {
        JsonValueKind.Object => "object",
        JsonValueKind.Array => "array",
        JsonValueKind.String => "string",
        JsonValueKind.Number => "number",
        JsonValueKind.True or JsonValueKind.False => "boolean",
        JsonValueKind.Null => "null",
        _ => kind.ToString()
    };
}

static async Task<string> WriteIndexProbeArtefactsAsync(
    IReadOnlyDictionary<string, string> artefacts,
    DateTimeOffset completedUtc)
{
    var expected = IndexProbeExpectedFileNames();
    var files = new Dictionary<string, string>(artefacts, StringComparer.Ordinal);
    var expectedWithoutGenerated = expected
        .Where(name => name is not "redaction-check.json" and not "artefact-manifest.sha256.json")
        .OrderBy(name => name, StringComparer.Ordinal)
        .ToArray();
    if (!files.Keys.OrderBy(name => name, StringComparer.Ordinal).SequenceEqual(
            expectedWithoutGenerated,
            StringComparer.Ordinal))
    {
        throw new InvalidOperationException("Index-probe prospective artefact set не равен approved seven-file input set.");
    }

    files.Add("redaction-check.json", SerializeJson(new
    {
        Passed = true,
        FilesScannedBeforeWrite = 9,
        ForbiddenMarkerHitCount = 0,
        FullWorkspaceResponsePersisted = false,
        FullSchemaResponsePersisted = false,
        CaptionDescriptionOrLocalizedValuesPersisted = false,
        LoginResponsePersisted = false,
        SessionMaterialPersisted = false,
        UnsafeIndexScalarValuesPersisted = false,
        ExactByteManifestIncluded = true
    }));

    var manifestEntries = files
        .OrderBy(file => file.Key, StringComparer.Ordinal)
        .Select(file => new
        {
            FileName = file.Key,
            ByteLength = Encoding.UTF8.GetByteCount(file.Value),
            Sha256 = ComputeTextDigest(file.Value)
        })
        .ToArray();
    files.Add("artefact-manifest.sha256.json", SerializeJson(new
    {
        Algorithm = "SHA-256",
        Encoding = "UTF-8 without BOM",
        SelfExcluded = true,
        Files = manifestEntries
    }));

    if (!files.Keys.OrderBy(name => name, StringComparer.Ordinal).SequenceEqual(
            expected.OrderBy(name => name, StringComparer.Ordinal),
            StringComparer.Ordinal))
    {
        throw new InvalidOperationException("Index-probe final artefact set не равен approved nine-file set.");
    }

    var forbiddenMarkers = new[]
    {
        "UserPassword",
        "Set-Cookie",
        "BPMCSRF",
        "Authorization",
        "Bearer "
    };
    var markerHits = files
        .SelectMany(file => forbiddenMarkers
            .Where(marker => file.Value.Contains(marker, StringComparison.OrdinalIgnoreCase))
            .Select(marker => file.Key + ":" + marker))
        .ToArray();
    if (markerHits.Length != 0)
    {
        throw new InvalidOperationException(
            $"Index-probe pre-write redaction scan found {markerHits.Length} forbidden marker hits; nothing was saved.");
    }

    var outputDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "probe-output",
        completedUtc.ToString("yyyyMMddTHHmmssZ"));
    if (Directory.Exists(outputDirectory))
    {
        throw new InvalidOperationException("Timestamped index-probe evidence directory already exists; overwrite is forbidden.");
    }

    Directory.CreateDirectory(outputDirectory);
    var utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    foreach (var file in files)
    {
        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, file.Key),
            file.Value,
            utf8WithoutBom);
    }

    return outputDirectory;
}

static string ComputeTextDigest(string value)
{
    return Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(value)))
        .ToLowerInvariant();
}

static string[] IndexProbeExpectedFileNames()
{
    return new[]
    {
        "index-probe-request-contract.json",
        "workspace-selection-Account-Test1.mapping.json",
        "schema-Account-Test1.response-shape.json",
        "schema-Account-Test1.own-columns.mapping.json",
        "schema-Account-Test1.indexes.shape.json",
        "schema-Account-Test1.index-candidates.mapping.json",
        "run-result.json",
        "redaction-check.json",
        "artefact-manifest.sha256.json"
    };
}

static AssemblyContractEvidence VerifyAssemblyContract()
{
    var common = VerifyAssembly(
        BpmSoftCommonAssemblyPath,
        ExpectedAssemblyVersion,
        BpmSoftCommonAssemblySha256);
    var nuiServiceModel = VerifyAssembly(
        BpmSoftNuiServiceModelAssemblyPath,
        ExpectedAssemblyVersion,
        BpmSoftNuiServiceModelAssemblySha256);
    return new AssemblyContractEvidence(common, nuiServiceModel, true);
}

static AssemblyFileEvidence VerifyAssembly(
    string path,
    string expectedVersion,
    string expectedSha256)
{
    if (!File.Exists(path))
    {
        throw new InvalidOperationException($"Required local assembly is missing: {path}");
    }

    var actualVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(path).FileVersion;
    if (!string.Equals(actualVersion, expectedVersion, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            $"Assembly version mismatch for {Path.GetFileName(path)}; requests were not started.");
    }

    using var stream = File.OpenRead(path);
    var actualSha256 = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(stream))
        .ToLowerInvariant();
    if (!string.Equals(actualSha256, expectedSha256, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            $"Assembly SHA-256 mismatch for {Path.GetFileName(path)}; requests were not started.");
    }

    return new AssemblyFileEvidence(path, actualVersion!, actualSha256);
}

static Uri ReadLocalTarget(string input)
{
    if (!Uri.TryCreate(input.Trim(), UriKind.Absolute, out var uri) ||
        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
        !uri.IsLoopback ||
        !string.IsNullOrEmpty(uri.UserInfo) ||
        uri.AbsolutePath != "/" ||
        !string.IsNullOrEmpty(uri.Query) ||
        !string.IsNullOrEmpty(uri.Fragment))
    {
        throw new InvalidOperationException(
            "Разрешён только local HTTP(S) target authority без credentials, path, query или fragment.");
    }

    return new Uri(uri.GetLeftPart(UriPartial.Authority).TrimEnd('/') + "/");
}

static string ReadValue(string prompt, string defaultValue)
{
    Console.Write(defaultValue.Length == 0 ? $"{prompt}: " : $"{prompt} [{defaultValue}]: ");
    var value = Console.ReadLine();
    return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
}

static string ReadPassword()
{
    Console.Write("Пароль: ");
    var characters = new StringBuilder();
    ConsoleKeyInfo key;
    while ((key = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
    {
        if (key.Key == ConsoleKey.Backspace && characters.Length > 0)
        {
            characters.Length--;
            continue;
        }

        if (!char.IsControl(key.KeyChar))
        {
            characters.Append(key.KeyChar);
        }
    }

    Console.WriteLine();
    return characters.ToString();
}

static async Task<JsonDocument> ReadSchemaAsync(HttpClient client, WorkspaceSchemaHandle schema)
{
    return await PostJsonAsync(
        client,
        "ServiceModel/EntitySchemaDesignerService.svc/GetSchema",
        new { schemaUId = schema.UId });
}

static async Task<JsonDocument> PostJsonAsync(HttpClient client, string relativePath, object payload)
{
    using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    using var response = await client.PostAsync(relativePath, content);
    EnsureSuccessfulResponse(response, relativePath);
    return JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
}

static async Task<JsonDocument> PostEmptyJsonAsync(HttpClient client, string relativePath)
{
    using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
    using var response = await client.PostAsync(relativePath, content);
    EnsureSuccessfulResponse(response, relativePath);
    return JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
}

static void EnsureSuccessfulResponse(HttpResponseMessage response, string relativePath)
{
    if (!response.IsSuccessStatusCode)
    {
        throw new HttpRequestException(
            $"Read-only запрос {relativePath} завершился HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).");
    }
}

static bool IsSuccessfulLogin(JsonElement root)
{
    return root.TryGetProperty("Code", out var code) &&
           code.ValueKind == JsonValueKind.Number &&
           code.GetInt32() == 0;
}

static void EnsureSuccessfulEnvelope(JsonElement root, string operation)
{
    if (!root.TryGetProperty("success", out var success) || success.ValueKind != JsonValueKind.True)
    {
        throw new InvalidOperationException($"{operation} не вернул success=true.");
    }
}

static int CountArray(JsonElement root, string propertyName)
{
    return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Array
        ? value.GetArrayLength()
        : 0;
}

static WorkspaceSchemaHandle FindUniqueWorkspaceSchema(
    JsonElement workspaceResponse,
    string schemaName,
    string packageName)
{
    var matches = EnumerateWorkspaceSchemas(workspaceResponse)
        .Where(schema => schema.Name == schemaName && schema.PackageName == packageName)
        .ToArray();
    if (matches.Length != 1)
    {
        throw new InvalidOperationException(
            $"Ожидалась ровно одна object schema {schemaName}/{packageName}; найдено {matches.Length}.");
    }

    return matches[0];
}

static WorkspaceSchemaHandle FindDeterministicExtensionSchema(
    JsonElement workspaceResponse,
    string schemaName,
    string basePackageName)
{
    var matches = EnumerateWorkspaceSchemas(workspaceResponse)
        .Where(schema => schema.Name == schemaName && schema.PackageName != basePackageName)
        .OrderBy(schema => schema.PackageName, StringComparer.Ordinal)
        .ThenBy(schema => schema.UId, StringComparer.Ordinal)
        .ToArray();
    if (matches.Length == 0)
    {
        throw new InvalidOperationException(
            $"Для bounded extension sample не найдена non-{basePackageName} schema {schemaName}.");
    }

    return matches[0];
}

static IEnumerable<WorkspaceSchemaHandle> EnumerateWorkspaceSchemas(JsonElement workspaceResponse)
{
    if (!workspaceResponse.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
    {
        throw new InvalidOperationException("GetWorkspaceItems не вернул массив items.");
    }

    foreach (var item in items.EnumerateArray())
    {
        if (!item.TryGetProperty("type", out var type) ||
            type.ValueKind != JsonValueKind.Number ||
            type.GetInt32() != 3)
        {
            continue;
        }

        var name = GetRequiredString(item, "name", "workspace item");
        var packageName = GetRequiredString(item, "packageName", $"workspace item {name}");
        var uId = GetRequiredGuid(item, "uId", $"workspace item {name}/{packageName}");
        yield return new WorkspaceSchemaHandle(name, packageName, uId);
    }
}

static SchemaMappingEvidence CreateSchemaMapping(
    WorkspaceSchemaHandle requestedSchema,
    JsonElement response)
{
    EnsureSuccessfulEnvelope(response, $"GetSchema {requestedSchema.Name}/{requestedSchema.PackageName}");
    if (!response.TryGetProperty("schema", out var schema) || schema.ValueKind != JsonValueKind.Object)
    {
        throw new InvalidOperationException(
            $"GetSchema {requestedSchema.Name}/{requestedSchema.PackageName} не вернул object schema.");
    }

    var schemaName = GetRequiredString(schema, "name", "GetSchema.schema");
    var schemaUId = GetRequiredGuid(schema, "uId", $"GetSchema.schema {schemaName}");
    var sysSchemaIdCandidate = GetRequiredGuid(schema, "id", $"GetSchema.schema {schemaName}");
    if (!string.Equals(schemaUId, requestedSchema.UId, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            $"GetSchema.schema.uId не совпал с requested workspace item для {requestedSchema.Name}/{requestedSchema.PackageName}.");
    }

    if (!string.Equals(schemaName, requestedSchema.Name, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            $"GetSchema.schema.name не совпал с requested name {requestedSchema.Name}.");
    }

    string? parentSchemaName = null;
    string? parentSchemaUId = null;
    if (schema.TryGetProperty("parentSchema", out var parentSchema) &&
        parentSchema.ValueKind != JsonValueKind.Null)
    {
        if (parentSchema.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException($"GetSchema.schema.parentSchema для {schemaName} имеет неизвестную форму.");
        }

        parentSchemaName = GetRequiredString(parentSchema, "name", $"{schemaName}.parentSchema");
        parentSchemaUId = GetRequiredGuid(parentSchema, "uId", $"{schemaName}.parentSchema");
    }

    var columns = new List<ColumnMappingEvidence>();
    AppendColumnMappings(schema, "columns", "Own", schemaName, columns);
    AppendColumnMappings(schema, "inheritedColumns", "Inherited", schemaName, columns);

    return new SchemaMappingEvidence(
        requestedSchema.Name,
        requestedSchema.PackageName,
        requestedSchema.UId,
        schemaName,
        schemaUId,
        sysSchemaIdCandidate,
        parentSchemaName,
        parentSchemaUId,
        CountArray(schema, "indexes"),
        columns,
        new
        {
            SchemaUId = "schema.uId",
            SysSchemaIdCandidate = "schema.id",
            ParentSchemaUId = "schema.parentSchema.uId",
            ColumnUId = "schema.columns[].uId / schema.inheritedColumns[].uId",
            ReferenceSchemaUId = "column.referenceSchema.uId"
        });
}

static void AppendColumnMappings(
    JsonElement schema,
    string propertyName,
    string ownership,
    string schemaName,
    List<ColumnMappingEvidence> destination)
{
    if (!schema.TryGetProperty(propertyName, out var columns) || columns.ValueKind != JsonValueKind.Array)
    {
        throw new InvalidOperationException($"GetSchema.schema.{propertyName} для {schemaName} не является array.");
    }

    foreach (var column in columns.EnumerateArray())
    {
        var columnName = GetRequiredString(column, "name", $"{schemaName}.{propertyName}[]");
        var columnUId = GetRequiredGuid(column, "uId", $"{schemaName}.{columnName}");
        var type = GetRequiredInt32(column, "type", $"{schemaName}.{columnName}");
        var requirementType = GetRequiredInt32(column, "requirementType", $"{schemaName}.{columnName}");
        var indexed = GetRequiredBoolean(column, "indexed", $"{schemaName}.{columnName}");

        string? referenceSchemaName = null;
        string? referenceSchemaUId = null;
        if (column.TryGetProperty("referenceSchema", out var referenceSchema) &&
            referenceSchema.ValueKind != JsonValueKind.Null)
        {
            if (referenceSchema.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException(
                    $"referenceSchema для {schemaName}.{columnName} имеет неизвестную форму.");
            }

            referenceSchemaName = GetRequiredString(
                referenceSchema, "name", $"{schemaName}.{columnName}.referenceSchema");
            referenceSchemaUId = GetRequiredGuid(
                referenceSchema, "uId", $"{schemaName}.{columnName}.referenceSchema");
        }

        destination.Add(new ColumnMappingEvidence(
            columnName,
            columnUId,
            ownership,
            type,
            requirementType,
            indexed,
            referenceSchemaName,
            referenceSchemaUId));
    }
}

static async Task<OrderedPassResult> ReadOrderedPassAsync(
    HttpClient client,
    string rootSchemaName,
    int pageSize,
    int orderDirection,
    bool captureRegistryIdentity)
{
    var recordIds = new List<string>();
    var registryIdentities = new List<RegistryRowIdentity>();
    var pages = new List<PageEvidence>();
    JsonNode? firstPageShape = null;
    var offset = 0;
    var terminalReached = false;

    for (var pageNumber = 0; pageNumber < MaximumPageCount; pageNumber++)
    {
        using var document = await PostJsonAsync(
            client,
            SelectQueryPath,
            CreateOrderedSelectPayload(rootSchemaName, pageSize, offset, orderDirection));
        ValidateSelectResponse(document.RootElement, rootSchemaName, offset);
        firstPageShape ??= CreateResponseShape(document.RootElement);

        var rows = GetRows(document.RootElement, rootSchemaName, offset);
        ValidateRowShapes(rows, rootSchemaName, offset);
        var pageIds = new List<string>(rows.GetArrayLength());
        foreach (var row in rows.EnumerateArray())
        {
            var recordId = GetRequiredGuid(row, "Id", $"{rootSchemaName} row at offset {offset}");
            pageIds.Add(recordId);
            recordIds.Add(recordId);

            if (captureRegistryIdentity)
            {
                var (schemaUId, representation) = GetRequiredGuidValue(
                    row, "SysEntitySchemaUId", $"{rootSchemaName} row {recordId}");
                registryIdentities.Add(new RegistryRowIdentity(recordId, schemaUId, representation));
            }
        }

        pages.Add(new PageEvidence(
            pageNumber,
            offset,
            pageIds.Count,
            pageIds.FirstOrDefault(),
            pageIds.LastOrDefault()));

        if (pageIds.Count < pageSize)
        {
            terminalReached = true;
            break;
        }

        offset += pageSize;
    }

    if (!terminalReached)
    {
        throw new InvalidOperationException(
            $"{rootSchemaName} достиг bounded limit {MaximumPageCount} pages без terminal page.");
    }

    var duplicateCount = recordIds.Count - recordIds.Distinct(StringComparer.Ordinal).Count();
    if (duplicateCount != 0)
    {
        throw new InvalidOperationException(
            $"{rootSchemaName} ordered pass содержит {duplicateCount} duplicate record IDs.");
    }

    using var terminalConfirmation = await PostJsonAsync(
        client,
        SelectQueryPath,
        CreateOrderedSelectPayload(rootSchemaName, pageSize, recordIds.Count, orderDirection));
    ValidateSelectResponse(terminalConfirmation.RootElement, rootSchemaName, recordIds.Count);
    var confirmationRows = GetRows(terminalConfirmation.RootElement, rootSchemaName, recordIds.Count);
    ValidateRowShapes(confirmationRows, rootSchemaName, recordIds.Count);
    if (confirmationRows.GetArrayLength() != 0)
    {
        throw new InvalidOperationException(
            $"{rootSchemaName} terminal confirmation at offset {recordIds.Count} вернул строки.");
    }

    return new OrderedPassResult(
        rootSchemaName,
        pageSize,
        orderDirection,
        recordIds,
        registryIdentities,
        pages,
        recordIds.Count,
        ComputeSequenceDigest(recordIds),
        firstPageShape ?? new JsonObject());
}

static object CreateRequestContract(AssemblyContractEvidence assemblyContract)
{
    return new
    {
        AssemblyContract = assemblyContract,
        AssemblyBackedOrdering = new
        {
            Property = "BPMSoft.Nui.ServiceModel.DataContract.SelectQueryColumn.OrderDirection",
            PropertyType = "BPMSoft.Common.OrderDirection",
            EnumWireValues = new { None = 0, Ascending = 1, Descending = 2 },
            JsonConverter = new
            {
                Type = "BPMSoft.Nui.ServiceModel.DataContract.JsonConverters.SelectQueryColumnsJsonConverter",
                ReadJsonMethodToken = "0x06000809",
                ItemsProperty = "items"
            },
            Builder = new
            {
                Type = "BPMSoft.Nui.ServiceModel.Extensions.QueryExtension+EsqBuilder",
                BuildMethodToken = "0x06000903",
                AddQueryColumnMethodToken = "0x060008d4",
                SelectAllColumnsMethodToken = "0x060008d8",
                Rationale = "Build uses SelectAllColumns when allColumns=true and only uses explicit Columns when allColumns=false; AddQueryColumn applies OrderDirection and OrderPosition."
            }
        },
        SelectEndpoint = "/" + SelectQueryPath,
        OrderingHypothesis = new
        {
            ColumnPath = "Id",
            AscendingOrderDirection = AscendingOrder,
            DescendingOrderDirection = DescendingOrder,
            ProofRule = "Two identical ascending passes and one exact reverse descending pass."
        },
        ActivityPriorityOffsetZeroPayload = CreateOrderedSelectPayload(
            "ActivityPriority", 2, 0, AscendingOrder),
        LookupOffsetZeroPayload = CreateOrderedSelectPayload(
            "Lookup", 50, 0, AscendingOrder),
        SchemaEndpoint = "/ServiceModel/EntitySchemaDesignerService.svc/GetSchema",
        SchemaPayloadShape = new { schemaUId = "<workspace-item-uId>" }
    };
}

static object CreateOrderedSelectPayload(
    string rootSchemaName,
    int rowCount,
    int rowsOffset,
    int orderDirection)
{
    var items = new Dictionary<string, object>(StringComparer.Ordinal)
    {
        ["Id"] = new
        {
            caption = "Id",
            orderDirection,
            orderPosition = 0,
            isVisible = true,
            expression = new
            {
                expressionType = 0,
                columnPath = "Id"
            }
        }
    };
    if (string.Equals(rootSchemaName, "Lookup", StringComparison.Ordinal))
    {
        items["SysEntitySchemaUId"] = new
        {
            caption = "SysEntitySchemaUId",
            orderDirection = 0,
            orderPosition = -1,
            isVisible = true,
            expression = new
            {
                expressionType = 0,
                columnPath = "SysEntitySchemaUId"
            }
        };
    }

    return new
    {
        rootSchemaName,
        rowCount,
        rowsOffset,
        isPageable = true,
        allColumns = false,
        useLocalization = true,
        columns = new
        {
            items
        }
    };
}

static void ValidateSelectResponse(JsonElement response, string schemaName, int offset)
{
    EnsureSuccessfulEnvelope(response, $"SelectQuery {schemaName} offset {offset}");
    if (!response.TryGetProperty("notFoundColumns", out var notFoundColumns) ||
        notFoundColumns.ValueKind != JsonValueKind.Array ||
        notFoundColumns.GetArrayLength() != 0)
    {
        throw new InvalidOperationException(
            $"SelectQuery {schemaName} offset {offset} не подтвердил пустой notFoundColumns.");
    }
}

static JsonElement GetRows(JsonElement response, string schemaName, int offset)
{
    if (!response.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
    {
        throw new InvalidOperationException(
            $"SelectQuery {schemaName} offset {offset} не вернул rows array.");
    }

    return rows;
}

static void ValidateRowShapes(JsonElement rows, string schemaName, int offset)
{
    var expectedFields = schemaName switch
    {
        "ActivityPriority" => new HashSet<string>(new[] { "Id" }, StringComparer.Ordinal),
        "Lookup" => new HashSet<string>(new[] { "Id", "SysEntitySchemaUId" }, StringComparer.Ordinal),
        _ => throw new InvalidOperationException(
            $"No approved row-shape contract exists for {schemaName}.")
    };

    foreach (var row in rows.EnumerateArray())
    {
        if (row.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"SelectQuery {schemaName} offset {offset} returned a non-object row.");
        }

        var actualFields = row.EnumerateObject()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        if (!actualFields.SetEquals(expectedFields))
        {
            throw new InvalidOperationException(
                $"SelectQuery {schemaName} offset {offset} returned an unexpected row shape; " +
                "no row values were persisted.");
        }
    }
}

static PaginationProofEvidence VerifyOrderedProof(
    string schemaName,
    OrderedPassResult ascendingPass1,
    OrderedPassResult ascendingPass2,
    OrderedPassResult descendingControl)
{
    if (!ascendingPass1.RecordIds.SequenceEqual(ascendingPass2.RecordIds, StringComparer.Ordinal))
    {
        throw new InvalidOperationException(
            $"{schemaName}: два ascending pass дали разные ordered ID sequences.");
    }

    var reversed = ascendingPass1.RecordIds.AsEnumerable().Reverse();
    if (!reversed.SequenceEqual(descendingControl.RecordIds, StringComparer.Ordinal))
    {
        throw new InvalidOperationException(
            $"{schemaName}: descending control не равен exact reverse ascending sequence.");
    }

    return new PaginationProofEvidence(
        schemaName,
        ascendingPass1.PageSize,
        ascendingPass1.RecordIds.Count,
        ascendingPass1.Pages,
        ascendingPass2.Pages,
        descendingControl.Pages,
        ascendingPass1.SequenceDigest,
        ascendingPass2.SequenceDigest,
        descendingControl.SequenceDigest,
        true,
        true,
        0,
        0,
        ascendingPass1.RecordIds);
}

static Dictionary<string, string> CreateFailedActivityOrderingArtefacts(
    object requestContract,
    DateTimeOffset completedUtc,
    string target,
    OrderedPassResult ascendingPass1,
    OrderedPassResult ascendingPass2,
    OrderedPassResult descendingControl,
    string failureReason)
{
    var ascendingPassesMatch = ascendingPass1.RecordIds.SequenceEqual(
        ascendingPass2.RecordIds, StringComparer.Ordinal);
    var descendingIsExactReverse = ascendingPass1.RecordIds
        .AsEnumerable()
        .Reverse()
        .SequenceEqual(descendingControl.RecordIds, StringComparer.Ordinal);
    var sanitizedReason = ascendingPassesMatch && !descendingIsExactReverse
        ? "ActivityPriority descending control is not the exact reverse of ascending pass 1."
        : "ActivityPriority two-pass ordering assertion was not confirmed.";

    if (!failureReason.StartsWith("ActivityPriority:", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("Unexpected diagnostic failure context; evidence was not written.");
    }

    var runResult = new
    {
        Status = "NOT_CONFIRMED",
        Reason = sanitizedReason,
        Target = target,
        CompletedUtc = completedUtc,
        ActivityPriority = CreateDiagnosticPassSummary(
            ascendingPass1, ascendingPass2, descendingControl),
        Completed = new[]
        {
            "bounded schema reads and in-memory mapping (not persisted as proof)",
            "ActivityPriority ascending pass 1",
            "ActivityPriority ascending pass 2",
            "ActivityPriority descending control",
            "line-by-line ascending pass comparison",
            "line-by-line reversed-ascending versus descending comparison"
        },
        NotRun = new[]
        {
            "Lookup traversal",
            "lookup-registry mapping",
            "success proof and mapping artefacts",
            "alternative ordering payload",
            "full-catalog pull",
            "write/manage operations",
            "Excel generation"
        }
    };

    return new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["ActivityPriority.asc-pass-1.ids.txt"] = FormatIdFile(ascendingPass1.RecordIds),
        ["ActivityPriority.asc-pass-2.ids.txt"] = FormatIdFile(ascendingPass2.RecordIds),
        ["ActivityPriority.desc-control.ids.txt"] = FormatIdFile(descendingControl.RecordIds),
        ["request-contract.json"] = SerializeJson(requestContract),
        ["run-result.json"] = SerializeJson(runResult)
    };
}

static object CreateDiagnosticPassSummary(
    OrderedPassResult ascendingPass1,
    OrderedPassResult ascendingPass2,
    OrderedPassResult descendingControl)
{
    return new
    {
        AscendingPass1 = new
        {
            Count = ascendingPass1.RecordIds.Count,
            FileSha256 = ascendingPass1.SequenceDigest
        },
        AscendingPass2 = new
        {
            Count = ascendingPass2.RecordIds.Count,
            FileSha256 = ascendingPass2.SequenceDigest
        },
        DescendingControl = new
        {
            Count = descendingControl.RecordIds.Count,
            FileSha256 = descendingControl.SequenceDigest
        },
        AscendingPassesLineByLineEqual = ascendingPass1.RecordIds.SequenceEqual(
            ascendingPass2.RecordIds, StringComparer.Ordinal),
        ReversedAscendingPass1LineByLineEqualsDescending = ascendingPass1.RecordIds
            .AsEnumerable()
            .Reverse()
            .SequenceEqual(descendingControl.RecordIds, StringComparer.Ordinal)
    };
}

static string FormatIdFile(IReadOnlyList<string> recordIds)
{
    return string.Join("\n", recordIds);
}

static string SerializeJson(object value)
{
    return JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
}

static RegistryMappingEvidence CreateRegistryMapping(
    SchemaMappingEvidence lookupDataSchema,
    IReadOnlyList<RegistryRowIdentity> pass1,
    IReadOnlyList<RegistryRowIdentity> pass2,
    IReadOnlyList<RegistryRowIdentity> descendingControl,
    IReadOnlyList<string> lookupDataRecordIds)
{
    var matchingPass1 = pass1
        .Where(item => item.SysEntitySchemaUId == lookupDataSchema.SchemaUId)
        .ToArray();
    var matchingPass2 = pass2
        .Where(item => item.SysEntitySchemaUId == lookupDataSchema.SchemaUId)
        .ToArray();
    var matchingDescending = descendingControl
        .Where(item => item.SysEntitySchemaUId == lookupDataSchema.SchemaUId)
        .ToArray();

    if (matchingPass1.Length != 1 || matchingPass2.Length != 1 || matchingDescending.Length != 1)
    {
        throw new InvalidOperationException(
            $"Lookup registry mapping для {lookupDataSchema.SchemaName} не является unique во всех passes.");
    }

    if (matchingPass1[0] != matchingPass2[0] || matchingPass1[0] != matchingDescending[0])
    {
        throw new InvalidOperationException(
            $"Lookup registry mapping для {lookupDataSchema.SchemaName} различается между passes.");
    }

    return new RegistryMappingEvidence(
        lookupDataSchema.SchemaName,
        lookupDataSchema.SchemaUId,
        matchingPass1[0].LookupRecordId,
        matchingPass1[0].SysEntitySchemaUId,
        matchingPass1[0].Representation,
        lookupDataSchema.ParentSchemaName,
        lookupDataSchema.ParentSchemaUId,
        lookupDataRecordIds,
        new
        {
            LookupRecordId = "Lookup row.Id",
            SysEntitySchemaUId = "Lookup row.SysEntitySchemaUId",
            RecordId = $"{lookupDataSchema.SchemaName} row.Id",
            BaseSchemaUId = "GetSchema.schema.parentSchema.uId"
        });
}

static string GetRequiredString(JsonElement element, string propertyName, string context)
{
    if (!element.TryGetProperty(propertyName, out var value) ||
        value.ValueKind != JsonValueKind.String ||
        string.IsNullOrWhiteSpace(value.GetString()))
    {
        throw new InvalidOperationException($"{context}.{propertyName} отсутствует или не является непустой string.");
    }

    return value.GetString()!;
}

static string GetRequiredGuid(JsonElement element, string propertyName, string context)
{
    var value = GetRequiredString(element, propertyName, context);
    if (!Guid.TryParse(value, out var parsed))
    {
        throw new InvalidOperationException($"{context}.{propertyName} не является UUID.");
    }

    return parsed.ToString("D");
}

static (string Value, string Representation) GetRequiredGuidValue(
    JsonElement element,
    string propertyName,
    string context)
{
    if (!element.TryGetProperty(propertyName, out var property))
    {
        throw new InvalidOperationException($"{context}.{propertyName} отсутствует.");
    }

    if (property.ValueKind == JsonValueKind.String && Guid.TryParse(property.GetString(), out var scalar))
    {
        return (scalar.ToString("D"), "scalar-string");
    }

    if (property.ValueKind == JsonValueKind.Object &&
        property.TryGetProperty("value", out var wrappedValue) &&
        wrappedValue.ValueKind == JsonValueKind.String &&
        Guid.TryParse(wrappedValue.GetString(), out var wrapped))
    {
        return (wrapped.ToString("D"), "lookup-wrapper.value");
    }

    throw new InvalidOperationException($"{context}.{propertyName} имеет неизвестную или не-UUID форму.");
}

static int GetRequiredInt32(JsonElement element, string propertyName, string context)
{
    if (!element.TryGetProperty(propertyName, out var value) ||
        value.ValueKind != JsonValueKind.Number ||
        !value.TryGetInt32(out var result))
    {
        throw new InvalidOperationException($"{context}.{propertyName} отсутствует или не является Int32.");
    }

    return result;
}

static bool GetRequiredBoolean(JsonElement element, string propertyName, string context)
{
    if (!element.TryGetProperty(propertyName, out var value) ||
        (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False))
    {
        throw new InvalidOperationException($"{context}.{propertyName} отсутствует или не является Boolean.");
    }

    return value.GetBoolean();
}

static string ComputeSequenceDigest(IEnumerable<string> recordIds)
{
    var canonical = string.Join("\n", recordIds);
    return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))
        .ToLowerInvariant();
}

static async Task<string> WriteSafeArtefactsAsync(
    IReadOnlyDictionary<string, string> artefacts,
    DateTimeOffset pulledUtc)
{
    var files = new Dictionary<string, string>(artefacts, StringComparer.Ordinal);
    var redactionCheck = new
    {
        Passed = true,
        FilesScannedBeforeWrite = artefacts.Count + 1,
        ForbiddenMarkerHitCount = 0,
        RawLookupValuesPersisted = false,
        CaptionOrDescriptionValuesPersisted = false,
        LoginRequestOrResponsePersisted = false,
        SessionValuesPersisted = false
    };
    files.Add("redaction-check.json", SerializeJson(redactionCheck));

    var forbiddenMarkers = new[]
    {
        "UserPassword",
        "Set-Cookie",
        "BPMCSRF",
        "Authorization"
    };
    var hits = files
        .SelectMany(file => forbiddenMarkers
            .Where(marker => file.Value.Contains(marker, StringComparison.OrdinalIgnoreCase))
            .Select(marker => new { File = file.Key, Marker = marker }))
        .ToArray();
    if (hits.Length != 0)
    {
        throw new InvalidOperationException(
            $"Safe artefact pre-write scan обнаружил {hits.Length} forbidden marker hits; ничего не сохранено.");
    }

    var outputDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "probe-output",
        pulledUtc.ToString("yyyyMMddTHHmmssZ"));
    if (Directory.Exists(outputDirectory))
    {
        throw new InvalidOperationException("Timestamped evidence directory уже существует; overwrite запрещён.");
    }

    Directory.CreateDirectory(outputDirectory);
    var utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    foreach (var file in files)
    {
        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, file.Key), file.Value, utf8WithoutBom);
    }

    return outputDirectory;
}

static JsonNode CreateResponseShape(JsonElement element)
{
    return element.ValueKind switch
    {
        JsonValueKind.Object => CreateObjectShape(element),
        JsonValueKind.Array => CreateArrayShape(element),
        JsonValueKind.String => JsonValue.Create("string")!,
        JsonValueKind.Number => JsonValue.Create("number")!,
        JsonValueKind.True or JsonValueKind.False => JsonValue.Create("boolean")!,
        JsonValueKind.Null => JsonValue.Create("null")!,
        _ => JsonValue.Create(element.ValueKind.ToString())!
    };
}

static JsonObject CreateObjectShape(JsonElement element)
{
    var shape = new JsonObject();
    foreach (var property in element.EnumerateObject())
    {
        shape[property.Name] = CreateResponseShape(property.Value);
    }

    return shape;
}

static JsonObject CreateArrayShape(JsonElement element)
{
    var shape = new JsonObject { ["$kind"] = "array", ["length"] = element.GetArrayLength() };
    if (element.GetArrayLength() > 0)
    {
        shape["firstItemShape"] = CreateResponseShape(element[0]);
    }

    return shape;
}

enum ProbeMode
{
    Ordering,
    IndexProbe
}

sealed record StructuralNodeEvidence(
    string Path,
    string? PropertyName,
    string Kind,
    int? ArrayLength,
    string? SafeValue,
    string? SafeValueClass,
    bool ValueOmitted);

sealed record StructuralLedgerResult(
    IReadOnlyList<StructuralNodeEvidence> Nodes,
    IReadOnlyList<string> UnsafeScalarPaths);

sealed record ColumnLocation(JsonElement Element, string Path);

sealed record IndexProbeOwnColumnEvidence(
    string ColumnName,
    string? ColumnUId,
    string OwnershipCandidate,
    bool? Indexed,
    int OwnMatchCount,
    int InheritedMatchCount,
    string? ColumnPath,
    string? ColumnUIdPath,
    string? IndexedPath);

sealed record IndexColumnMatchEvidence(
    string ColumnName,
    string? ColumnUId,
    string Path,
    string MatchValueClass);

sealed record IndexCandidateEvidence(
    int IndexOrdinal,
    string? IndexNameCandidate,
    string? IndexNamePath,
    IReadOnlyList<string> MatchedColumnNames,
    IReadOnlyList<IndexColumnMatchEvidence> ColumnMatches,
    string? MemberArrayPath,
    int? MemberCount,
    bool? UniqueFlagCandidate,
    bool IsUnambiguousSimpleOneColumn,
    IReadOnlyList<string> Issues,
    string? UniqueFlagPath,
    string? UniqueFlagRelativePath);

sealed record IndexProbeAnalysis(
    string? GetSchemaSchemaIdCandidate,
    IReadOnlyList<IndexProbeOwnColumnEvidence> OwnColumns,
    StructuralLedgerResult IndexStructure,
    IReadOnlyList<IndexCandidateEvidence> Candidates,
    IReadOnlyList<string> ValidationErrors,
    bool Confirmed,
    string Reason);

sealed record WorkspaceSchemaHandle(string Name, string PackageName, string UId);

sealed record AssemblyFileEvidence(string Path, string FileVersion, string Sha256);

sealed record AssemblyContractEvidence(
    AssemblyFileEvidence BpmSoftCommon,
    AssemblyFileEvidence BpmSoftNuiServiceModel,
    bool VerifiedBeforeLogin);

sealed record ColumnMappingEvidence(
    string ColumnName,
    string ColumnUId,
    string Ownership,
    int Type,
    int RequirementType,
    bool Indexed,
    string? ReferenceSchemaName,
    string? ReferenceSchemaUId);

sealed record SchemaMappingEvidence(
    string RequestedName,
    string RequestedPackageName,
    string RequestedWorkspaceItemUId,
    string SchemaName,
    string SchemaUId,
    string SysSchemaIdCandidate,
    string? ParentSchemaName,
    string? ParentSchemaUId,
    int IndexCount,
    IReadOnlyList<ColumnMappingEvidence> Columns,
    object SourcePaths);

sealed record RegistryRowIdentity(
    string LookupRecordId,
    string SysEntitySchemaUId,
    string Representation);

sealed record PageEvidence(
    int PageNumber,
    int RowsOffset,
    int RowCount,
    string? FirstRecordId,
    string? LastRecordId);

sealed record OrderedPassResult(
    string SchemaName,
    int PageSize,
    int OrderDirection,
    IReadOnlyList<string> RecordIds,
    IReadOnlyList<RegistryRowIdentity> RegistryIdentities,
    IReadOnlyList<PageEvidence> Pages,
    int TerminalConfirmationOffset,
    string SequenceDigest,
    JsonNode FirstPageShape);

sealed record PaginationProofEvidence(
    string SchemaName,
    int PageSize,
    int RecordCount,
    IReadOnlyList<PageEvidence> AscendingPass1Pages,
    IReadOnlyList<PageEvidence> AscendingPass2Pages,
    IReadOnlyList<PageEvidence> DescendingControlPages,
    string AscendingPass1Digest,
    string AscendingPass2Digest,
    string DescendingControlDigest,
    bool PassDigestsMatch,
    bool DescendingIsExactReverse,
    int DuplicateRecordIdCount,
    int PageOverlapRecordIdCount,
    IReadOnlyList<string> AscendingRecordIds);

sealed record RegistryMappingEvidence(
    string SchemaName,
    string SchemaUId,
    string LookupRecordId,
    string SysEntitySchemaUId,
    string SysEntitySchemaUIdRepresentation,
    string? BaseSchemaName,
    string? BaseSchemaUId,
    IReadOnlyList<string> RecordIds,
    object SourcePaths);
