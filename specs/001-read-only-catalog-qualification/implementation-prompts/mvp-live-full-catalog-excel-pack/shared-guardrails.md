# Shared guardrails

Перед работой каждый агент читает `AGENTS.md`, `.specify/feature.json`, текущий
`HANDOFF.md`, `project-specific-info.md` этого pack, свой prompt, acceptance/handoff
предыдущего этапа и только относящиеся к его этапу canonical artifacts/code/tests.
Active feature должна быть 001.

Не изменять `preparation/docs/product-specs/**`, `preparation/PrototypeReadOnlyPull/**`,
`preparation/WorkbookDeliveryTool/**` и `preparation/workbooks/**`. Они read-only
reference. Не выполнять Git mutation и не затирать чужие dirty changes.

Сохранять все существующие CLI/offline/probe возможности. Запрещены BPMSoft write,
Manage, compile/save/create/update/delete, Compare, Apply, browser write и Git actions.
Индексы только read-only; `INDEX_SYNC_UNRESOLVED` сохраняется.

Credentials/session material никогда не передаются через chat/args/config/files/logs/
evidence. Пользователь вводит URL/login/password только в интерактивном terminal.
Фактические lookup values допустимы в локальных output Excel, но не в audit/evidence.

Pass A и Pass B — два полных независимых чтения одного sealed target/scope. B не
использует данные/кэш A. После расхождения —
`TARGET_STATE_CHANGED_DURING_QUALIFICATION`, без retry/Pass C/автоматического rerun.
Descending probe не является Pass B.

Каждый worker пишет тесты и evidence только своего этапа, не принимает собственную
работу и не начинает следующий этап. Каждый reviewer независим, не исправляет код и
классифицирует результат как `Blocker|Fix|Log|Pass`.

Runtime evidence этапов:
`specs/001-read-only-catalog-qualification/verification/mvp-slices/<SXX>/`.
Не создавать acceptance/handoff заранее. Они появляются только после фактической
реализации и независимого `Pass`.
