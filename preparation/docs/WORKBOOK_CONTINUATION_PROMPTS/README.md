# Пакет продолжения workbook-orchestrator

Этот пакет предназначен для нового task и содержит только оставшуюся часть уже начатой цепочки. Исследовательские роли `01`–`04` из прежнего пакета не повторяются: их evidence, owner decisions G1–G4 и ограничения перенесены в `docs/PROJECT_HANDOFF.md`, раздел 14.

Порядок запуска:

1. `00-orchestrator-continuation.md` — основной prompt нового task.
2. `01-workbook-evidence-reviewer.md` — независимая offline-проверка исправленной пары книг и инструмента.
3. `02-g5-decision-preparer.md` — criterion-to-evidence matrix и рекомендация владельцу.
4. `03-owner-decision-recorder.md` — только после явного решения владельца на G5.

Для всех ролей рекомендуются только модели семейства GPT-5.6 и reasoning `high`; `xhigh` и `gpt-6-astra` в этом пакете не используются.
