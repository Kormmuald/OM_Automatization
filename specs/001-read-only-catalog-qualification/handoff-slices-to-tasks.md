# Planning handoff: approved slices → generated `/SpecKit Tasks`

**Статус:** S01–S03 утверждены явным human decision, а `tasks.md` сформирован. Это
не acceptance report и не evidence того, что какой-либо slice реализован, проверен или
принят.

## Current target and artifact state

- Active target: `specs/001-read-only-catalog-qualification`; recheck
  `.specify/feature.json` before the next step.
- `spec.md` (including clarifications), `plan.md` and Phase 0/design artifacts exist.
- `implementation-slices.md` is approved for S01–S03 by an explicit 2026-09-09 human
  decision recorded in that artifact; `tasks.md` exists and has passed format validation.
- No Feature 001 production `src/` or `tests/` tree exists. Legacy
  `preparation/PrototypeReadOnlyPull/Program.cs` and
  `preparation/WorkbookDeliveryTool/Program.cs` are reference/characterization only,
  not implementation or test evidence.
- No slice has started, been implemented, checked, accepted or supplied evidence.

## Canonical input paths

- `AGENTS.md`; `.specify/feature.json`; `.specify/memory/constitution.md`;
  `.specify/project.yml`; `.specify/extensions.yml`.
- Immutable common vision:
  `preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md`.
- Active artifacts: `spec.md`, `checklists/requirements.md`, `plan.md`,
  `research.md`, `data-model.md`, `contracts/cli-contract.md`, `quickstart.md`,
  `test-plan.md`, `implementation-slices.md`.
- Source drafts under `preparation/docs/product-specs/` other than that common vision
  are not handoff inputs and must not be changed.

## Slice map and dependencies

`S01 Safe offline entry` → `S02 Lossless ordered inventory` →
`S03 Two-pass qualification and safe decision`.

S01 establishes only offline/fake transport. S02 uses it for typed inventory and
deterministic pass input. S03 uses both for offline qualification, safe append-only
evidence and diagnosis. The complete `requirement → slice → future task → check`
matrix is in `implementation-slices.md`; no task may sit outside it.

## Requirements, verification and honest absences

The matrix covers US1–US3, FR-001–FR-019 and SC-001–SC-007. Planned checks from
`test-plan.md` are allowlist capture, paging/shape fixtures, fingerprint vectors,
process tests, run-store/schema/scanner tests, architecture/CLI tests and offline E2E
regression. These are plans, not evidence.

Absent: production projects/tests; Feature 001 fixtures; executed test output; verified
evidence; implemented manual live-invocation path; and Write/Manage rights. `tasks.md` and
approved S01–S03 are planning artifacts, not proof of safety, completeness, implementation
or acceptance. Legacy code is not proof of safety/completeness/acceptance and must not be
ported by copy.

## Preserved gates, risks and stop conditions

- `FULL_CATALOG_NOT_QUALIFIED` stays open through offline checks. Afterwards, a future
  conditional manual live-verification task is started manually by the operator for its exact
  target/scope, or by an agent only after a direct current user request; no
  `AuthorizationReference` is required.
- `INDEX_SYNC_UNRESOLVED` stays open; no index plan/load/apply/mutation.
- Write/Manage is absent; retain `WRITE_ALLOWLIST_UNAPPROVED` and
  `WRITE_SEMANTICS_UNPROVEN` as out of scope.
- No Excel, compare, Apply, browser action or Git action; no expansion from fixture to
  live target.
- `TARGET_STATE_CHANGED_DURING_QUALIFICATION` is terminal: no Pass C, new double pass,
  retry loop or silent downgrade; a future new run needs a new manual invocation or direct current user request.
- Stop before task generation on missing approval, target mismatch, Spec/Plan/Test Plan
  conflict, missing slice coverage, or any task weakening a gate. Report the precise
  conflict; do not silently repair it.

## Boundaries of future `/SpecKit Tasks`

Only after mandatory hooks and explicit human approval, it may create `tasks.md`.
Every task must retain slice ID, requirement IDs, exact component/path, dependency,
test/safe evidence and stop condition. Its order is setup/test/fixture/security;
implementation; slice verification; final solution validation. It must not execute
Analyze/Implement, create slice-pack, call live BPMSoft, request credentials or do
write/browser/Git actions. It may create, but must not execute during task generation,
the conditional manual read-only verification task after successful offline checks.

## Readiness for task generation

Satisfied 2026-09-09: explicit human approval of `implementation-slices.md` for S01–S03;
unchanged exact active target; and current Critique
`critiques/critique-20260909-100855.md` after the conditional manual live-verification
clarification. Still required before task generation: no further scope expansion or artifact
contradiction and test/evidence coverage for every slice. Project-local order:
`/SpecKit Plan` → `/SpecKit Tasks` → `/SpecKit Analyze` → `/SpecKit Implement`. This handoff
grants neither implementation nor live run. After successful task generation, the next separate
step is `/SpecKit Analyze`.
