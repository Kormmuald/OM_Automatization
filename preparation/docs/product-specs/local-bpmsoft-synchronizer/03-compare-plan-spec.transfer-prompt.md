# Промпт переноса спецификации 03

Перенеси неизменяемый источник
`preparation/docs/product-specs/local-bpmsoft-synchronizer/03-compare-plan-spec.md`
в корпоративный Spec Kit как отдельную L2 feature: строго read-only compare,
HTML report и immutable machine-readable plan. Не изменяй источник и не
используй каталог черновиков в качестве feature directory.

До `/SpecKit Specify` прочитай `.specify/project.yml`,
`.specify/memory/constitution.md`, `.specify/workflows/speckit/class-workflow.yml`
и `l2-pilot.yml`, затем выполни `speckit-class-gate`. Создавай документ только
в `specs/<feature>/spec.md`, на русском языке. Jira/Confluence отключены —
никаких integration artifacts.
Создай новую отдельную папку `specs/<feature>/` для этой feature. Её checklist,
research, data-model (если применимо), quickstart, plan и tasks должны создаваться
исключительно в той же папке на следующих L2-шагах; в данном запуске создай только
`spec.md`.

Сохрани смысл `CMP-001`…`CMP-012` и перечисленных именованных blockers. Можно
перестроить текст вокруг входов, канонической модели, правил идентичности,
детерминированного графа операций, валидности плана, безопасного отчёта и
приёмки, но обязательно обеспечь трассировку каждого `CMP-*` к новому требованию.
В частности, не ослабляй:

- read-only endpoint allowlist и отсутствие HTTP writes;
- запрет global match по `Name`/`Code`, Delete, rename/type/package changes и
  любых index operations;
- единый canonical model для отчёта и плана;
- точные hashes/fingerprints/allowlist version в плане;
- invalidation при изменении входов или live state;
- отсутствие пересчёта diff при будущем Apply и безопасную диагностику без
  секретов/исходных значений.

Опиши L2-пилот как ограниченную, безопасную проверку сравнения на заданной паре
книг и target fingerprint; задай success/stop criteria, fallback и решение
человека. Apply — только будущая зависимая feature: не создавай write capability,
не утверждай allowlist и не выполняй действия в BPMSoft. Не создавай plan/tasks/код;
после Specify следующий обязательный шаг — `/SpecKit Clarify`.
