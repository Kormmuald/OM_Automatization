# 04 Workbook delivery — результат Phase 2

**Дата:** 2026-09-04  
**Роль:** `04-workbook-delivery`  
**Статус роли:** `PASS WITH LIMITS — TEMPLATE V2 CANONICAL`; v2 пара установлена по canonical paths и повторно проверена, остаётся owner manual usability confirmation и независимый G5 review  
**Scope:** `VerifiedBoundedBaseline` — исследовательская ограниченная выборка, не полный каталог  
**PairId:** `cb3722e8-ee9e-4f47-a299-723769ce7bbf`  
**PullRunId:** `80fac4ee-e388-4f25-adf0-a8305950c3c6`  
**PairBaselineHash:** `f2480cdbdaa036856ebedbbbae45fe6dd55b919a4156bb6af78833a602e4f65a`

## 1. Что сделано

Создан минимальный локальный инструмент без режима загрузки/применения. Он умеет:

- `capture` — интерактивно выполнить только разрешённые read-only запросы к локальному BPMSoft;
- `generate` — офлайн создать пару Excel-книг из сохранённого JSON;
- `verify` — прочитать OOXML обеих книг обратно и дословно сверить с источником;
- `self-test` — проверить положительные и отрицательные сценарии без BPMSoft.

Созданы книги:

- `workbooks/BPMSoft.ModelCatalog.xlsx`;
- `workbooks/BPMSoft.LookupCatalog.xlsx`.

Они связаны одинаковыми `PairId`, `PullRunId`, `PairBaselineHash` и `BaselineTargetFingerprint`. В книгах нет формул, внешних связей, макросов, подключений и секретов. Служебные/фактические ячейки защищены; разрешённые контрактом поля редактирования разблокированы. `ValidationLists` скрыт как `veryHidden`.

## 2. Свежий bounded source

Источник:

`WorkbookDeliveryTool/runs/2026/09/04/80fac4ee-e388-4f25-adf0-a8305950c3c6/evidence/20260904T212051Z-bounded-baseline.json`

- bytes: `67,434`;
- SHA-256: `b87e6e6d3e82ead7c9724cb7735a428740200c44fa06dced96b43772fc97a562`;
- manifest bytes/hash verification: `PASS` для обоих listed evidence files;
- independent secret-marker scan: `0` hits;
- workspace items: `5`;
- schema layers: `5`;
- columns: `124` = прежние 90 в первых четырёх слоях + 34 inherited в `Account/Test1`;
- `ActivityPriority` lookup rows/values: `3 / 6`;
- ordered proofs: `2` (`Lookup`, `ActivityPriority`).

Разбивка схем: `Account/Test1 34 columns + 2 indexes`; `Account/Base 34`; `Lookup/Base 12`; `ActivityPriority/Base 9`; `Account/Completeness 35`. Она совпала с утверждённым bounded baseline и не потребовала full-catalog допущений.

## 3. Первая repaired template-v1 пара — superseded

Следующие hashes относятся к промежуточной repaired template-v1 паре. Она открывалась без recovery, но затем была заменена по owner usability/layout decision; текущие canonical hashes находятся в подразделе template v2 ниже.

| Книга | Sheets | Data rows | SHA-256 | Canonical worksheet XML hash |
|---|---:|---:|---|---|
| `BPMSoft.ModelCatalog.xlsx` | 8 | 165 | `ab871d83069222ce9de76f4aaad63db48bbe7c18d8b70cc9a1341f170962f9e5` | `8c28a6b75f27622c8b6bfb2f4f4cd97fd5da136b57f88a0064fb6416abd55607` |
| `BPMSoft.LookupCatalog.xlsx` | 7 | 39 | `19a6713a49909d9736c76a0db827b254676da2987297d26feaf1ef6d9ab51e58` | `44242e7c45b1b945bffe90ba340113ed46e83ead9727f76889d170b0517458d8` |

`SysSchemaId` намеренно оставлен пустым: непроверенный `schema.id` сохранён только в локальном source как `GetSchemaSchemaIdCandidate` и не превращён в публичную идентичность.

### Template v2 после owner usability review — 2026-09-05

Владелец подтвердил, что repaired template v1 открывается без recovery и визуально содержит данные, но обнаружил два дефекта: нельзя менять ширину колонок/критерии фильтров, а `LookupRows` дублирует record metadata из `LookupValues`. По его явному решению генератор изменён:

- `TemplateVersion` поднят до `2-bounded-research`;
- protection сохраняет locked service cells, но разрешает format columns, existing filters, sorting и selection;
- `LookupRows` удалён;
- `LookupValues` объединяет metadata записи и value fields в 15 колонках;
- source-level validation проверяет уникальность metadata row, полный набор `Name`/`Description`, совпадение fingerprint и отсутствие orphan values.

После закрытия Excel проверенная v2 пара установлена по canonical paths. Предыдущая template-v1 пара сохранена в `.../audit/rejected/20260905T113437Z-*.superseded-template-v1.xlsx`; принудительное закрытие Excel не выполнялось.

| Книга v2 | Sheets | Data rows | SHA-256 | Canonical worksheet XML hash |
|---|---:|---:|---|---|
| `workbooks/BPMSoft.ModelCatalog.xlsx` | 8 | 165 | `e42ccd1187c744c846805ad6c4ee59d1b75a50c1bb19a252f45f0bd534129ca0` | `db78487f736a4814a6358e0452ffec9f259a4b8436b4ed7de5330831f986fd25` |
| `workbooks/BPMSoft.LookupCatalog.xlsx` | 6 | 36 | `9ebe5369cef454a6e0cdfb135da9c5ac862af0950c2cf7dbc976c9ccebc1b2e1` | `20c3fe0bc43b164e4ee5a609e7bb434512b61e5cbfb8c67144d765bb078e0c7e` |

Pending-stage audit: `WorkbookDeliveryTool/runs/2026/09/04/80fac4ee-e388-4f25-adf0-a8305950c3c6/audit/20260905T114012Z-workbook-template-v2-verification.json`. Canonical verification audit: `.../audit/20260905T114516Z-workbook-template-v2-canonical-verification.json`.

## 4. Обязательная проверка ActualIndexed и Indexes

**Экспорт индексов: PASS. Общий смысл: PARTIAL, только из-за ещё не доказанной семантики `column.indexed`.**

Факты одной свежей выборки:

- `ActualIndexed=true`: `64` значения среди выбранных column representations;
- реальных members в `schema.indexes[].columns[]`: `2`;
- строк на листе `Indexes`: ровно `2`;
- экспортированные tuple `(SchemaUId, IndexUId, IndexName, IsUnique, ColumnUId, Ordinal)` полностью совпали с source;
- `Index1` сохранился как unique для inherited `Account.Code`;
- `Index2` сохранился как non-unique для inherited `Account.Name`.

Это важный результат: 64 флага `ActualIndexed=true` **не превратились в 64 индекса**. Лист `Indexes` построен только из `schema.indexes[]`, а `ActualIndexed` показан отдельно как фактический compatibility flag.

Отрицательные fixtures:

- `ActualIndexed=false` при существующем membership — index retained: `PASS`;
- `ActualIndexed=true` без membership — index not fabricated: `PASS`;
- подмена `columnUId` значением member `.uId` — rejected: `PASS`;
- неправильная/неоднозначная связь с column — rejected: `PASS`.

Любая будущая загрузка индексов остаётся заблокирована статусом **`INDEX_SYNC_UNRESOLVED`**. Ни `ActualIndexed`, ни различия листа `Indexes` нельзя пока превращать в write-операции.

## 5. Verification matrix

| Проверка | Результат | Evidence |
|---|---|---|
| .NET Release build, 0 warnings/errors | PASS | команда ниже |
| Fresh capture identity, manifest, redaction, bounded counts | PASS | source/manifest и audit JSON |
| OOXML relationship/content-type closure | PASS | `verify` |
| Exact sheet order, headers, rows and native bool/int types | PASS | `verify`, `xlsx_reader.py` |
| Pair IDs/hashes identical | PASS | оба `Manifest`, `verify` |
| Formula cells | PASS: 0 | `formula_check.py`, `verify` |
| External links/macros/connections | PASS: 0 | `verify`, negative fixtures |
| Protection/unlocked map, validations, veryHidden lists | PASS | `verify` |
| Frozen headers/autofilters | PASS | `verify` |
| Style audit | PASS, 0 violations/warnings | `style_audit.py` |
| Parser read-back | PASS | `xlsx_reader.py --json` и `--quality` |
| Expected null/blank fields | PASS WITH EXPECTED WARNINGS | quality audit: blank desired/draft/reference fields and sparse validation lists are intentional |
| Second offline generation canonical XML equality | PASS | `self-test` |
| Pair mismatch, duplicate layer, invalid GUID, unknown column | PASS: rejected | `self-test` |
| Formula/external-part tampering | PASS: rejected | `self-test` |
| LibreOffice Tier 2 | SKIPPED | `libreoffice_recalc.py --check` exit `2`: LibreOffice unavailable |
| Automated render | SKIPPED | требует LibreOffice |
| Desktop Excel manual check | NOT RUN | требуется владелец перед G5 acceptance |

### Исправление после первой ручной проверки — 2026-09-05

Первая созданная пара была **отозвана как непригодная**: desktop Excel предложил восстановление и после него показывал пустые листы. Recovery log доказал, что Excel заменил все worksheet parts из-за schema-order ошибки. Генератор выдавал `autoFilter`, затем `dataValidations`, затем `sheetProtection`; Excel требует `sheetProtection` перед `autoFilter`, а `dataValidations` — после него. Обычные XML/ZIP/formula readers проверяли well-formed XML, но не эту строгую последовательность SpreadsheetML.

Исправлено:

- порядок worksheet children приведён к Excel-compatible;
- восстановлены шаблонные workbook-view attributes;
- `verify` получил regression guard на порядок `sheetProtection -> autoFilter -> dataValidations` и обязательные размеры workbook window;
- обе книги заново сгенерированы из того же immutable bounded source и повторно прошли build, self-test, tool verify, formula check, style audit и reader/quality checks;
- установленный desktop Excel программно открыл обе исправленные книги read-only без восстановления: 8 и 7 исходных листов, состояние `Saved=true`; файлы закрыты без сохранения.

Первые повреждённые книги и recovery log сохранены в `.../audit/rejected/`. Append-only correction audit: `WorkbookDeliveryTool/runs/2026/09/04/80fac4ee-e388-4f25-adf0-a8305950c3c6/audit/20260905T062211Z-workbook-repair-verification.json`. Ручная визуальная проверка исправленной пары всё ещё `NOT RUN`.

Устойчивый машинный audit:

`WorkbookDeliveryTool/runs/2026/09/04/80fac4ee-e388-4f25-adf0-a8305950c3c6/audit/20260904T212706Z-workbook-verification.json`

## 6. Reproduction commands

Запускались из `C:\CodingAgents\codex\projects\OM_Automatization\preparation`:

```powershell
dotnet build '.\WorkbookDeliveryTool\BpmSoftWorkbookDelivery.csproj' -c Release

dotnet run --project '.\WorkbookDeliveryTool\BpmSoftWorkbookDelivery.csproj' -c Release --no-build -- capture

dotnet run --project '.\WorkbookDeliveryTool\BpmSoftWorkbookDelivery.csproj' -c Release --no-build -- generate --source '.\WorkbookDeliveryTool\runs\2026\09\04\80fac4ee-e388-4f25-adf0-a8305950c3c6\evidence\20260904T212051Z-bounded-baseline.json' --model '.\workbooks\BPMSoft.ModelCatalog.xlsx' --lookup '.\workbooks\BPMSoft.LookupCatalog.xlsx'

dotnet run --project '.\WorkbookDeliveryTool\BpmSoftWorkbookDelivery.csproj' -c Release --no-build -- verify --source '.\WorkbookDeliveryTool\runs\2026\09\04\80fac4ee-e388-4f25-adf0-a8305950c3c6\evidence\20260904T212051Z-bounded-baseline.json' --model '.\workbooks\BPMSoft.ModelCatalog.xlsx' --lookup '.\workbooks\BPMSoft.LookupCatalog.xlsx'

dotnet run --project '.\WorkbookDeliveryTool\BpmSoftWorkbookDelivery.csproj' -c Release --no-build -- self-test
```

Для каждой книги также выполнены с exit `0`:

```powershell
python '<minimax-xlsx>\scripts\formula_check.py' <book.xlsx> --json
python '<minimax-xlsx>\scripts\style_audit.py' <book.xlsx> --json
python '<minimax-xlsx>\scripts\xlsx_reader.py' <book.xlsx> --json
python '<minimax-xlsx>\scripts\xlsx_reader.py' <book.xlsx> --quality --json
```

## 7. Зафиксированная версия minimax-xlsx inputs

| File | SHA-256 |
|---|---|
| `templates/minimal_xlsx/_rels/.rels` | `04fb672e829ec6d5942a95014326ff1a89365346d8593631fe58a067379f79db` |
| `templates/minimal_xlsx/[Content_Types].xml` | `af1b5e212ea1c6081649269e2126f0f8c28bea7cc0b8039731f4cdd064eb9a09` |
| `templates/minimal_xlsx/xl/_rels/workbook.xml.rels` | `f60ff34ef9eeae7a2c01f2e316180b6771cce3ce96bbef4a6ca4c8c4c60704c9` |
| `templates/minimal_xlsx/xl/sharedStrings.xml` | `2f5b6bb2ad8bc9b7d3077555917377404f7c14e0b2736ad7ba936ae5b99fca95` |
| `templates/minimal_xlsx/xl/styles.xml` | `7a83fa48c30277de1f2110a699f18fe6496af31a6f7264a01e3cbdff5313da32` |
| `templates/minimal_xlsx/xl/workbook.xml` | `64a5961f39a8c255ba5d0c49d1e0be86c01d25ca1fd87445759d2d92756380dc` |
| `templates/minimal_xlsx/xl/worksheets/sheet1.xml` | `666bff4e34691adb89a53cecddc3d84a6f1f04b38eba353c188b7d1693e342ac` |
| `scripts/xlsx_pack.py` | `a267748f5e0265987094380b99e6d7a61bc3af73a82aa55763d5e99c917e22d9` |
| `scripts/xlsx_reader.py` | `585071399f9e5683700081db959812bcce02edb694bb5a7dece94d9b89a5e1f0` |
| `scripts/formula_check.py` | `94661b6ebbfef0450b762392b45477678a7cd641982e0eb9397fef7bcb716e15` |
| `scripts/style_audit.py` | `4ac17193e5ad306cb89007a7fe8492b9117de0d4b0136a3ffaa65223e60abcb6` |
| `scripts/libreoffice_recalc.py` | `a7967a78aff2f8f74f4975db2f674215a4dce1086c38b5312e353f7bad084f87` |

## 8. Не выполнялось и ограничения

- full catalog;
- BPMSoft Write/Manage/compile/save/create/update/delete;
- Google input;
- изменение `SyncOM`, contracts, SDD drafts, handoff, prototype или старого evidence;
- индексная загрузка/apply;
- LibreOffice recalculation/render — LibreOffice отсутствует;
- повторная ручная визуальная проверка исправленных книг владельцем в desktop Excel.

## 9. Что должен проверить владелец перед G5

Открыть обе оригинальные книги в desktop Excel и подтвердить:

1. Excel не показывает repair/recovery prompt;
2. порядок видимых листов верный, `ValidationLists` не виден;
3. заголовки закреплены, фильтры работают, ширины читаемы;
4. фактические ID/actual/fingerprint/index cells не редактируются, а разрешённые `Desired*`/comment/value cells доступны;
5. в Model book видны все пять bounded schema layers, включая два слоя `Account` из scope;
6. `Indexes` содержит ровно `Index1/Code/unique` и `Index2/Name/non-unique` для `Account/Test1`;
7. manifest обеих книг показывает один и тот же `PairId`, `PullRunId` и `PairBaselineHash`.

Рекомендация роли для следующего verification этапа: **передать пару в `05-verification-and-report` как `PASS WITH LIMITS`**, сохранив `VerifiedBoundedBaseline`, `INDEX_SYNC_UNRESOLVED`, пропущенный Tier 2 и необходимость ручной проверки владельцем. Это не human acceptance и не закрытие G5.

## 10. Template v3 после owner contract correction — 2026-09-05

После независимого FAIL-report `20260905T120633Z-01-workbook-evidence-review.md` владелец изменил contract:

- `ReferenceRecordId` и `ReferenceDraftRowToken` разрешено заполнять одновременно;
- будущая загрузка применяет приоритет `valid ReferenceRecordId -> resolved ReferenceDraftRowToken -> empty reference`; некорректный непустой GUID блокирует загрузку;
- Excel sheet protection остаётся только на полностью read-only листах;
- mixed-листы `Schemas`, `Columns`, `LookupRegistry`, `LookupValues` доступны для редактирования целиком; будущий parser/compare остаётся hard gate.

Генератор и verifier обновлены, `TemplateVersion=3-bounded-research`. Обе книги пересозданы из того же immutable bounded source и установлены вместе по canonical paths; template-v2 пара сохранена как `audit/rejected/20260905T122311Z-*.superseded-template-v2.xlsx`.

Canonical template-v3:

| Книга | Sheets | Data rows | SHA-256 | Canonical worksheet XML hash |
|---|---:|---:|---|---|
| `workbooks/BPMSoft.ModelCatalog.xlsx` | 8 | 165 | `7fb01fd5f4af039beb45e7e77819385d9c25beac6762616cf6212e6ac32a849f` | `80449ff6fb4fed845e7860b5ad4ef8ff6c35f2d9ac0d79e570e687a67869f25d` |
| `workbooks/BPMSoft.LookupCatalog.xlsx` | 6 | 36 | `48719140d6ecf4a7b21c61fc381eccb8aa95f2b8e32f437c360ed14e96f07b2d` | `2d2a75f943fc5ef4a115793e9f23e7e0e39748408158c1cbaded7be0bb0e7358` |

Build, self-test, tool verify, Tier 1, style/read-back и desktop Excel checks прошли. Excel подтвердил отсутствие recovery, protection только read-only листов, редактируемость mixed-листов, одновременный ввод обоих reference fields, блокировку записи на read-only листах и разрешённые resize/filter/sort operations. Tier 2/render остаётся skipped, поскольку LibreOffice отсутствует. Audit: `WorkbookDeliveryTool/runs/2026/09/04/80fac4ee-e388-4f25-adf0-a8305950c3c6/audit/20260905T122353Z-workbook-template-v3-canonical-verification.json`.

## 11. Independent correction к template-v3 verification — 2026-09-05

Report `20260905T183700Z-01-workbook-evidence-review-v3.md` подтвердил все перечисленные проверки, кроме фактической сортировки protected read-only sheets. `Protection.AllowSorting=True` и OOXML `sort=0` являются только permission flags: Excel 16 всё равно блокирует перестановку locked cells. `Range.Sort` и UI sort не изменили порядок на protected `WorkspaceInventory`/`Readme`; контроль на unprotected `Schemas`/`LookupValues` прошёл. Resize и real dropdown/context filter на protected sheets прошли.

Поэтому фраза выше о подтверждённых sort operations уточняется: она доказана только для unprotected mixed sheets, но не для protected read-only sheets. Текущий independent verdict — `FAIL — REQUEST CHANGES` до явного owner выбора между сохранением protection и обязательной ручной сортировкой read-only листов.

## 12. Owner resolution: protected read-only sort waived — 2026-09-05

Владелец сохранил Excel protection полностью read-only листов и явно снял обязательность фактической сортировки их locked ranges. Для таких листов acceptance проверяет resize, existing filters и блокировку записи; mixed-листы остаются unprotected и сортируются обычными средствами Excel. `sort=0`/`AllowSorting=True`, если присутствует, является best-effort metadata и не считается доказательством сортируемости.

Изменение относится к contract/acceptance criteria, поэтому canonical template-v3 книги не пересоздавались и их hashes остаются неизменными. Verifier больше не требует sort permission для protected sheets. После Release build, self-test и tool verify нужен независимый rerun по обновлённому prompt; только его PASS разрешает переход к G5 package.

## 13. Independent rerun после owner resolution — PASS

Report `20260905T191555Z-01-workbook-evidence-review-v3-rerun.md`, SHA-256 `b45f598dd7aeb1fdbbd7a3da13f33a1956f822a5a0c8ab883e0f3478040bc6ca`, независимо подтвердил все обязательные критерии обновлённого bounded contract. Включены clean Release build/self-test/verify, Tier 1, OOXML/source/audit/hash/secret/index checks и реальные Excel 16 operations: no recovery, write-block всех 10 read-only sheets, editability всех 4 mixed sheets, resize, existing filters, simultaneous reference fields и control sorting на mixed sheets.

Verdict: `PASS`. Canonical `.xlsx` и их hashes не изменились. Tier 2/render `SKIPPED` из-за отсутствия LibreOffice; G5 не закрыт; будущий loader/reference precedence не реализован; `INDEX_SYNC_UNRESOLVED` сохраняется.
