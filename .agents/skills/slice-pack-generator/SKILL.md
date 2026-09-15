---
name: slice-pack-generator
description: Create or validate sequential implementation slice prompt packs with an orchestrator, separated implementation/test/review/verification/handoff roles, explicit model guidance, evidence gates, and previous-slice refresh. Use when a large approved implementation scope must be decomposed into context-safe agent tasks. Do not use it to invent product requirements or bypass canonical SpecKit artifacts.
---

# Slice Pack Generator

Package an approved implementation plan into bounded, sequential prompts whose results
can be independently reviewed and handed to the next slice without relying on chat
history.

Provenance: adapted as a project-local Codex skill from the unchanged course material at
`C:/CodingAgents/codex/projects/VibeCeH/docs/pilot-course/lessons/slice-generator`
(course snapshot dated 2026-07-06). The source templates are copied unchanged under
`references/`.

## Core workflow

1. Identify canonical spec, plan, slices, tasks, constitution, current handoff and any
   project-specific orchestration policy. Optional history never overrides them.
2. Run a readiness check before generating prompts. Stop on conflicting sources,
   unclear slice boundaries, unmapped tasks or missing required model allocation.
3. Decompose by independently verifiable outcomes and dependency order, not merely by
   file count. Keep each implementation role inside one slice.
4. Separate responsibilities: orchestrator sequences work; implementation changes
   production code; unit-test owns scoped tests; review audits alignment; verification
   collects evidence; docs/handoff transfers state. No role accepts its own work.
5. Embed exact model and reasoning guidance in the prompt index, orchestrator prompt and
   each subagent prompt. Model choice never weakens gates or evidence requirements.
6. After implementation and tests, require an independent review against the original
   MVP/spec/plan/task boundary. Route concrete fixes back to the owning role and rerun
   review before continuing.
7. Finish each slice with acceptance evidence and a concise handoff. Before the next
   slice, compare actual previous evidence with the pre-generated prompt assumptions.
   Never fabricate completion evidence.
8. Future packs may be generated early only as planning-pending artifacts. They become
   execution-ready only after the previous acceptance/handoff is checked.

## Reference routing

- For creating one pack, read
  [next-slice-prompt-pack-generator.md](references/next-slice-prompt-pack-generator.md).
- For preparing all packs in advance, also read
  [all-slice-prompt-pack-runner.md](references/all-slice-prompt-pack-runner.md).
- For project-specific source/path configuration, adapt
  [project-specific-info.md](references/project-specific-info.md); its PEnergy values are
  examples and are not defaults for another repository.
- For context-window and effort calibration, read
  [slice-budget-preflight.md](references/slice-budget-preflight.md).
- For orchestration and acceptance document shapes, read
  [orchestration-task-template.md](references/orchestration-task-template.md) and
  [acceptance-report-template.md](references/acceptance-report-template.md).

## Non-negotiable checks

- Every canonical implementation task maps to exactly one slice or is explicitly
  deferred with an approved reason.
- Each slice states In/Out scope, dependencies, changed areas, tests, evidence and stop
  conditions.
- Each implementation result receives independent alignment review before the next
  implementation slice starts.
- Reviewer findings are classified as `Blocker`, `Fix`, `Log` or `Pass` and cite
  evidence; `Blocker`/`Fix` must be resolved or explicitly accepted by the human.
- Prompt packs preserve dirty-worktree ownership and external-action authorization
  boundaries.
- Live systems, credentials, publishing, writes, deployment or other external effects
  require the authorization defined by the project and user; pack generation itself is
  never that authorization.
