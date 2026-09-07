Работай из `C:\CodingAgents\codex\projects\OM_Automatization\preparation` как task, следующий за отдельным orchestration task.

**Первое действие:** запроси у владельца итоговый output отдельного оркестратора, запущенного по `docs\READ_ONLY_RESEARCH_PROMPTS\00-orchestrator.md`: role split, порядок, control gates, риски и его единственный вопрос для G1. Не запускай `00-orchestrator.md` повторно и не начинай BPMSoft probe, `.xlsx` или production work, пока этот output не предоставлен.

После получения output:

1. Сверь его с `docs\PROJECT_HANDOFF.md`, раздел 13, и тремя файлами `docs\SDD_DRAFTS\`.
2. Если он противоречит источникам или не содержит required gates, верни один source-backed gap и жди решения владельца.
3. Если он согласован, задай владельцу только G1-вопрос: разрешает ли он запуск strictly read-only researcher по `01-read-only-researcher.md`.

Source of truth: `docs\PROJECT_HANDOFF.md`, раздел 13, три файла в `docs\SDD_DRAFTS\` и предоставленный owner orchestration output. Строго соблюдай exact stop condition из handoff. Credentials, cookies, CSRF и login response не запрашивай, не принимай и не сохраняй.
