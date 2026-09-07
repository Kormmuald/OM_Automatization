# G5 decision package — canonical template v3

**Подготовлено (UTC):** 2026-09-06 06:41:02  
**Роль:** `02-g5-decision-preparer`  
**Статус:** proposal/recommendation for owner G5; не human acceptance и не решение от лица владельца  
**Scope:** `VerifiedBoundedBaseline`; не full catalog, не BPMSoft write/load/apply  
**Рекомендация:** **accept with limits**

## 1. Предлагаемое решение и его граница

Рекомендуется принять canonical template v3 **только как bounded read/export workbook delivery**: пара воспроизводится из immutable bounded source, прошла независимый rerun по обновлённому owner contract и пригодна как ограниченный исследовательский baseline. Рекомендация не принимает результат за владельца и не утверждает full-catalog completeness, production parser/compare/loader, BPMSoft Write/Manage, safe index load/apply или весь MVP synchronizer.

Текущие canonical файлы:

| Файл | SHA-256 | Статус |
|---|---|---|
| `workbooks/BPMSoft.ModelCatalog.xlsx` | `7fb01fd5f4af039beb45e7e77819385d9c25beac6762616cf6212e6ac32a849f` | canonical template v3 |
| `workbooks/BPMSoft.LookupCatalog.xlsx` | `48719140d6ecf4a7b21c61fc381eccb8aa95f2b8e32f437c360ed14e96f07b2d` | canonical template v3 |

Обе книги имеют `TemplateVersion=3-bounded-research`, `PairId=cb3722e8-ee9e-4f47-a299-723769ce7bbf`, `PullRunId=80fac4ee-e388-4f25-adf0-a8305950c3c6` и `PairBaselineHash=f2480cdbdaa036856ebedbbbae45fe6dd55b919a4156bb6af78833a602e4f65a`.

## 2. Выполненные роли

### Прежняя orchestration

| Роль | Результат |
|---|---|
| `01-read-only-researcher` | Собрал bounded strictly read-only BPMSoft evidence для explicit order/repeatable pagination, selected schema/lookup mapping и отдельный non-empty-index probe. |
| `02-evidence-reviewer` | Независимо проверил evidence; ограничил выводы bounded scope и выявил ложный own-only oracle для index members. |
| `03-contract-decision-preparer` | Подготовил P3-C proposal и после owner G3 зафиксировал `columnUId -> Columns.ColumnUId`, Own/Inherited target, `IndexUId` и отделение `ActualIndexed` от `Indexes`. |
| `04-workbook-delivery` | После owner G4 выполнил bounded capture, создал минимальный локальный delivery tool, immutable source/evidence и пару `.xlsx`; incident lineage v1/v2 сохранён append-only. |

### Continuation orchestration

| Роль | Результат |
|---|---|
| `00-orchestrator-continuation` | Восстановил актуальную точку continuation: canonical template v3, owner corrections, historical FAIL и необходимость independent rerun до G5. |
| `01-workbook-evidence-reviewer` | Первый v3 review сохранил `FAIL — REQUEST CHANGES` из-за тогда обязательной фактической сортировки locked protected ranges. После owner waiver независимый rerun `20260905T191555Z` дал `PASS` по обновлённому bounded contract. |
| `02-g5-decision-preparer` | Сформировал этот decision package; G5 остаётся открытым до явного ответа владельца. |
| `03-owner-decision-recorder` | **NOT RUN**; допустим только после явного G5 решения владельца. |

## 3. Changed files и lineage

Этот запуск создаёт только `docs/READ_ONLY_RESEARCH_RESULTS/20260906T064102Z-02-g5-decision-package.md`. Canonical `.xlsx`, `WorkbookDeliveryTool/Program.cs`, contract, SDD drafts и handoff не изменялись.

Зафиксированный предыдущими ролями change lineage:

| Этап | Созданные или изменённые paths |
|---|---|
| Read-only research/review | `PrototypeReadOnlyPull/Program.cs`; `PrototypeReadOnlyPull/bin/Release/net10.0/probe-output/20260904T132710Z/`; `PrototypeReadOnlyPull/bin/Release/net10.0/probe-output/20260904T151951Z/`; `docs/READ_ONLY_RESEARCH_RESULTS/20260904T132710Z-02-evidence-review.md`; два `20260904T151951Z-Account-Test1-index-*.md`. |
| G3 P3-C correction | `docs/WORKBOOK_CONTRACT_VISION.md`; `docs/PROJECT_HANDOFF.md`; все три `docs/SDD_DRAFTS/*.draft.md`; `docs/READ_ONLY_RESEARCH_PROMPTS/01-read-only-researcher.md`; `docs/READ_ONLY_RESEARCH_PROMPTS/02-evidence-reviewer.md`; `docs/READ_ONLY_RESEARCH_RESULTS/20260904-P3-C-contract-change.md`. |
| G4 bounded delivery | `WorkbookDeliveryTool/BpmSoftWorkbookDelivery.csproj`; `WorkbookDeliveryTool/Program.cs`; run root `WorkbookDeliveryTool/runs/2026/09/04/80fac4ee-e388-4f25-adf0-a8305950c3c6/`; `workbooks/BPMSoft.ModelCatalog.xlsx`; `workbooks/BPMSoft.LookupCatalog.xlsx`; `docs/READ_ONLY_RESEARCH_RESULTS/20260904-04-workbook-delivery-plan.md`; `docs/READ_ONLY_RESEARCH_RESULTS/20260904-04-workbook-delivery-result.md`. |
| Post-G4 owner corrections | `docs/READ_ONLY_RESEARCH_RESULTS/20260905-owner-workbook-editability-reference-precedence.md`; `docs/READ_ONLY_RESEARCH_RESULTS/20260905-owner-protected-sheet-sort-waiver.md`; contract/SDD/handoff/continuation criteria; `WorkbookDeliveryTool/Program.cs`; canonical template-v3 pair. Superseded pairs preserved under run `audit/rejected/`. |
| Independent continuation reviews | `docs/READ_ONLY_RESEARCH_RESULTS/20260905T183700Z-01-workbook-evidence-review-v3.md`; `docs/READ_ONLY_RESEARCH_RESULTS/20260905T191555Z-01-workbook-evidence-review-v3-rerun.md`. |

## 4. Owner decisions G1–G4 и post-G4

| Gate/decision | Зафиксированное решение |
|---|---|
| G1 | Разрешено strictly read-only исследование локального стенда с интерактивным вводом credentials человеком и без их сохранения. |
| G2 | Закрыт для workbook v1 с ограничениями: explicit order и repeatable pagination доказаны только для bounded `Lookup` и `ActivityPriority`; exact mapping — только для выбранных schema layers и одной lookup-registry relation. Full catalog не является acceptance condition этой delivery. |
| G3 | Server UId является identity схем/колонок; `GetSchema schema.id` остаётся diagnostic candidate. `Indexes` строится только из `schema.indexes[]`; member relation только `columnUId -> Columns.ColumnUId`, target может быть Own/Inherited, `IndexUId` сохраняется, member `.uId` не является `ColumnUId`; `ActualIndexed` остаётся отдельным полем. |
| G4 | Разрешены bounded read-only capture, две `.xlsx` и минимальный filler/verification tool. BPMSoft Write/Manage, Google input и изменение `SyncOM/` не разрешены. |
| Post-G4 editability/reference | Mixed sheets `Schemas`, `Columns`, `LookupRegistry`, `LookupValues` полностью unprotected/editable; parser/compare должен блокировать недопустимые изменения. Оба reference fields могут быть заполнены; будущий приоритет: valid `ReferenceRecordId`, иначе resolved `ReferenceDraftRowToken`, иначе empty; invalid non-empty GUID блокирует load. Это contract only, loader не реализован. |
| Post-G4 protected sort | Protection fully read-only sheets сохранена; фактическая сортировка locked ranges явно owner-waived и не является acceptance check. Обязательны no recovery, write-block, mixed editability, resize и existing filters. `AllowSorting=True`/`sort=0` — только best-effort metadata. |
| G5 | Не закрыт. Этот документ содержит только recommendation. |

## 5. Команды evidence

Все относительные пути разрешаются от `C:\CodingAgents\codex\projects\OM_Automatization\preparation`.

`C1` — clean Release build, выполненный независимым rerun из immutable temporary source copy:

```powershell
dotnet build C:\Users\Evgenii_2\AppData\Local\Temp\bpmsoft-template-v3-rerun-20260905T190754Z\BpmSoftWorkbookDelivery.csproj -c Release --nologo
```

`C2` — independent self-test:

```powershell
dotnet C:\Users\Evgenii_2\AppData\Local\Temp\bpmsoft-template-v3-rerun-20260905T190754Z\bin\Release\net10.0\BpmSoftWorkbookDelivery.dll self-test
```

`C3` — independent source-to-workbook verify:

```powershell
dotnet C:\Users\Evgenii_2\AppData\Local\Temp\bpmsoft-template-v3-rerun-20260905T190754Z\bin\Release\net10.0\BpmSoftWorkbookDelivery.dll verify --source C:\CodingAgents\codex\projects\OM_Automatization\preparation\WorkbookDeliveryTool\runs\2026\09\04\80fac4ee-e388-4f25-adf0-a8305950c3c6\evidence\20260904T212051Z-bounded-baseline.json --model C:\CodingAgents\codex\projects\OM_Automatization\preparation\workbooks\BPMSoft.ModelCatalog.xlsx --lookup C:\CodingAgents\codex\projects\OM_Automatization\preparation\workbooks\BPMSoft.LookupCatalog.xlsx
```

`C4` — Tier 1 formula checks, по одной команде для каждой canonical книги:

```powershell
python C:\Users\Evgenii_2\.codex\skills\minimax-xlsx\scripts\formula_check.py C:\CodingAgents\codex\projects\OM_Automatization\preparation\workbooks\BPMSoft.ModelCatalog.xlsx --json
python C:\Users\Evgenii_2\.codex\skills\minimax-xlsx\scripts\formula_check.py C:\CodingAgents\codex\projects\OM_Automatization\preparation\workbooks\BPMSoft.LookupCatalog.xlsx --json
```

`C5` — style/read-back quality checks, выполненные для обеих canonical книг:

```powershell
python C:\Users\Evgenii_2\.codex\skills\minimax-xlsx\scripts\style_audit.py C:\CodingAgents\codex\projects\OM_Automatization\preparation\workbooks\BPMSoft.ModelCatalog.xlsx --json
python C:\Users\Evgenii_2\.codex\skills\minimax-xlsx\scripts\xlsx_reader.py C:\CodingAgents\codex\projects\OM_Automatization\preparation\workbooks\BPMSoft.ModelCatalog.xlsx --json
python C:\Users\Evgenii_2\.codex\skills\minimax-xlsx\scripts\xlsx_reader.py C:\CodingAgents\codex\projects\OM_Automatization\preparation\workbooks\BPMSoft.ModelCatalog.xlsx --quality --json
python C:\Users\Evgenii_2\.codex\skills\minimax-xlsx\scripts\style_audit.py C:\CodingAgents\codex\projects\OM_Automatization\preparation\workbooks\BPMSoft.LookupCatalog.xlsx --json
python C:\Users\Evgenii_2\.codex\skills\minimax-xlsx\scripts\xlsx_reader.py C:\CodingAgents\codex\projects\OM_Automatization\preparation\workbooks\BPMSoft.LookupCatalog.xlsx --json
python C:\Users\Evgenii_2\.codex\skills\minimax-xlsx\scripts\xlsx_reader.py C:\CodingAgents\codex\projects\OM_Automatization\preparation\workbooks\BPMSoft.LookupCatalog.xlsx --quality --json
```

Результаты и tool hashes зафиксированы в `docs/READ_ONLY_RESEARCH_RESULTS/20260905T191555Z-01-workbook-evidence-review-v3-rerun.md`, разделы 3–5.

`C6` — LibreOffice availability gate:

```powershell
python C:\Users\Evgenii_2\.codex\skills\minimax-xlsx\scripts\libreoffice_recalc.py --check
```

`C7` — canonical SHA-256:

```powershell
Get-FileHash -Algorithm SHA256 C:\CodingAgents\codex\projects\OM_Automatization\preparation\workbooks\BPMSoft.ModelCatalog.xlsx,C:\CodingAgents\codex\projects\OM_Automatization\preparation\workbooks\BPMSoft.LookupCatalog.xlsx
```

`C8` — source manifest/hash reconciliation:

```powershell
Get-Content -Raw WorkbookDeliveryTool\runs\2026\09\04\80fac4ee-e388-4f25-adf0-a8305950c3c6\evidence\20260904T212051Z-source-manifest.sha256.json | ConvertFrom-Json
Get-FileHash -Algorithm SHA256 WorkbookDeliveryTool\runs\2026\09\04\80fac4ee-e388-4f25-adf0-a8305950c3c6\evidence\20260904T212051Z-bounded-baseline.json,WorkbookDeliveryTool\runs\2026\09\04\80fac4ee-e388-4f25-adf0-a8305950c3c6\evidence\20260904T212051Z-actualindexed-indexes-check.json
```

`C9` — bounded ordering/pagination evidence review operations:

```powershell
Get-ChildItem PrototypeReadOnlyPull\bin\Release\net10.0\probe-output\20260904T132710Z -File | Sort-Object Name
Get-ChildItem PrototypeReadOnlyPull\bin\Release\net10.0\probe-output\20260904T132710Z -File | Get-FileHash -Algorithm SHA256
```

Дополнительно reviewer выполнил in-memory UUID/duplicate checks, direct `ASC-1 = ASC-2`, `reverse(ASC-1) = DESC` и page-ledger reconciliation; exact hashes/steps находятся в `docs/READ_ONLY_RESEARCH_RESULTS/20260904T132710Z-02-evidence-review.md`.

`C10` — independent desktop Excel 16 operations на fresh temporary copies, `SaveChanges=false`:

```powershell
$wb = $excel.Workbooks.Open($copyPath, 0, $false)
$cell.Value2 = $cell.Value2
$cell.Formula = '=TRUE()'
$cell.Value2 = '__edit_probe__'; $cell.ClearContents()
$ws.Range('M2').Value2 = '11111111-1111-1111-1111-111111111111'
$ws.Range('N2').Value2 = 'draft-local-1'
$ws.Columns.Item(1).ColumnWidth = $before + 1
$ws.Range('B2').Select(); $excel.CommandBars.FindControl(1,12232).Execute()
$range.Sort($keyCell, 2)
$wb.Close($false)
```

Canonical no-write open:

```powershell
$wb = $excel.Workbooks.Open($canonicalPath, 0, $true)
$wb.Close($false)
```

## 6. Criterion-to-evidence matrix

Допустимые статусы в этой матрице: `PASS`, `FAIL`, `PARTIAL`, `NOT RUN`.

| Критерий | Статус | Exact evidence path | Команда | Вывод/граница |
|---|---|---|---|---|
| Canonical file identity | **PASS** | `workbooks/BPMSoft.ModelCatalog.xlsx`; `workbooks/BPMSoft.LookupCatalog.xlsx`; rerun report §3 | `C7` | Hashes совпадают с canonical v3: Model `7fb0…49f`, Lookup `4871…7b2d`. |
| Clean Release build | **PASS** | `WorkbookDeliveryTool/Program.cs`; `WorkbookDeliveryTool/BpmSoftWorkbookDelivery.csproj`; rerun report §§3–5 | `C1` | Exit 0, 0 warnings, 0 errors. |
| Offline positive/negative fixtures | **PASS** | rerun report §5; `WorkbookDeliveryTool/Program.cs` | `C2` | Generation/read-back, reproducibility, pair/schema/unknown-column/external/formula blockers, inherited index join and ActualIndexed separation passed. |
| Exact bounded source projection and pair binding | **PASS** | `.../evidence/20260904T212051Z-bounded-baseline.json`; `.../evidence/20260904T212051Z-source-manifest.sha256.json`; `.../audit/20260905T122353Z-workbook-template-v3-canonical-verification.json` | `C3`, `C8` | Source hashes reconcile; Model 8 sheets/165 rows, Lookup 6/36; pair metadata and canonical worksheet hashes match. |
| Bounded deterministic order and repeatable pagination | **PASS** | `PrototypeReadOnlyPull/bin/Release/net10.0/probe-output/20260904T132710Z/pagination-Lookup-proof.json`; `pagination-ActivityPriority-proof.json`; reviewer report `20260904T132710Z-02-evidence-review.md` | `C9` plus recorded in-memory comparisons | Proven for `Lookup` 109 rows/page 50 and `ActivityPriority` 3 rows/page 2; repeated ASC digests equal, DESC reverse, terminal empty-page assertion source-traced. |
| Schema/lookup registry mapping | **PARTIAL** | four `schema-*.mapping.json` and `lookup-registry-ActivityPriority.mapping.json` under evidence root `20260904T132710Z`; bounded source JSON | `C9`, `C3` | Exact for selected layers and one registry relation; not generalized to every schema/lookup variant. `schema.id` remains diagnostic candidate. |
| Exact sheets, order, headers, rows, native types | **PASS** | rerun report §§5–7; canonical pair | `C3`, `C5` | Model 8/165 and Lookup 6/36; `LookupRows` absent; `LookupValues` has exact 15 columns and 6 rows. |
| Formula/external/VBA/connections absence | **PASS** | rerun report §§5–6; canonical OOXML | `C3`, `C4` plus independent OOXML parsing recorded in rerun | 0 formulas/shared ranges/errors; no external relationships or forbidden parts. |
| Style/read-back quality | **PASS** | rerun report §5 | `C5` | 0 style violations/warnings; only expected blank/sparse-list quality records and pandas deprecation warnings. |
| Secret/redaction boundary | **PASS** | bounded source, index evidence, source manifest, v3 audit and both workbook XML/rels parts; rerun report §§5,7 | independent marker/value scans recorded in rerun | 0 high-risk hits; credentials/login response/cookie/CSRF/raw lookup response were not persisted. |
| Protection map | **PASS** | v3 audit `.../audit/20260905T122353Z-workbook-template-v3-canonical-verification.json`; rerun report §§5–8 | `C3`, `C10` | 10 fully read-only sheets protected; all 4 mixed sheets unprotected. |
| Independent desktop Excel 16 operation evidence | **PASS** | `docs/READ_ONLY_RESEARCH_RESULTS/20260905T191555Z-01-workbook-evidence-review-v3-rerun.md`, §§5,8 | `C10` | Both canonical books opened read-only with no recovery and exact sheets/data; 10 protected sheets blocked writes; 4 mixed sheets accepted all used-cell writes; resize and real existing-filter operations passed; mixed-sheet sort passed. |
| **Owner manual desktop Excel check: no recovery; data/sheets visible; resize; existing filter apply/remove; protected-cell block; mixed-sheet editability** | **NOT RUN** | rerun report §10; delivery result §9 | owner opens `workbooks/BPMSoft.ModelCatalog.xlsx` and `workbooks/BPMSoft.LookupCatalog.xlsx` in desktop Excel | Independent Excel automation passed these behaviors, but owner manual visual/usability confirmation is not recorded and this package is not human acceptance. |
| Protected locked-range sort | **NOT RUN** | `docs/READ_ONLY_RESEARCH_RESULTS/20260905-owner-protected-sheet-sort-waiver.md`; historical FAIL report §§7–8; rerun report §§1,10 | intentionally not rerun on protected ranges | Explicitly owner-waived and **not a required check**. Historical limitation remains true; `sort=0`/`AllowSorting=True` is not functional evidence. |
| Simultaneous reference-field edit | **PASS** | rerun report §§5,8 | `C10` | `LookupValues!M2` and `N2` accepted GUID and draft token together on a temporary copy. |
| Runtime reference precedence and invalid-GUID blocker | **NOT RUN** | owner decision `20260905-owner-workbook-editability-reference-precedence.md`; rerun report §§1,10 | no loader command exists | Contract defined, but parser/loader and runtime fixtures are not implemented. |
| `ActualIndexed` versus `Indexes` export projection | **PASS** | `.../evidence/20260904T212051Z-actualindexed-indexes-check.json`; bounded source; rerun report §7 | `C2`, `C3`, `C8` | 64 `ActualIndexed=true` representations remain separate; exactly 2 members exported from `schema.indexes[]`; no inference; exact tuples match. |
| Safe index add/drop/load/apply | **NOT RUN** | contract §3.3/§8; G4 decision; rerun report §10 | no write/load command authorized | `INDEX_SYNC_UNRESOLVED` remains mandatory future gate. |
| LibreOffice Tier 2/render | **NOT RUN** | rerun report §§1,5,10 | `C6` | LibreOffice unavailable; this is a recorded environment gap. |
| Full-catalog scale/generalisation | **NOT RUN** | G4 decision; bounded source `ScopeMode=VerifiedBoundedBaseline`; rerun report §10 | no full-catalog command authorized | No claim for catalog-wide completeness, scale, composite/auto-name/order semantics or all schema variants. |
| BPMSoft Write/Manage/compile/save/create/update/delete | **NOT RUN** | run journal `.../20260904T212051Z-run-journal.json`; G4 decision; rerun report §10 | no command authorized | No write/load/apply readiness is proven. |
| G5 human acceptance | **NOT RUN** | this package | none | Owner decision is still required. |

В текущем обязательном bounded criterion set нет `FAIL`. Historical `20260905T183700Z` FAIL не переписан: он остаётся evidence фактического ограничения Excel, но его единственный failing criterion superseded явным owner waiver и последующим independent PASS rerun.

## 7. Findings

1. Canonical template v3 воспроизводится из immutable bounded source и независимо прошёл build, self-test, verify, Tier 1, OOXML/source/audit/hash/secret/index и desktop Excel checks.
2. Первая Excel-invalid пара и superseded template v1/v2 не скрыты: recovery evidence и старые пары сохранены append-only в `audit/rejected/`.
3. Current workbook layout соответствует post-G4 решениям: `LookupRows` отсутствует; mixed sheets полностью editable; read-only sheets защищены; одновременный ввод обоих reference fields разрешён.
4. Independent Excel evidence подтверждает no recovery, expected sheets/data, protected-cell block, mixed-sheet editability, column resize и existing filters. Owner manual visual/usability acceptance всё ещё не зафиксирован.
5. Historical protected-sort FAIL корректно разрешён contract decision, а не заявлением об исчезновении Excel limitation.

## 8. Bounded read/export против не доказанного full catalog/write/load

| Доказано | Не доказано |
|---|---|
| Bounded `Lookup`/`ActivityPriority` explicit order и repeatable pagination. | Полнота, scale и generalisation всего каталога. |
| Selected schema layers, one lookup-registry relation, 5 workspace items, 5 schema layers, 124 columns, 3 lookup records/6 values и 2 index members. | Все schema/lookup variants, target fingerprint для production compare, snapshot UX/scale. |
| Offline generation/read-back двух template-v3 workbooks, exact source projection, pair binding, OOXML closure, formulas/external/VBA/connections absence. | Production pull/refresh, parser/compare, immutable plan, apply, writeback и Git success workflow. |
| Excel 16 no-recovery open и обязательное bounded workbook behavior на временных копиях. | Owner manual acceptance, LibreOffice Tier 2/render и non-Windows/non-Excel environments. |
| Read-only export отображает `Indexes` независимо от `ActualIndexed`. | Любая безопасная index mutation/loading, а также composite/auto-name/broader order semantics. |
| Workbook-only simultaneous reference-field input. | Runtime precedence, token resolution/create-read-back, invalid-GUID blocker и любой BPMSoft load. |

## 9. `ActualIndexed`, `Indexes` и `INDEX_SYNC_UNRESOLVED`

Для bounded export результат положительный и точный: source содержит 64 column representations с `ActualIndexed=true`, но workbook `Indexes` содержит ровно два фактических member tuple из `schema.indexes[]`:

| Index | IndexUId | IsUnique | Column | ColumnUId | Ordinal |
|---|---|---|---|---|---:|
| `Index1` | `03d8954e-6967-41b5-88c7-0a08a802880e` | `TRUE` | `Code` | `60cc5643-4ee2-4adf-b76b-06000ad0b067` | 0 |
| `Index2` | `bace03c9-8747-4f26-970c-96cc3940543e` | `FALSE` | `Name` | `7c81a01e-f59b-47df-830c-8e830f1bf889` | 0 |

`Code` сохраняет membership при `ActualIndexed=false`; `ActualIndexed=true` без membership не фабрикует индекс; member `.uId` не принимается вместо `columnUId`. Это доказывает отсутствие потери или искажения bounded read-only export, но **не** безопасный future load/apply. Поэтому `INDEX_SYNC_UNRESOLVED` остаётся blocker перед любым index planning/loading. Допустимая граница текущего принятия — оставить индексы только read-only evidence.

## 10. Known gaps и not-run checks

- Owner manual visual/usability confirmation canonical v3 не зафиксирован.
- LibreOffice Tier 2 и render не выполнены из-за отсутствия executable.
- Full-catalog traversal, scale, snapshot UX и catalog-wide generalisation не выполнены.
- Composite/auto-name/broader `orderDirection` и inherited `column.indexed` semantics не доказаны.
- `GetSchema schema.id` не повышен из diagnostic candidate до semantic `SysSchemaId` identity.
- Production parser/compare/immutable plan, target fingerprint и stale-plan checks не реализованы.
- Runtime reference precedence, `DraftRowToken -> RecordId`, create/read-back и invalid-GUID blocker не реализованы.
- BPMSoft Write/Manage, compile/save/create/update/delete, mutation, apply и writeback не выполнялись и не разрешены.
- Google input, изменение `SyncOM/`, full MVP acceptance, Git remote workflow и G5 recorder не выполнялись.
- `INDEX_SYNC_UNRESOLVED` сохраняется до отдельного future index-load gate.

## 11. Recommendation

**accept with limits** — принять canonical template v3 как прошедшую independent verification bounded workbook delivery, сохранив ограничения `VerifiedBoundedBaseline`, отсутствие owner manual acceptance, пропуск LibreOffice Tier 2, отсутствие full-catalog/write/load доказательств и обязательный future blocker `INDEX_SYNC_UNRESOLVED`. Это не разрешает BPMSoft Write/Manage/load/apply, не принимает весь synchronizer и не запускает `03-owner-decision-recorder` без явного ответа владельца.

Подтверждаете ли вы после личного открытия обеих canonical template-v3 книг в desktop Excel отсутствие recovery, видимость ожидаемых данных и листов, работу resize и existing filters, блокировку protected cells и редактируемость mixed sheets, а также решение G5 `accept with limits` строго для `VerifiedBoundedBaseline` без разрешения full-catalog/write/load и со сохранением `INDEX_SYNC_UNRESOLVED`?
