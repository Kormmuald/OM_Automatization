# Актуальный handoff — Feature 001: read-only catalog qualification

**Дата обновления:** 2026-09-14  
**Статус:** единственный актуальный handoff Feature 001. Исторические передачи находятся в `docs/archive/handoffs/` и не задают текущий порядок работы.

## Контекст

- Active feature: `specs/001-read-only-catalog-qualification` (проверить `.specify/feature.json` перед продолжением).
- Класс инициативы: `L2 / l2-pilot`; обязательные hooks из `.specify/extensions.yml` сохраняются.
- Неизменяемый общий источник: `preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md`. Его и прочие source drafts не изменять.
- Canonical planning artifacts: `spec.md`, `plan.md`, `tasks.md`. Единственный formal tracker задач — `tasks.md`; S04 pack и supplemental tasks служат входами и трассировкой, но его не заменяют.

## Состояние реализации

В рабочей копии начата S04 live-readiness foundation: typed catalog sources, exact-two-pass qualification, canonical fingerprint, fixture source и fake-HTTP source, manual invocation policy, append-only evidence/diagnosis и CLI-команды `catalog validate-offline`, `catalog qualify`, `catalog diagnose --run`.

Согласно последней зафиксированной проверке, сборка Release и четыре custom test executables прошли только на local fixtures/fake HTTP. Реальные BPMSoft/HTTP, credentials, browser, Excel, compare, Apply, Write/Manage и T046 не выполнялись. Все canonical `S04-001…S04-023` и `T025…T046` остаются unchecked: частичная реализация не является приемкой или evidence их завершения.

## Текущая цель и следующий шаг

Владелец подтвердил расширение Feature 001: ручная CLI-выгрузка полного текущего состояния объектной модели и справочников из локального BPMSoft должна создавать новую датированную пару Excel-книг и не перезаписывать canonical книги.

Перед дальнейшей реализацией или live execution нужно обновить mutable artifacts Feature 001 через class-aware `/SpecKit Clarify` → `/SpecKit Plan` → `/SpecKit Tasks`, чтобы закрепить full scope, endpoint/request matrix, paging/scale/completeness stop conditions, safe evidence protocol, ручной G-2 и output contract. Это решение заменяет ранее актуальный совет сразу переходить к `/SpecKit Implement`.

## Непереговорные ограничения

- Read-only и deny-by-default: только документированные endpoint IDs; `GET_PACKAGES` остаётся defect-to-remove.
- Credentials вводит только оператор в terminal prompt при разрешённом ручном запуске; не хранить и не передавать их через arguments, config, skills, logs или evidence.
- `FULL_CATALOG_NOT_QUALIFIED` и `INDEX_SYNC_UNRESOLVED` остаются открытыми. Нет Write/Manage, browser, Excel mutation, compare, Apply или Git действий.
- После `TARGET_STATE_CHANGED_DURING_QUALIFICATION` запрещены Pass C, retry и автоматический новый запуск. Новый run требует ручного запуска оператора либо прямой текущей просьбы пользователя.
- `HUMAN_REVIEW_REQUIRED`, документы и offline tests не являются разрешением на Apply или приемкой.

## Предшественники

- `preparation/PrototypeReadOnlyPull` — legacy reference/characterization-прототип read-only capture для Feature 001; не production implementation.
- `preparation/WorkbookDeliveryTool` — legacy technical predecessor Excel generation для Feature 002; его bounded baseline не заменяет будущую full-catalog реализацию.
- `docs/read-only-handoff/` — актуальный операторский пакет; он остаётся на месте и проверяется задачами/тестами.

## Перед продолжением

Прочитать `AGENTS.md`, `.specify/feature.json`, `.specify/project.yml`, этот файл, canonical `spec.md`/`plan.md`/`tasks.md`, актуальный исходный код и regression tests. Сохранить существующие чужие изменения в dirty worktree; не делать reset или checkout.
