# Implementation plan: controlled rewrite `SyncOM` → local CLI + Excel

Статус: **SDD candidate — owner review required; conditional plan only; no implementation or live action authorized**.  
Related spec: [local BPMSoft synchronizer](../product-specs/local-bpmsoft-synchronizer/spec.md).  
Constitution: [project constitution](../product-specs/constitution.md).

## 1. Target outcome

Создать передаваемый Windows-комплект на .NET 10/C#, который реализует управляемый cycle read/pull → workbook pair → strictly read-only compare → immutable plan → allowlisted staged Apply → read-back/browser verification → guarded workbook writeback/Git.

План описывает технический путь и checkpoints. Он не распределяет работу по implementation slices и не разрешает ни одну задачу к исполнению.

## 2. Inputs and current state

- Feature package: [README](../product-specs/local-bpmsoft-synchronizer/README.md), top spec и five child specs.
- Clarification/gates: [clarify-review.md](../product-specs/local-bpmsoft-synchronizer/clarify-review.md).
- Workbook contract: [WORKBOOK_CONTRACT_VISION.md](../WORKBOOK_CONTRACT_VISION.md).
- Latest status: [PROJECT_HANDOFF.md §16](../PROJECT_HANDOFF.md#16-g5-human-acceptance--2026-09-06).
- Legacy reference: `../../SyncOM/`, read-only.
- Evidence: `../READ_ONLY_RESEARCH_RESULTS/` and preserved probe/run roots.
- Accepted bounded reference: template v3 `VerifiedBoundedBaseline`; it does not prove production/full-catalog behavior.

Current production implementation does not exist. `PrototypeReadOnlyPull` and `WorkbookDeliveryTool` are evidence/prototype references, not codebases to incrementally turn into the product unless a later reviewed decision says otherwise.

## 3. Strategy and source precedence

Use a controlled rewrite. Legacy `SyncOM` defines historical workflows, mappings and API candidates, while current requirements redefine identity, determinism, safety, workbook structure and verification.

Precedence:

1. current owner decisions and workbook contract;
2. reproducible BPMSoft evidence and independent reviews;
3. legacy characterization tests;
4. static legacy code and external documentation.

For every legacy behavior record one disposition:

- `reuse-semantics`: useful behavior retained behind new boundaries;
- `rewrite`: goal retained, algorithm replaced;
- `drop`: unsafe/out-of-scope behavior intentionally absent;
- `defer`: potentially useful but blocked by evidence/decision.

No behavior may remain implicit.

## 4. Architecture boundaries

| Component boundary | Responsibility | Must not own |
|---|---|---|
| Domain core | typed identities, canonical values, invariants, dependency graph | HTTP, Excel COM/OOXML, prompts |
| BPMSoft read adapter | session, read allowlist, paging, redacted shapes, fingerprint inputs | compare decisions, writes |
| Workbook contract layer | pair parser/validator/renderer/snapshot model | BPMSoft mutation, hidden merge |
| Compare/plan core | desired-vs-actual canonical diff, blockers, stable operations, plan/report model | workbook mutation, HTTP write |
| Write adapter/executor | owner-approved operation allowlist, exact plan execution, stop-first-error | diff recalculation, unapproved fields |
| Verification/audit | read-back reconciliation, sampling, evidence schemas, run journal | approvals, secrets, corrective writes |
| CLI application | guided state machine and explicit human gates | bypass of component invariants |
| Codex skills | launch/interpret CLI, read-only browser checks, Git choreography | credentials, plan approval, parallel business logic |

Planned source layout (created only during future authorized implementation):

```text
preparation/LocalBpmSoftSynchronizer/
  BpmSoftSync.sln
  src/
    BpmSoftSync.Domain/
    BpmSoftSync.BpmSoft/
    BpmSoftSync.Workbooks/
    BpmSoftSync.Planning/
    BpmSoftSync.Apply/
    BpmSoftSync.Cli/
  tests/
    BpmSoftSync.Domain.Tests/
    BpmSoftSync.Contract.Tests/
    BpmSoftSync.Integration.Tests/
  fixtures/
  docs/
```

Concrete libraries are selected during the first approved engineering task, subject to offline/Windows/self-contained constraints. The path, boundaries and dependency direction are defaults accepted with this plan; changing a boundary requires plan review.

## 5. Work sequence and checkpoints

### Phase P0 — immutable legacy characterization

1. Recompute and record SHA-256 for all six `SyncOM` files without modifying them.
2. Build a finite behavior inventory: scenario, source lines, inputs, endpoint/payload, identity/matching, mutation, failure behavior and disposition.
3. Extract sanitized fixtures for schema/column types, own/inherited relations, lookup references/values, create/update/no-op, missing/ambiguous identity and dependency order.
4. Record non-portable constructs: Name/Code fallback, fixed row counts, client GUIDs, hard-coded `CreatedBy`, random/license branches, destructive schema-link replacement and mixed UI/domain/network logic.
5. Reconcile inventory with [traceability.md](../product-specs/local-bpmsoft-synchronizer/traceability.md).

Checkpoint P0: every material legacy path is classified; hashes match; no implicit behavior or source modification.

### Phase P1 — domain core and offline contracts

1. Define distinct identity/value types for package, schema layer, column, index/member, lookup registry/record and draft token.
2. Define canonical value states and supported type mapping seeded from legacy/evidence.
3. Define unknown-shape envelope, blocker taxonomy, operation key and canonical hashing contracts.
4. Implement only pure/offline tests first: canonicalization, identity, dependency graph, reference precedence and unsupported cases.

Checkpoint P1: legacy-supported fixtures pass under current contract; unknown types fail closed; domain package has no adapter dependencies.

### Phase P2 — generalized read-only adapter

1. Implement interactive in-memory session and deny-by-default read endpoint registry.
2. Implement generalized pageable reader with explicit order, duplicates/skips, terminal/max-page guards and telemetry.
3. Implement package/workspace/schema/lookup parsers preserving package layers and lossless redacted unknown shapes.
4. Regress bounded evidence offline, then perform only separately authorized bounded live read regression.
5. Finalize target fingerprint candidate and document its assumptions.

Checkpoint P2: bounded two-pass results are reproducible, endpoint capture has zero writes, and each unknown shape becomes inventory + scoped blocker.

### Phase P3 — single full-catalog qualification gate

1. After explicit read-only run authorization, inventory all readable workspace items twice on unchanged target.
2. Detail-read only supported EntitySchema/lookup scopes; retain all others as inventory-only/unsupported.
3. Compare counts, ordered identities, canonical hashes, skips/duplicates/retries, response sizes, time and projected Excel limits.
4. Route new shapes to lossless support, explicit unsupported status or one bounded research question.
5. Publish qualification report and independent reconciliation.

Checkpoint P3: PASS reproducible full-catalog baseline, or finite accepted blocker list. No silent omission. This is qualification of the reader, not broad discovery or write authority.

### Phase P4 — workbook pair production path

1. Separate parser, validator, renderer and snapshot manager.
2. Promote template-v3 bounded contract into regression fixtures without modifying accepted reference books.
3. Generate pair atomically from qualified baseline; validate exact contract and source projection.
4. Implement controlled `S_*` snapshot and refresh transaction semantics.
5. Execute offline/temporary-copy OOXML, round-trip, negative, desktop Excel and scale tests.

Checkpoint P4: qualified full-catalog pair opens without recovery, reconciles 1:1 to source/inventory, preserves unsupported items and meets desktop Excel contract.

### Phase P5 — parser, compare and immutable plan

1. Implement parser blockers independently of sheet protection.
2. Implement pure per-field compare rules and reference precedence.
3. Build stable dependency graph and operation ordering; exclude every denied kind.
4. Generate versioned canonical plan + HTML report from one model.
5. Bind plan to workbook/template/pair/target/affected-state fingerprints and precompute browser sample.
6. Implement standalone Apply pre-validator that never recalculates diff.

Checkpoint P5: identical inputs yield stable semantic plan; mutation of every bound input blocks with zero writes; report/plan reconcile exactly.

### Phase P6 — operation-level write preflight package

This phase cannot start before owner decisions `OD-02`, `OD-03`, `OD-05` in clarify/review.

1. Materialize exact candidate matrix from owner allowlist; all unlisted operations remain denied.
2. For each candidate prepare endpoint/payload/expected response/read-back and negative cases.
3. Run only explicitly authorized bounded preflight on disposable/local state.
4. Independently review permissions, identity response, failure semantics, stop behavior and recovery boundary.
5. Owner accepts/rejects each operation kind individually for runtime allowlist.

Checkpoint P6: every enabled kind has version-specific evidence. A failed kind is removed; it does not grant or block unrelated kinds. Index operations remain absent while `INDEX_SYNC_UNRESOLVED` exists.

### Phase P7 — bounded Apply and workbook writeback

1. Add executor registry containing only accepted operation kinds.
2. Validate exact plan, fingerprints, allowlist version, backup acknowledgement and whole-plan approval.
3. Execute dependency order with per-operation audit and stop-first-error.
4. Gate lookup stage on structural save/compile/read-back PASS.
5. Perform complete affected-state read-back.
6. Add separate workbook-only writeback with unchanged-content gate and cell mapping audit.

Checkpoint P7: bounded success reconciles expected state and IDs; fault injection at every boundary proves no later writes/commit after failure.

### Phase P8 — browser verification, audit and Git

1. Implement run artifact schemas, metadata and secret scans.
2. Expose deterministic sample/checklist for read-only browser verification.
3. Record 100% outcome and block success on missing/failed checks.
4. Implement Git settings validation, atomic two-book commit, conflict-safe push and retry rules.

Checkpoint P8: success is traceable plan → writes → read-back → browser → commit; failed/partial cycle has no workbook commit.

### Phase P9 — skills, packaging and independent handoff

1. Create minimal mandatory skills only after CLI contracts stabilize.
2. Package self-contained Windows application, non-secret settings schema and upgrade procedure.
3. Produce operator runbook, failure playbook and evidence-reading guide.
4. Perform clean-machine technical verification and independent human acceptance.

Checkpoint P9: BPMSoft developer completes supported scenario without owner oral context.

## 6. Validation strategy

- Characterization tests for legacy semantics, with assertions rewritten to current contract.
- Unit/property tests for identities, canonical values/hashes, row-order independence, dependency graph and reference precedence.
- Golden/redacted contract fixtures for BPMSoft and workbook shapes.
- Endpoint contract tests proving read-only paths cannot invoke writes.
- Full-catalog double-pass qualification and independent evidence reconciliation.
- Workbook OOXML closure, formula/external/VBA/connection checks, parser round-trip, source projection, desktop Excel checks.
- Plan/report equivalence, stale-input mutation matrix and immutable-plan tests.
- Operation-level integration/fault tests only after allowlist authorization.
- Secret canary scans for every persisted artifact type.
- Browser sample determinism and Git success/failure/conflict tests.
- Clean-machine human acceptance as final gate.

## 7. Research policy

Open no broad research track when legacy inventory + existing fixture/evidence covers a behavior outside a new safety boundary. Additional work is limited to:

- one full-catalog qualification of the generalized reader;
- one bounded question for a newly observed blocking shape;
- operation-level write preflight for explicitly owner-allowlisted kinds.

Each live action requires separate authority and predeclared endpoint, payload shape, invariants, PASS/FAIL and forbidden calls.

## 8. Risks and responses

| Risk | Response |
|---|---|
| Legacy appears authoritative | disposition inventory + source precedence + characterization against current contract |
| Full catalog has novel shapes | lossless unsupported inventory; scoped blocker; never fabricate/drop |
| Duplicate schema names | UId + package-layer identity; name-only matching rejected |
| Fixed legacy paging truncates data | generalized reader and full-catalog double pass |
| Excel mixed sheets allow tampering | parser/compare hard gate and negative fixtures |
| Plan becomes stale or mutable | content/target binding, canonical hash, Apply-only validation |
| Partial Apply causes continued writes | stop-first-error, staged gates, fault injection |
| New IDs resolved by heuristics | response/read-back proof only |
| Index semantics delay core MVP | read-only evidence retained; mutation excluded under blocker |
| Evidence leaks secrets/raw data | deny-by-default schemas, canary/marker scans, no raw values by default |
| Git action records bad state | full success gate + human confirmation; no commit on failed/partial |

## 9. Decisions that implementation may not change

- Controlled rewrite and source precedence.
- Separate read/compare/apply and immutable-plan model.
- Human-only secrets, backup acknowledgement and whole-plan approval.
- Typed server identity; no Name/Code fallback or client-generated server GUIDs.
- No deletes/automatic rollback/browser writes.
- Full-catalog as one qualification gate.
- Operation-level write allowlist/preflight.
- Fail-closed/lossless unknown shapes.
- `INDEX_SYNC_UNRESOLVED` until separate proof/decision.
- Workbook, browser, audit and Git rules in constitution/spec.

## 10. Dependencies and owner gates

Before any decomposition into slices: owner reviews the SDD package and closes OD-01–OD-04. Before any live write research: OD-05. Before implementation execution: separate slice package and OD-06.

Task-level dependencies and validations are listed in [tasks.md](../product-specs/local-bpmsoft-synchronizer/tasks.md). No slice list or slice prompt package exists in this plan.
