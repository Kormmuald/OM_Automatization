# Рабочее видение workbook contract v1

Статус: **logical Excel contract v1 утверждён владельцем 2026-09-03; это не инструкция на реализацию и не разрешение на write**.  
Основание: `PROJECT_HANDOFF.md`, включая принятые решения о scope, безопасности и идентификаторах.  
Граница: этот документ не разрешает создавать `.xlsx`, выполнять write API или менять `SyncOM/`.

## 1. Зафиксированный вход

- Первая выгрузка охватывает весь доступный каталог намеренно чистого локального BPMSoft-стенда.
- Google-книги — только источник прежнего пользовательского формата. Их значения не являются desired state нового стенда.
- Порядок работы остаётся: `read/validate -> strictly read-only compare -> immutable plan -> whole-plan approval -> explicit staged apply -> read-back -> optional workbook-only writeback -> audit`.
- Удаления схем, колонок и строк не входят в MVP. `Removed` исключает элемент из push; pull не выставляет его автоматически.
- Нет внешних формул между файлами, `IMPORTRANGE`, Google API или Apps Script.
- Existing configuration metadata связывается по BPMSoft `UId`; записи данных — по их server `Id`. `SysSchema.Id` — наблюдаемая metadata, а не relation key contract.
- GUID новых схем, колонок, записей `Lookup` и lookup rows создаёт BPMSoft. До успешного read-back соответствующие server ID пусты.
- `DraftRowToken` — локальный корреляционный маркер новой lookup-строки; это не BPMSoft ID и не server payload.

## 2. Принято: две физические книги как одна логическая пара

Принято сохранить исторически понятное разделение, сделав его явным и детерминированным:

```text
BPMSoft.ModelCatalog.xlsx      — inventory и configuration metadata
BPMSoft.LookupCatalog.xlsx     — registry справочников и lookup data
                │
                └── одна логическая пара: PairId + PullRunId + baseline fingerprints
```

Это лучше одной книги для первого полного каталога по трём причинам:

1. Metadata ОМ и потенциально крупные lookup rows имеют разные объёмы, частоту редактирования и проверки.
2. Снимки, фильтры и пользовательская навигация по справочникам не раздувают рабочую книгу ОМ.
3. Нет потребности в cross-workbook formulas: синхронизатор валидирует explicit IDs, а не cached Excel values.

Операционное следствие: compare всегда открывает и валидирует пару. Нельзя смешать книгу ОМ из одного pull с книгой справочников из другого.

### 2.1. Общие control-правила пары

В обеих книгах присутствует защищённый лист `Manifest` со следующими derived полями:

| Поле | Значение |
|---|---|
| `ContractVersion` | `1` |
| `PairId` | неизменяемый UUID логической пары |
| `PullRunId` | UUID конкретного successful pull; одинаков в обеих книгах |
| `TargetAlias` | несекретное имя/URL target, без credentials |
| `ScopeMode` | `AllReadableCatalog` |
| `PullStartedUtc`, `PullCompletedUtc` | время исходной выгрузки |
| `PairBaselineHash` | SHA-256 канонического server-origin baseline обеих книг, без self-referential manifest fields и snapshot sheets |
| `BaselineTargetFingerprint` | отпечаток стенда на момент pull |
| `TemplateVersion` | версия layout/validation contract |

`CurrentContentHash`, `TemplateFingerprint` и fingerprint затронутого live state вычисляются при compare и повторно перед apply. Они входят в immutable plan/audit, но не записываются как полный file hash внутрь самой книги.

Защита листов применяется только к полностью read-only листам. Листы со смешанными derived и пользовательскими полями (`Schemas`, `Columns`, `LookupRegistry`, `LookupValues`) не защищаются на уровне Excel и доступны для редактирования целиком; безопасность обеспечивается обязательной parser/compare validation, которая блокирует недопустимые изменения до формирования plan. На защищённых read-only листах пользователь может менять ширину колонок и применять существующие фильтры. Фактическая ручная сортировка locked ranges не является требованием: Excel 16 блокирует перестановку защищённых ячеек даже при `AllowSorting=True`. Наличие этого permission flag допустимо как best-effort metadata, но не считается evidence сортируемости.

## 3. Принятый состав листов: Model Catalog

Файл: `BPMSoft.ModelCatalog.xlsx`.

| Лист | Роль | Статус |
|---|---|---|
| `Readme` | Краткие правила, легенда editable/derived, версия contract | derived, protected; принято |
| `Manifest` | Идентичность пары и baseline | derived, protected; принято |
| `WorkspaceInventory` | Все доступные workspace items, включая unsupported | derived, filterable; принято |
| `Schemas` | Реестр поддерживаемых EntitySchema и lookup schemas | mixed: desired + derived; принято |
| `Columns` | Own и inherited колонки схем | mixed: desired + derived; принято |
| `Indexes` | Read-only evidence текущих index definitions и их members | derived, protected; принято с bounded P3-C evidence |
| `ValidationLists` | Списки Excel validation | derived, hidden/protected; принято |
| `PullConflicts` | Результат последнего pull-conflict analysis | derived, filterable; принято |
| `S_*` | Единственный полный набор snapshot-копий затронутых рабочих листов непосредственно перед последней попыткой pull | read-only archive; принято |

### 3.1. `WorkspaceInventory`

Назначение — доказать полноту scope и не потерять unsupported items. Рекомендуемые колонки:

```text
WorkspaceItemUId | Name | ItemType | PackageName | PackageUId |
SupportStatus | SupportReason
```

`SupportStatus`: `Structured`, `InventoryOnly`, `Unreadable`, `Unsupported`. В v1 `Structured` получают только `EntitySchema`, включая lookup schemas; только они получают строки в `Schemas`/`Columns`. Для клиентских схем, исходного кода и остальных workspace items сохраняется только этот лёгкий inventory из `GetWorkspaceItems`: их содержимое, детальное состояние и исходный код не читаются, structural или write scope отсутствует.

### 3.2. `Schemas`

Одна строка на поддерживаемую схему, в том числе на схему справочника.

Точный набор колонок: `SchemaName`, `SchemaUId`, `SysSchemaId`, `SchemaKind`, `ParentSchemaName`, `ParentSchemaUId`, `DesiredPackageName`, `ActualPackageName`, `ActualPackageUId`, `DesiredState`, `ServerPresence`, `ActualFingerprint`.

| Колонки | Статус и правило |
|---|---|
| `SchemaName` | required; identity по коду для proposed schema, display/context для existing |
| `SchemaUId` | derived; required у existing, пуст у `Proposed` до read-back |
| `SysSchemaId` | derived observed metadata; не участвует в relations |
| `SchemaKind` | derived: entity/lookup/other supported kind; новая схема выбирает разрешённый kind |
| `CaptionRu`, `CaptionEn` | исключены из v1: имена и заголовки существующих схем не изменяются и не materialize в workbook contract |
| `ParentSchemaName`, `ParentSchemaUId` | name — desired dependency для proposed; UId — derived фактическая связь |
| `DesiredPackageName`, `ActualPackageName`, `ActualPackageUId` | `DesiredPackageName` editable только для `Proposed`; перенос existing схемы между пакетами вне v1 и блокируется |
| `DesiredState` | validation: `Active`, `Proposed`, `Removed` |
| `ServerPresence` | derived: `Present`, `PotentiallyDeleted`, `NotRead` |
| `ActualFingerprint` | derived; входит в conflict/plan validation |

Rename existing `SchemaName`, изменение captions схем, package transfer existing schema и редактирование базовых/унаследованных схем должны давать validation error либо `Unsupported`, а не план изменения.

### 3.3. `Columns`

Точный набор колонок: `SchemaName`, `ParentSchemaUId`, `ColumnName`, `ColumnUId`, `Ownership`, `DataType`, `ReferenceSchemaName`, `ReferenceSchemaUId`, `DesiredRequired`, `ActualRequired`, `DesiredIndexed`, `ActualIndexed`, `DesiredState`, `ServerPresence`, `ActualFingerprint`.

| Колонки | Статус и правило |
|---|---|
| `SchemaName`, `ParentSchemaUId` | принадлежность колонки; UId derived, name — context/dependency для proposed |
| `ColumnName` | required; identity внутри owner schema; не переименовывается в v1 |
| `ColumnUId` | derived; required для existing, пуст до read-back для proposed |
| `Ownership` | derived: `Own`, `Inherited`, `System` |
| `CaptionRu`, `CaptionEn` | исключены из v1: captions колонок не materialize в workbook contract и не изменяются |
| `DataType` | derived/read-only у existing колонки; required editable только у `Proposed` колонки в поддерживаемом наборе типов |
| `ReferenceSchemaName`, `ReferenceSchemaUId` | для Lookup; name — desired dependency до создания, UId — derived actual relation |
| `DesiredRequired`, `ActualRequired` | `DesiredRequired` editable у existing собственной колонки и у `Proposed`, в обе стороны; `ActualRequired` derived/writeback. Изменение входит в plan только после field-level API verification |
| `DesiredIndexed`, `ActualIndexed` | `ActualIndexed` сохраняется отдельным derived/writeback flag для совместимости с историческим Google `bpmColumn.indexed`. Тот механизм был coarse/lossy column flag, не новым source of truth: Google data не импортируются. `ActualIndexed` не является membership в `Indexes` и не выводится автоматически из `schema.indexes[]`. Семантика `column.indexed` у inherited column в текущем package layer не доказана. Во время workbook delivery/pull обязательно проверяется, что раздельное хранение `ActualIndexed` и `Indexes` не теряет и не искажает фактические индексы. Перед любым будущим index load/apply требуется отдельная проверка, что это разделение не создаёт ложный add/drop plan. Любая неоднозначность — blocker `INDEX_SYNC_UNRESOLVED`; владелец может исключить загрузку индексов из текущей версии, оставив только read-only `Indexes`. `DesiredIndexed` остаётся потенциальным editable intent у existing own column и у `Proposed`, но add/drop/write API сейчас не разрешён: `false → true` может стать планируемым только после отдельного field-level write preflight; `true → false` остаётся blocker `INDEX_DROP_NOT_SUPPORTED` и ручной DB-операцией. |
| `DesiredState`, `ServerPresence`, `ActualFingerprint` | как у схем |

Inherited и system columns — обязательный read-only контекст. Они не могут быть случайно изменены только потому, что видны в Excel.

### 3.4. `Indexes`

Одна строка на member индекса; составной индекс представлен несколькими строками с общими `SchemaName`, `SchemaUId`, `IndexUId` и `IndexName`. Лист строго read-only и служит evidence текущего состава индексов; собственных index-операций он не создаёт. `SchemaUId` идентифицирует package layer; package provenance получается через соответствующую строку `Schemas` (`ActualPackageName`/`ActualPackageUId`) и здесь не дублируется.

```text
SchemaName | SchemaUId | IndexUId | IndexName | IsUnique |
ColumnName | ColumnUId | Ordinal | ActualFingerprint
```

`IndexUId` добавлен как protected derived observed identity: bounded P3-C evidence подтвердил `schema.indexes[].uId`. Read mapping строится из `schema.indexes[]`: `IndexName <- .name`, `IsUnique <- .isUnique`, member relation `ColumnUId <- .columns[].columnUId`, а `Ordinal` — zero-based позиция member в `.columns[]`. Member `.columns[].uId` — identity объекта-члена индекса, **не** `ColumnUId` и не substitute для связи с `Columns`.

`ColumnUId` обязан разрешиться в `Columns.ColumnUId` текущего schema/package layer; target column может иметь `Ownership = Own` или `Inherited`. Bounded sample доказал два simple, one-member, non-auto-named indexes и `orderDirection=0` только как observed numeric value. Composite indexes, auto-name, более широкая семантика `orderDirection` и full-catalog generalisation не доказаны; до их evidence pull/tool не должен упрощать или фабриковать index rows. Add/change/drop index API по-прежнему не разрешён: даже simple add может рассматриваться только после отдельного field-level write preflight; изменение/удаление existing и composite indexes вне v1.

## 4. Принятый состав листов: Lookup Catalog

Файл: `BPMSoft.LookupCatalog.xlsx`.

| Лист | Роль | Статус |
|---|---|---|
| `Readme` | Правила данных справочников, `RecordId` и draft rows | derived, protected; принято |
| `Manifest` | Та же logical-pair metadata | derived, protected; принято |
| `LookupRegistry` | Реестр lookup schemas и соответствующих записей системного `Lookup` | mixed: desired + derived; принято |
| `LookupValues` | Metadata записи и нормализованные значения её колонок в одной таблице | mixed; принято |
| `ValidationLists` | Списки и допустимые state/type values | derived, hidden/protected; принято |
| `PullConflicts` | Конфликты последнего pull | derived, filterable; принято |
| `S_*` | Единственный полный набор snapshot-копий непосредственно перед последней попыткой pull | read-only archive; принято |

### 4.1. `LookupRegistry`

Одна строка на lookup schema, а не на Excel sheet.

```text
SchemaName | SysEntitySchemaUId | LookupRecordId |
BaseSchemaName | BaseSchemaUId |
DesiredState | ServerPresence | ActualFingerprint
```

`SysEntitySchemaUId` идентифицирует схему данных справочника. `LookupRecordId` — ID её записи в системном объекте `Lookup`; они не взаимозаменяемы. Для новой схемы и её registry record server IDs пусты до read-back. `DesiredState` здесь относится именно к registry record, а не к жизненному циклу schema: v1 должен покрыть как новую lookup schema, так и регистрацию уже существующей `EntitySchema` как lookup. Package справочника определяется только соответствующей строкой `ModelCatalog.Schemas` и не дублируется здесь. Captions справочника исключены из v1 и не изменяются.

`BaseSchemaName` и `BaseSchemaUId` — read-only context до отдельной проверки API: они не редактируются и не являются fallback identity или самостоятельной командой изменения base schema.

### 4.2. `LookupValues`

Одна нормализованная таблица устраняет дублирование структуры между прежними листами `LookupRows` и `LookupValues`, не требует отдельного worksheet на каждый справочник и поддерживает нестандартные сигнатуры строк. `ModelCatalog.Columns` — единственный source of truth для data shape, типов и допустимых колонок; parser валидирует `LookupValues` напрямую против него после проверки общей pair identity. Отдельные `LookupRows` и `LookupColumns` не создаются.

Точный набор `LookupValues`: `SchemaName`, `SysEntitySchemaUId`, `RecordId`, `DraftRowToken`, `DesiredState`, `ServerPresence`, `SourceFingerprint`, `Comment`, `ColumnName`, `ValueState`, `Value`, `ValueKind`, `ReferenceRecordId`, `ReferenceDraftRowToken`, `CanonicalValue`.

```text
SchemaName | SysEntitySchemaUId | RecordId | DraftRowToken |
DesiredState | ServerPresence | SourceFingerprint | Comment |
ColumnName | ValueState | Value | ValueKind |
ReferenceRecordId | ReferenceDraftRowToken | CanonicalValue
```

Одна запись справочника представлена несколькими строками — по одной на фактическую колонку. Поэтому metadata записи повторяется внутри её группы значений, но существует только в одном worksheet и только в одном наборе колонок. Parser обязан проверить одинаковость `SysEntitySchemaUId`, `DesiredState`, `ServerPresence`, `SourceFingerprint` и `Comment` внутри группы `(SchemaName, RecordId|DraftRowToken)`; противоречивые значения блокируют compare.

Правило identity самой строки не меняется: заполнен **ровно один** из `RecordId` и `DraftRowToken`. Для значения lookup-колонки разрешено одновременно хранить `ReferenceRecordId` и `ReferenceDraftRowToken`. При будущей загрузке применяется детерминированный приоритет: валидный непустой `ReferenceRecordId`; иначе разрешённый через подтверждённый mapping `ReferenceDraftRowToken -> RecordId`; иначе пустая ссылка. Некорректный непустой `ReferenceRecordId` блокирует загрузку и не трактуется как отсутствие GUID. `DraftRowToken` не является BPMSoft GUID и не входит в server payload; без доказуемого mapping создание ссылки на новую строку = `DO NOT START`.

`ValueState`: `Null`, `EmptyString`, `Value`; это отличает SQL `NULL` от пустой строки. `ValueKind` повторяет typed contract (`Text`, `Integer`, `Decimal`, `Boolean`, `Date`, `DateTime`, `Guid`, `LookupReference`). `CanonicalValue` derived и используется для сравнения, а не для пользовательского ввода.

## 5. Editable, derived и validation

- Полностью read-only листы защищены от редактирования. Листы со смешанными полями не защищены на уровне Excel; поэтому любые изменения IDs, fingerprints, actual fields, `ServerPresence`, canonical values и control metadata должны быть обнаружены parser/compare validation и блокировать plan, если contract не разрешает их изменение.
- Новая lookup row создаётся только явной workbook-only командой «добавить draft lookup row»: она выдаёт неизменяемый локальный `DraftRowToken` и создаёт связанные пустые `LookupValues` для допустимых колонок. Compare не изменяет workbook, ручной ввод `DraftRowToken` не допускается.
- Workbook validation помогает с простыми enum/Boolean/value-kind ошибками, но не является security boundary. Parser обязательно проверяет mandatory names, UUID syntax непустых server IDs и взаимное исключение identity-пары `RecordId`/`DraftRowToken`. Для `ReferenceRecordId`/`ReferenceDraftRowToken` взаимное исключение не требуется; применяется зафиксированный приоритет GUID → resolved draft token → пустая ссылка.
- Parser — источник истины. Он проверяет межстрочные и межкнижные связи, uniqueness, inheritance, типы, unsupported mutations, package boundaries и stable dependency graph.
- Для existing schema/column изменение связанной с её server ID пары `SchemaName`/`ColumnName` — blocker `RENAME_NOT_SUPPORTED`: план не создаётся, пользователь переименовывает элемент вручную в BPMSoft. Редактируемые caption/rename-поля не допускаются; их зарезервированные или иные неописанные колонки также дают этот blocker, а не молчаливый mapping.
- Изменение `DataType` existing колонки — blocker `TYPE_CHANGE_NOT_SUPPORTED`: plan не создаётся; пользователь меняет тип вручную в BPMSoft, затем выполняет pull. `DataType` обязателен и редактируем только у `Proposed` колонки.
- Для existing собственной колонки снятие индекса (`ActualIndexed = true`, `DesiredIndexed = false`) — blocker `INDEX_DROP_NOT_SUPPORTED`: plan не создаётся; пользователь снимает индекс вручную через БД. Добавление индекса (`false → true`) допустимо только после field-level API verification.
- Parser получает membership только из `Indexes`, построенного из `schema.indexes[].columns[].columnUId`; он не заменяет эту связь значением `Columns.ActualIndexed`, member `.uId`, `IndexColumn_*` name или позицией в Excel. Для каждого member проверяются `SchemaUId` package layer, `IndexUId`, `ColumnUId`, разрешение Own/Inherited target и zero-based `Ordinal`.
- Внешние Excel links, unspecified columns, формулы в editable tables и сопоставление по позиции запрещены и являются parser blocker; молчаливый mapping невозможен.
- Фильтры и сортировка допустимы как presentation, но canonical parser order задаётся явно и не зависит от текущего Excel sort state.

## 6. Pull, compare, apply и writeback

### Pull

1. Валидировать пару и её current state; непосредственно перед записью pull создать единственный полный набор `S_*` copy-snapshots всех затронутых рабочих листов, включая values, formulas, formatting, validations, hidden state, filters и sorting. Это не история попыток: следующий набор заменяет предыдущий только после успешной записи и проверки нового набора; при сбое подготовки предыдущий набор сохраняется.
2. Получить полный scope со stable ordering и verified pagination.
3. Записать server-wins state в working sheets, включая read-only `Indexes` только из `schema.indexes[]` и доказанных member `columnUId` relations. Локально существующий, но отсутствующий на сервере элемент сохраняется с `ServerPresence = PotentiallyDeleted`; `DesiredState = Removed` автоматически не меняется.
4. Показать конфликтующие ячейки в snapshot и `PullConflicts`. Набор `S_*` хранит только состояние непосредственно перед текущей или последней попыткой pull; история snapshots не накапливается. Конкретные цвет и точные имена `S_*` пока открыты.

### Compare и apply

Compare проверяет обе книги, все pair-control fields, content/template fingerprints и live target state; затем создаёт неизменный read-only plan. Никакая операция apply не пересчитывает diff.

Для new metadata план строит dependency order по `SchemaName`/`ColumnName`, а после server create/read-back заполняет полученные UIds. Для new lookup rows план обязан иметь точный путь `DraftRowToken -> server RecordId`; если response/read-back этого не доказывает, операция создания строки не включается в plan.

Apply остаётся двухэтапным: сначала ОМ и definitions с B0, read-back/compile/verify; затем lookup rows с B1. Успешный apply не означает автоматический writeback.

### Workbook-only writeback

Только после успешного apply и read-back, а также только если повторный content hash совпадает с планом:

- заполняет `SchemaUId`, `ColumnUId`, `LookupRecordId`, `RecordId` и только отдельно доказанные actual Required/Index values; `ActualIndexed` не выводится автоматически из index membership;
- заменяет ссылки на `DraftRowToken` соответствующими server IDs;
- не пишет в BPMSoft;
- сохраняет хэши до/после и cell-level mapping без секретов в audit.

Если книга изменилась, writeback прекращается без merge. Пользователь запускает новый cycle.

## 7. Миграция исторических Google-книг

В первый full-catalog pull миграции нет: данные Google не переносятся и не становятся baseline.

Если позже нужен legacy import, это отдельная явная операция: известные input-колонки отображаются на `Desired*`/`Value`, а diagram, JSON, formulas, `IMPORTRANGE`, недопустимые sheet names, безымянные колонки и ambiguous identities не импортируются автоматически. Legacy `Code`/`Name` могут быть пользовательскими values, но не заменяют server `Id`.

## 8. Открытые решения и риски для совместной проработки

| Тема | Текущее видение | Почему это ещё открыто | Проверка / stop condition |
|---|---|---|---|
| Exact API properties | Проект использует `SchemaUId`, `ColumnUId`, `ReferenceSchemaUId`, `LookupRecordId`, `RecordId` как semantics | Public docs не публикуют точный JSON contract локального 1.8 | Read-only samples репрезентативных схем/lookup; несовпадение = пересобрать mapping, не писать |
| Индексы и advanced column flags | Bounded P3-C доказал read-only `schema.indexes[].uId/name/isUnique/columns[].columnUId` для двух simple members; `Indexes` хранит lossless member rows и `IndexUId`. `ActualIndexed` остаётся отдельным compatibility flag, не membership source. | Не доказаны current inherited `column.indexed`, composite/auto-name/orderDirection semantics, full-catalog generalisation и любая mutation API. | До representative read evidence нельзя упрощать/фабриковать сложные index rows; add/change/drop исключены из plan до dedicated field-level write preflight. `UsageType`, `IsSimpleLookup`, `IntegrityMode` и `CascadeMode` исключены из v1. |
| Разделение `ActualIndexed` / `Indexes` | Read-only membership хранится в `Indexes`; исторический column flag остаётся отдельно. | Нужно доказать, что при pull это не скрывает index membership, а при будущем load/apply не порождает ложные add/drop действия. | Workbook delivery обязана выдать отдельный результат проверки. Перед index load/apply — новый stop gate; при проблеме `INDEX_SYNC_UNRESOLVED` и решение владельца об исключении загрузки индексов из текущей версии. |
| New-row correlation | `DraftRowToken -> RecordId` обязателен | `Code`/`Name` не гарантируют uniqueness | Create response или read-back должны дать однозначный mapping; иначе `DO NOT START` |
| Full-catalog scale | Нормализованные tables вместо листа на lookup | Неизвестны итоговые row count, Excel limits и скорость snapshots | Измерить после approved read-only pull; при лимите — split/export design before implementation |
| Snapshot UX | Full in-workbook snapshots обязательны; хранится ровно один набор состояния непосредственно перед pull | Цвета и точные имена `S_*` не выбраны | Новый набор должен быть полностью записан и проверен до замены прежнего; при ошибке подготовки не начинать pull и сохранить прежний набор |
| Target fingerprint / pagination | Pair baseline + target state required; stale plan не применяется | Exact mechanism qualification двойного чтения, fingerprint и pagination намеренно отложен | Проработать и проверить read-only механизм до реализации full-catalog pull/compare; до этого write scope не открывается |
| Журнал запусков и пакет ошибки | Обязательны вне обеих книг, локально и только с добавлением; секреты и полные lookup values по умолчанию не сохраняются | Не выбраны путь, формат и срок хранения | Выбрать до первого write-capable slice |
| Write semantics | Server IDs только после read-back; registry record может создаваться для `Proposed` lookup schema или existing `EntitySchema` | Manage rights, exact create/save response, возможность регистрации existing schema, compile and atomicity не проверены | До dedicated write preflight — никаких write calls; если read-only API evidence не подтверждает регистрацию existing schema, этот сценарий = `DO NOT START` |

Риски обрабатываются по трём воротам: (1) после утверждения contract — строго read-only проверка API чтения, pagination, полного scope и масштаба; (2) после read-only baseline и до plan — проверка допустимости конкретных операций Required/Index; (3) только перед отдельно согласованной записью — проверка прав, create/save/read-back, `DraftRowToken -> RecordId`, компиляции, поведения при ошибке и журнала. Ни один открытый риск сам по себе не открывает write scope.

## 9. Следующий безопасный шаг

Contract утверждён. Следующий агент не пересматривает его без нового конкретного возражения владельца: он готовит strictly read-only проверку локального BPMSoft 1.8, затем — при безопасном интерактивном вводе credentials — full-catalog pull и проверку созданной пары книг. Exact API mapping, pagination/stable ordering, масштаб snapshots и остальные строки раздела 8 остаются обязательными воротами; ни одна из них не разрешает write.
