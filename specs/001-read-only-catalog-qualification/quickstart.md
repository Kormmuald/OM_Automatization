# Quickstart validation guide

## Scope

This guide validates the future feature with offline sanitized fixtures. It does not authorize or perform a live BPMSoft run, write, browser action or Git action.

## Prerequisites

- Windows with the selected .NET 10 SDK.
- Fresh build of the planned solution and only fixtures under `tests/fixtures/read-only/`.
- A writable temporary run root outside tracked workbooks.

## Offline validation path

1. Run the unit and adapter contract suites listed in [test-plan.md](test-plan.md).
2. Execute `catalog validate-offline --fixture <target-change fixture>`.
3. Confirm non-success result `TARGET_STATE_CHANGED_DURING_QUALIFICATION`, one sealed run root, audit/evidence hashes, no third pass and no network capture.
4. Repeat with every negative paging and secret/value-canary fixture. Confirm named blocker, scanner report without matched secret/value, and no PASS artifact.
5. Execute the clean fixture with fake transport. Confirm two passes reconcile, every inventory item has a status, evidence schema/scan pass and output is `HUMAN_REVIEW_REQUIRED` rather than Apply permission.

## Условный ручной путь проверки на реальном стенде

Этот путь выполняется только после успешного offline validation path. Оператор вручную
запускает `catalog qualify` для одного неизменного local target и declared read scope либо
агент действует по прямой текущей просьбе пользователя в доступном чате. Отдельный
`AuthorizationReference` не нужен. Credentials вводятся оператором исключительно в
terminal prompt и нигде не сохраняются; агент не начинает live run без такой прямой просьбы.

Если два прохода расходятся из-за изменения target, CLI возвращает
`TARGET_STATE_CHANGED_DURING_QUALIFICATION`, запечатывает безопасные артефакты и требует
нового ручного запуска либо новой прямой просьбы пользователя для любого последующего run. Он не выполняет loop, retry или скрытый
повтор. Этот этап остаётся read-only: Excel, compare, Apply, browser write и Git не
выполняются.

## Expected evidence

`run-journal.json`, safe audit/evidence envelopes, schema/scan report, endpoint capture summary, per-pass count/hash reconciliation, unsupported diagnostics and gate outcomes. No raw lookup values, login response, cookies, CSRF or password may appear.
