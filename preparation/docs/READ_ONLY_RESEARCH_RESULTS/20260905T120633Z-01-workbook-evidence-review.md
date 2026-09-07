# 01 Workbook evidence review — независимая offline-проверка template v2

**Дата проверки (UTC):** 2026-09-05  
**Роль:** `01-workbook-evidence-reviewer`  
**Итог:** **FAIL — REQUEST CHANGES**  
**Scope:** `VerifiedBoundedBaseline`; это не full catalog и не разрешение на BPMSoft write/load/apply  
**Неизменный blocker будущей загрузки индексов:** `INDEX_SYNC_UNRESOLVED`

## 1. Решение

Известные исправления template v2 подтверждены: обе canonical книги имеют ожидаемую структуру, `LookupRows` удалён, шесть исходных value rows без потерь объединены в `LookupValues`, Excel 16.0 открывает обе книги read-only без recovery, изменение ширины, реальный фильтр и сортировка работают при сохранённой защите. Release build, `self-test`, `verify`, обязательный Tier 1, OOXML closure/order, styles/types, pair/hash/secret checks и index projection прошли.

Однако пара не соответствует утверждённому `WORKBOOK_CONTRACT_VISION.md`, раздел 5, по workbook validation. На `LookupValues` колонки `ReferenceRecordId` (`M`) и `ReferenceDraftRowToken` (`N`) разблокированы для пользовательского ввода, но на них отсутствуют data validation rules. Во всей паре существуют только list-validations; custom validation отсутствует. Поэтому Excel не проверяет:

- UUID syntax для редактируемого `ReferenceRecordId`;
- взаимное исключение `ReferenceRecordId` / `ReferenceDraftRowToken`.

На временной копии Excel принял `M2=not-a-guid`, а затем одновременно заполненные `M2=11111111-1111-1111-1111-111111111111` и `N2=draft-local-1`. Это точный contract defect и blocker для G5 acceptance. Никаких исправлений не вносилось.

## 2. Границы и источники

Проверка выполнена строго offline. Не выполнялись обращения к BPMSoft, Google input, write/Manage/compile/save/create/update/delete, workbook generate, index load/apply, изменения книг, кода, contract, SDD или handoff. Canonical книги открывались через Excel только с `ReadOnly=true`; presentation/input checks выполнялись на временных копиях и закрывались с `SaveChanges=false`.

Прочитаны полностью role prompt, инструкции `minimax-xlsx` для READ/VALIDATE, `PROJECT_HANDOFF.md` раздел 14, delivery result, contract, `Program.cs` и `.csproj`. Также проверены bounded source, source manifest, ActualIndexed/Indexes evidence, run journal и audit lineage.

## 3. SHA-256 проверенных файлов

| Файл | Bytes | SHA-256 |
|---|---:|---|
| `docs/WORKBOOK_CONTINUATION_PROMPTS/01-workbook-evidence-reviewer.md` | 3,584 | `7718c54735840b4f0ce38eb019c36135da0d6f698b73247c8fb3806d9b9fa54b` |
| `docs/PROJECT_HANDOFF.md` | 83,188 | `27ef0372e7468cb902bae0d7ea911eabb97f5de93713b4b80880c319b8572b14` |
| `docs/READ_ONLY_RESEARCH_RESULTS/20260904-04-workbook-delivery-result.md` | 16,679 | `57abfe8b3720d8c704d295361826ca60592610b2db78ab9fe990a3b9d98612a3` |
| `docs/WORKBOOK_CONTRACT_VISION.md` | 34,539 | `7009ea98cf6587838ef7a1beba1b54f86d838381f46c994883966bfc1bd4b5d3` |
| `WorkbookDeliveryTool/Program.cs` | 105,420 | `0ae8350838f5db0ba60f36acef76858d68f23835a7b28cc214cc47262bbbc835` |
| `WorkbookDeliveryTool/BpmSoftWorkbookDelivery.csproj` | 352 | `9352356a7456122d4ed4185480c1e00be32b2ed36c24ae37036ebd05a4617072` |
| bounded baseline JSON | 67,434 | `b87e6e6d3e82ead7c9724cb7735a428740200c44fa06dced96b43772fc97a562` |
| ActualIndexed/Indexes check JSON | 22,389 | `44429f44ccf18d27304e4ef1a1b257a658e03f192972153cd9d44a126853b251` |
| source manifest JSON | 732 | `9440b1137ba76e9632747abf8e5f989b8807e6ab6d9c936ac4befd6eaabd3a0d` |
| `workbooks/BPMSoft.ModelCatalog.xlsx` | 29,584 | `e42ccd1187c744c846805ad6c4ee59d1b75a50c1bb19a252f45f0bd534129ca0` |
| `workbooks/BPMSoft.LookupCatalog.xlsx` | 9,326 | `9ebe5369cef454a6e0cdfb135da9c5ac862af0950c2cf7dbc976c9ccebc1b2e1` |
| canonical v2 audit | 2,101 | `f2b4802c9bce57d3183e4a7025eff990720a1df59056401d0b73eda5dd0d5dc5` |

Source manifest independently reproduced both listed byte counts and hashes: **PASS**.

Validation tool inputs used in this review:

| Tool | SHA-256 |
|---|---|
| `formula_check.py` | `94661b6ebbfef0450b762392b45477678a7cd641982e0eb9397fef7bcb716e15` |
| `style_audit.py` | `4ac17193e5ad306cb89007a7fe8492b9117de0d4b0136a3ffaa65223e60abcb6` |
| `xlsx_reader.py` | `585071399f9e5683700081db959812bcce02edb694bb5a7dece94d9b89a5e1f0` |
| `libreoffice_recalc.py` | `a7967a78aff2f8f74f4975db2f674215a4dce1086c38b5312e353f7bad084f87` |

## 4. Reproduction commands и результаты

Рабочая директория: `C:\CodingAgents\codex\projects\OM_Automatization\preparation`. Runtime: .NET SDK `10.0.102`, Python `3.12.10`, desktop Excel `16.0`.

Canonical source был скопирован без изменений во временный каталог, чтобы clean build не менял `bin/obj` workspace:

```powershell
dotnet build <temporary-copy>\BpmSoftWorkbookDelivery.csproj -c Release --nologo
dotnet <temporary-copy>\bin\Release\net10.0\BpmSoftWorkbookDelivery.dll self-test
dotnet <temporary-copy>\bin\Release\net10.0\BpmSoftWorkbookDelivery.dll verify `
  --source .\WorkbookDeliveryTool\runs\2026\09\04\80fac4ee-e388-4f25-adf0-a8305950c3c6\evidence\20260904T212051Z-bounded-baseline.json `
  --model .\workbooks\BPMSoft.ModelCatalog.xlsx `
  --lookup .\workbooks\BPMSoft.LookupCatalog.xlsx

python <minimax-xlsx>\scripts\formula_check.py <book.xlsx> --json
python <minimax-xlsx>\scripts\style_audit.py <book.xlsx> --json
python <minimax-xlsx>\scripts\xlsx_reader.py <book.xlsx> --json
python <minimax-xlsx>\scripts\xlsx_reader.py <book.xlsx> --quality --json
python <minimax-xlsx>\scripts\libreoffice_recalc.py --check
```

| Проверка | Результат | Evidence |
|---|---|---|
| Clean Release build | **PASS** | exit 0; 0 warnings, 0 errors |
| `self-test` | **PASS** | generation/read-back, reproducibility, pair/schema guards, unknown-column/external/formula blockers, ordering, inherited index join, ActualIndexed separation |
| `verify` | **PASS** | Model 8 sheets/165 data rows; Lookup 6/36; hashes and canonical worksheet hashes match audit |
| Tier 1 Model | **PASS** | 0 formulas, 0 shared ranges, 0 errors |
| Tier 1 Lookup | **PASS** | 0 formulas, 0 shared ranges, 0 errors |
| Style audit Model | **PASS** | 2,177 cells; 0 violations, 0 warnings |
| Style audit Lookup | **PASS** | 282 cells; 0 violations, 0 warnings |
| Reader/quality | **PASS WITH EXPECTED WARNINGS** | blank desired/draft/reference/value fields and sparse `ValidationLists`; no formula-cache interpretation |
| LibreOffice Tier 2 | **SKIPPED** | `libreoffice_recalc.py --check` exit 2: LibreOffice not available |
| Automated LibreOffice render | **NOT RUN** | LibreOffice unavailable |

Первая попытка изолировать intermediate output через `BaseIntermediateOutputPath` была исключена из результата: изменение стандартного MSBuild exclude path привело к повторному включению существующего `obj` и duplicate assembly attributes. Это дефект метода запуска, а не проекта. Повторный clean build неизменённой временной копии прошёл с 0 warnings/errors.

## 5. Structure, rows и source projection

### Model Catalog

Точный порядок листов: `Readme`, `Manifest`, `WorkspaceInventory`, `Schemas`, `Columns`, `Indexes`, `ValidationLists`, `PullConflicts` — **PASS**.

Data rows: `9 / 10 / 5 / 5 / 124 / 2 / 10 / 0` соответственно; всего 165 — **PASS**. Заголовки каждого листа дословно совпали с contract/tool expectation.

### Lookup Catalog

Точный порядок листов: `Readme`, `Manifest`, `LookupRegistry`, `LookupValues`, `ValidationLists`, `PullConflicts` — **PASS**. `LookupRows` отсутствует — **PASS**.

Data rows: `9 / 10 / 1 / 6 / 10 / 0`; всего 36 — **PASS**. `LookupValues` содержит ровно 15 утверждённых полей в утверждённом порядке.

Bounded source содержит 3 metadata rows и 6 values. Независимый OOXML read-back доказал:

- 3 группы `(ActivityPriority, RecordId)`, по 2 value rows каждая;
- в каждой группе ровно `Description` и `Name`;
- `SysEntitySchemaUId`, `DesiredState`, `ServerPresence`, `SourceFingerprint`, `Comment` единообразны внутри группы;
- source row fingerprint и fingerprints обеих value rows совпадают;
- для каждой строки заполнен ровно один из `RecordId`/`DraftRowToken`;
- все 15 значений каждой строки дословно равны projection из source, включая `Средний`, `Низкий`, `Высокий` и `EmptyString`.

Результат merged layout/source preservation: **PASS**.

## 6. OOXML, protection, styles и native types

Для обеих книг независимо проверено:

- ZIP/XML parse и internal relationship target closure: **PASS**, missing targets 0;
- `[Content_Types].xml` closure: **PASS**, missing parts 0;
- workbook window attributes: `xWindow=0`, `yWindow=0`, `windowWidth=20140`, `windowHeight=10960`;
- `ValidationLists` — единственный `veryHidden` лист;
- каждый лист имеет frozen header (`ySplit=1`, `topLeftCell=A2`) и существующий `autoFilter` полного диапазона;
- worksheet child order `sheetProtection -> autoFilter -> dataValidations` (если validations есть): **PASS**;
- protection сохраняется; `formatColumns=0`, `autoFilter=0`, `sort=0`, `selectLockedCells=0`, `selectUnlockedCells=0`, то есть разрешены требуемые действия;
- style 13 имеет `locked=0`; exact locked/unlocked map совпадает с generator contract для текущих existing rows;
- `Columns.ActualRequired`/`ActualIndexed` и `Indexes.IsUnique` сохранены как native Boolean; `Indexes.Ordinal` — native numeric integer;
- формулы, external relationships, externalLinks, VBA и connections: 0.

## 7. Desktop Excel 16.0

Canonical книги открыты отдельным COM instance с `ReadOnly=true`, `Saved=true`, `DisplayAlerts=false`, затем закрыты без сохранения:

| Проверка | Model | Lookup |
|---|---|---|
| Open without recovery/error | **PASS** | **PASS** |
| Expected sheets/data | 8 sheets, `Readme!A1=Topic` | 6 sheets, `Readme!A1=Topic` |
| `Protection.AllowFiltering` | `True` | `True` |
| `AllowFormattingColumns` | `True` | `True` |
| `AllowSorting` | `True` | `True` |
| Column width change on temp copy | 13.29 -> 14.29, **PASS** | 13.29 -> 14.29, **PASS** |
| Locked cell write | blocked as expected | blocked as expected |
| Editable cell write | allowed as expected | allowed as expected |
| Existing filter, real Excel context-menu command | `Schemas!A2=Account`: 3/5 visible, **PASS** | `LookupValues!I2=Description`: 3/6 visible, **PASS** |
| COM sort on protected sheet | A descending changed and matched expected order, **PASS** | RecordId descending changed and matched expected order, **PASS** |

`Range.AutoFilter(...)` как объектный toggle/criteria method ожидаемо блокируется на защищённом листе и не использован как acceptance evidence. Фильтр подтверждён реальной встроенной Excel context-menu командой `Фильтр по значению выделенной ячейки` (control id 12232), которая изменила `FilterMode` и число видимых строк.

Owner manual visual/usability acceptance именно template v2 остаётся **NOT RUN**; эта reviewer-проверка не закрывает G5 от имени владельца.

## 8. Pair metadata, hashes и secret scan

Оба `Manifest` имеют одинаковые 10 keys и значения:

- `ContractVersion=1`;
- `PairId=cb3722e8-ee9e-4f47-a299-723769ce7bbf`;
- `PullRunId=80fac4ee-e388-4f25-adf0-a8305950c3c6`;
- `PairBaselineHash=f2480cdbdaa036856ebedbbbae45fe6dd55b919a4156bb6af78833a602e4f65a`;
- `TemplateVersion=2-bounded-research`;
- одинаковые `TargetAlias`, timestamps и `BaselineTargetFingerprint`.

Canonical workbook hashes совпали с handoff/final audit. Credential/secret marker scan bounded source и всех XML parts обеих книг дал 0 hits для password/userpassword/BPMCSRF/authorization/bearer/API-key/client-secret/access-token/refresh-token patterns.

Результат: **PASS**.

## 9. ActualIndexed / Indexes

Source facts: 124 column representations; `ActualIndexed=true` — 64; index members — 2. Evidence matrix имеет 65 relevant rows: 1 informative agreement и 64 separation-required cases. В фактическом source `Account/Test1.Code` имеет `ActualIndexed=false` при одном index membership, а `Name` — `ActualIndexed=true` при одном membership.

Лист `Indexes` — точная projection `schema.indexes[].columns[].columnUId`, не projection флага `ActualIndexed`:

| Index | Unique | Column | ColumnUId | Ordinal |
|---|---|---|---|---:|
| `Index1` | `TRUE` | `Code` | `60cc5643-4ee2-4adf-b76b-06000ad0b067` | 0 |
| `Index2` | `FALSE` | `Name` | `7c81a01e-f59b-47df-830c-8e830f1bf889` | 0 |

Независимое source-vs-OOXML tuple comparison: **PASS**. `self-test` повторно прошёл отрицательные fixtures:

- `ActualIndexed=false` + membership present не теряет index;
- `ActualIndexed=true` + membership absent не фабрикует index;
- member `.uId` вместо `columnUId` rejected;
- unresolved/ambiguous ColumnUId relation блокируется validation logic.

Это не доказательство безопасной будущей загрузки. Статус сохраняется: **`INDEX_SYNC_UNRESOLVED`**.

## 10. Defect evidence — request changes

Contract, раздел 5, требует, чтобы workbook validation ловила UUID syntax и взаимное исключение `ReferenceRecordId` / `ReferenceDraftRowToken`. Реализованный editable map в `Program.cs` разблокирует `LookupValues` columns M/N (`EditableColumns` indices 12/13), но `CreateValidations` создаёт правила только для известных list headers.

Фактический OOXML `LookupValues` содержит ровно четыре validation rule:

| Range | Type | Formula |
|---|---|---|
| `E2:E500` | list | `DesiredState` |
| `F2:F500` | list | `ServerPresence` |
| `J2:J500` | list | `ValueState` |
| `L2:L500` | list | `ValueKind` |

`M2:M500` и `N2:N500` validation отсутствуют. В Model книге все 8 validations и в Lookup книге все 6 validations имеют `type=list`; `type=custom` отсутствует. При этом `M2` и `N2` имеют unlocked style 13.

Disposition: **не исправлено; request changes**. После исправления требуется заново сгенерировать пару из того же immutable source и повторить весь набор проверок, особенно Excel ввод invalid GUID/both-reference fixture, build/self-test/verify, Tier 1, hashes и canonical audit.

## 11. Incident lineage

| Этап | SHA-256 audit/evidence | Статус |
|---|---|---|
| Initial delivery audit `20260904T212706Z` | `8a5ea0c3fe0f66af6e4caa3cb964b73aac27ac3522dc07a0728d163f98cf2e71` | superseded; исходные workbook были Excel-invalid |
| Excel recovery log | `df7d10cb643c7767925851a1a20672b9d6c7124c48ef4c8dd7db05a449f26dbc` | подтверждает replacement worksheet parts |
| Repair audit `20260905T062211Z` | `7a241dad2921b6a2787c83f958bc689e86f92c14e7728666ce5b4ada35a17279` | OOXML order/window repair passed; template v1 later superseded |
| Template v2 pending audit `20260905T114012Z` | `1da4af2d3f6d931b6bca071c1cd2ead95b5b09363f8d16391688896470dae32f` | usability/merged layout passed before canonical replacement |
| Template v2 canonical audit `20260905T114516Z` | `f2b4802c9bce57d3183e4a7025eff990720a1df59056401d0b73eda5dd0d5dc5` | canonical pair installed; independently reproduced except validation gap found here |

Rejected invalid pair hashes: Model `62e5892111e1d31cf0b0472867676eee79bce29181dfe3b2dff3b56b2c3f0b3e`, Lookup `9f2378c637629bc941e9292b93a49523e3584eca450cb4d9ce6dbf4e113addc1`. Superseded template-v1 hashes: Model `ab871d83069222ce9de76f4aaad63db48bbe7c18d8b70cc9a1341f170962f9e5`, Lookup `19a6713a49909d9736c76a0db827b254676da2987297d26feaf1ef6d9ab51e58`.

## 12. Gaps / not run

- LibreOffice Tier 2 and render: **SKIPPED/NOT RUN**, tool unavailable.
- Owner manual visual/usability acceptance template v2: **NOT RUN**.
- Full-catalog scale/generalisation, composite/auto-name/order semantics, inherited `column.indexed` semantics: **NOT RUN / unresolved**.
- BPMSoft mutation/write/Manage, DraftRowToken-to-RecordId create/read-back, index add/drop/load/apply: **NOT RUN and not authorised**.
- G5 owner decision: **NOT TAKEN** by this reviewer.

## 13. Final recommendation

**FAIL — REQUEST CHANGES.** Не передавать текущую canonical пару как G5-acceptable до добавления и доказательства contract-required validation для редактируемых reference identity fields. Остальные подтверждённые свойства template v2 сохраняют статус PASS; future index loading остаётся заблокированным `INDEX_SYNC_UNRESOLVED`.
