# S05 worker — atomic Excel materialization

Model: `gpt-5.6-sol`; reasoning: `high`.

Создай adapter-layer Excel/OOXML consumer `QualifiedCatalogSnapshot/v1`. Перепиши
нужные production semantics `WorkbookDeliveryTool`; не создавай runtime dependency на
`preparation/*` и не меняй prototype/templates. При необходимости создай owned,
versioned, sanitized templates внутри текущего приложения.

Атомарно создавай новую пару под уникальным RunId: Model sheets Readme/Manifest/
WorkspaceInventory/Schemas/Columns/Indexes/ValidationLists/PullConflicts и Lookup sheets
Readme/Manifest/LookupRegistry/LookupValues/ValidationLists/PullConflicts. Соблюдай
точные headers/order/styles/protection/validations/допустимые formulas; никаких external
links/VBA/connections. Проверяй OOXML closure, read-back, pair binding, deterministic
hashes, 1:1 projection и ActualIndexed/Indexes separation.

Tests-first с synthetic full snapshot и fault injection публикации. Не подключаться к
BPMSoft и не менять application qualification. Верни diff, generated fake-data pair и
verification evidence.
