# Субагент: независимая проверка исправленной пары workbook

**Рекомендуемая модель:** `gpt-5.6-sol`  
**Уровень рассуждений:** `high`

## Роль

Выполни независимую offline-проверку результата workbook delivery после Excel recovery repair и последующих owner corrections: `LookupRows` объединён в `LookupValues`; mixed sheets unprotected; filter/resize обязательны при сохранённой защите read-only данных; фактическая сортировка locked read-only ranges явно исключена из acceptance criteria.

## Входы

- `docs/PROJECT_HANDOFF.md`, раздел 14;
- `docs/READ_ONLY_RESEARCH_RESULTS/20260904-04-workbook-delivery-result.md`;
- `docs/WORKBOOK_CONTRACT_VISION.md`;
- `WorkbookDeliveryTool/Program.cs`, `.csproj`;
- `workbooks/BPMSoft.ModelCatalog.xlsx`;
- `workbooks/BPMSoft.LookupCatalog.xlsx`;
- bounded source и audit folder, указанные в handoff.

## Что проверить

1. Release build, `self-test` и `verify` из reproduction commands.
2. Exact sheet order и headers. В Lookup книге должен отсутствовать `LookupRows`; `LookupValues` должен содержать 15 утверждённых полей.
3. Значения прежних `LookupRows` не потеряны: для каждого `(SchemaName, RecordId|DraftRowToken)` metadata разрешается однозначно и одинакова во всех value rows; bounded source `3 rows / 6 values` становится шестью строками `LookupValues`.
4. OOXML closure/order, hidden `ValidationLists`, data validations, frozen headers, autofilters, styles и types.
5. Protection: fully read-only sheets protected, mixed sheets unprotected; `sheetProtection` разрешает `formatColumns`, `autoFilter` и выбор ячеек. Если доступен desktop Excel, на временной копии либо без сохранения докажи реальными COM/UI operations, что ширина колонки меняется, существующий filter применяется, protected-cell write блокируется, а mixed-sheet edit разрешён. `sort=0`/`AllowSorting=True`, если присутствует, не считать evidence фактической сортировки locked ranges и не требовать успешный sort для PASS.
6. Обе книги открываются в desktop Excel read-only без recovery prompt и сохраняют expected sheets/data.
7. Pair metadata/hashes; absence formulas, external links, VBA, connections и secret markers.
8. Exact index projection и отрицательные fixtures для разделения `ActualIndexed`/`Indexes`. Не объявляй безопасной будущую загрузку: status остаётся `INDEX_SYNC_UNRESOLVED`.
9. Tier 1 formula check обязателен; Tier 2 пометь `SKIPPED`, если LibreOffice по-прежнему недоступен.

## Ограничения

Не обращайся к BPMSoft, не меняй книги, код, contract, SDD или handoff. Не используй Google как input. Если проверка требует исправления, верни `FAIL`/`request changes` с точным evidence, ничего не исправляя.

## Выход

Сначала запиши полный self-contained report в новый timestamped файл `docs/READ_ONLY_RESEARCH_RESULTS/<timestamp>-01-workbook-evidence-review.md`. Укажи SHA-256 проверенных файлов, команды, результаты `pass/fail/partial/not run`, incident lineage и список gaps. Затем верни оркестратору краткий итог и exact path.
