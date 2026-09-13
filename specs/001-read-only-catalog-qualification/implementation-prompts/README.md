# Prompts реализации Feature 001

Этот пакет делит уже утверждённую реализацию Feature 001 на три prompts, строго
соответствующие S01–S03 из [implementation-slices.md](../implementation-slices.md).
Он не заменяет `tasks.md`, не изменяет source of truth и не является разрешением на
реализацию, live BPMSoft, ввод credentials, Write/Manage, Excel, compare, Apply,
browser или Git actions.

## Порядок

1. [S01 — safe offline entry](S01-safe-offline-entry.md): базовая strictly-offline
   граница и задачи T001–T013.
2. [S02 — lossless ordered inventory](S02-lossless-ordered-inventory.md):
   инвентарь и постраничное чтение, T014–T024; зависит от завершённого S01.
3. [S03 — two-pass qualification and safe decision](S03-two-pass-qualification-safe-decision.md):
   qualification, evidence, diagnostics и controlled handoff, T025–T046; зависит от S01 и S02.

S01 также содержит отложенные интеграционные задачи T038 и T042. Их не выполняют
до выполнения зависимостей S02/S03, указанных в `tasks.md`.

## Общие неизменяемые ограничения

- Использовать только sanitized fixtures и fake transport до T045.
- Сохранять `FULL_CATALOG_NOT_QUALIFIED` и `INDEX_SYNC_UNRESOLVED`.
- Не добавлять Write/Manage, Excel, compare, Apply, browser или Git actions.
- При `TARGET_STATE_CHANGED_DURING_QUALIFICATION` запрещены Pass C, retry и
  automatic new double pass; новый run возможен только после нового ручного запуска или прямой текущей просьбы пользователя.
- T046 — conditional manual run: offline и automatic paths имеют zero terminal credential
  prompts и zero HTTP sends; вручную запущенный `catalog qualify` не требует `AuthorizationReference`.
- Не изменять `preparation/docs/product-specs/`.

Полные requirements, dependencies, exact paths, checks и stop conditions находятся в
[tasks.md](../tasks.md); при конфликте он и конституция имеют приоритет над prompt.
