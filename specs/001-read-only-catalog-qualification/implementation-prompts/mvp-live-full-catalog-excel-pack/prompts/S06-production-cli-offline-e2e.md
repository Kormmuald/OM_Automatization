# S06 worker — production CLI and offline E2E

Model: `gpt-5.6-terra`; reasoning: `high`.

Соедини accepted S01–S05 в единственный `BpmSoftSync.Cli` composition root. Сохрани
старые команды/aliases. Manual live command должен быть реально собран, но в S06
проверяется только fixture/fake-handler. Убери hardcoded fixture target/scope, synthetic
production pages и helper-only обходы.

Добавь полный production-process E2E и adversarial/security regression: login→full
catalog→Pass A/B→snapshot→Excel pair→safe evidence, zero writes, no secret/raw leakage,
safe `catalog diagnose`, atomic output и backward compatibility. Создай отдельный
opt-in live integration harness/CLI mode с обязательными `--live --manual`, который не
входит в default tests/CI и пока не запускается.

Выполни Release build и все offline executables, создай fresh safe report и
`verification/live-mvp-verification-prompt.md`. Реальный стенд не трогай до S07 human
gate. Верни exact commands/results и readiness/blockers.
