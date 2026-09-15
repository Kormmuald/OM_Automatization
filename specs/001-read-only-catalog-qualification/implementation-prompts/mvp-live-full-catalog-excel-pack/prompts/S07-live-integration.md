# S07 worker — opt-in live integration and Excel verification

Model: `gpt-5.6-sol`; reasoning: `high`.

Запускай только после accepted S06 и сообщения оркестратора, что пользователь в текущем
чате подтвердил «стенд запущен». Это прямая авторизация одного read-only T046 run.

Прочитай `verification/live-mvp-verification-prompt.md`, проверь build/offline evidence,
запусти dedicated `--live --manual` harness и предоставь пользователю terminal для
самостоятельного ввода URL/login/password. Никогда не проси credentials в chat.

Выполни ровно один Pass A/B full-catalog run, создай новую Excel-пару, проверь endpoint
matrix/writeCallCount=0, reconciliation, OOXML/read-back/manifests/1:1 projection и safe
evidence. Можно открыть книги read-only для пользователя. Не выводи lookup cell contents
в chat/evidence. При любом blocker остановись без retry; новый запуск требует нового
подтверждения.

Не исправляй production code в этом stage. Верни команды, safe aggregates, RunId,
Excel/evidence paths, blocker или human-review result.
