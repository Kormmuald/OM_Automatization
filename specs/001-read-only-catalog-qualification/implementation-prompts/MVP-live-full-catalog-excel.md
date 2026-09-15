# Промпт реализации MVP Feature 001: live full-catalog pull в Excel

> **Статус:** этот документ сохраняет полную исходную постановку MVP, но больше не
> предназначен для исполнения одним агентом. Исполняемый context-safe prompt pack:
> [`mvp-live-full-catalog-excel-pack/README.md`](mvp-live-full-catalog-excel-pack/README.md).
> Оркестрацию начинать только с `mvp-live-full-catalog-excel-pack/orchestrator.md`.

Работай как implementation agent в основной рабочей копии
`C:/CodingAgents/codex/projects/OM_Automatization`. Все сообщения пользователю в
Codex chat пиши на русском языке.

## Цель

На базе существующего приложения `src/BpmSoftSync.Cli` доведи Feature 001 до
рабочего MVP, который оператор запускает вручную и который:

1. интерактивно подключается к реальному стенду BPMSoft;
2. выполняет строго read-only полную выгрузку доступного состояния объектной модели
   и всех доступных справочников;
3. дважды независимо читает один и тот же полный scope и fail-closed сверяет оба
   прохода;
4. только после успешной сверки создаёт новую пару реальных Excel-книг
   `BPMSoft.ModelCatalog.xlsx` и `BPMSoft.LookupCatalog.xlsx`;
5. использует проверенные семантику, структуру книг и OOXML-наработки
   `preparation/WorkbookDeliveryTool`, но переносит необходимую production-логику в
   текущую solution и не создаёт runtime dependency на `preparation/*`;
6. сохраняет safe append-only audit/evidence без credentials, session material и
   raw lookup values;
7. не удаляет и не ухудшает ни одну существующую возможность `BpmSoftSync.Cli`,
   `PrototypeReadOnlyPull` или offline regression suite.

Реальное подключение не запрещено правилами проекта. Оно разрешено после фактического
завершения production-доработки и успешных обязательных offline checks: отдельным
ручным запуском оператора либо агентом по прямой текущей просьбе пользователя. Не
пытайся подключаться раньше технической готовности CLI и не называй эту очередность
политическим запретом на live-доступ.

По завершении production-доработки и обязательных offline checks подготовь отдельный
готовый промпт совместной live-проверки. Затем прямо в чате попроси пользователя
запустить локальный стенд BPMSoft и дождись явного ответа, что стенд запущен. После
этого разрешено в том же implementation flow запустить opt-in live integration suite:
сама последовательность теста автоматизирована, но её admission остаётся ручным, а
URL/login/password пользователь вводит только в terminal. Агент должен выполнить
реальную read-only выгрузку, проверить созданные Excel-книги и сохранить безопасный
результат проверки. Это соответствует `T046`; отдельный `AuthorizationReference` не
требуется.

## Авторитетные входы и обязательный протокол

До любых изменений полностью прочитай:

- `AGENTS.md`;
- `.specify/feature.json`, `.specify/project.yml`, `.specify/extensions.yml`;
- `.specify/memory/constitution.md`;
- единственный текущий
  `specs/001-read-only-catalog-qualification/HANDOFF.md`;
- текущие `spec.md`, `plan.md`, `research.md`, `data-model.md`,
  `contracts/cli-contract.md`, `test-plan.md`, `tasks.md`, `quickstart.md` Feature 001;
- фактический код `src/` и все regression tests;
- `preparation/PrototypeReadOnlyPull/Program.cs` как characterization reference
  реальной read-only сессии и BPMSoft request shapes;
- `preparation/WorkbookDeliveryTool/Program.cs` как technical predecessor генерации
  и проверки Excel;
- `preparation/docs/WORKBOOK_CONTRACT_VISION.md` и
  `specs/002-workbook-pair-control/spec.md` как контракт проекции данных в книги;
- неизменяемое общее видение
  `preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md`.

Все файлы в `preparation/docs/product-specs/` являются immutable source drafts: не
редактируй, не перемещай, не переименовывай и не удаляй их. Не изменяй и не
перезаписывай `preparation/workbooks/*.xlsx`, код прототипов или их исторические run
artifacts. Они служат только reference/fixture материалом.

Перед реализацией проверь `.specify/feature.json`: active target обязан оставаться
`specs/001-read-only-catalog-qualification`.

Текущий `HANDOFF.md` фиксирует, что расширение MVP на полную live-выгрузку и создание
новой датированной Excel-пары уже подтверждено владельцем, но mutable artifacts
Feature 001 ещё должны быть согласованы с этим решением. Поэтому до production code:

1. через class-aware корпоративный workflow обнови mutable `spec.md`, `plan.md` и
   `tasks.md` Feature 001 так, чтобы они явно включали полный live pull, output
   contract и MVP Excel materialization;
2. для L2/l2-pilot соблюдай локальный порядок
   `/SpecKit Plan` -> `/SpecKit Tasks` -> `/SpecKit Analyze` -> `/SpecKit Implement`;
3. перед каждой командой выполни обязательный `speckit-class-gate`, а на её границе —
   все применимые hooks из `.specify/extensions.yml`;
4. не подменяй `tasks.md` этим промпом и не отмечай задачи выполненными без их точного
   evidence;
5. если workflow требует пользовательского решения, которое не следует из этого
   промпта или текущего handoff, остановись и задай один конкретный вопрос; не выдумывай
   контракт;
6. сохрани все существующие чужие изменения dirty worktree; не выполняй reset,
   checkout, clean, commit, push или другие Git mutations.

## Зафиксированное решение владельца

Разрешено расширить MVP Feature 001 созданием Excel-пары как временной сквозной
материализации результата чтения. Это не разрешает реализовывать Compare, Plan, Apply,
writeback, обновление BPMSoft, browser actions или Git operations из Features 003–004.

Excel-материализация в этом MVP должна быть узким adapter/output consumer полного
read-only snapshot. Доменный слой Feature 001 не должен зависеть от Excel/OOXML типов.
Будущая Feature 002 сможет переиспользовать или вынести этот adapter без дублирования
reader, identity, qualification и snapshot logic.

## Непереговорный принцип сохранения функционала

Ничего работающего не урезай. В частности:

- сохрани команды `catalog validate-offline`, `catalog qualify` и
  `catalog diagnose` либо обеспечь полностью обратно совместимый CLI alias;
- сохрани fixture source, fake HTTP handler, negative paging/shape/security tests;
- сохрани полезную семантику ordering/index probes; если она не соответствует правилу
  ровно двух full passes, оставь её отдельной диагностической командой/режимом, а не
  удаляй и не включай как скрытый Pass C;
- не заменяй typed domain/application architecture монолитным копированием prototype
  `Program.cs`;
- любые legacy semantics сначала классифицируй как
  `reuse-semantics|rewrite|drop|defer`; port-by-copy запрещён;
- существующие canonical/template книги не перезаписывай. Каждый успешный live run
  создаёт новую пару под уникальным `RunId`.

## Требуемый пользовательский сценарий MVP

Сохрани существующий ручной admission contract либо расширь его обратно совместимым
способом. Целевой сценарий должен быть доступен из `BpmSoftSync.Cli`, например:

```text
catalog qualify --target <safe-alias> --scope full --manual [--output-root <path>]
```

Точное имя новой дополнительной команды допустимо изменить в Plan, но единственный
production workflow должен выполнять qualification и Excel output без второго
параллельного reader.

После ручного запуска CLI интерактивно спрашивает:

1. точный URL стенда;
2. login;
3. password с masked/no-echo вводом.

URL, login и особенно password нельзя принимать как CLI argument. В evidence хранится
safe target alias, но не login и не URL с потенциально чувствительными частями.
Password, cookies, `BPMCSRF`, login request/response и authorization headers живут
только в памяти и очищаются/dispose при любом исходе.

`--output-root` не является секретом, но должен быть нормализован, проверен и не может
указывать на source template/prototype directories. Если option не передан, используй
безопасный документированный user-local default, а не `Path.GetTempPath()` как
production root.

## 1. Production composition root

Собери один путь:

```text
CLI manual admission
  -> interactive in-memory session
  -> exact read-only BPMSoft transport
  -> full catalog source
  -> generic ordered reader
  -> Pass A
  -> Pass B
  -> terminal reconciliation
  -> in-memory QualifiedCatalogSnapshot/v1
  -> Excel output adapter
  -> safe audit/evidence seal
```

Fixture и fake-handler режимы обязаны проходить через тот же application use case.
Запрещены command-specific shortcuts, synthetic `pages` в production parser,
hardcoded `fixture` target/scope и отдельная упрощённая qualification logic.

## 2. Реальная read-only BPMSoft сессия

Реализуй и offline-протестируй через fake `HttpMessageHandler`:

- `POST /ServiceModel/AuthService.svc/Login`;
- cookie container и получение `BPMCSRF` после успешного login;
- `POST /ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems`;
- `POST /ServiceModel/EntitySchemaDesignerService.svc/GetSchema` только с одним
  проверенным `schemaUId`;
- `POST /DataService/json/SyncReply/SelectQuery` только с canonical reader-generated
  payload;
- `Accept: application/json`, bounded response reading, cancellation и timeouts;
- redirects disabled;
- exact-origin enforcement для каждого запроса;
- deny-before-send для метода/path/body/host/query/fragment/traversal mismatch;
- безопасные именованные blockers для HTTP, login, envelope и parse failures.

Поддержи как loopback HTTP(S), так и явно введённый реальный HTTPS origin, если он
проходит принятую target policy. Не разрешай произвольный redirect/alternate host.
Изменение target policy должно быть явно отражено в `research.md`, Plan, tests и
operator documentation.

Никаких Write/Manage/delete/compile/save/create/update endpoints. Captured call matrix
на всех offline и будущих live paths должна содержать 100% allowlisted calls и ровно
0 write calls.

## 3. Полный WorkspaceInventory и объектная модель

Pass A и Pass B каждый независимо начинают с полного `GetWorkspaceItems`.

Для каждого доступного item обязательно сохрани typed identity, package-layer identity,
item type, `Structured|InventoryOnly|Unreadable|Unsupported` и safe reason. Нельзя
пропустить item, объединить одинаковые display names или заменить неизвестные поля
default-значениями.

Для каждого подтверждённого EntitySchema item выполни `GetSchema` и построй полную
модель как минимум из:

- `SchemaName`, `SchemaUId`, доказанного server schema Id candidate при наличии;
- parent schema name/UId;
- actual package name/UId и package layer identity;
- own и inherited columns;
- `ColumnName`, `ColumnUId`, ownership, BPMSoft type code;
- required/requirement semantics без догадок;
- `ActualIndexed` как отдельного observed flag;
- reference schema name/UId;
- всех `schema.indexes[]`, `IndexUId`, name, uniqueness, auto-name при наличии;
- всех index members в исходном ordinal order.

Index member relation строится только по
`schema.indexes[].columns[].columnUId`. Member `.uId`, display name, позиция и
`ActualIndexed` не могут подменять `ColumnUId`. `INDEX_SYNC_UNRESOLVED` остаётся
активным запретом на любые index mutations.

Для неизвестных item/schema/property/type/shape создай lossless обезличенный structural
envelope с property names, JSON kinds, array/object structure, scalar class/length/hash
и source digest. Raw data не должна попадать в envelope. Неизвестная структура либо
представлена losslessly, либо даёт явный scoped blocker; скрытый пропуск запрещён.

## 4. Полная выгрузка справочников

Через полный ordered `SelectQuery` прочитай системный `Lookup` registry и сохрани для
каждой записи как минимум:

- `LookupRecordId` (`Lookup.Id`);
- `SysEntitySchemaUId`;
- связь с точным schema/package layer;
- доказанную base schema identity, если она доступна из read contract.

По registry и schema metadata определи все доступные lookup schemas. Для каждой из них
прочитай все доступные записи и все поддержанные контрактом колонки. Это не чтение
business rows обычных EntitySchema: data pull ограничен реестром справочников и
связанными с ним lookup schemas.

Каждый lookup должен читаться canonical `SelectQuery` с explicit columns,
`allColumns=false`, стабильным order по `Id`, bounded page size и terminal empty-page
проверкой. Для каждой записи/колонки snapshot должен различать:

- `Null`;
- `EmptyString`;
- `Value`;
- scalar `ValueKind`;
- canonical value;
- `ReferenceRecordId` для reference values;
- row/source fingerprint.

Неизвестный или неподдержанный тип колонки нельзя молча исключить. Он должен получить
lossless/scoped diagnostic или именованный blocker с точным schema/column identity.

## 5. Общий ordered reader и два полных прохода

Reader должен быть общим для `Lookup` registry и каждой lookup collection. Он обязан
контролировать:

- зарегистрированный stable order;
- page/offset progression;
- duplicates, overlap и detectable gaps;
- повтор страницы и loop;
- empty middle page;
- non-empty page after terminal;
- maximum pages/rows/response bytes;
- cancellation/timeout;
- counts, page manifests, ordered identity digest, duration и response-size buckets.

### Каноническая расшифровка Pass A и Pass B

Не выводи смысл Pass A/Pass B из названий и не трактуй их как «до/после изменения»,
ascending/descending control, dry-run/live-run либо чтение разных частей каталога.
Оба pass являются полными независимыми read-only снимками одного фактического состояния
одного BPMSoft target в рамках одного qualification run.

Источники определения, в порядке приоритета:

1. `research.md`, Decision 3 — алгоритм двух проходов, scope seal и reconciliation;
2. `spec.md`, `FR-004`, `FR-010`, `SC-002` — обязательная полнота, сравнение и terminal
   outcomes;
3. `data-model.md`, сущности `CatalogPass`, `PageManifest`, `CatalogQualification` и
   `TargetFingerprint`;
4. `plan.md`, Delivery Sequence и S04 invariants;
5. `test-plan.md` и canonical `tasks.md` — проверяемые случаи и evidence.

Этот промпт уточняет полный MVP scope для новых mutable artifacts. Если прежний
planning artifact описывает только bounded/fixture scope, сначала обнови его через
обязательный class-aware workflow; не сужай настоящий full-catalog pass до старой
фикстуры.

**Pass A — первый полный снимок и фиксация области.** После успешного login Pass A:

1. фиксирует immutable `ScopeDescriptor` для всего run: exact target alias/origin,
   allowlist version, перечень разрешённых типов/collections, query/order contracts,
   page/row/response limits и snapshot/workbook projection version;
2. свежими HTTP-запросами читает полный `WorkspaceInventory`;
3. читает все поддержанные EntitySchema/package layers, columns, references, indexes и
   index members;
4. полностью читает системный `Lookup` registry;
5. полностью читает каждую обнаруженную lookup collection и все поддержанные значения;
6. валидирует paging/termination/shape без retry;
7. формирует `CatalogPass A`: ordered identities, counts, page manifests, safe telemetry,
   unsupported set, component digests, value/content digests и `TargetFingerprint/v1`;
8. после успешного завершения запечатывает scope и результаты A только для последующего
   сравнения. Pass A ничего не пишет в BPMSoft и ещё не создаёт финальные Excel-книги.

**Pass B — второй независимый полный снимок того же scope.** Pass B запускается ровно
один раз сразу после завершения A и:

1. использует тот же запечатанный `ScopeDescriptor`, target, session policy, collection
   registry, query columns, stable order и limits;
2. заново выполняет все реальные HTTP-чтения полного workspace/schema/lookup scope;
3. не переиспользует rows/pages/schemas/lookup values из A как свои входные данные и не
   выдаёт cache A за второй проход;
4. независимо строит `CatalogPass B` с теми же типами manifests, counts, telemetry,
   unsupported set, component/value digests и fingerprint;
5. ничего не изменяет в BPMSoft между A и B.

**Reconciliation.** После B сравниваются как минимум:

- sealed scope и target/version evidence;
- полный ordered workspace/schema/column/index/registry/lookup identity inventory;
- количество элементов и строк каждой collection;
- page manifests и ordered identity digests;
- schema/index/reference component digests;
- hashes нормализованных lookup values, включая различия `Null|EmptyString|Value` и
  reference IDs, без сохранения raw values в evidence;
- support statuses, lossless unknown-shape digests и unsupported set;
- общий `TargetFingerprint/v1`.

Только при полном совпадении A и B создаётся единый
`QualifiedCatalogSnapshot/v1`. Для materialization используй проверенное содержимое B
как самое позднее фактическое чтение, но только после доказательства его полного
равенства A; pair manifest обязан ссылаться на digests обоих pass и reconciliation.

Любое paging/shape нарушение является соответствующим terminal blocker текущего pass.
Любое различие корректно прочитанных A и B означает
`TARGET_STATE_CHANGED_DURING_QUALIFICATION`, `RetryCount = 0`, без Pass C, retry,
автоматического нового double pass или частичной Excel-выгрузки. Новый run возможен
только после нового ручного запуска или новой прямой текущей просьбы пользователя.

Descending/reverse-order probe из legacy-прототипа не является Pass B. Сохрани его как
отдельный bounded diagnostic/contract test там, где direction semantics доказаны, но не
добавляй его как третий full-catalog read qualification run.

## 6. Контракт `QualifiedCatalogSnapshot/v1`

Введи typed application/domain contract полного snapshot, достаточный для 1:1
материализации workbook contract:

- workspace items;
- schemas;
- columns;
- indexes и members;
- lookup registry;
- normalized lookup values;
- source/run/pair identities;
- target/component fingerprints;
- support statuses и scoped unsupported diagnostics;
- counts и scale telemetry.

Snapshot создаётся только из согласованных Pass A/Pass B. Domain/Application не должны
ссылаться на `HttpClient`, console, Excel, OOXML, browser или Git types.

Не создавай durable JSON с raw lookup values без отдельного утверждённого контракта.
Для MVP фактические values передаются Excel adapter в памяти. Если технически нужен
temporary file, он должен находиться только внутри уникального run staging root,
иметь restrictive local permissions, никогда не входить в audit/evidence, удаляться
после успешной атомарной materialization и иметь тесты crash/failure cleanup. Не
расширяй это исключение без необходимости.

## 7. Реальная Excel-пара

Создай в текущей solution отдельный Excel/OOXML adapter project или эквивалентный
adapter-layer component. Переиспользуй доказанные semantics и алгоритмы
`preparation/WorkbookDeliveryTool`, но перепиши их в production-модули и покрой тестами.

Каждый успешный run создаёт новый каталог:

```text
<output-root>/runs/yyyy/MM/dd/<RunId>/
  audit/
  evidence/
  output/
    BPMSoft.ModelCatalog.xlsx
    BPMSoft.LookupCatalog.xlsx
  run-journal.json
```

Не перезаписывай существующий run root или workbook. Обе книги публикуются атомарно:
до финального rename/move обе должны быть полностью созданы и проверены. При сбое не
оставляй одну книгу как успешный результат пары.

Минимальная проекция должна точно соответствовать
`preparation/docs/WORKBOOK_CONTRACT_VISION.md` и bounded v3 prototype:

**Model Catalog**

- `Readme`;
- `Manifest`;
- `WorkspaceInventory`;
- `Schemas`;
- `Columns`;
- `Indexes`;
- `ValidationLists`;
- `PullConflicts`.

**Lookup Catalog**

- `Readme`;
- `Manifest`;
- `LookupRegistry`;
- `LookupValues`;
- `ValidationLists`;
- `PullConflicts`.

Отдельные `LookupRows` и `LookupColumns` не создавай. Используй нормализованный точный
набор `LookupValues` из workbook contract, включая `RecordId`, `ColumnName`,
`ValueState`, `Value`, `ValueKind`, references, `CanonicalValue` и fingerprints.

Сохрани совместимые headers, sheet order, editable/derived semantics, validation lists,
styles, filters, protection и допустимые template formulas. Не добавляй формулы во
вводимые/identity/derived data tables, где контракт их запрещает, и не создавай внешние
links, connections, macros/VBA или cross-workbook formulas.

Разрешено создать owned templates/assets уже внутри текущего приложения, если прямое
production-создание через OOXML не обеспечивает нужное оформление. В этом случае:

- создай templates в production-owned каталоге вне `preparation/`;
- зафиксируй их version и SHA-256;
- добавь их в project output deterministically;
- проверь, что они не содержат данных старого стенда, external links, connections,
  macros или secrets;
- не изменяй исходные prototype templates.

Перед публикацией пары выполни OOXML closure/read-back verification, exact sheet/header
inventory, pair identity/manifest checks, отсутствие внешних частей и запрещённых
формул, exact 1:1 source projection и проверку `ActualIndexed`/`Indexes` separation.

## 8. Разделение output и evidence

Excel-книги являются фактическим пользовательским output и могут содержать значения
справочников. Они не являются `EvidenceEnvelope` и не должны сканироваться правилом,
которое запрещает любое raw lookup value как таковое.

`audit/`, `evidence/`, `run-journal.json`, CLI output и diagnostic output не могут
содержать raw lookup values, login, password, URL credentials, cookies, CSRF, request/
response bodies или authorization headers. В них допустимы только schema-versioned
metadata, stable technical IDs, safe target alias, counts, duration/size buckets,
relative output paths, versions, hashes, statuses и blockers.

Evidence validation остаётся allow-by-schema. Scanner выполняется до каждой durable
audit/evidence write и перед seal. Workbook hashes и canonical worksheet hashes можно
фиксировать в evidence; содержимое ячеек — нельзя.

## 9. Tests-first, offline validation и opt-in live integration test

До фактического завершения production path и успешного прохождения обязательной offline
matrix не используй реальный target и не запрашивай credentials. Все automated HTTP
tests выполняй только через fake `HttpMessageHandler`/transport. Это временный
технический gate перед первым live run, а не постоянный запрет реального подключения.

После успешных offline checks реальный read-only запуск разрешён правилами Feature 001.
Для этой реализации он является отдельным opt-in integration test: его начинает
оператор вручную либо агент по прямой текущей просьбе пользователя. Отдельный
`AuthorizationReference` не нужен. Credentials вводит только пользователь в terminal
prompt; передавать их в chat, CLI arguments, config, files, logs или evidence запрещено.

До production code добавь failing tests, затем реализуй поведение. Минимальная matrix:

1. Полный многостраничный fake catalog с несколькими packages, одноимёнными schemas,
   own/inherited columns, references, несколькими lookup schemas, нестандартными lookup
   колонками, null/empty/value/reference values и composite indexes.
2. Доказательство ровно двух полных source reads и отсутствия Pass C/retry.
3. Exact endpoint/request/origin capture и zero write calls.
4. Login/cookie/CSRF lifecycle без сериализации secrets.
5. Все существующие negative paging cases плюс malformed login/workspace/schema/select,
   host/redirect/body mismatch и target change.
6. Unknown workspace/schema/column/index/value shapes без default/drop.
7. Canonical fingerprint golden/metamorphic vectors.
8. Exact 1:1 snapshot -> workbook projection: нет пропущенных, лишних или дублированных
   строк во всех business sheets.
9. Workbook reproducibility для одного snapshot, pair/manifest binding, read-back,
   OOXML closure, sheet/header/style/protection/validation/formula/external/VBA guards.
10. Atomic pair publication и fault injection между staging/validation/publication.
11. Run root collision/concurrency, evidence schema и secret/raw-value canaries.
12. Production `Program.Main` E2E через fixture и fake HTTP adapters, а не helper wiring.
13. Регрессия всех ранее работавших commands, probes и test executables.

Выполни Release build и все custom test executables solution. Не считай простую сборку,
имя теста или historical PASS достаточным evidence. Сохрани безопасный fresh offline
report с точными командами, exit codes, fixture IDs/SHA-256, output tree hashes,
projection/reconciliation/schema/scan summaries и тестовыми результатами.

Если полный fake snapshot превышает доказанные Excel limits, результатом должен быть
`WORKBOOK_SCALE_DECISION_REQUIRED`, а не обрезанная книга. Никаких скрытых row/column
limits или silent truncation.

### Обязательный opt-in live integration suite

Добавь отдельный live integration harness/project либо явно отделённый CLI test mode.
Он не должен входить в обычный unattended/default test run и не должен запускаться в
CI или по одному `dotnet test`/общему test executable без специального opt-in. Для
допуска требуются одновременно:

- production path реализован;
- вся обязательная offline matrix прошла;
- указан явный `--live` и `--manual` либо эквивалентный однозначный opt-in;
- пользователь в текущем чате подтвердил, что локальный стенд запущен;
- terminal интерактивен и пользователь сам вводит URL/login/password.

После offline PASS напиши пользователю один ясный запрос, например:

> Production path и offline checks готовы. Пожалуйста, запустите локальный стенд
> BPMSoft и сообщите «стенд запущен». После подтверждения я открою terminal, где вы
> самостоятельно введёте URL, login и password, и запущу read-only live integration
> test с полной выгрузкой в новую Excel-пару.

Дождись ответа пользователя. До ответа не открывай сетевое соединение и не симулируй
live PASS. После подтверждения запусти ровно один live integration run.
Автоматизированный сценарий должен:

1. проверить readiness доступным read-only способом в рамках утверждённого endpoint
   contract либо сразу выполнить login;
2. выполнить login и установить ephemeral cookie/CSRF session;
3. выполнить полный Pass A и Pass B без Pass C/retry;
4. прочитать полные workspace/schema/lookup scope и сформировать snapshot;
5. создать новую уникальную Excel-пару;
6. выполнить OOXML/read-back, pair/manifest и 1:1 projection checks;
7. проверить captured endpoint matrix и `writeCallCount = 0`;
8. сохранить только безопасные live evidence: counts, hashes, durations, size buckets,
   statuses, blockers и относительные пути;
9. завершиться PASS для human review либо одним явным terminal blocker.

Стенд считается почти пустым, и пользователь явно разрешает обработку его реальных
данных в рамках этой проверки. Это разрешает чтение данных и помещение значений
справочников в локальные output Excel-книги, но не отменяет защиту credentials/session
material и не разрешает помещать raw response bodies/lookup values в chat, test logs,
audit/evidence, source fixtures или Git-tracked файлы.

Если тест обнаружил `TARGET_STATE_CHANGED_DURING_QUALIFICATION`, paging/shape blocker,
ошибку login или недоступный стенд, остановись без автоматического retry. Новый live run
возможен только после нового явного запроса/подтверждения пользователя.

## 10. Документация и CLI diagnostics

Обнови `docs/read-only-handoff/` и CLI help:

- prerequisites и установка на чистой Windows-машине;
- manual command и интерактивные prompts;
- target/origin policy;
- output-root/run tree;
- различие output Excel и safe evidence;
- blockers и recovery/next allowed action;
- отсутствие Write/Manage/Compare/Apply/browser/Git полномочий;
- запрет повторного запуска после target change без нового manual invocation;
- граница `FULL_CATALOG_NOT_QUALIFIED` до успешного opt-in live integration run.

`catalog diagnose --run <RunId>` показывает только safe reason, scope, recovery и next
allowed action. Он не читает и не печатает workbook cells или raw response files.

## 11. Обязательный deliverable: промпт совместной live-проверки

После успешного code review и offline validation создай файл:

`specs/001-read-only-catalog-qualification/verification/live-mvp-verification-prompt.md`

Это должен быть самостоятельный русскоязычный промпт для первоначальной или повторной
совместной live-проверки. Implementation agent создаёт его до запроса о запуске стенда
и может исполнить его в текущем flow после явного подтверждения пользователя. Промпт
обязан:

1. сначала проверить current feature, build/test evidence и отсутствие незавершённых
   implementation blockers;
2. попросить пользователя запустить локальный BPMSoft и дождаться явного подтверждения
   «стенд запущен» до любого сетевого обращения;
3. объяснить пользователю точную команду и ожидаемые interactive prompts;
4. не просить присылать credentials в чат и не принимать их через args/config/file;
5. открыть локальный terminal и дать пользователю самому ввести URL/login/password;
6. выполнить ровно один вручную допущенный автоматизированный read-only integration run;
7. при blocker остановиться, показать safe diagnosis и не делать retry автоматически;
8. проверить captured endpoint matrix и zero write calls;
9. сверить Pass A/Pass B counts, ordered IDs, hashes, unsupported list, duration, response
   sizes и workbook-scale result;
10. проверить наличие новой уникальной Excel-пары, её OOXML/read-back validation,
    manifests, sheet/header counts и 1:1 projection summary;
11. по возможности открыть обе книги пользователю для визуального просмотра, не
    редактируя их и не выполняя browser/Apply/Git actions;
12. показать только безопасные агрегаты и пути; не выводить содержимое lookup cells в
    chat/evidence;
13. закончить совместным решением пользователя: принять результат, зафиксировать
    ограниченный blocker list либо сформировать перечень исправлений. Агент не принимает
    acceptance за пользователя.

Само создание этого промпта не запускает сеть автоматически. После его подготовки и
предъявления успешного offline evidence agent обязан спросить пользователя о запуске
локального стенда. Ответ пользователя, подтверждающий запуск стенда и продолжение
проверки, разрешает немедленно выполнить opt-in live integration suite в текущем flow.
Подключение к реальному BPMSoft не только допустимо, но и является обязательным
условием закрытия `FULL_CATALOG_NOT_QUALIFIED` и проверки MVP. Не требуй дополнительный
`AuthorizationReference` и не ссылайся на общий запрет live-доступа.

## Stop conditions

Немедленно остановись и верни безопасный blocker, если:

- active feature не 001;
- обязательный class gate/hook не прошёл;
- для реализации требуется изменить immutable source draft или prototype artifact;
- невозможно доказать read-only endpoint/body contract;
- target требует redirect/alternate host или неподтверждённый auth flow;
- полный inventory/lookup невозможно прочитать без скрытого пропуска;
- два прохода расходятся;
- scanner/schema validation не прошли;
- Excel projection неполна, обрезана или не проходит read-back;
- требуется Write/Manage/Compare/Apply/browser write/Git mutation;
- production path или обязательные offline checks ещё не завершены, а для продолжения
  уже требуются реальные credentials/live target. После подтверждённой технической
  готовности это больше не blocker: переходи к отдельному ручному `T046` только по
  прямой текущей просьбе пользователя.

Не ослабляй gate, не создавай ложный PASS и не заменяй blocker предупреждением.

## Критерии готовности MVP

MVP завершён только когда одновременно выполнено всё следующее:

- `BpmSoftSync.Cli` имеет реально собранный и offline-проверенный opt-in live integration
  path;
- fake-handler E2E доказывает login -> full inventory/schema/lookup pull -> ровно два
  прохода -> reconciliation -> Excel pair -> safe evidence;
- ни одна доступная workspace identity или lookup row/value не пропадает молча;
- две книги создаются в новом уникальном run output, проходят exact projection и
  OOXML/read-back проверки;
- значения справочников присутствуют в `LookupValues`, но отсутствуют в audit/evidence,
  CLI diagnostics и test logs;
- все старые и новые tests проходят в Release;
- mutable Feature 001 artifacts и task statuses соответствуют фактическому evidence;
- текущий `HANDOFF.md` переписан как один актуальный concise snapshot, а superseded
  handoff при необходимости архивирован по правилам `AGENTS.md`;
- создан, проверен и передан пользователю отдельный
  `verification/live-mvp-verification-prompt.md`;
- после offline PASS агент явно попросил пользователя запустить локальный стенд, а до
  подтверждения пользователя сетевых обращений не было;
- после подтверждения пользователя выполнен ровно один автоматизированный opt-in live
  integration run с terminal-only вводом credentials и `writeCallCount = 0`;
- live run создал и проверил отдельную Excel-пару на реальных данных локального стенда;
- `FULL_CATALOG_NOT_QUALIFIED` закрывается только успешным совместным live run и решением
  пользователя; при blocker остаётся открытым;
- `INDEX_SYNC_UNRESOLVED` и все запреты Write/Manage/Compare/Apply/browser/Git остаются.

## Финальный отчёт implementation agent

В финале на русском сообщи:

1. что реализовано и какие возможности сохранены;
2. какие mutable requirements/tasks были уточнены;
3. точный CLI contract;
4. полный список изменённых production/test/docs/template файлов;
5. команды и результаты Release build, offline tests и opt-in live integration test;
6. где лежит safe offline evidence;
7. где лежат созданные fake-data и live-data Excel-пары;
8. где лежит `live-mvp-verification-prompt.md`;
9. какие blockers остались;
10. явное подтверждение, что до прохождения implementation/offline gates реальные
    HTTP/credentials/live BPMSoft не использовались, затем пользователь подтвердил
    запуск стенда, credentials вводились только в terminal, а live test выполнил только
    allowlisted read calls и создал проверенную Excel-пару; если пользователь не смог
    запустить стенд, явно сообщи, что code-ready состояние достигнуто, но live verification
    и `FULL_CATALOG_NOT_QUALIFIED` остаются незавершёнными.
