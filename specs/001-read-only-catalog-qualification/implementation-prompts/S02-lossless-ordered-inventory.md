# Делегируемый prompt реализации S02: lossless ordered inventory

## Цель

Реализовать S02 из [implementation-slices.md](../implementation-slices.md):
детерминированный постраничный reader, lossless inventory, package-layer identity,
read-only index relation, `TargetFingerprint/v1` и legacy disposition.

## Предусловие

S01 (T001–T013) завершён с успешной offline verification. Перед работой повторно
прочитать [tasks.md](../tasks.md), [test-plan.md](../test-plan.md),
[research.md](../research.md), [data-model.md](../data-model.md), [plan.md](../plan.md),
[spec.md](../spec.md) с Clarifications и конституцию.

Это delegated subtask formal `/SpecKit Implement`, но не завершение Feature 001.
До первой записи в worktree агент ОБЯЗАН:

1. прочитать `AGENTS.md`, `.agents/skills/speckit-implement/SKILL.md`,
   `.specify/feature.json`, `.specify/project.yml`, `.specify/extensions.yml`,
   `.specify/memory/constitution.md`, этот prompt, `implementation-slices.md`,
   `tasks.md`, `spec.md`, `plan.md`, `research.md`, `data-model.md`,
   `contracts/cli-contract.md`, `quickstart.md` и `test-plan.md`;
2. прочитать immutable common vision
   `preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md` и при
   необходимости immutable source
   `preparation/docs/product-specs/local-bpmsoft-synchronizer/01-read-only-catalog-spec.md`;
   не изменять ничего в `preparation/docs/product-specs/`;
3. проверить `.specify/memory/spec.md`, `.specify/memory/plan.md`,
   `.specify/memory/changelog.md`, predecessor artifacts, фактический код и regression
   tests; отсутствующее явно считать отсутствующим;
4. выполнить mandatory pre-hook `speckit.class.gate` для `/SpecKit Implement` и
   продолжать только после `PASS`;
5. один раз выполнить `.specify/scripts/powershell/check-prerequisites.ps1 -Json
   -RequireTasks -IncludeTasks`, подтвердить exact target
   `specs/001-read-only-catalog-qualification` и вывести read-only status всех
   `checklists/` файлов. При unchecked item остановиться и запросить решение пользователя.

Generic создание/изменение ignore files не выполнять: не выполнять Git commands и не
создавать/изменять `.gitignore`. После S02 не запускать `speckit.converge`,
`speckit.verify.run`, Jira sync, `speckit.archive.run` или другие feature-wide
`after_implement` hooks: они выполняются только после полного formal run всей Feature 001.

## Границы задачи

Выполнить T014–T024 в указанном dependency order: fixtures и failing tests,
reader/page-manifest, inventory adapter/lossless envelope, canonical fingerprint,
legacy disposition manifest и S02 verification.

Начать с T014, затем создать failing tests T015–T019. Не писать T020–T023 до того,
как соответствующие tests были записаны и первоначально зафиксировали failure. Отмечать
`[X]` в `tasks.md` только после фактического completion с safe offline evidence. Внутри
S02 разрешены только sanitized fixtures и `FakeReadOnlyTransport`; S01 уже завершён,
но не повторять T001–T013. Не выполнять S03, включая T025–T046, T038 или T042.

Если доступный индикатор контекста показывает 85 % использования либо обнаружено
сжатие контекста, остановиться перед началом следующей T-задачи и передать точный
handoff: выполненные task IDs, изменённые пути, результаты checks, открытые зависимости
и сохранённые gates. До такого условия не декомпозировать S02 заранее.

## Непереговорные checks

- Negative paging fixtures: duplicate, overlap, gap, empty-middle,
  nonempty-after-terminal, loop и max-page всегда дают
  `CATALOG_ORDER_OR_PAGING_UNQUALIFIED`, без hang, retry или false PASS.
- Каждый item имеет ровно один support status; `Name`/`Code` не используется для join.
- Unknown shape получает safe lossless structural envelope либо named blocker;
  raw lookup scalar не сохраняется.
- Index relation берётся только из `schema.indexes[].columns[].columnUId`;
  `INDEX_SYNC_UNRESOLVED` сохраняется и нет index mutation/plan/load/apply.
- Fingerprint детерминирован, не зависит от JSON property order и не превращает
  bounded fixture в evidence полного каталога.

## Stop conditions

Остановиться при неполной/нестабильной пагинации, default/drop unknown shape, Name/Code
merge, raw-value persistence, попытке live qualification, Excel/compare/Apply/browser/Git
action, Pass C/retry или расширении разрешений.

## Результат

После T024 сохранить только offline report с named blockers, complete status coverage и
deterministic digests. Не снимать `FULL_CATALOG_NOT_QUALIFIED` и не выполнять реальный run.
Все сообщения пользователю — на русском.
