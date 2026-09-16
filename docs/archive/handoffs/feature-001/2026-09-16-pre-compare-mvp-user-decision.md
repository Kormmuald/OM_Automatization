# Актуальный handoff — Feature 001 и разрешённый Compare MVP

**Дата:** 2026-09-16  
**Active feature:** `001-read-only-catalog-qualification` (подтверждён `.specify/feature.json`).

## Статус строгой Feature 001

Строгая Feature 001 **не квалифицирована**. Последний controlled full live run
`75e23f1f-d177-439a-b53e-ce566e13845c` завершился fail-closed на
`CATALOG_ORDER_OR_PAGING_UNQUALIFIED` (`RetryCount=0`). Это не разрешает
называть best-effort export qualified, безопасным full-catalog baseline либо
реализацией строгих Features 001–004.

Последняя строгая позиция и безопасные metadata перенесены без изменений в
`docs/archive/handoffs/feature-001/2026-09-16-handoff-before-compare-mvp-authorization.md`.
Этот архив — историческое evidence, не источник текущих решений.

## Явно разрешённый прагматичный Compare MVP

Владелец 2026-09-16 разрешил реализовать отдельный ограниченный vertical slice
Compare поверх **последней user-local best-effort пары**, указанной в предыдущем
handoff. Не копировать эту пару, её реальные lookup values, credentials, URL,
cookies или raw response в Git, репозиторий, чат, логи, tests либо handoff.

MVP использует fresh practical best-effort read-only состояние BPMSoft. Он не
доказывает полноту каталога и не заменяет strict qualification. Поддерживаемая
цель: сравнить выбранные простые editable intent и сформировать report/plan;
Apply, writeback, BPMSoft write, browser write, SQL mutation, Git push и delete
запрещены.

Разбор продолжается по независимым элементам: неполные, неизвестные,
повреждённые и неподдерживаемые изменения попадают в safe report как
корректируемые blocker items и исключаются только из затронутых операций.
Полная остановка допустима лишь когда пару невозможно безопасно прочитать или
связать.

## Формат артефактов и конфиденциальность

Compare создаёт в user-local output root:

- `compare-report.md` — безопасный человекочитаемый diff без raw lookup values;
- `compare-plan.json` — единый конфиденциальный локальный machine-readable plan.

`compare-plan.json` обязан хранить полные нормализованные **desired values только
для planned operations**, чтобы будущий Apply мог выполнить их после отдельного
разрешения. Он также содержит hashes/bindings для staleness checks, но не
содержит credentials, URL, cookies, write endpoint или скрытую Apply-команду.
Файл нельзя печатать целиком, добавлять в Git, report, logs, handoff или чат.
Примеры и tests используют только synthetic non-production values.

До первого live запуска обязательны сборка и минимум local happy-path + blocker
проверок. Перед каждым отдельным live CLI запуском требуется новое явное
разрешение владельца с целью, точной командой/режимом, ожидаемыми read endpoint
IDs, подтверждением отсутствия BPMSoft/Excel writes и ожидаемым безопасным
критерием результата. Credentials вводит только владелец интерактивно.

## Следующее действие

Реализовать Compare MVP в существующих CLI/domain/application/adapter проектах
после подтверждённого scope. Исполнитель не выполняет live access до отдельного
разрешения владельца; после local validation требуется запрос разрешения на
первый ровно описанный live read-only запуск.
