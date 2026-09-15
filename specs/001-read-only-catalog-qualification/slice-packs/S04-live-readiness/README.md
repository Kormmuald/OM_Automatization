# S04 live-readiness — repo-native slice pack

## Status

This is the implementation-preparation pack for corrective Slice S04 of active Feature
`001-read-only-catalog-qualification`.  It is a **planning input**, not completion
evidence, permission for a live run, or a replacement for canonical
[`../tasks.md`](../../tasks.md).

## Provenance and generator finding

`docs/archive/handoffs/project-discovery/2026-09-06-project-discovery-handoff.md` identifies the intended `slice-generator` as an
external course asset and names its expected materials: `project-specific-info.md`,
`slice-budget-preflight.md`, `next-slice-prompt-pack-generator.md`,
`all-slice-prompt-pack-runner.md`, `orchestration-task-template.md`, and
`acceptance-report-template.md`.  Within this repository, a complete text search finds
no installed `slice-generator`, no executable/script, and no imported source templates.
The current handoff reaches the same conclusion for S01–S03.  This pack therefore does
**not** claim that the external generator was run or that its templates were copied.

The pack is a repo-native equivalent assembled from the nearest actual project
conventions: S01–S03 implementation prompts, `implementation-slices.md`,
`s04-live-readiness-slice.md`, `s04-live-readiness-tasks.md`, the active Feature 001
artifacts, and the L2 SpecKit rules.  It intentionally records that limitation so a
later reviewer can distinguish it from generator output.

## Contents and use order

1. Read [project-specific-info.md](project-specific-info.md) and
   [slice-budget-preflight.md](slice-budget-preflight.md).
2. The completed Plan → Tasks reconciliation has mapped S04-001…S04-023 into canonical
   `plan.md` and `tasks.md`; `s04-live-readiness-tasks.md` remains supplemental.
3. A new implementation agent uses [s04-implementation-prompt.md](s04-implementation-prompt.md)
   with canonical `tasks.md` as the sole formal tracker.
4. A coordinator starts with [s04-prompt-pack-runner.md](s04-prompt-pack-runner.md) and
   uses [s04-orchestration-task.md](s04-orchestration-task.md) to assess bounded progress
   without inventing parallel or live work.
5. Capture only proven completion in
   [acceptance-report-template.md](acceptance-report-template.md).  A blank template is
   not evidence and does not close any T-task.

## Immutable and safety boundary

All files under `preparation/docs/product-specs/` are immutable source material.  S04
development and automated checks use only sanitized fixtures and fake
`HttpMessageHandler`/transport.  Real BPMSoft, credentials, browser, Excel, compare,
Apply, Git actions, Write/Manage calls, automatic live invocation, retries/Pass C, and
secret/raw-value persistence are outside the pack.

## Relationship to the existing slices

S04 corrects incomplete S01–S03 behavior rather than superseding their history.  It
must preserve the single reader → qualification/reconciliation → evidence pipeline that
later Features 002–004 reuse.  It may not mark T025–T045 complete merely because the
pack, a class, or an old offline report exists; T046 remains a separately human-gated
manual procedure after fresh offline evidence.
