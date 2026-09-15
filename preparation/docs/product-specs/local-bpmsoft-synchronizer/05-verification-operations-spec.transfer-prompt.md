# Промпт переноса спецификации 05

Перенеси неизменяемый источник
`preparation/docs/product-specs/local-bpmsoft-synchronizer/05-verification-operations-spec.md`
в корпоративный Spec Kit как отдельную L2 feature операционной проверки,
evidence, ограничений Codex skills и Git finalization. Не редактируй, не
перемещай и не назначай исходный каталог каталогом feature.

Перед `/SpecKit Specify` прочитай `.specify/project.yml`,
`.specify/memory/constitution.md`, `.specify/workflows/speckit/class-workflow.yml`
и `l2-pilot.yml`, затем выполни `speckit-class-gate`. Создавай только
`specs/<feature>/spec.md` на русском языке. Интеграции Jira и Confluence
отключены, поэтому не создавай связанные mapping/trace files.
Создай новую отдельную папку `specs/<feature>/` для этой feature. Она будет
содержать её checklist, research, data-model (если применимо), quickstart, plan и
tasks, когда соответствующие L2-команды будут выполнены; в текущем запуске создай
только `spec.md`.

Сохрани смысл `OPS-001`…`OPS-013`; редакционная перестройка допустима только с
явной трассировкой исходных IDs. Зафиксируй как обязательные:

- date-based versioned run folders, append-only audit/evidence/journal и
  безопасные метаданные версий;
- deny-by-default evidence schema и automated secret scanning;
- отдельный interactive browser login, только read-only browser verification и
  авторитетность CLI read-back;
- 100% browser acceptance структурных операций и детерминированную выборку
  lookup rows `10%`, минимум `3`, максимум `10`;
- отсутствие Git commit при partial/failed run, один commit обеих книг только
  после полного успеха и явного human approval, без automatic conflict resolution;
- границу skills: они оркестрируют CLI, не получают секреты и не принимают
  безопасность/одобрения вместо человека.

Ограничь пилот безопасным проверяемым operator workflow; Git finalization и
browser acceptance должны зависеть от наличия разрешённого upstream plan/Apply,
а не предполагаться завершёнными. Не создавай browser write, credentials storage,
automatic retry/cleanup или live operations. Не выдумывай retention, доступы,
сроки и evidence.

Не создавай plan, tasks или код. После Specify следующий обязательный L2-шаг —
`/SpecKit Clarify`.
