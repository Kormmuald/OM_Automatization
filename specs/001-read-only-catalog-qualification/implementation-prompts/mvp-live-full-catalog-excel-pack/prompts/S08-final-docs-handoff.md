# S08 worker — final documentation and handoff

Model: `gpt-5.6-terra`; reasoning: `medium`.

На основании accepted S00–S07 обнови только документацию, canonical task statuses,
safe verification summaries и единственный текущий Feature 001 `HANDOFF.md`. Не
фабрикуй PASS; при live blocker оставь `FULL_CATALOG_NOT_QUALIFIED` открытым. Устаревший
handoff архивируй по `AGENTS.md` только если он действительно заменяется.

Operator docs должны содержать build/install, offline suite, запрос запуска стенда,
terminal-only credentials, opt-in live command, output tree, diagnosis, Excel inspection,
no-retry и read-only boundaries. Отрази фактические models/agents/reviews/evidence.

Не меняй production/test code и не выполняй новый live run. Верни changed docs,
состояние требований/tasks, unresolved blockers и точный следующий шаг.
