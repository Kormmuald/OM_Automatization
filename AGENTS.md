# Corporate Spec Kit Agent Instructions

## Project Rules

### Artifact Policy

Creating and updating project artifacts is authorized.

### Draft Preservation

The authoritative source drafts for GitHub Spec Kit are in
`preparation/docs/product-specs/`. Do not rename, move, edit, delete, or
overwrite any file in this directory. Use these files only as source material
when creating or updating Spec Kit artifacts.

Keep Spec Kit outputs separate from the source drafts:

- Constitution: `.specify/memory/constitution.md`
- Feature specification and its follow-on artifacts: `specs/<feature>/`

When invoking a Spec Kit skill, explicitly name the relevant source-draft
path and state that it is immutable. Never set `SPECIFY_FEATURE_DIRECTORY` to
`preparation/docs/product-specs/`.

## Corporate Lifecycle Routing

When the user asks in natural language to run the full lifecycle for the current specification, continue the current specification to completion, finish the current feature, or otherwise perform an end-to-end Spec Kit cycle, interpret the request through the corporate class-aware workflow, not through the stock Spec Kit `Full SDD Cycle`.

This applies to Russian and English phrasings, including but not limited to: "пройди весь жизненный цикл для текущей спецификации", "выполни полный цикл", "доведи текущую спецификацию до конца", "заверши текущую спецификацию", "run the full lifecycle", "complete the current spec", and "finish the current feature".

For such requests:

1. Read `.specify/project.yml` first.
2. Use `initiative.class` and its selected workflow overlay/profile from `.specify/project.yml` as the authoritative routing source.
3. Load `.specify/workflows/speckit/class-workflow.yml`.
4. Load the selected overlay from `.specify/workflows/overlays/speckit/<selected-overlay>.yml`.
5. Determine the current feature from `.specify/feature.json` and existing `specs/<feature>/` artifacts.
6. Follow the selected class `required_commands` order from `class-workflow.yml`, starting at the first required command whose required inputs/artifacts are missing or stale for the current feature, except for the project-local Analyze/Tasks ordering rule below.
7. Respect all mandatory hooks from `.specify/extensions.yml`; do not bypass `speckit-class-gate`, `speckit.critique.run`, `speckit.converge`, `speckit.verify.run`, `speckit.archive.run`, Jira hooks, or Confluence hooks when they apply.
8. If the class, overlay, current feature, or required prior artifact cannot be determined, stop and explain the missing prerequisite in Russian instead of falling back to the stock workflow.

The stock `.specify/workflows/speckit/workflow.yml` is an upstream baseline reference only for this template. It must not override the class-specific workflow selected in `.specify/project.yml`.

## Project-Local Analyze/Tasks Ordering

For this project, when the selected workflow is `L2 / l2-pilot`, run
`/SpecKit Tasks` immediately after the successful `/SpecKit Plan` stage and run
`/SpecKit Analyze` only after `/SpecKit Tasks` has successfully produced a complete
`tasks.md`. The effective local order is therefore:

`/SpecKit Plan` → `/SpecKit Tasks` → `/SpecKit Analyze` → `/SpecKit Implement`.

This is an explicit project-owner decision that resolves the installed-tool mismatch:
`.agents/skills/speckit-analyze/SKILL.md` requires an existing complete `tasks.md` and
invokes prerequisites with `-RequireTasks`. This narrow local rule takes precedence over
the opposite Analyze/Tasks order shown in the current `class-workflow.yml` and
`l2-pilot.yml`; it does not modify those shared workflow files.

All registered hooks remain mandatory at their actual command boundaries: run the
`before_tasks` and `after_tasks` hooks with `/SpecKit Tasks`, then the `before_analyze`
and `after_analyze` hooks with `/SpecKit Analyze`. Do not create a fake or placeholder
`tasks.md`, do not skip analysis, and do not treat this ordering decision as permission
for implementation, live BPMSoft access, Apply, browser writes, or Git operations.

## User-Facing Chat Language

All user-facing text emitted into the Codex chat window MUST be in Russian regardless of the document language configured in `.specify/project.yml`.

This includes progress notes, questions, completion reports, summaries, warnings, next actions, and error explanations.

Preserve technical identifiers exactly when translating would change their meaning: command names, skill names, file paths, URLs, code identifiers, JSON/YAML keys, branch names, Jira keys, requirement IDs, status tokens used by integrations, and literal user-provided text.

## Накопительная разработка четырёх features

Общим неизменяемым видением инструмента является
`preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md`; источником
сквозных `OPS-001`…`OPS-013` является
`preparation/docs/product-specs/local-bpmsoft-synchronizer/05-verification-operations-spec.md`.
Они не являются отдельными feature и не заменяют конституцию. Четыре feature-папки:
`specs/001-read-only-catalog-qualification/`,
`specs/002-workbook-pair-control/`,
`specs/003-compare-read-only-plan/` и
`specs/004-gated-apply-operations/`.

На `/SpecKit Specify`, `/SpecKit Clarify`, `/SpecKit Plan`, `/SpecKit Analyze`,
`/SpecKit Tasks` и `/SpecKit Implement` необходимо читать общее видение
`preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md` как
неизменяемый общий spec проекта. Обязательное ознакомление с этапными source
drafts для этих команд не требуется; их читают только при необходимости для
конкретного вопроса или требования. Перед Plan или Implement следующей feature надо
прочитать существующие `.specify/memory/spec.md`, `.specify/memory/plan.md`,
`.specify/memory/changelog.md`, нужные артефакты предшественников, фактический код
и regression tests; отсутствующее считается отсутствующим. Новая feature расширяет
единый инструмент, а не создаёт независимую копию reader/parser.

В feature spec указываются dependencies и квалифицированные source IDs; будущий
plan фиксирует reuse/change impact, а будущие test-plan/tasks — integration и
cumulative regression. После Verify/Archive должна обновляться project memory.
Перед работой всегда сверяйте `.specify/feature.json` и явный target: номер или
ветка сами по себе не переключают context. Отсутствие реализации предшественника
никогда не считается выполненной зависимостью: подготовка будущей spec допустима,
но её интеграция требует фактического предшественника. Эти правила не расширяют
Allowed Sources команды Archive: общее видение попадает в memory только через
утверждённые feature artifacts; нельзя загрузить весь корпус как уже реализованный.

## Document Language Scope

The document language configured in `project.document_language` in `.specify/project.yml` applies only where a specific skill says it applies. In the corporate template, it applies to documents created by `/SpecKit Constitution` and `/SpecKit Specify`.

For legacy projects only, a skill may fall back to the old document language section in `.specify/memory/constitution.md` when `.specify/project.yml` or `project.document_language` is missing.

Documents created by other skills keep their own artifact language policy. This does not change the Russian chat-output rule above.
