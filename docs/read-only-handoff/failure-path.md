# Обработка blocker

Сохраняйте в новом append-only run root только safe reason, scope, recovery,
next action, approved hashes/counts и relative paths. Не записывайте secrets, raw
response bodies или lookup values.

При `SCHEMA_INVENTORY_UNQUALIFIED`, `UNKNOWN_SHAPE_UNQUALIFIED`,
`TARGET_STATE_CHANGED_DURING_QUALIFICATION` или paging/shape blocker остановите run.
Не выполняйте retry, Pass C, automatic rerun или partial Excel publication; не
запускайте Compare, Apply, Write, Manage, browser/Git/index actions. Нужны human
decision и отдельный новый manual admission.

S07 фактически завершился `exit 2` с `SCHEMA_INVENTORY_UNQUALIFIED` /
`UNKNOWN_SHAPE_UNQUALIFIED`, `RetryCount=0`; `output/` пуст, `.xlsx=0`, success seal
отсутствует.
