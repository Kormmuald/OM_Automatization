# 01 Workbook evidence review v3 rerun — owner sort waiver

**Дата проверки (UTC):** 2026-09-05 19:07–19:15  
**Роль:** `01-workbook-evidence-reviewer`, independent offline rerun  
**Итог:** **PASS**  
**Scope:** `VerifiedBoundedBaseline`; не full catalog, не G5 acceptance и не разрешение BPMSoft write/load/apply  
**Future index-load gate:** `INDEX_SYNC_UNRESOLVED`

## 1. Verdict

Canonical template v3 **PASS** по обновлённому owner contract.

После воспроизводимого Excel finding из предыдущего review владелец явно сохранил protection полностью read-only sheets и снял обязательность фактической сортировки их locked ranges. Поэтому реальная сортировка protected locked ranges в этом rerun не является acceptance criterion. Наличие OOXML `sort="0"` / Excel `AllowSorting=True` только наблюдалось и **не использовалось как evidence работоспособности сортировки**.

Независимо повторены и пройдены обязательные проверки: clean Release build, `self-test`, tool `verify`, Tier 1, OOXML/source/audit/hash/secret/index checks, desktop Excel open/read-only без recovery, write-block всех fully read-only sheets, полная редактируемость mixed sheets, resize, применение существующего фильтра и одновременный ввод обоих reference fields. Сортировка на unprotected mixed sheets дополнительно прошла как контроль обычного Excel behavior.

LibreOffice отсутствует, поэтому Tier 2 корректно **SKIPPED** согласно `minimax-xlsx/references/validate.md`. Будущий loader precedence остаётся contract only: `valid non-empty ReferenceRecordId > unambiguously resolved ReferenceDraftRowToken > empty`; invalid non-empty GUID blocks. Loader не реализован и не объявляется выполненным; его отсутствие не является defect bounded workbook delivery.

## 2. Contract basis, границы и прочитанные источники

Рабочий root:

`C:\CodingAgents\codex\projects\OM_Automatization\preparation`

Полностью прочитаны до выполнения проверок:

- `docs\WORKBOOK_CONTINUATION_PROMPTS\01-workbook-evidence-reviewer.md`;
- `docs\PROJECT_HANDOFF.md`, разделы 15.4–15.5;
- `docs\WORKBOOK_CONTRACT_VISION.md`;
- все три `docs\SDD_DRAFTS\*.draft.md`;
- `docs\READ_ONLY_RESEARCH_RESULTS\20260904-04-workbook-delivery-result.md`, разделы 10–12;
- `docs\READ_ONLY_RESEARCH_RESULTS\20260905-owner-protected-sheet-sort-waiver.md`;
- `WorkbookDeliveryTool\Program.cs` и `WorkbookDeliveryTool\BpmSoftWorkbookDelivery.csproj`;
- предыдущий `docs\READ_ONLY_RESEARCH_RESULTS\20260905T183700Z-01-workbook-evidence-review-v3.md`;
- `C:\Users\Evgenii_2\.codex\skills\minimax-xlsx\SKILL.md`;
- `C:\Users\Evgenii_2\.codex\skills\minimax-xlsx\references\read-analyze.md`;
- `C:\Users\Evgenii_2\.codex\skills\minimax-xlsx\references\validate.md` полностью.

Owner decision record содержит точную цитату: «да, подтверждаю, что сортировка не особо нужна». Его применённое толкование: protection/write-block сохраняется; resize и existing-filter обязательны; фактическая сортировка locked protected ranges исключена; mixed sheets остаются unprotected и редактируемыми; sort flags не являются доказательством операции.

Не выполнялись обращения к BPMSoft, Google input, `capture`, `generate` в canonical paths, Manage/Write/compile/save/create/update/delete, index load/apply или изменения canonical workbook/code/contract/SDD/handoff/audit. Canonical files открывались Excel только с `ReadOnly=true`; interactive probes выполнялись на временных копиях и закрывались с `SaveChanges=false`. Единственный созданный workspace-файл — этот отчёт. Предыдущий FAIL-report не изменялся.

## 3. Exact inputs, bytes и SHA-256

Все относительные пути ниже разрешаются от exact root из раздела 2.

| Файл | Bytes | SHA-256 |
|---|---:|---|
| `docs/PROJECT_HANDOFF.md` | 89,195 | `d94cd5f35777b595a08b8f9a1bbb685c03fb7d70a24fc2039fece0d4ecdf4042` |
| `docs/WORKBOOK_CONTINUATION_PROMPTS/01-workbook-evidence-reviewer.md` | 3,967 | `960d3c3d8b91cbe1b5636630323ac61e733489db3430b434c4eef686d21f8364` |
| `docs/WORKBOOK_CONTRACT_VISION.md` | 36,044 | `584dc55bb54edd50bf84e4649af1b4c060c095cd3f4bf603c0cde0a52162f9ed` |
| `docs/SDD_DRAFTS/clarify-review.draft.md` | 16,709 | `6659daa7011eb12f49381e9916786a72da3b038ee9168e1d873ae1e5b1d93acf` |
| `docs/SDD_DRAFTS/constitution.draft.md` | 14,655 | `603c9f3abd7217d14ac57490cbe7c9a9f37da1671dc530c77faafd93f00d189f` |
| `docs/SDD_DRAFTS/first-feature-spec.draft.md` | 19,266 | `3a6726997d2274b42d9220bbe9861c6410f8e5c931b9653ec57984a8a0a36bb4` |
| `docs/READ_ONLY_RESEARCH_RESULTS/20260904-04-workbook-delivery-result.md` | 21,482 | `af6336e95ed46bbfb2f512d596017ce70768588f3cf44327c328e454946a166e` |
| owner sort waiver | 2,027 | `ee91ba5538bd7ad2f6e41f2521bbd9dfe57172e0d854270c1b2da5b6bc0bf61e` |
| previous v3 FAIL report | 24,379 | `6f9c061ff41ff44ceb8d800ef0ca4ae9c6aeb0104c9d4ee7583ddc4237a113c9` |
| `WorkbookDeliveryTool/Program.cs` | 106,014 | `c4a9958efebf54f46a8e4b82db8e479c5de9a492ad4da5566d2958592f3cb76d` |
| `WorkbookDeliveryTool/BpmSoftWorkbookDelivery.csproj` | 352 | `9352356a7456122d4ed4185480c1e00be32b2ed36c24ae37036ebd05a4617072` |
| `workbooks/BPMSoft.ModelCatalog.xlsx` | 29,437 | `7fb01fd5f4af039beb45e7e77819385d9c25beac6762616cf6212e6ac32a849f` |
| `workbooks/BPMSoft.LookupCatalog.xlsx` | 9,203 | `48719140d6ecf4a7b21c61fc381eccb8aa95f2b8e32f437c360ed14e96f07b2d` |

Evidence lineage inputs:

| Exact path under run root `WorkbookDeliveryTool/runs/2026/09/04/80fac4ee-e388-4f25-adf0-a8305950c3c6` | Bytes | SHA-256 |
|---|---:|---|
| `evidence/20260904T212051Z-bounded-baseline.json` | 67,434 | `b87e6e6d3e82ead7c9724cb7735a428740200c44fa06dced96b43772fc97a562` |
| `evidence/20260904T212051Z-actualindexed-indexes-check.json` | 22,389 | `44429f44ccf18d27304e4ef1a1b257a658e03f192972153cd9d44a126853b251` |
| `evidence/20260904T212051Z-source-manifest.sha256.json` | 732 | `9440b1137ba76e9632747abf8e5f989b8807e6ab6d9c936ac4befd6eaabd3a0d` |
| `20260904T212051Z-run-journal.json` | 943 | `a3fd1502cf3976700852945a5ee683d1958e0301aa959ea9f897789e9257d071` |
| `audit/20260905T122353Z-workbook-template-v3-canonical-verification.json` | 2,991 | `c6e7a097eab007d9b4b7a1bd0fcd353e53f57673ce170cf362729ad1f0aff613` |

Validation assets:

| Exact path under `C:\Users\Evgenii_2\.codex\skills\minimax-xlsx` | SHA-256 |
|---|---|
| `SKILL.md` | `4ffb70af6ccd00e90c3b14516d50a850ae0f2f1807a01d326aa1393e0067dd41` |
| `references/read-analyze.md` | `78fc8f0c982ae36e3c3141a0d7e2d93a9d85b6c04c1a57645001628ac32c59a4` |
| `references/validate.md` | `fee73ec103df50dcc88c1cfd5ce769c24ef85570fd90dab1dacf45239261da08` |
| `scripts/formula_check.py` | `94661b6ebbfef0450b762392b45477678a7cd641982e0eb9397fef7bcb716e15` |
| `scripts/style_audit.py` | `4ac17193e5ad306cb89007a7fe8492b9117de0d4b0136a3ffaa65223e60abcb6` |
| `scripts/xlsx_reader.py` | `585071399f9e5683700081db959812bcce02edb694bb5a7dece94d9b89a5e1f0` |
| `scripts/libreoffice_recalc.py` | `a7967a78aff2f8f74f4975db2f674215a4dce1086c38b5312e353f7bad084f87` |

Canonical workbook hashes после всех desktop probes совпали с исходными. Временные copies после `Close(false)` также имели byte-identical SHA-256 canonical файлов.

## 4. Runtime и reproduction commands

Runtime: .NET SDK `10.0.102`; Python `3.12.10`; desktop Excel `16.0`, build `16327`; LibreOffice отсутствует.

Clean build выполнялся из неизменённой временной source copy, чтобы не менять workspace `bin/obj`:

```powershell
dotnet build C:\Users\Evgenii_2\AppData\Local\Temp\bpmsoft-template-v3-rerun-20260905T190754Z\BpmSoftWorkbookDelivery.csproj -c Release --nologo
dotnet C:\Users\Evgenii_2\AppData\Local\Temp\bpmsoft-template-v3-rerun-20260905T190754Z\bin\Release\net10.0\BpmSoftWorkbookDelivery.dll self-test
dotnet C:\Users\Evgenii_2\AppData\Local\Temp\bpmsoft-template-v3-rerun-20260905T190754Z\bin\Release\net10.0\BpmSoftWorkbookDelivery.dll verify `
  --source C:\CodingAgents\codex\projects\OM_Automatization\preparation\WorkbookDeliveryTool\runs\2026\09\04\80fac4ee-e388-4f25-adf0-a8305950c3c6\evidence\20260904T212051Z-bounded-baseline.json `
  --model C:\CodingAgents\codex\projects\OM_Automatization\preparation\workbooks\BPMSoft.ModelCatalog.xlsx `
  --lookup C:\CodingAgents\codex\projects\OM_Automatization\preparation\workbooks\BPMSoft.LookupCatalog.xlsx
```

XLSX skill commands:

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
$cell.Value2 = $cell.Value2                         # non-Boolean used-cell write probe
$cell.Formula = '=TRUE()'                           # Boolean probe; '=FALSE()' аналогично
$cell.Value2 = '__edit_probe__'; $cell.ClearContents() # blank-cell probe
$ws.Range('M2').Value2 = '11111111-1111-1111-1111-111111111111'
$ws.Range('N2').Value2 = 'draft-local-1'
$ws.Columns.Item(1).ColumnWidth = $before + 1
$ws.Range('B2').Select(); $excel.CommandBars.FindControl(1,12232).Execute()
$range.Sort($keyCell, 2)                            # only unprotected mixed-sheet control
$wb.Close($false)
```

Canonical open without write:

```powershell
$wb = $excel.Workbooks.Open($canonicalPath, 0, $true)
$wb.Close($false)
```

Independent OOXML checks used `System.IO.Compression.ZipFile` plus XML parsing of `[Content_Types].xml`, every `.rels`, `xl/workbook.xml` and all worksheet parts. Source/audit/index checks used `ConvertFrom-Json`, recomputed SHA-256 and exact tuple comparisons; no generated output was written into canonical paths.

## 5. Criterion evidence matrix

| Criterion | Result | Independent evidence |
|---|---|---|
| Clean Release build | **PASS** | exit `0`; 0 warnings, 0 errors |
| `self-test` | **PASS** | generation/read-back, reproducibility, pair/schema guards, unknown-column/external/formula blockers, ordering, inherited index join, ActualIndexed separation |
| tool `verify` | **PASS** | Model 8 sheets/165 data rows; Lookup 6/36; exact file and canonical worksheet hashes |
| Pair binding | **PASS** | `PairBaselineHash=f2480cdbdaa036856ebedbbbae45fe6dd55b919a4156bb6af78833a602e4f65a`; both manifests bind to source |
| Tier 1 Model | **PASS** | 0 formulas, 0 shared ranges, 0 errors |
| Tier 1 Lookup | **PASS** | 0 formulas, 0 shared ranges, 0 errors |
| Tier 2 | **SKIPPED** | `libreoffice_recalc.py --check` exit `1`: `LibreOffice NOT available` |
| Style audit Model | **PASS** | 2,177 cells; 0 violations; 0 warnings |
| Style audit Lookup | **PASS** | 282 cells; 0 violations; 0 warnings |
| Reader/quality | **PASS WITH EXPECTED WARNINGS** | exact 8/6 sheet shapes; 16 quality records per book are intentional blanks/sparse validation lists and mixed-type manifest; pandas deprecation warnings only |
| OOXML closure | **PASS** | missing relationship/content-type targets 0; external relationships 0 |
| Formula/external/VBA/connections | **PASS** | formula nodes 0; forbidden parts 0 |
| Secret-marker scan | **PASS** | 0 high-risk marker hits in source, index evidence, manifest, audit and all workbook XML/rels parts |
| Sheet order/rows/headers | **PASS** | independently parsed; exact values below |
| `LookupRows` absent / merged `LookupValues` | **PASS** | exact Lookup sheet list has no `LookupRows`; 15 headers; 6 rows |
| Protection map | **PASS** | protection only on fully read-only sheets; all four mixed sheets unprotected |
| Protected permissions required by current contract | **PASS** | `formatColumns=0`, `autoFilter=0`, `selectLockedCells=0`, `selectUnlockedCells=0` on all 10 protected sheets |
| Resize protected sheet | **PASS** | Model `WorkspaceInventory!A` 19.29→20.29; Lookup `Readme!A` 11.29→12.29 |
| Existing-filter operation | **PASS** | real Excel command id `12232`; Model `B2=Account` leaves 3/5 visible; Lookup `A2=Workbook` leaves 1/9 visible |
| Read-only write block | **PASS** | mutation of `A1` blocked and value unchanged on every 6 Model + 4 Lookup protected sheet |
| Mixed full editability | **PASS** | 72/72, 1,875/1,875, 16/16, 105/105 used cells wrote with 0 errors; `A2` marker accepted on all four mixed sheets |
| Mixed-sheet sorting control | **PASS** | `Schemas!B2` `05c…→cc64…`; `LookupValues!C2` `ab96…→d625…` |
| Protected locked-range sorting | **WAIVED / NOT AN ACCEPTANCE CHECK** | no success required; no protected sort operation used as PASS evidence |
| Sort metadata | **OBSERVED ONLY** | OOXML `sort=0` on protected sheets; explicitly not evidence of successful sort |
| Simultaneous reference input | **PASS** | `LookupValues!M2` GUID and `N2=draft-local-1` accepted and read back together |
| Loader precedence | **CONTRACT ONLY / NOT IMPLEMENTED** | absence is not a bounded workbook-delivery defect; runtime precedence/invalid-GUID fixtures are not claimed |
| ActualIndexed/Indexes projection | **PASS WITH FUTURE GATE** | 64 ActualIndexed=true, exactly 2 exported members, exact projection, no inference; `INDEX_SYNC_UNRESOLVED` remains |
| Excel open/read-only/no recovery | **PASS** | both canonical books opened with `ReadOnly=true`, `Saved=true`, `FileFormat=51`, exact sheets and `Readme!A1=Topic`; no repair/recovery/error |

## 6. Workbook structure and OOXML

Model exact sheet order:

`Readme`, `Manifest`, `WorkspaceInventory`, `Schemas`, `Columns`, `Indexes`, `ValidationLists`, `PullConflicts`

Model data rows: `9 / 10 / 5 / 5 / 124 / 2 / 10 / 0` = 165.

Lookup exact sheet order:

`Readme`, `Manifest`, `LookupRegistry`, `LookupValues`, `ValidationLists`, `PullConflicts`

Lookup data rows: `9 / 10 / 1 / 6 / 10 / 0` = 36. `LookupRows` отсутствует.

Exact `LookupValues` headers:

`SchemaName | SysEntitySchemaUId | RecordId | DraftRowToken | DesiredState | ServerPresence | SourceFingerprint | Comment | ColumnName | ValueState | Value | ValueKind | ReferenceRecordId | ReferenceDraftRowToken | CanonicalValue`

Protection map:

- Model protected: `Readme`, `Manifest`, `WorkspaceInventory`, `Indexes`, `ValidationLists`, `PullConflicts`;
- Model unprotected: `Schemas`, `Columns`;
- Lookup protected: `Readme`, `Manifest`, `ValidationLists`, `PullConflicts`;
- Lookup unprotected: `LookupRegistry`, `LookupValues`.

`ValidationLists` — единственный `veryHidden` sheet. Каждый sheet имеет frozen header `ySplit=1`, `topLeftCell=A2`, state `frozen` и один `autoFilter` полного used range. Child order корректен: `sheetData > sheetProtection > autoFilter` для protected и `sheetData > autoFilter > dataValidations` для mixed. Data-validation counts: Model `Schemas=3`, `Columns=5`; Lookup `LookupRegistry=2`, `LookupValues=4`.

Текущий source verifier соответствует waiver: `Program.cs:1366–1367` прямо описывает sorting как best-effort metadata и обязательный permission loop содержит только `formatColumns`, `autoFilter`, `selectLockedCells`, `selectUnlockedCells`; `sort` в loop не входит. Это source observation, не замена Excel operation evidence для обязательных resize/filter/write-block/editability criteria.

## 7. Source, audit, lookup и index projection

Source manifest независимо воспроизведён: byte counts и SHA-256 обоих перечисленных evidence files совпали.

Bounded source содержит:

- 5 workspace items;
- 5 schema package layers;
- 124 columns;
- 3 lookup metadata rows;
- 6 lookup values;
- 2 index members;
- 64 column representations с `ActualIndexed=true`.

Каждая из трёх lookup record groups содержит ровно два values: `Description,Name`; повторённый `SourceFingerprint` внутри каждой группы единственен и совпадает с metadata. Все 6 сериализованных `LookupValues` tuples посимвольно совпали с source projection.

Exact independently compared index tuples:

| Index | IndexUId | IsUnique | Column | ColumnUId | Ordinal |
|---|---|---|---|---|---:|
| `Index1` | `03d8954e-6967-41b5-88c7-0a08a802880e` | `TRUE` | `Code` | `60cc5643-4ee2-4adf-b76b-06000ad0b067` | 0 |
| `Index2` | `bace03c9-8747-4f26-970c-96cc3940543e` | `FALSE` | `Name` | `7c81a01e-f59b-47df-830c-8e830f1bf889` | 0 |

Source tuples и workbook tuples совпали exactly. Self-test повторно прошёл negative fixtures: membership не выводится из `ActualIndexed`; `ActualIndexed=false` при membership present сохраняется; `ActualIndexed=true` при membership absent не фабрикует индекс; member `uId` не принимается вместо `columnUId`.

Canonical v3 audit имеет `Status=PASS_WITH_LIMITS`, `TemplateVersion=3-bounded-research`, тот же pair hash и `FutureIndexLoadGate=INDEX_SYNC_UNRESOLVED`. Этот audit не заменял independent checks.

## 8. Desktop Excel 16.0 evidence

Обе canonical книги открылись через documented three-argument `Workbooks.Open(path, 0, true)` и закрылись без сохранения:

- Model: `ReadOnly=true`, `Saved=true`, `FileFormat=51`, 8 exact sheets, `Readme!A1=Topic`, `Readme!B2=Model Catalog`;
- Lookup: `ReadOnly=true`, `Saved=true`, `FileFormat=51`, 6 exact sheets, `Readme!A1=Topic`, `Readme!B2=Lookup Catalog`;
- ни recovery prompt, ни repair/error не возникли.

На fresh temporary copies:

- Model mixed sheets: `Schemas` 72/72 и `Columns` 1,875/1,875 used cells приняли assignment, 0 errors;
- Lookup mixed sheets: `LookupRegistry` 16/16 и `LookupValues` 105/105 used cells приняли assignment, 0 errors;
- итого 2,068/2,068 used cells на четырёх unprotected mixed sheets; пустые cells реально принимали marker с последующей очисткой, Boolean cells — `Formula='=TRUE()/=FALSE()'`, derived `A2` принимал marker на каждом mixed sheet;
- изменение `A1` блокировалось защитой на всех 10 fully read-only sheets; исходное значение оставалось неизменным;
- ширина колонки реально менялась на representative protected sheet каждой книги;
- существующий filter реально применялся через enabled Excel UI command id `12232`, меняя hidden-row state до ожидаемого количества;
- `LookupValues!M2` и `N2` одновременно приняли соответственно `11111111-1111-1111-1111-111111111111` и `draft-local-1`, затем оба значения прочитаны обратно;
- обычная сортировка на unprotected `Schemas` и `LookupValues` реально изменила first-key ordering.

Harness incident: прямой COM-вызов `Range.AutoFilter(field, criteria)` на protected sheet был отклонён Excel как команда изменения protected sheet. Он исключён как неэквивалентный user interaction. Реальный existing-filter operation через Excel command `12232` прошёл на обеих книгах и является acceptance evidence. Canonical files не затронуты.

Protected locked-range sort намеренно не повторялся: owner waiver уже основан на воспроизведённом Excel limitation, а новый contract прямо исключает такую операцию из acceptance. Наличие enabled/allowed flags не подменяет этот факт.

## 9. Incident lineage

| Этап | SHA-256 | Disposition |
|---|---|---|
| Initial delivery audit `20260904T212706Z` | `8a5ea0c3fe0f66af6e4caa3cb964b73aac27ac3522dc07a0728d163f98cf2e71` | superseded; initial Excel-invalid pair |
| Excel recovery log | `df7d10cb643c7767925851a1a20672b9d6c7124c48ef4c8dd7db05a449f26dbc` | replacement worksheet parts evidenced |
| Repair audit `20260905T062211Z` | `7a241dad2921b6a2787c83f958bc689e86f92c14e7728666ce5b4ada35a17279` | OOXML repair; template v1 later superseded |
| Template v2 pending audit | `1da4af2d3f6d931b6bca071c1cd2ead95b5b09363f8d16391688896470dae32f` | superseded |
| Template v2 canonical audit | `f2b4802c9bce57d3183e4a7025eff990720a1df59056401d0b73eda5dd0d5dc5` | superseded protection/XOR assumption |
| Superseded v2 Model/Lookup | `e42ccd1187c744c846805ad6c4ee59d1b75a50c1bb19a252f45f0bd534129ca0` / `9ebe5369cef454a6e0cdfb135da9c5ac862af0950c2cf7dbc976c9ccebc1b2e1` | preserved in rejected lineage |
| Template v3 canonical audit | `c6e7a097eab007d9b4b7a1bd0fcd353e53f57673ce170cf362729ad1f0aff613` | `PASS_WITH_LIMITS`; independently rerun here |
| Previous independent v3 review | `6f9c061ff41ff44ceb8d800ef0ca4ae9c6aeb0104c9d4ee7583ddc4237a113c9` | FAIL solely because actual locked-range sort was then mandatory |
| Owner sort waiver | `ee91ba5538bd7ad2f6e41f2521bbd9dfe57172e0d854270c1b2da5b6bc0bf61e` | preserves protection; removes that single acceptance criterion |
| This independent rerun | this report | **PASS** under updated owner contract |

Новый PASS не переписывает исторический FAIL и не утверждает, что Excel limitation исчез. Он фиксирует, что limitation больше не является требованием после явного owner decision, а все оставшиеся mandatory criteria independently passed.

## 10. Known gaps / not run

- LibreOffice Tier 2/render: **SKIPPED / NOT RUN**, executable unavailable.
- Owner manual visual/usability acceptance and G5: **NOT RUN**; этот review не закрывает G5.
- Full-catalog scale/generalisation, snapshot UX, composite/auto-name/orderDirection semantics и inherited `column.indexed` semantics: **NOT RUN / unresolved**.
- BPMSoft access, mutation, Manage/Write, compile/save/create/update/delete, Google input и `SyncOM` changes: **NOT RUN and not authorised**.
- Future parser/loader, runtime GUID precedence, token resolution/create-read-back и invalid-GUID blocker fixtures: **NOT IMPLEMENTED / NOT RUN**; это будущий contract, не defect текущей bounded delivery.
- Safe index add/drop/load/apply: **NOT RUN**; `INDEX_SYNC_UNRESOLVED` остаётся mandatory future gate.
- Фактическая сортировка locked ranges на protected read-only sheets: **explicitly waived**, не пропущенный mandatory check. `sort=0`/`AllowSorting=True` не считается functional evidence.

## 11. Final recommendation

**PASS.** Canonical template v3 соответствует обновлённому bounded workbook contract и может быть передан владельцу как прошедший независимый workbook evidence rerun с перечисленными limits. Этот verdict не является G5 acceptance, не открывает BPMSoft Write/Manage/load/apply и не снимает `INDEX_SYNC_UNRESOLVED` для будущей index sync.

## 12. Report integrity

`Payload SHA-256` ниже относится к UTF-8 bytes всех строк этого файла до заголовка `## 12. Report integrity`, включая завершающий перевод строки перед заголовком. Полный file SHA-256 вычисляется после финальной записи и передаётся вместе с exact path; встроить полный hash файла в сам файл без self-reference невозможно.

`1befec2f914c87ceee34ba21dada9bacb5917a2d343c24e3017db5541b5f18a0`
