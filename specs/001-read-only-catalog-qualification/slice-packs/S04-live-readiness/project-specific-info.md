# Project-specific information — S04 live readiness

## Exact context

- Repository and main worktree: `C:/CodingAgents/codex/projects/OM_Automatization`.
- Active target: `.specify/feature.json` =
  `specs/001-read-only-catalog-qualification`.
- Initiative: L2 / `l2-pilot`; local command order is Plan → Tasks → Analyze → Implement.
- Current state authority: `../handoff-current-thread-20260914.md`, then the actual
  worktree where it differs.
- Immutable common vision:
  `preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md`.

## Slice objective

Implement one production application workflow in which `FixtureCatalogSource` and
fake-handler-tested `HttpCatalogSource` both use the same validated reader,
exact-two-pass qualification/reconciliation, and per-`RunId` safe evidence lifecycle.
The expected offline clean-fixture result is `HUMAN_REVIEW_REQUIRED`, never live
acceptance or Apply permission.

## Current factual gap

The actual source contains useful offline foundation but not the required composed
workflow: `ICatalogSource` only validates a fixture, `CatalogQualificationService` takes
synthetic passes, `TargetFingerprint` is delimiter-based rather than canonical JSON,
`AppendOnlyRunStore.StoreAsync` creates a fresh root per write, and the CLI remains
fail-closed through `AuthorizationGate`/`AuthorizationReference`.  Existing E2E tests
manually compose helpers.  These facts define S04 correction scope; they are not proof
that the dirty worktree belongs to this slice.

## Non-negotiable decisions

- The exact `ReadEndpointAllowlist/v1` has one settled documented/runtime/tested endpoint
  set: remove undocumented `GET_PACKAGES` as a defect. Extension is possible only after an
  owner-approved amendment to authoritative `research.md`, never through local S04 judgment.
- Live read-only admission is only `ManualTerminal` or `DirectCurrentChatRequest`; no
  persisted `AuthorizationReference`, issuer, or verifier exists.
- Offline/automatic paths have zero credential prompts and zero HTTP sends.  Credentials,
  if a later G-2 manual invocation occurs, are terminal-only and in-memory.
- Exactly Pass A and Pass B occur.  Target mutation is terminal with `RetryCount = 0`;
  no Pass C, retry, or automatic new double pass.
- Typed identity/package layers, lossless unknown shapes, deterministic paging/order,
  canonical fingerprinting, schema/scanner-before-write, append-only evidence and safe
  diagnosis remain mandatory.
- `FULL_CATALOG_NOT_QUALIFIED` and `INDEX_SYNC_UNRESOLVED` remain open; neither human
  review nor documentation authorizes Write/Manage, Excel, compare, Apply, browser, or Git.

## Required source set before implementation

Read `AGENTS.md`; `.specify/project.yml`; `.specify/feature.json`; `.specify/extensions.yml`;
`.specify/memory/constitution.md`; current project memory; `spec.md`, `plan.md`,
`research.md`, `data-model.md`, `contracts/cli-contract.md`, `quickstart.md`,
`test-plan.md`, `tasks.md`, `implementation-slices.md`; both S04 documents; the latest
handoff; actual source and regression tests.  The common vision above is read-only.
Missing predecessor evidence is missing, not implied by an S01–S03 prompt or an old PASS.
Before feature-wide Archive, `.specify/memory/spec.md`, `.specify/memory/plan.md`, and
`.specify/memory/changelog.md` are expected to be absent. Do not fabricate them and do not
treat their absence as a stop condition for S04 implementation.
