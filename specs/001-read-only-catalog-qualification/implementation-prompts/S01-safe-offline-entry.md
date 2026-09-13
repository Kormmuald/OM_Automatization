# Prompt реализации S01: safe offline entry

## Цель

Реализовать только S01 из [implementation-slices.md](../implementation-slices.md):
строго автономный вход `catalog validate-offline` с sanitised fixture/fake transport,
exact `ReadEndpointAllowlist/v1`, ephemeral session boundary и безопасным CLI result.

## Обязательные источники и Implement protocol

Это delegated subtask формального `/SpecKit Implement`, а не самостоятельное завершение
всей Feature 001. Перед любыми изменениями полностью прочитать:

1. `AGENTS.md` и `.agents/skills/speckit-implement/SKILL.md`;
2. `.specify/feature.json`, `.specify/project.yml`, `.specify/extensions.yml` и
   `.specify/memory/constitution.md`;
3. [implementation-slices.md](../implementation-slices.md), [tasks.md](../tasks.md),
   [spec.md](../spec.md) с Clarifications, [plan.md](../plan.md),
   [research.md](../research.md), [data-model.md](../data-model.md),
   [contracts/cli-contract.md](../contracts/cli-contract.md), [quickstart.md](../quickstart.md)
   и [test-plan.md](../test-plan.md);
4. неизменяемые source: `preparation/docs/product-specs/local-bpmsoft-synchronizer/01-read-only-catalog-spec.md`
   и `preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md`.

Оба source в `preparation/docs/product-specs/` только читаются и никогда не изменяются.
Проверить наличие `.specify/memory/spec.md`, `.specify/memory/plan.md`,
`.specify/memory/changelog.md`, predecessor artifacts, фактического кода и regression tests;
отсутствующее явно зафиксировать как отсутствующее, не выдумывать и не считать завершённой
зависимостью.

До первой записи в рабочее дерево выполнить обязательный pre-hook
`speckit.class.gate` для `/SpecKit Implement` и дождаться PASS. Затем один раз выполнить
`.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks`,
проверить exact target `specs/001-read-only-catalog-qualification` и вывести read-only
статус всех файлов `checklists/`. Если в checklist есть unchecked items — остановиться и
запросить решение пользователя, как требует `speckit-implement`.

Применять правила `/SpecKit Implement`: тесты до реализации, dependency order,
phase checkpoint, scoped test run после каждой завершённой фазы и отметка `[X]` в
`tasks.md` только после фактического завершения соответствующей T-задачи с evidence.
Generic шаг создания/изменения ignore files не применять: S01 и feature scope запрещают
Git actions; не выполнять Git commands и не создавать/изменять `.gitignore`.

После S01 не запускать `speckit.converge`, `speckit.verify.run`, Jira sync,
`speckit.archive.run` или другие `after_implement` hooks: они обязательны только после
завершения всех задач Feature 001 в едином formal `/SpecKit Implement` run.

## Границы задачи

Выполнить в dependency order T001–T013. Сначала создать solution/test foundation,
typed contracts и test doubles, затем написать failing contract tests, после чего
реализовать classifier, ephemeral session и fixture-only CLI command. Проверить S01
задачей T013.

Поздние S01-задачи T038 и T042 относятся к cross-slice CLI/architecture verification:
не выполнять их в первой поставке S01. Выполнять их только после удовлетворения их
явных зависимостей из `tasks.md`.

## Контекстная граница и условная декомпозиция

По умолчанию выполнить весь S01 (T001–T013) в одном Codex-треде по этому prompt;
не декомпозировать slice заранее. Отслеживать доступный индикатор использования
контекстного окна. Когда использовано 85% контекста, остановить реализацию в ближайшей
безопасной точке: не начинать следующую T-задачу, не оставлять незавершённую запись и
дать точный handoff с выполненными задачами, изменёнными путями, результатами checks,
незавершёнными зависимостями и сохранёнными gates.

Если сжатие контекста произошло раньше, чем был замечен порог, остановиться сразу
после обнаружения сжатия и подготовить такой же handoff. Только в одном из этих двух
случаев разрешается разложить оставшуюся часть S01 на пять ранее определённых пакетов:
T001–T003, T004–T006, T007–T009, T010–T012 и T013. Уже завершённые задачи повторно
не выполнять; каждый пакет сохраняет dependency order, scoped checks и stop conditions.
Если до завершения S01 не достигнуты ни 85%, ни сжатие контекста, декомпозицию не
создавать.

## Непереговорные checks

- Reject method/path/body вне exact allowlist до send; rejected sends и write calls — 0.
- Password, cookie и CSRF существуют только в памяти; offline tests не открывают
  terminal prompt и не сохраняют raw value.
- `catalog validate-offline` принимает только sanitised fixture и не выполняет login,
  network I/O, Excel/Git actions или live qualification.
- Domain/Application не получают HTTP, Excel, browser или Git types; `preparation/*`
  не становится runtime dependency.

## Stop conditions

Немедленно остановиться при необходимости live target/credentials, любом HTTP send,
неclassified endpoint, persisted secret/raw lookup value, Write/Manage capability,
Excel/compare/Apply/browser/Git action либо ослаблении gates. Не обходить проблему
fallback или retry.

## Результат

Отметить выполненными только фактически завершённые задачи. Сохранить только safe
offline test output/capture summary, требуемые T013; не заявлять `HUMAN_REVIEW_REQUIRED`
как live authorization или Apply permission.
