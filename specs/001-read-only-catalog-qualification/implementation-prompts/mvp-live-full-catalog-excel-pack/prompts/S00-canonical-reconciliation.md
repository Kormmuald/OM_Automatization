# S00 worker — canonical reconciliation

Model: `gpt-5.6-terra`; reasoning: `high`.

Цель: до кода согласовать mutable Feature 001 artifacts с утверждённым MVP full live
pull + Excel output. Используй обязательные class-aware planning stages и hooks с
локальным порядком Plan→Tasks→Analyze. `/SpecKit Implement` в S00 не запускай: успешный
S00 только подготавливает canonical входы для последующих implementation stages.

Прочитай монолитную постановку, immutable common vision, current spec/plan/research/
data-model/CLI contract/test-plan/tasks/HANDOFF и workbook contract. Обнови только
разрешённые mutable artifacts: точный scope, Pass A/B, live opt-in test, data-vs-evidence,
Excel adapter, stage/task mapping и acceptance. Каждая implementation task должна
принадлежать ровно S01–S08 этого pack. Source drafts не менять.

Проверь отсутствие противоречий и выполни применимые SpecKit validations. Верни список
изменений, mapping requirement→stage→task и команды/результаты. Не заявляй реализацию.
