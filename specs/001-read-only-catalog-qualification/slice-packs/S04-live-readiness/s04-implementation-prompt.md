# Implementation prompt — S04 live-readiness corrective slice

Work as the full implementation agent in the main worktree
`C:/CodingAgents/codex/projects/OM_Automatization`.  Your first read is
`specs/001-read-only-catalog-qualification/handoff-current-thread-20260914.md`.
Then read this entire pack, the current canonical Feature 001 artifacts and actual
worktree.  All user-facing messages are Russian.

## Objective

Implement the S04 correction so fixture and fake-handler-tested HTTP adapters traverse
one production composition root and one reader → exact-two-pass qualification/reconciliation
→ per-RunId safe-evidence pipeline.  Do not infer completed work from old S01–S03 reports
or from the presence of a class/test.  The clean offline result remains
`HUMAN_REVIEW_REQUIRED`; it is not acceptance or permission to Apply.

## Mandatory inputs and formal protocol

1. Read `AGENTS.md`, `.specify/feature.json`, `.specify/project.yml`,
   `.specify/extensions.yml`, `.specify/memory/constitution.md`, current project memory,
   `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/cli-contract.md`,
   `quickstart.md`, `test-plan.md`, `implementation-slices.md`, canonical `tasks.md`,
   `s04-live-readiness-slice.md`, `s04-live-readiness-tasks.md`, this pack, actual source,
   current regression tests, and immutable
   `preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md`.
   Before feature-wide Archive, `.specify/memory/spec.md`, `.specify/memory/plan.md`, and
   `.specify/memory/changelog.md` are expected to be absent. Do not fabricate them or make
   their absence a false stop for S04 implementation.
2. Confirm the current canonical plan/tasks explicitly include the S04 mapping.  If they
   do not, stop and return the gap; supplemental `S04-*` is not a replacement tracker.
3. Before writing, run the mandatory `speckit.class.gate` for `/SpecKit Implement`, then
   once run `.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks
   -IncludeTasks`.  Confirm Feature 001 and report every checklist status.  Honor the
   current `speckit-implement` instructions and all applicable L2 hooks.
4. Preserve pre-existing dirty changes.  Do not reset, checkout, delete, attribute, or
   accidentally stage them.  Use tests before production implementation and dependency
   order.  Mark a canonical task `[X]` only after its exact acceptance evidence exists.

## Required technical order

Execute the current canonically mapped version of S04-001…S04-021 in the phase order
listed in `s04-live-readiness-tasks.md`. At a minimum, remove undocumented `GET_PACKAGES`
as the settled allowlist defect; an extension requires an owner-approved `research.md`
amendment. Replace capture-only/synthetic qualification with typed
reader inputs, implement Decision 2 canonical JSON fingerprinting, complete bounded
paging/inventory validation and fixture-manifest coverage, and enforce full reconciliation
with exactly two reads and terminal no-retry target-change behavior.

Implement the manual live-invocation boundary without `AuthorizationReference`; only a
manually started interactive command or direct current user request can reach a terminal
credential prompt.  Offline/automatic paths prove zero prompt and zero send.  Build the
per-run append-only journal/evidence/seal lifecycle with schema/scanner before every
durable write and a safe `catalog diagnose --run <RunId>` path.  Add `HttpCatalogSource`
and session behavior only against local fake `HttpMessageHandler`/transport and wire it
with the fixture source through the same CLI/application composition root.

Write production-workflow E2E, adversarial, metamorphic, architecture, CLI and leakage
regressions.  Independently calculate expected fixture manifests/hashes; no test may
hand-wire qualification/reconciliation/run-store helpers as the evidence path.

## Absolute boundaries

Never access BPMSoft, request or persist credentials, issue a real HTTP send, use browser,
Excel, compare, Apply, Git, or Write/Manage actions.  Never admit arbitrary endpoints,
redirects, alternate hosts, query/fragment/traversal, method/path/body mismatch, a second
HTTP entry point, unknown-shape default/drop, raw lookup value, cookie/CSRF/login response,
Pass C, retry, automatic new double pass, or success seal after schema/scanner failure.
Keep Domain free of HTTP/console/Excel/browser/Git types.

## Evidence and completion

After S04-001…S04-021, perform S04-022 against original T025–T042 criteria one task at a
time. Then complete S04-023 only as the preparatory/control checkpoint for fresh evidence;
it neither reruns nor closes any T-task and does not create a package. After that checkpoint,
run T043 and T044 once each on the new composition root, then create T045 as the sole new
safe evidence package: fixture IDs/SHA-256, commands and exit codes, run-tree hashes,
reconciliation/scan/schema summaries, tests and failures. Do not run T046: it requires
a separate G-2 manual invocation or direct current user request after G-1 and remains
read-only/human-gated.  Update the handoff with exact facts and use the acceptance report
template from this pack.  Run Converge → Verify → Archive only after the complete formal
Feature 001 implementation is truly done—not after partial S04 delivery.
