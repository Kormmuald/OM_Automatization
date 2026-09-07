# 01 Workbook evidence review v3 — независимая offline-проверка canonical template v3

**Дата проверки (UTC):** 2026-09-05 18:25–18:37  
**Роль:** `01-workbook-evidence-reviewer`, independent rerun after owner correction  
**Итог:** **FAIL — REQUEST CHANGES**  
**Scope:** `VerifiedBoundedBaseline`; не full catalog, не G5 acceptance и не разрешение BPMSoft write/load/apply  
**Неизменный blocker будущей загрузки индексов:** `INDEX_SYNC_UNRESOLVED`

## 1. Решение

Owner correction учтён как новый источник истины. Одновременное заполнение `ReferenceRecordId` и `ReferenceDraftRowToken` допустимо. Mixed-листы `Schemas`, `Columns`, `LookupRegistry`, `LookupValues` должны быть unprotected и целиком редактируемы. Будущий приоритет `valid non-empty ReferenceRecordId -> unambiguously resolved ReferenceDraftRowToken -> empty reference`, включая blocker для invalid non-empty GUID, является contract будущего parser/loader; loader в bounded workbook delivery отсутствует и не объявляется реализованным.

Canonical template v3 прошёл build, `self-test`, tool `verify`, Tier 1, OOXML closure/order, source projection, audit/hash/secret/index, Excel open/read-only, mixed-sheet editability, read-only write block, resize, filter и simultaneous reference-field input.

Обнаружен один воспроизводимый defect относительно утверждённого contract: **защищённые read-only листы фактически не сортируются в desktop Excel 16.0**. OOXML и Excel object model показывают `AllowSorting=True`, но `Range.Sort` на `WorkspaceInventory` и `Readme` блокируется Excel сообщением о защищённом листе, поскольку сортируемые cells locked. Встроенная Excel-команда `Sort Descending` (`CommandBars` control id `211`) enabled, но не меняет порядок. Контроль тем же `Range.Sort` на unprotected mixed-листах `Schemas` и `LookupValues` реально меняет порядок, поэтому это не дефект sort harness.

Из-за обязательного требования реальной sort operation verdict: **FAIL — REQUEST CHANGES**. Ничего не исправлялось.

## 2. Границы и источники

Проверка выполнена строго offline из:

`C:\CodingAgents\codex\projects\OM_Automatization\preparation`

Полностью прочитаны:

- `docs/PROJECT_HANDOFF.md`, разделы 14 и 15;
- `docs/READ_ONLY_RESEARCH_RESULTS/20260905-owner-workbook-editability-reference-precedence.md`;
- `docs/READ_ONLY_RESEARCH_RESULTS/20260905T120633Z-01-workbook-evidence-review.md`;
- `docs/READ_ONLY_RESEARCH_RESULTS/20260904-04-workbook-delivery-result.md`;
- `docs/WORKBOOK_CONTRACT_VISION.md`;
- все три `docs/SDD_DRAFTS/*.draft.md`;
- `WorkbookDeliveryTool/Program.cs` и `WorkbookDeliveryTool/BpmSoftWorkbookDelivery.csproj`;
- `docs/WORKBOOK_CONTINUATION_PROMPTS/01-workbook-evidence-reviewer.md`;
- `minimax-xlsx/SKILL.md`, `references/read-analyze.md`, `references/validate.md`.

Не выполнялись обращения к BPMSoft, Google input, `capture`, generate в canonical paths, Write/Manage/compile/save/create/update/delete, index load/apply либо изменения source/code/contract/SDD/handoff/workbooks/audit. Canonical workbooks открывались Excel с `ReadOnly=true`. Input/presentation operations выполнялись на временных копиях, закрытых с `SaveChanges=false`; временные каталоги после проверки удалены. Единственный созданный workspace-файл — этот отчёт.

## 3. Проверенные exact paths и SHA-256

| Файл | Bytes | SHA-256 |
|---|---:|---|
| `docs/PROJECT_HANDOFF.md` | 86,141 | `11adac68db8cd636659d8f05d9dd8766e1be1072d40087fa885a8298edac33ca` |
| owner correction | 1,781 | `ba282e81d2217bee88759dd0ddebdae8b8809d9088bcff3d2b18823d116d868e` |
| prior v2 reviewer report | 18,646 | `23478251bfc21b2ccb6ed761ac83916e8ee687557a18b4010ecfc0eee826f4fe` |
| delivery result | 19,088 | `2e0b9d02a23ab6812c64db76eae5393bebfd8abba270de0112284d09124199e0` |
| `docs/WORKBOOK_CONTRACT_VISION.md` | 35,693 | `9bce4f45cf5b7535b9e52965a4c5d93ce1a28d411643e793def92c8cd6bdc3bf` |
| `docs/SDD_DRAFTS/clarify-review.draft.md` | 15,988 | `bd4d7d141bcd6c22883a6f0e3f225f41e6ce1a05553cfb768a5f60b659d8f418` |
| `docs/SDD_DRAFTS/constitution.draft.md` | 14,655 | `603c9f3abd7217d14ac57490cbe7c9a9f37da1671dc530c77faafd93f00d189f` |
| `docs/SDD_DRAFTS/first-feature-spec.draft.md` | 18,998 | `1ed18c5fc40359a00b0c7ffc994ea0db00495e0eee1ee12b685223f46d5e283b` |
| reviewer prompt | 3,584 | `7718c54735840b4f0ce38eb019c36135da0d6f698b73247c8fb3806d9b9fa54b` |
| `WorkbookDeliveryTool/Program.cs` | 105,830 | `e27410392bb6400ef8c0113b45c2b50c57f6f7380c899ef89129551713c62f7b` |
| `.csproj` | 352 | `9352356a7456122d4ed4185480c1e00be32b2ed36c24ae37036ebd05a4617072` |
| bounded baseline JSON | 67,434 | `b87e6e6d3e82ead7c9724cb7735a428740200c44fa06dced96b43772fc97a562` |
| ActualIndexed/Indexes evidence | 22,389 | `44429f44ccf18d27304e4ef1a1b257a658e03f192972153cd9d44a126853b251` |
| source manifest | 732 | `9440b1137ba76e9632747abf8e5f989b8807e6ab6d9c936ac4befd6eaabd3a0d` |
| run journal | 943 | `a3fd1502cf3976700852945a5ee683d1958e0301aa959ea9f897789e9257d071` |
| template-v3 canonical audit | 2,991 | `c6e7a097eab007d9b4b7a1bd0fcd353e53f57673ce170cf362729ad1f0aff613` |
| `workbooks/BPMSoft.ModelCatalog.xlsx` | 29,437 | `7fb01fd5f4af039beb45e7e77819385d9c25beac6762616cf6212e6ac32a849f` |
| `workbooks/BPMSoft.LookupCatalog.xlsx` | 9,203 | `48719140d6ecf4a7b21c61fc381eccb8aa95f2b8e32f437c360ed14e96f07b2d` |

Validation tools:

| Tool | SHA-256 |
|---|---|
| `formula_check.py` | `94661b6ebbfef0450b762392b45477678a7cd641982e0eb9397fef7bcb716e15` |
| `style_audit.py` | `4ac17193e5ad306cb89007a7fe8492b9117de0d4b0136a3ffaa65223e60abcb6` |
| `xlsx_reader.py` | `585071399f9e5683700081db959812bcce02edb694bb5a7dece94d9b89a5e1f0` |
| `libreoffice_recalc.py` | `a7967a78aff2f8f74f4975db2f674215a4dce1086c38b5312e353f7bad084f87` |

Source manifest был независимо воспроизведён: byte counts и SHA-256 обоих listed evidence files совпали — **PASS**.

## 4. Reproduction commands и runtime

Runtime: .NET SDK `10.0.102`; Python `3.12.10`; desktop Excel `16.0`, build `16327`; LibreOffice отсутствует.

Clean build выполнялся из неизменённой временной копии `Program.cs`/`.csproj`, чтобы не менять workspace `bin/obj`:

```powershell
dotnet build C:\Users\Evgenii_2\AppData\Local\Temp\bpmsoft-template-v3-review-20260905T182534Z\BpmSoftWorkbookDelivery.csproj -c Release --nologo
dotnet C:\Users\Evgenii_2\AppData\Local\Temp\bpmsoft-template-v3-review-20260905T182534Z\bin\Release\net10.0\BpmSoftWorkbookDelivery.dll self-test
dotnet C:\Users\Evgenii_2\AppData\Local\Temp\bpmsoft-template-v3-review-20260905T182534Z\bin\Release\net10.0\BpmSoftWorkbookDelivery.dll verify `
  --source C:\CodingAgents\codex\projects\OM_Automatization\preparation\WorkbookDeliveryTool\runs\2026\09\04\80fac4ee-e388-4f25-adf0-a8305950c3c6\evidence\20260904T212051Z-bounded-baseline.json `
  --model C:\CodingAgents\codex\projects\OM_Automatization\preparation\workbooks\BPMSoft.ModelCatalog.xlsx `
  --lookup C:\CodingAgents\codex\projects\OM_Automatization\preparation\workbooks\BPMSoft.LookupCatalog.xlsx
```

XLSX validation:

```powershell
python C:\Users\Evgenii_2\.codex\skills\minimax-xlsx\scripts\formula_check.py <book.xlsx> --json
python C:\Users\Evgenii_2\.codex\skills\minimax-xlsx\scripts\style_audit.py <book.xlsx> --json
python C:\Users\Evgenii_2\.codex\skills\minimax-xlsx\scripts\xlsx_reader.py <book.xlsx> --json
python C:\Users\Evgenii_2\.codex\skills\minimax-xlsx\scripts\xlsx_reader.py <book.xlsx> --quality --json
python C:\Users\Evgenii_2\.codex\skills\minimax-xlsx\scripts\libreoffice_recalc.py --check
```

Критические Excel operations на fresh temporary copies, без сохранения:

```powershell
$wb = $excel.Workbooks.Open($copyPath, 0, $false)
$cell.Value2 = $cell.Value2                 # все non-Boolean used cells mixed sheets
$cell.Formula = '=TRUE()'                   # Boolean-cell write probe; '=FALSE()' аналогично
$ws.Range('M2').Value2 = '11111111-1111-1111-1111-111111111111'
$ws.Range('N2').Value2 = 'draft-local-1'
$ws.Columns.Item(1).ColumnWidth = $before + 1
$ws.Range('B2').Select(); $excel.CommandBars.FindControl(1,12232).Execute()
$range.Sort($keyCell, 2)                    # xlDescending
$wb.Close($false)
```

Canonical read-only open:

```powershell
$wb = $excel.Workbooks.Open($canonicalPath, 0, $true)
$wb.Close($false)
```

Первый 15-argument `Workbooks.Open` harness call был исключён: PowerShell COM binder вернул `Missing parameter does not have a default value`; корректный documented three-argument call прошёл для обеих книг. Ранние Boolean `Value2` self-assignment attempts также исключены как COM binder artifact (`Boolean`/`Double` to `String` cast); isolated `Formula='=TRUE()/=FALSE()'` probe прошёл все Boolean cells. Эти harness incidents не меняли canonical files и не использованы как workbook evidence.

## 5. Verification matrix

| Проверка | Результат | Evidence |
|---|---|---|
| Clean Release build | **PASS** | exit `0`; 0 warnings, 0 errors |
| `self-test` | **PASS** | generation/read-back, reproducibility, pair/schema/unknown-column/external/formula guards, ordering, inherited index join, ActualIndexed separation |
| tool `verify` | **PASS** | Model 8 sheets/165 data rows; Lookup 6/36; exact hashes and canonical worksheet hashes |
| Tier 1 Model | **PASS** | 0 formulas, 0 shared ranges, 0 errors |
| Tier 1 Lookup | **PASS** | 0 formulas, 0 shared ranges, 0 errors |
| Tier 2 | **SKIPPED** | `libreoffice_recalc.py --check` exit `2`; `soffice`/`libreoffice` not found |
| Style audit Model | **PASS** | 2,177 cells; 0 violations/warnings |
| Style audit Lookup | **PASS** | 282 cells; 0 violations/warnings |
| Reader/quality | **PASS WITH EXPECTED WARNINGS** | intentional blank desired/draft/reference/value fields; sparse `ValidationLists`; pandas deprecation warnings only |
| OOXML ZIP/XML and relationship closure | **PASS** | missing relationship targets 0; missing content-type targets 0 |
| Formula/external/VBA/connections | **PASS** | 0 formula nodes; 0 external relationships/forbidden parts |
| Secret-marker scan | **PASS** | 0 hits in bounded JSON/evidence and every XML/rels part of both workbooks |
| Exact sheet order/headers/rows | **PASS** | independently read plus tool verify |
| `LookupRows` absent / merged `LookupValues` | **PASS** | 15 headers; 6 exact value rows; 3 groups x 2; consistent repeated metadata |
| Protection map | **PASS** | only fully read-only sheets protected; all four mixed sheets unprotected |
| Mixed full editability | **PASS** | 72/72 + 1,875/1,875 + 16/16 + 105/105 used-cell writes; 0 errors; derived `A2` marker accepted on each mixed sheet |
| Read-only write block | **PASS** | `A1` write blocked on every 6 Model and 4 Lookup read-only sheets |
| Resize on protected sheet | **PASS** | Model `WorkspaceInventory!A`: 19.29 -> 20.29; Lookup `Readme!A`: 11.29 -> 12.29 |
| Filter on protected sheet | **PASS** | real Excel context command id `12232`; Model 3/5 visible for `Account`; Lookup 1/9 visible for `Workbook` |
| Sort permission attribute | **PASS** | `Protection.AllowSorting=True` on every read-only sheet; OOXML `sort=0` |
| Sort operation on protected read-only sheet | **FAIL** | Model `WorkspaceInventory` and Lookup `Readme`: `Range.Sort(...,2)` blocked by protection; UI sort command no-op |
| Sort harness control on mixed sheet | **PASS** | `Schemas` first key `05c... -> cc64...`; `LookupValues` `ab96... -> d625...` |
| Simultaneous reference input | **PASS** | `LookupValues!M2` GUID and `N2=draft-local-1` accepted/read back together |
| Loader precedence | **CONTRACT ONLY / NOT IMPLEMENTED** | absence of loader is not a bounded workbook-delivery defect; no claim of parser fixtures or runtime precedence |
| ActualIndexed / Indexes projection | **PASS WITH FUTURE BLOCKER** | 64 `ActualIndexed=true`, exactly 2 exported members, no inference; `INDEX_SYNC_UNRESOLVED` remains |

## 6. Structure, OOXML и source projection

Model sheet order: `Readme`, `Manifest`, `WorkspaceInventory`, `Schemas`, `Columns`, `Indexes`, `ValidationLists`, `PullConflicts`. Data rows: `9 / 10 / 5 / 5 / 124 / 2 / 10 / 0` = 165.

Lookup sheet order: `Readme`, `Manifest`, `LookupRegistry`, `LookupValues`, `ValidationLists`, `PullConflicts`. Data rows: `9 / 10 / 1 / 6 / 10 / 0` = 36. `LookupRows` отсутствует.

Обе книги имеют workbook view `xWindow=0`, `yWindow=0`, `windowWidth=20140`, `windowHeight=10960`. `ValidationLists` — единственный `veryHidden` sheet. Все sheets имеют frozen header `ySplit=1`, `topLeftCell=A2` и один `autoFilter` полного диапазона. Worksheet child order корректен: protected sheets — `sheetData > sheetProtection > autoFilter`; mixed sheets — `sheetData > autoFilter > dataValidations`.

Protection map:

- Model protected: `Readme`, `Manifest`, `WorkspaceInventory`, `Indexes`, `ValidationLists`, `PullConflicts`;
- Model unprotected: `Schemas`, `Columns`;
- Lookup protected: `Readme`, `Manifest`, `ValidationLists`, `PullConflicts`;
- Lookup unprotected: `LookupRegistry`, `LookupValues`.

Каждый protected sheet имеет `formatColumns=0`, `autoFilter=0`, `sort=0`, `selectLockedCells=0`, `selectUnlockedCells=0`. Это даёт Excel permission flags, но не преодолевает ограничение сортировки locked cells.

Validations остаются list-only и соответствуют workbook assistance role: Model `Schemas` — 3, `Columns` — 5; Lookup `LookupRegistry` — 2, `LookupValues` — 4. Отсутствие workbook-level XOR/reference GUID validation не является defect после owner correction; invalid non-empty GUID должен блокировать будущий parser/loader.

Bounded source: 5 workspace items, 5 schema layers, 124 columns, 3 lookup metadata rows, 6 lookup values, 2 index members, 64 column representations с `ActualIndexed=true`. Шесть independently extracted `LookupValues` tuples посимвольно совпали с source projection, включая `Средний`, `Низкий`, `Высокий` и три `EmptyString`; каждая record group содержит ровно `Description,Name`, metadata variants = 1.

Два independently extracted index tuples точно совпали с source:

| Index | IsUnique | Column | ColumnUId | Ordinal |
|---|---|---|---|---:|
| `Index1` | `TRUE` | `Code` | `60cc5643-4ee2-4adf-b76b-06000ad0b067` | 0 |
| `Index2` | `FALSE` | `Name` | `7c81a01e-f59b-47df-830c-8e830f1bf889` | 0 |

`Code` имеет `ActualIndexed=false` и membership 1; `Name` — `ActualIndexed=true` и membership 1. Self-test повторно подтвердил separation fixtures. Это не доказывает безопасный index load/apply.

## 7. Desktop Excel 16.0

Canonical files открылись `ReadOnly=true`, `Saved=true`, `FileFormat=51` без recovery/error и закрылись без сохранения. Model сохранил 8 expected sheets, Lookup — 6; `Readme!A1=Topic`, `TemplateVersion=3-bounded-research` в обеих книгах.

На fresh temporary copies:

- все 2,068 Model mixed-sheet used cells и все 121 Lookup mixed-sheet used cells приняли write assignment; protected flag отсутствует; изменение derived `A2` принято в каждом mixed sheet;
- GUID в `LookupValues!M2` и `draft-local-1` в `N2` одновременно приняты и прочитаны обратно;
- попытка изменить `A1` блокировалась на каждом fully read-only sheet;
- resize и filter реально меняли Excel state на protected sheets;
- sort на protected read-only sheets не менял state и через `Range.Sort` выдавал: `Ячейка или диаграмма, которую вы пытаетесь изменить, находится на защищенном листе...`;
- тот же `Range.Sort` на unprotected mixed sheets успешно изменил first-key ordering.

Таким образом `AllowSorting=True`/OOXML `sort=0` — необходимый, но недостаточный evidence реальной сортируемости locked read-only data.

## 8. Defect evidence — request changes

Contract `WORKBOOK_CONTRACT_VISION.md`, раздел 2.1, требует, чтобы на protected read-only sheets пользователь мог применять сортировку. FR-22 также требует desktop Excel evidence для resize/filter/sort при сохранённой защите read-only data. Текущий generator создаёт `sheetProtection sort="0"`, но все cells read-only sheets остаются locked. Excel 16 разрешает sort на protected sheet только там, где операция не требует перестановки locked cells; здесь перестановка блокируется.

Минимальная reproduction:

1. Скопировать canonical workbook во временный файл.
2. Открыть copy через Excel 16.0 с `ReadOnly=false`.
3. Убедиться: `WorkspaceInventory.ProtectContents=True`, `WorkspaceInventory.Protection.AllowSorting=True`.
4. Выполнить `WorkspaceInventory.Range("A1:G6").Sort(WorkspaceInventory.Range("A2"), 2)`.
5. Получить protection error; `A2` остаётся `05c46595-729a-4668-94bc-56061dad4fb9` вместо ожидаемого descending first `cc642965-191f-4b55-b1ea-fb8336e623b9`.
6. Аналогично `Lookup Readme.Range("A1:B10").Sort(Readme.Range("A2"), 2)` блокируется.
7. Контроль: identical call на unprotected `Schemas`/`LookupValues` меняет first key.

Disposition: **не исправлено; request changes**. Необходимо выбрать contract-compatible workbook mechanism и затем повторить Excel operations, OOXML, build/self-test/verify/Tier 1/hash/audit checks. Простое наличие `sort=0` не является достаточным acceptance evidence.

### Owner choice

Проверенный Excel 16 behaviour показывает конфликт текущих требований: standard sheet protection блокирует перестановку locked cells даже при `AllowSorting=True`. Владелец должен явно выбрать один путь; reviewer не выбирает его автоматически:

1. **Сохранить оба требования:** fully read-only sheets остаются protected/write-blocked и обязаны реально сортироваться. Тогда текущая пара остаётся rejected, нужен отдельный redesign presentation/sort mechanism и полный independent rerun. Это рекомендуемый путь, если sort является обязательной пользовательской функцией.
2. **Изменить acceptance contract:** сохранить protected/write-blocked data, resize и filter, но убрать требование фактической сортировки locked read-only ranges. `AllowSorting=True` нельзя при этом описывать как доказанную sort operation; G5 возможен только после явной owner correction документов.
3. **Ослабить workbook write-block:** сделать такие sheets unprotected и полагаться на parser/compare hard gate, как уже решено для mixed sheets. Это возвращает реальную сортировку, но отменяет требование Excel-level block для read-only data и поэтому требует отдельного явного owner решения и новых acceptance checks.

Допустим также новый presentation-only representation, не меняющий protected source data, но его exact design не исследовался и не является рекомендацией к реализации без отдельного решения.

## 9. Incident lineage

| Этап | SHA-256 | Статус |
|---|---|---|
| Initial delivery audit `20260904T212706Z` | `8a5ea0c3fe0f66af6e4caa3cb964b73aac27ac3522dc07a0728d163f98cf2e71` | superseded; initial workbooks Excel-invalid |
| Excel recovery log | `df7d10cb643c7767925851a1a20672b9d6c7124c48ef4c8dd7db05a449f26dbc` | доказал replacement worksheet parts |
| Repair audit `20260905T062211Z` | `7a241dad2921b6a2787c83f958bc689e86f92c14e7728666ce5b4ada35a17279` | OOXML repair; template v1 later superseded |
| Template v2 pending audit `20260905T114012Z` | `1da4af2d3f6d931b6bca071c1cd2ead95b5b09363f8d16391688896470dae32f` | later superseded |
| Template v2 canonical audit `20260905T114516Z` | `f2b4802c9bce57d3183e4a7025eff990720a1df59056401d0b73eda5dd0d5dc5` | prior reviewer found obsolete XOR/protection assumption |
| Prior independent v2 review | `23478251bfc21b2ccb6ed761ac83916e8ee687557a18b4010ecfc0eee826f4fe` | FAIL under superseded assumption; owner corrected contract |
| Superseded v2 Model/Lookup pair | `e42ccd1187c744c846805ad6c4ee59d1b75a50c1bb19a252f45f0bd534129ca0` / `9ebe5369cef454a6e0cdfb135da9c5ac862af0950c2cf7dbc976c9ccebc1b2e1` | preserved under `audit/rejected/20260905T122311Z-*` |
| Template v3 canonical audit `20260905T122353Z` | `c6e7a097eab007d9b4b7a1bd0fcd353e53f57673ce170cf362729ad1f0aff613` | claims PASS_WITH_LIMITS; this review reproduces all material checks except real protected-sheet sort |
| This v3 review | this report | **FAIL — REQUEST CHANGES** |

## 10. Gaps / not run

- LibreOffice Tier 2 and render: **SKIPPED / NOT RUN**, executable unavailable.
- Owner manual visual/usability acceptance template v3: **NOT RUN**; this reviewer does not close G5.
- Full-catalog scale/generalisation, snapshot UX, composite/auto-name/orderDirection semantics and inherited `column.indexed` semantics: **NOT RUN / unresolved**.
- BPMSoft mutation, Write/Manage, compile/save/create/update/delete, Google input, `SyncOM` changes: **NOT RUN and not authorised**.
- Future parser/loader, GUID precedence runtime, token resolution/create-read-back and invalid-GUID blocker fixtures: **NOT IMPLEMENTED / NOT RUN**. This is an explicit future contract, not a defect of bounded workbook generation.
- Safe index add/drop/load/apply: **NOT RUN**; `INDEX_SYNC_UNRESOLVED` remains.

## 11. Final recommendation

**FAIL — REQUEST CHANGES.** Не передавать canonical template v3 как полностью прошедшую independent workbook acceptance, пока владелец не выберет один из вариантов выше: доказать реальную sort operation при сохранённом write-block, явно снять sort requirement либо явно снять Excel-level write-block с затронутых sheets. Все остальные перечисленные bounded workbook checks имеют статус PASS/PASS WITH EXPECTED WARNINGS; будущий loader precedence остаётся только contract и не объявляется реализованным. G5 остаётся решением владельца.
