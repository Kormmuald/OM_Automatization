# Project Specific Info: PEnergy Block 2 MVP Demo

Дата фиксации: 2026-07-06.

Этот документ содержит проектные вводные для использования общего генератора slice prompt-pack в проекте PEnergy Block 2 MVP Demo.

Документ не заменяет canonical Spec Kit / SDD sources. Если сведения ниже конфликтуют со spec, constitution, implementation plan, slices, tasks, previous handoff или previous acceptance report, генерацию prompt-pack нужно остановить и явно показать конфликт человеку.

## Project Identity

- Project name: PEnergy Block 2 MVP Demo
- Prompt-pack target: first or next implementation slice
- Orchestration root: `docs/orchestration`
- Slice prompt-pack layout convention: `docs/orchestration/slice-<n>/...`

## Orchestration Workflow Paths

- Generator prompt: `docs/orchestration/next-slice-prompt-pack-generator.md`
- Slice budget preflight prompt: `docs/orchestration/slice-budget-preflight.md`
- Orchestration task template: `docs/orchestration/orchestration-task-template.md`
- Acceptance report template: `docs/orchestration/acceptance-report-template.md`
- Slice budget history ledger: `docs/orchestration/slice-budget-history.jsonl`
- Prompt-pack target directory pattern: `docs/orchestration/slice-<n>/`
- Prompt-pack prompt directory pattern: `docs/orchestration/slice-<n>/prompts/`

## Protected / Special-Purpose Folders

Эти folders нельзя использовать как source of truth для prompt-pack generation и нельзя изменять без отдельного human decision:

- `docs/orchestration/spec-kit-migration/` - migration-only context, not a runner/generator source.

## Canonical Spec Kit / SDD Source Paths

Перед генерацией prompt-pack прочитай:

- Spec: `specs/001-block-2-mvp-demo/spec.md`
- Clarify checklists: `specs/001-block-2-mvp-demo/checklists/`
- Implementation plan: `specs/001-block-2-mvp-demo/plan.md`
- Data model: `specs/001-block-2-mvp-demo/data-model.md`
- Quickstart / verification scenarios: `specs/001-block-2-mvp-demo/quickstart.md`
- Slices: `docs/exec-plans/block-2-implementation-slices.md`
- Tasks: `specs/001-block-2-mvp-demo/tasks.md`
- Constitution: `docs/sdd/constitution.md`
- SDD workflow: `docs/sdd/sdd-workflow.md`

## Optional Source-History Paths

Эти документы можно читать для уточнения происхождения требований, но их нельзя использовать вместо canonical Spec Kit / SDD sources:

- Product spec source: `docs/product-specs/block-2-mvp-demo-spec.md`
- Clarify checklist source: `docs/product-specs/block-2-clarify-checklist.md`
- Repo-native implementation plan source: `docs/exec-plans/block-2-mvp-demo-implementation-plan.md`
- Roadmap/source plan: `docs/exec-plans/block-2-mvp-demo-roadmap.md`

## First-Slice Mode Inputs

Для Slice 1 отсутствие previous `acceptance-report` или `handoff` не является stop condition.

- Previous acceptance report: `N/A - first slice`
- Previous handoff: `N/A - first slice`
- Initial baseline: canonical spec/plan/slices/tasks, quickstart, data model, SDD workflow and templates.
- Primary slice source: `docs/exec-plans/block-2-implementation-slices.md`, section `Slice 1. App Shell And Demo Data`
- Primary tasks source: `specs/001-block-2-mvp-demo/tasks.md`, tasks for Slice 1 only

## Previous-Slice Input Convention For Slice 2+

Для Slice 2 и следующих slices обязательно используй:

- Previous acceptance report: `docs/orchestration/slice-<previous-number>/acceptance-report.md`
- Previous handoff: `docs/orchestration/slice-<previous-number>/handoff.md`
- Practical next-step input: `"Do next"` from previous handoff, checked against canonical slices/tasks.

Для bulk/forecast prompt-pack generation отсутствие previous acceptance/handoff не является stop condition, если prompt-pack явно помечает evidence как pending и требует execution-time refresh перед реализацией slice.

Для execution-ready prompt-pack или запуска реализации Slice 2+ previous acceptance/handoff обязательны. Если они отсутствуют или конфликтуют с canonical Spec Kit / SDD sources, это stop condition: нельзя надежно определить стартовое состояние следующего slice.

## Project-Specific Notes For Slice 1

Эти notes являются вспомогательным проектным контекстом. Используй их только после сверки с canonical Spec Kit / SDD sources.

Slice 1 должен подготовить prompt pack для app shell and synthetic demo data. Он не должен останавливаться только потому, что нет previous acceptance/handoff.

Для Slice 1 особенно важно не добавлять:

- registry rendering beyond the accepted Slice 1 boundary;
- actual routing or dispatch detail behavior;
- filters, summary, local demo actions, deploy or public publication;
- backend, database, auth, persistence, network calls, real integrations, real customer data, real AO "ATS" statuses, real confirmations, real receipt IDs, resend or initiate sending behavior.

## Project-Specific Notes For Slice 2+

Эти notes являются вспомогательным проектным контекстом. Используй их только после сверки с canonical Spec Kit / SDD sources, previous acceptance report и previous handoff.

Для Slice 2+ практический вход должен идти из previous handoff, особенно из секции `"Do next"`, и затем сверяться с `docs/exec-plans/block-2-implementation-slices.md` и `specs/001-block-2-mvp-demo/tasks.md`.

## Anti-Scope And Architecture Constraints

Project-specific anti-scope и architecture constraints не дублируются здесь как источник истины. Их нужно извлекать из canonical Spec Kit / SDD sources и previous handoff/acceptance.

Если project-specific notes в этом документе расходятся с authoritative SDD / Spec Kit sources:

- не создавай prompt-pack как готовый к исполнению;
- перечисли конфликтующие утверждения;
- укажи, какие файлы нужно обновить или подтвердить человеком;
- продолжай только после явного human approval.
