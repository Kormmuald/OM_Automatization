# Оркестратор remediation — Feature 001 live export в Excel

Работай как оркестратор **прямо в текущем Codex-чате и текущем workspace/worktree**.
Не создавай отдельный Codex-тред, соседний worktree, user-facing delegated task или новый
чат для этой работы. Не вызывай `create_thread`, `fork_thread`, `handoff_thread` и не
просите пользователя перейти в другой чат. Пользовательские сообщения и все создаваемые
документы — на русском.

Не выполняй исследование, исправления или проверки сам: для каждого содержательного этапа
создавай внутренних субагентов. Они работают в том же workspace и возвращают результат
этому оркестратору; пользователь общается только с оркестратором в текущем чате. Не
запускай независимые диагностические циклы параллельно.

## Цель

Довести Feature 001 до рабочего состояния: программа подключается к локальному BPMSoft,
выполняет read-only выгрузку текущего состояния объектной модели и справочников и создаёт
связанную пару Excel-книг в разрешённом user-local output-каталоге.

Стартовая известная проблема: единственный ранее принятый live run завершился fail-closed
с `SCHEMA_INVENTORY_UNQUALIFIED` / `UNKNOWN_SHAPE_UNQUALIFIED`, exit `2`, без Excel output.
Это target/schema qualification blocker, а не доказанный production defect. Источник
фактов: `specs/001-read-only-catalog-qualification/verification/mvp-slices/S07/` и
текущий `specs/001-read-only-catalog-qualification/HANDOFF.md`.

## Обязательное чтение до действий

Прочитай `AGENTS.md`, `.specify/feature.json`, `.specify/project.yml`,
`.specify/extensions.yml`, текущий `HANDOFF.md`, Feature 001 `spec.md`, `plan.md`,
`tasks.md`, contracts/test-plan, все S00–S08 acceptance/handoff/review artifacts,
`verification/live-mvp-verification-prompt.md`, current code/tests и фактический git
status. Source drafts в `preparation/docs/product-specs/**`, legacy research runs,
`preparation/PrototypeReadOnlyPull/**`, `preparation/WorkbookDeliveryTool/**` и
историческая программа Google Sheets являются read-only reference: не изменяй их.

Перед первым remediation cycle:

1. Проверь, что active feature — `001-read-only-catalog-qualification`.
2. Зафиксируй нынешний git status и diff summary в новом run-документе.
3. Создай baseline commit текущего состояния. Перед commit проверь target paths, исключи
   credentials, live outputs, `bin/`, `obj/`, temporary files и output-каталоги. Commit
   message: `codex: baseline before live schema remediation`.
4. Создай/обнови единый mutable журнал remediation в
   `specs/001-read-only-catalog-qualification/verification/live-remediation/README.md`.
   Не создавай append-only handoff вместо актуального `HANDOFF.md`.

## Особое разрешение для этого диагностического треда

Сначала попроси пользователя запустить локальный BPMSoft и ответить `стенд запущен`.
После ответа запроси в этом треде URL, login и password. Это разрешено только для этой
диагностической работы и только с явного пользователя в этом треде. Можно повторно
использовать их для ограниченных retry при проверке гипотез.

Никогда не записывай credentials, cookies, CSRF, login response или raw secret values в
файлы, git, prompts, evidence, arguments, config, command history или subagent messages.
Не используй их в штатном интерфейсе готовой программы: в завершённой программе URL/login/
password каждый запуск вводятся интерактивно в CLI. Не выполняй Write/Manage/Compare/Apply,
browser write, SQL mutation, delete или Git push.

## Распределение субагентов

- Базовое назначение каждого субагента: `gpt-5.6-terra`, `high`.
- `gpt-5.6-sol`, `high` допускается только для явно обоснованной сложной задачи
  (например, многослойная protocol/identity diagnosis); зафиксируй обоснование в журнале.
- Каждый субагент читает нужные artifacts сам, получает узкую границу, не принимает
  собственную работу и возвращает exact files, commands, results, evidence и blockers.
- Новый reviewer всегда отдельный от worker. Reviewer не исправляет код.
- Субагенты — внутренние: не создавай для них отдельные видимые пользователю Codex tasks,
  worktrees или чаты. Все prompts, вопросы пользователю, human gates и итоговые статусы
  остаются в текущем чате оркестратора.

## Не более двух последовательных remediation cycles

### Cycle N

1. **Гипотезы.** Запусти субагента для обновления
   `verification/live-remediation/hypotheses.md`. Он собирает только новые проверяемые
   гипотезы из фактического blocker evidence, code, tests, read-only legacy/prototype
   runs и сравнения с Google Sheets implementation. Предыдущие гипотезы не дублирует:
   отмечает их status, evidence и связь с новыми. Для каждой новой гипотезы фиксирует
   вероятность, affected components, безопасный test/probe, PASS/FAIL criterion и
   запрещённые действия.

2. **Выбор.** Запусти отдельного subagent-reviewer для выбора наиболее вероятной и
   проверяемой гипотезы. Он не меняет код. Оркестратор фиксирует выбор и rationale.

3. **Проверка гипотезы.** Запусти worker для controlled проверок: сначала fixtures/
   characterization/read-only inspection, затем при необходимости ограниченный live
   diagnostic run. Use existing user-provided thread credentials only in the interactive
   process; do not expose them. Сохрани only safe structural evidence.

4. **Исправление.** Если гипотеза подтверждена, запусти implementation worker только на
   affected production/tests/docs. Затем отдельного test/validation worker. Перед новым
   live run запусти independent reviewer по spec/plan/tasks/current hypothesis.

5. **Проверка работоспособности.** При reviewer Pass запусти один controlled live run
   using the already supplied credentials. Проверяй: no write calls by architecture and
   safe runtime evidence, successful Pass A/B, qualified snapshot, canonical Model/Lookup
   Excel pair, pair/read-back/OOXML checks, safe evidence/seal and no secrets outside
   Lookup workbook output.

6. **Решение по изменению.**
   - Если live success доказан, останови циклы и подготовь factual final report.
   - Если гипотеза опровергнута и изменения не дают измеримого улучшения, откати только
     изменения этого cycle recoverably (например, revert commit), сохрани evidence и
     вернись к шагу 1 следующего cycle.
   - Если изменения дали частичное доказуемое улучшение или необходимы как часть
     многоместного исправления, не откатывай их: зафиксируй measured improvement,
     remaining gap и обоснование продолжения.

После двух циклов, если работоспособность не доказана, обязательно остановись. Дай
пользователю отчёт: полный список гипотез, проверенные/непроверенные, evidence,
почему они не сработали, текущие изменения и два-три безопасных варианта следующего
действия. Не запускай следующие два цикла без нового явного запроса пользователя.

## Источники для сравнения

Субагенты могут read-only сравнить текущую реализацию с:

- безопасными historical research/diagnostic runs, где read-only извлечение работало;
- legacy/prototype implementation в `preparation/PrototypeReadOnlyPull/`;
- программой для Google Sheets, только как semantic/API reference.

Любой перенос — controlled rewrite с tests; legacy code не доказывает endpoint semantics,
полноту, safety или identity correctness.

## Завершение

Итоговый report обязан перечислить: выполненные cycles/subagents/models, baseline и
subsequent commits, changed files, exact build/test commands and outcomes, live attempts,
safe evidence paths, actual Excel paths/hashes (если созданы), open blockers и human
decisions. Не называй результат успешным без доказанного live qualification и pair output.
