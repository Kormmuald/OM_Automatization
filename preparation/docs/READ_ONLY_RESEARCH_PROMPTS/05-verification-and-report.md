# Субагент: интеграционная проверка и пакет решения владельца

**Рекомендуемая модель:** `gpt-5.6-sol`, reasoning `high`.

## Цель

Проверить всю разрешённую цепочку как единый результат: research evidence → owner-approved contract → две Excel-книги/optional tool. Подготовить отчёт для обсуждения с владельцем и только после его решения зафиксировать итог.

## Входы

- outputs и evidence `01`–`04`;
- approved owner decisions G3/G4;
- `docs/PROJECT_HANDOFF.md`, SDD drafts и workbook contract.

## Проверки

Сопоставь каждый relevant acceptance criterion с evidence. Проверь: read-only boundaries; deterministic order/pagination proof; schema/lookup mapping; pair metadata/IDs; workbook contract; generated artefacts; absence secrets/external links; build/tests/manual checks. Обязательно отдельно проверь, что раздельные `Columns.ActualIndexed` и `Indexes` не потеряли и не исказили index membership при выгрузке. Не считай это доказательством безопасной загрузки: будущий index load/apply требует самостоятельного gate против ложных add/drop операций; неоднозначность маркируется `INDEX_SYNC_UNRESOLVED` и может привести к исключению index loading из текущей версии. Полный каталог не является acceptance condition текущей workbook delivery. Явно укажи `pass/fail/partial/not run` и path/command для каждого claim.

## Stop conditions

Не подменяй отсутствующую проверку выводом агента. Если evidence не подтверждает claim, contract противоречив или `.xlsx`/tool выходит за approved scope, остановись с `request changes` и одним точным вопросом владельцу. Не меняй handoff/SDD/contract до обсуждения.

## Два этапа выхода

1. До обсуждения: draft report с criterion-to-evidence matrix, gaps, risks и recommendation `accept / accept with limits / request changes / reject`.
2. После явного решения владельца: обнови только согласованные SDD/contract/handoff files, укажи exact next step и сохрани фактические evidence paths. Не фабрикуй acceptance.
