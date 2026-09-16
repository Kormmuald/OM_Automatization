# Актуальный handoff — доказанный ограниченный Compare MVP, Apply discovery

**Дата:** 2026-09-16
**Активный контекст:** `.specify/feature.json` остаётся `001-read-only-catalog-qualification`. Это не переключает контекст на строгие Features 003/004 и не квалифицирует Feature 001.

## Статус

Строгая Feature 001 остаётся **неквалифицированной**: controlled full run остановился на `CATALOG_ORDER_OR_PAGING_UNQUALIFIED`; qualified full-catalog baseline отсутствует. Этот факт не изменён Compare MVP.

Прагматичный user-approved Compare MVP реализован как отдельный ограниченный vertical slice. Он использует latest user-local `catalog export-best-effort` пару только как practical input, не как qualified baseline. Реальные lookup values, credentials, origin, cookies, raw responses и содержимое локальных plans не попадают в repository, handoff, chat, logs или tests.

## Подтверждённый Compare MVP

- Read-only CLI: `catalog compare-mvp --model ... --lookup ... --target ... --scope best-effort --manual --live --output-root <new-user-local-dir>`.
- Excel reader отклоняет formulae, macros, connections и external links, проверяет pair binding, known sheets/headers/validation lists и поддерживает normal desktop-Excel round-trip (`sharedStrings` и объединённые `sqref`).
- `LookupValues.ColumnUId` добавлен в новые exports. Для legacy pair без него Compare выдаёт `LOOKUP_VALUE_COLUMN_IDENTITY_MISSING`, без fallback по name.
- Поддерживаются только `Columns.DesiredRequired` для existing `Own` columns и typed scalar `LookupValues.Value`; `Null`, `EmptyString` и `Value` различаются. `LookupReference.ReferenceRecordId` остаётся opaque intent.
- `compare-report.md` value-free; confidential local `compare-plan.json` содержит raw normalized desired values только planned operations.

## Safe live evidence

Отдельные manually authorized read-only runs создали новую best-effort pair, затем подтвердили:

- OM intent: `completed`, `operations=1`, `blockers=0`.
- Combined OM + typed lookup intent: `completed`, `operations=2`, `blockers=0`.

Последний result достаточен для доказательства **ограниченного Compare MVP**: Excel pair была прочитана, fresh practical state получен, report и local plan созданы, BPMSoft/Excel не изменялись. Это не является доказательством Apply, target-wide completeness, прав записи или безопасности любой write operation.

## Принятые решения и запреты

1. Можно начать **Apply MVP discovery/design** с отдельным агентом и owner. Реализация Apply, write-capable transport, write endpoints, BPMSoft writes, browser write, backup/writeback, Git commit/push и delete всё ещё запрещены.
2. Будущий Apply потребляет exact immutable local `compare-plan.json`; он не пересчитывает diff и не использует fallback по `Name`/`Code`.
3. Каждому write preflight/probe и каждому Apply-run требуется новое явное owner authorization. Credentials вводит только пользователь интерактивно.
4. `ReferenceRecordId` требует будущего preflight: format, target existence, permissions, reference graph и dependencies. Их отсутствие не обходится.
5. `DesiredState`, `DesiredIndexed`, `DesiredPackageName`, registry/comments, structural/new/deleted rows, indexes, `ReferenceDraftRowToken` и unknown shapes остаются denied/blocker territory.

## Отложено в TODO

Актуальный список открыт в `docs/TODO.md` (раздел `Apply MVP discovery`). Особенно важны: owner allowlist candidate kinds, version-specific write contracts, deny-by-default dispatcher, immutable-plan admission, per-operation preflight/read-back, human confirmations, backup/recovery boundary и full negative/fault test matrix.

## Следующее действие

Передать подготовленный prompt отдельному discovery-агенту. Он должен вместе с владельцем выбрать Apply MVP boundary и сформировать design/decision package, не выполняя code writes, BPMSoft requests или Apply.

Предыдущий handoff сохранён как historical evidence: `docs/archive/handoffs/feature-001/2026-09-16-compare-mvp-pre-apply-discovery.md`.
