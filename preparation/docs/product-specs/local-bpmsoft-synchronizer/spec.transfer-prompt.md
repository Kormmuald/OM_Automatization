# Промпт переноса верхнеуровневой спецификации

Используй корпоративный Spec Kit для подготовки **границ и трассируемости первой
L2 feature**, но не создавай единую feature для всего синхронизатора и не
начинай реализацию.

Неизменяемый исходный материал: `preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md`.
Не редактируй, не перемещай, не переименовывай и не используй этот каталог как
`SPECIFY_FEATURE_DIRECTORY`.

Перед работой прочитай `.specify/project.yml`,
`.specify/memory/constitution.md`,
`.specify/workflows/speckit/class-workflow.yml` и
`.specify/workflows/overlays/speckit/l2-pilot.yml`. Проект имеет класс `L2`,
профиль `l2-pilot`; язык создаваемой Constitution/Specify-документации — русский.
Jira и Confluence synchronization отключены: не создавай Jira Stage, mapping,
trace или Confluence-артефакты.

Сначала выдели из поверхностной спецификации первую самостоятельную и
проверяемую ценность пилота. По умолчанию это безопасный read-only контур и
qualification каталога, а не полный сквозной цикл и не Apply. Затем выполни
`/SpecKit Specify` только для этой feature и создай `specs/<feature>/spec.md`.
`specs/<feature>/` должна быть новой отдельной папкой этой feature и единственным
местом для её будущих артефактов: checklist, research, data-model (если
применимо), quickstart, plan и tasks. В текущем запуске создавай только `spec.md`;
последующие артефакты создаются соответствующими командами L2 в той же папке.

В новом `spec.md`:

- сохрани смысл применимых `FR-*` и `NFR-*`, но включи только относящиеся к
  выбранной feature требования;
- перечисли `FR-*`/`NFR-*` в разделе трассируемости источников и укажи
  `preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md` как
  неизменяемый source;
- явно отложи workbook pair, compare/immutable plan, Apply и Git finalization
  как будущие features, не теряя их исходных ограничений;
- задай границы L2-пилота: пользователи, реальная среда/данные, разрешённый
  безопасный subset, критерии успеха и остановки, fallback и решение человека
  после пилота;
- перенеси все применимые запреты: отсутствие Delete/rollback/browser write,
  fail-closed, строгая identity, отсутствие секретов в артефактах и
  `INDEX_SYNC_UNRESOLVED`;
- не выдумывай сроки, бюджет, коммуникации, write semantics, allowlist или
  доказательства. Неопределённости сделай явными вопросами для `/SpecKit Clarify`.

Не создавай plan, tasks, production-код или live BPMSoft write actions. По
завершении сообщи, что следующий обязательный шаг L2 — `/SpecKit Clarify`.
