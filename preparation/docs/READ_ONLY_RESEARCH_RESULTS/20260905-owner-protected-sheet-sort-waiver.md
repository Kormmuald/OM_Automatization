# Owner decision: protected read-only sheet sorting

Дата решения: 2026-09-05  
Статус: owner decision; не является G5 acceptance

## Дословное решение владельца

> да, подтверждаю, что сортировка не особо нужна.

## Зафиксированное толкование в контексте предложенного выбора

- Excel protection полностью read-only листов сохраняется.
- Фактическая ручная сортировка locked ranges на этих листах не является обязательным acceptance criterion.
- Изменение ширины колонок, применение существующих фильтров и блокировка записи на read-only листах остаются обязательными.
- Mixed-листы `Schemas`, `Columns`, `LookupRegistry`, `LookupValues` остаются полностью unprotected/editable и могут сортироваться обычными средствами Excel.
- `sort=0` / `Protection.AllowSorting=True`, если присутствует, считается best-effort metadata и не является evidence успешной сортировки locked cells.

## Последствия

Canonical template-v3 `.xlsx` не требуют пересоздания: решение меняет acceptance contract, а не данные или protection map. Обновляются contract, SDD drafts, continuation prompts и verifier. После локальных build/self-test/verify требуется независимый rerun; только его PASS открывает подготовку G5 package.

Без изменений остаются `INDEX_SYNC_UNRESOLVED`, пропущенный LibreOffice Tier 2, отсутствие full-catalog/generalisation evidence и запрет BPMSoft Write/Manage/Google input/изменений `SyncOM/`.
