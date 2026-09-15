# Операторский runbook — Feature 001 read-only catalog qualification

## Фактический статус

S01–S06 имеют независимо принятое offline evidence. S07 был выполнен ровно один раз
после `стенд запущен`, но target не квалифицирован: `exit 2`,
`SCHEMA_INVENTORY_UNQUALIFIED` / `UNKNOWN_SHAPE_UNQUALIFIED`, `RetryCount=0`.
`FULL_CATALOG_NOT_QUALIFIED` и `INDEX_SYNC_UNRESOLVED` остаются открытыми.

## Build и offline suite

Требуется .NET 10 SDK. Offline путь использует только sanitized fixtures/fake HTTP;
ему не нужны BPMSoft, Excel, браузер, URL или credentials.

```powershell
dotnet build BpmSoftSync.sln -c Release --no-restore
dotnet run --project tests/BpmSoftSync.Domain.Tests/BpmSoftSync.Domain.Tests.csproj -c Release
dotnet run --project tests/BpmSoftSync.Adapters.BpmSoft.Tests/BpmSoftSync.Adapters.BpmSoft.Tests.csproj -c Release
dotnet run --project tests/BpmSoftSync.Application.Tests/BpmSoftSync.Application.Tests.csproj -c Release
dotnet run --project tests/BpmSoftSync.Adapters.FileSystem.Tests/BpmSoftSync.Adapters.FileSystem.Tests.csproj -c Release
dotnet run --project tests/BpmSoftSync.Adapters.Excel.Tests/BpmSoftSync.Adapters.Excel.Tests.csproj -c Release
dotnet run --project tests/BpmSoftSync.Cli.Tests/BpmSoftSync.Cli.Tests.csproj -c Release --no-build
```

Совместимая offline команда: `BpmSoftSync.Cli catalog validate-offline --fixture <sanitized-fixture>`.
`HUMAN_REVIEW_REQUIRED` в offline output — evidence decision point, не live success и
не разрешение Apply.

## Opt-in live command и credentials

Новый live запуск возможен только после отдельного human decision об устранении
blocker и нового явного manual/current-user admission. В видимом local terminal:

```text
BpmSoftSync.Cli catalog qualify --target <safe-alias> --scope full --manual --live --output-root <new-user-local-directory>
```

URL/login/password вводятся только в terminal prompts и не попадают в args, config,
files, chat, logs или evidence. Default/CI/offline пути не вызывают live route.

## Stop rules, output и inspection

Разрешены только `AUTH_LOGIN`, `WORKSPACE_ITEMS`, `SCHEMA_GET`, `SELECT_QUERY`.
Запрещены Write, Manage, Compare, Apply, browser write, Git и index mutation. На
blocker или `TARGET_STATE_CHANGED_DURING_QUALIFICATION` — stop без retry, Pass C,
automatic rerun или partial output.

Только successful A/B equality публикует новую атомарную Excel-пару в
`<output-root>/runs/yyyy/MM/dd/<RunId>/output/`. Lookup values допустимы только в
локальной Lookup workbook; evidence/journal/diagnostics содержат лишь safe metadata.
Фактический S07 run `007b6fd7-e66b-471a-b262-7a24d32832bc` blocked-terminal:
`output/` пуст, `.xlsx=0`, `review-only-seal.json` отсутствует, поэтому
Excel/OOXML/read-back inspection неприменимы. Не повторяйте run для диагностики.

## Проверка Excel-пары после будущего `HUMAN_REVIEW_REQUIRED`

Этот раздел применяется только к новому успешному run с `HUMAN_REVIEW_REQUIRED` и
`review-only-seal.json`. Он не применяется к текущему blocked S07 run и не даёт права
на Compare, Apply или любое изменение BPMSoft.

1. Откройте обе книги только для чтения из одного `<RunId>/output/`:
   `BPMSoft.ModelCatalog.xlsx` и `BPMSoft.LookupCatalog.xlsx`. Убедитесь, что обе
   существуют вместе; не подменяйте одну книгу из другого run.
2. Проверьте точный порядок листов и точную строку заголовков каждого листа против
   workbook contract v1/read-back evidence. Model: `Readme`, `Manifest`,
   `WorkspaceInventory`, `Schemas`, `Columns`, `Indexes`, `ValidationLists`,
   `PullConflicts`. Lookup: `Readme`, `Manifest`, `LookupRegistry`, `LookupValues`,
   `ValidationLists`, `PullConflicts`. Не принимайте изменённый/пропущенный лист или
   заголовок, даже если книга открывается в Excel.
3. В обоих `Manifest` сверяйте одинаковые `RunId`, `PairId`, baseline hash, target
   fingerprint, Pass A/B digests, scope digest, component/count digests и безопасный
   `SourceIdentity` в форме `sha256:<64 lowercase hex>`. Сверьте pair/manifest
   binding с safe `workbook-pair` evidence и `review-only-seal.json` того же run.
4. По safe counts/manifests и read-back подтвердите 1:1 projection: каждая workspace
   item, schema, own/inherited column, index member, lookup registry record и
   normalized lookup value представлена ровно одной строкой в назначенном листе;
   не копируйте значения `LookupValues` в evidence или chat.
5. Выполните/проверьте recorded package inspection: OOXML package opens/read-backs,
   relationships and content types are closed and valid; нет external links, VBA,
   connections, embeddings, external/cross-workbook relationships или forbidden
   worksheet formulas. Любая ошибка, формула или package discrepancy — blocker:
   stop без retry/partial pair и передайте человеку только safe diagnosis.
