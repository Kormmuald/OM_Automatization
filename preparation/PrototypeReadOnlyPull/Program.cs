using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

const string DefaultTarget = "http://localhost:8002";

Console.WriteLine("BPMSoft read-only probe");
Console.WriteLine("Этот прототип не вызывает write API и не создаёт Excel.");

var target = ReadValue("URL стенда", DefaultTarget).TrimEnd('/');
var login = ReadValue("Логин", "Supervisor");
var password = ReadPassword();

try
{
    var cookies = new CookieContainer();
    using var handler = new HttpClientHandler { CookieContainer = cookies, UseCookies = true };
    using var client = new HttpClient(handler) { BaseAddress = new Uri(target + "/") };
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

    var sessionUri = new Uri(target + "/");
    var csrfCookie = cookies.GetCookies(sessionUri)["BPMCSRF"];
    if (csrfCookie is null || string.IsNullOrWhiteSpace(csrfCookie.Value))
    {
        throw new InvalidOperationException(
            "Login подтверждён, но не выдана session CSRF-cookie. Никакие ответы или cookies не сохранены.");
    }

    var csrfValue = csrfCookie.Value;
    client.DefaultRequestHeaders.Add("BPMCSRF", csrfValue);

    var outputDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "probe-output",
        DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssZ"));
    Directory.CreateDirectory(outputDirectory);

    using var packagesDocument = await PostEmptyJsonAsync(
        client, "ServiceModel/PackageService.svc/GetPackages");
    using var workspaceDocument = await PostEmptyJsonAsync(
        client, "ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems");

    var academyUrlSchemaUId = FindWorkspaceSchemaUId(
        workspaceDocument.RootElement, "AcademyURL", "Base");
    var activityPrioritySchemaUId = FindWorkspaceSchemaUId(
        workspaceDocument.RootElement, "ActivityPriority", "Base");

    using var academyUrlSchemaDocument = await PostJsonAsync(
        client,
        "ServiceModel/EntitySchemaDesignerService.svc/GetSchema",
        new { schemaUId = academyUrlSchemaUId });
    using var activityPrioritySchemaDocument = await PostJsonAsync(
        client,
        "ServiceModel/EntitySchemaDesignerService.svc/GetSchema",
        new { schemaUId = activityPrioritySchemaUId });

    using var activityPriorityPage0Document = await PostJsonAsync(
        client,
        "DataService/json/SyncReply/SelectQuery",
        new
        {
            rootSchemaName = "ActivityPriority",
            rowCount = 2,
            rowsOffset = 0,
            isPageable = true,
            allColumns = true,
            useLocalization = true
        });
    using var activityPriorityPage2Document = await PostJsonAsync(
        client,
        "DataService/json/SyncReply/SelectQuery",
        new
        {
            rootSchemaName = "ActivityPriority",
            rowCount = 2,
            rowsOffset = 2,
            isPageable = true,
            allColumns = true,
            useLocalization = true
        });
    client.DefaultRequestHeaders.Remove("BPMCSRF");
    csrfValue = string.Empty;

    await File.WriteAllTextAsync(
        Path.Combine(outputDirectory, "packages.response.json"),
        JsonSerializer.Serialize(packagesDocument.RootElement, new JsonSerializerOptions { WriteIndented = true }));
    await File.WriteAllTextAsync(
        Path.Combine(outputDirectory, "workspace-items.response.json"),
        JsonSerializer.Serialize(workspaceDocument.RootElement, new JsonSerializerOptions { WriteIndented = true }));
    await WriteResponseShapeAsync(
        Path.Combine(outputDirectory, "schema-AcademyURL.response-shape.json"), academyUrlSchemaDocument.RootElement);
    await WriteResponseShapeAsync(
        Path.Combine(outputDirectory, "schema-ActivityPriority.response-shape.json"), activityPrioritySchemaDocument.RootElement);
    await WriteResponseShapeAsync(
        Path.Combine(outputDirectory, "lookup-ActivityPriority-page-0.response-shape.json"), activityPriorityPage0Document.RootElement);
    await WriteResponseShapeAsync(
        Path.Combine(outputDirectory, "lookup-ActivityPriority-page-2.response-shape.json"), activityPriorityPage2Document.RootElement);

    var page0RecordIds = ExtractRecordIds(activityPriorityPage0Document.RootElement);
    var page2RecordIds = ExtractRecordIds(activityPriorityPage2Document.RootElement);

    var summary = new
    {
        Target = target,
        PulledUtc = DateTimeOffset.UtcNow,
        PackageCount = CountArray(packagesDocument.RootElement, "packages"),
        WorkspaceItemCount = CountArray(workspaceDocument.RootElement, "items"),
        SchemaProbes = new[]
        {
            CreateSchemaProbeSummary("AcademyURL", academyUrlSchemaDocument.RootElement),
            CreateSchemaProbeSummary("ActivityPriority", activityPrioritySchemaDocument.RootElement)
        },
        LookupPaginationProbe = new
        {
            SchemaName = "ActivityPriority",
            PageSize = 2,
            Offset0RowCount = CountArray(activityPriorityPage0Document.RootElement, "rows"),
            Offset2RowCount = CountArray(activityPriorityPage2Document.RootElement, "rows"),
            SharedRecordIdCount = page0RecordIds.Intersect(page2RecordIds, StringComparer.Ordinal).Count(),
            StableOrdering = "Not verified: no ordering parameter has been confirmed for this API version."
        },
        Note = "Read-only probe. Login response, cookies, CSRF values, password and lookup-row values are not saved."
    };
    await File.WriteAllTextAsync(
        Path.Combine(outputDirectory, "summary.json"),
        JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));

    Console.WriteLine($"Read-only ответы сохранены: {outputDirectory}");
    Console.WriteLine($"Пакетов: {summary.PackageCount}; workspace items: {summary.WorkspaceItemCount}.");
    Console.WriteLine("Сохранены read-only inventory, schema shapes и redacted lookup-row shapes; Excel не создавался.");
}
catch (Exception exception)
{
    password = string.Empty;
    Console.Error.WriteLine($"Проверка не завершена: {exception.Message}");
    Environment.ExitCode = 1;
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
    return root.TryGetProperty("Code", out var code) && code.ValueKind == JsonValueKind.Number && code.GetInt32() == 0;
}

static int CountArray(JsonElement root, string propertyName)
{
    return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Array
        ? value.GetArrayLength()
        : 0;
}

static string FindWorkspaceSchemaUId(JsonElement workspaceResponse, string schemaName, string packageName)
{
    if (!workspaceResponse.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
    {
        throw new InvalidOperationException("GetWorkspaceItems не вернул массив items.");
    }

    foreach (var item in items.EnumerateArray())
    {
        if (item.TryGetProperty("name", out var name) &&
            item.TryGetProperty("packageName", out var package) &&
            item.TryGetProperty("type", out var type) &&
            name.ValueKind == JsonValueKind.String &&
            package.ValueKind == JsonValueKind.String &&
            type.ValueKind == JsonValueKind.Number &&
            name.GetString() == schemaName &&
            package.GetString() == packageName &&
            type.GetInt32() == 3 &&
            item.TryGetProperty("uId", out var uId) &&
            uId.ValueKind == JsonValueKind.String)
        {
            return uId.GetString() ?? throw new InvalidOperationException(
                $"У схемы {schemaName} пустой uId.");
        }
    }

    throw new InvalidOperationException(
        $"В workspace inventory не найдена единственная object schema {schemaName} в пакете {packageName}.");
}

static object CreateSchemaProbeSummary(string schemaName, JsonElement response)
{
    return new
    {
        SchemaName = schemaName,
        Success = response.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True,
        OwnColumnCount = CountNestedArray(response, "schema", "columns"),
        InheritedColumnCount = CountNestedArray(response, "schema", "inheritedColumns"),
        IndexCount = CountNestedArray(response, "schema", "indexes")
    };
}

static int CountNestedArray(JsonElement root, string objectProperty, string arrayProperty)
{
    return root.TryGetProperty(objectProperty, out var nestedObject) &&
           nestedObject.ValueKind == JsonValueKind.Object &&
           nestedObject.TryGetProperty(arrayProperty, out var array) &&
           array.ValueKind == JsonValueKind.Array
        ? array.GetArrayLength()
        : 0;
}

static HashSet<string> ExtractRecordIds(JsonElement response)
{
    var recordIds = new HashSet<string>(StringComparer.Ordinal);
    if (!response.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
    {
        return recordIds;
    }

    foreach (var row in rows.EnumerateArray())
    {
        if (row.TryGetProperty("Id", out var id) && id.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(id.GetString()))
        {
            recordIds.Add(id.GetString()!);
        }
    }

    return recordIds;
}

static async Task WriteResponseShapeAsync(string path, JsonElement response)
{
    var shape = CreateResponseShape(response);
    await File.WriteAllTextAsync(
        path,
        JsonSerializer.Serialize(shape, new JsonSerializerOptions { WriteIndented = true }));
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
