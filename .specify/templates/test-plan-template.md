# Test Plan: [FEATURE NAME]

**Feature**: `[###-feature-name]`

**Spec**: [link to spec.md]

**Plan**: [link to plan.md]

## Purpose

This artifact defines the formal tests for both incremental delivery and final solution acceptance.
It is created during `$speckit-plan`, consumed by `$speckit-tasks`, and verified during
`$speckit-implement`.

## Increment Test Matrix

Every user story must be independently testable. Add at least one row for each story and cover all
functional requirements assigned to that story.

| Increment | Requirement / Scenario | Test Type | Test Artifact / Location | Steps Summary | Expected Result | Evidence |
|-----------|------------------------|-----------|--------------------------|---------------|-----------------|----------|
| US1 | [FR-001 / Scenario 1] | [unit/contract/integration/e2e/manual/data/visual] | [planned path] | [short steps] | [observable result] | [log/report/screenshot/file/sign-off] |

## Final Solution Test Matrix

These checks prove that the selected scope works as one delivered solution, not only as isolated
increments.

| Final Check | Scope Covered | Test Type | Test Artifact / Location | Steps Summary | Expected Result | Evidence |
|-------------|---------------|-----------|--------------------------|---------------|-----------------|----------|
| End-to-end acceptance | [primary release workflow] | [e2e/manual/data/visual/performance/security] | [planned path] | [short steps] | [observable result] | [log/report/screenshot/file/sign-off] |

## Requirement Coverage

| Requirement / Success Criterion | Covered By | Coverage Status | Notes |
|---------------------------------|------------|-----------------|-------|
| FR-001 | [increment/final test ID] | [covered/not covered/not testable] | [notes] |
| SC-001 | [final test ID] | [covered/not covered/not testable] | [notes] |

## Test Evidence Rules

- Automated checks must name the command or test file that produces the result.
- Manual checks must include concrete steps, expected observations, and evidence to save.
- Screenshots, logs, generated files, fixture diffs, reports, and human sign-off are acceptable
  evidence when they match the risk level of the initiative.
- Any uncovered requirement or success criterion must be explicitly listed with a reason and owner.

## Exit Criteria

- All increment tests for selected stories are implemented or executable as documented manual checks.
- All final solution tests are implemented or executable as documented manual checks.
- `tasks.md` contains corresponding test/preparation tasks before implementation tasks for each
  increment and final validation tasks before acceptance.
- `quickstart.md` includes the runnable validation path for the final solution.
