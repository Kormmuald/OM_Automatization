---
description: "Canonical implementation tracker for MVP full-catalog Excel pack"
---

# Tasks: Feature 001 full-catalog read-only pull and Excel pair

**Input**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/cli-contract.md`, `test-plan.md`, `quickstart.md`; immutable MVP `implementation-prompts/MVP-live-full-catalog-excel.md` and common vision.  
**Execution rule**: every task is unchecked until its own stage has worker evidence and independent reviewer `Pass`. Each task belongs to exactly one S01–S08 stage. S00 is planning only and is not a task acceptance. Later stage requires previous `verification/mvp-slices/<SXX>/acceptance-report.md` and `handoff.md`.

## Factual execution status — S08 worker record

The approved tracker has retained its unchecked-box format. The status below records
actual worker/stage evidence and does not mean Feature 001 acceptance or target
qualification.

| Task set | Factual status |
| --- | --- |
| S01-001…S06-004 | completed; independent stage evidence accepted offline |
| S07-001…S07-002 | completed as one authorized live attempt; stage evidence accepted, target not qualified |
| S08-001…S08-003 | documentation worker work complete; independent S08 review pending |

`FULL_CATALOG_NOT_QUALIFIED` and `INDEX_SYNC_UNRESOLVED` remain open. No status
authorizes retry, Pass C, Write, Manage, Compare, Apply, browser write, Git or index
mutation.

## Phase 1 — S01: session and HTTP boundary

- [ ] S01-001 [US1] Add fake-handler contract tests for exact `ReadEndpointAllowlist/v1` request IDs, method/path/body/origin/query/fragment/redirect rejection in `tests/BpmSoftSync.Adapters.BpmSoft.Tests/ReadEndpointAllowlistTests.cs`; Reqs: FR-002; Evidence: zero rejected sends/writes.
- [ ] S01-002 [US1] Add session lifecycle tests for interactive-only login, cookie/`BPMCSRF`, timeout/cancellation/disposal and secret nonserialization in `tests/BpmSoftSync.Adapters.BpmSoft.Tests/SessionTests.cs`; Reqs: FR-001; Evidence: fake HTTP only.
- [ ] S01-003 [US1] Implement one typed classified request/response transport and ephemeral session in `src/BpmSoftSync.Adapters.BpmSoft/`; Reqs: FR-001, FR-002, FR-014; Depends: S01-001–002.
- [ ] S01-004 [US1] Prove no second HTTP entrypoint and no `GET_PACKAGES` allowance with architecture/capture tests in `tests/BpmSoftSync.Cli.Tests/ArchitectureTests.cs`; Reqs: FR-002, FR-014.

## Phase 2 — S02: full object model

- [ ] S02-001 [US1] Create sanitized full workspace/schema fixtures and tests for package-layer identity, support statuses and lossless unknown shapes in `tests/fixtures/read-only/` and `tests/BpmSoftSync.Domain.Tests/WorkspaceInventoryTests.cs`; Reqs: FR-005; Evidence: no default/drop.
- [ ] S02-002 [US1] Add object-model tests for own/inherited columns, references, indexes and ordinal members via `columns[].columnUId` in `tests/BpmSoftSync.Adapters.BpmSoft.Tests/WorkspaceInventoryAdapterTests.cs`; Reqs: FR-005.
- [ ] S02-003 [US1] Implement typed full workspace/schema adapters and lossless envelope in `src/BpmSoftSync.Domain/` and `src/BpmSoftSync.Adapters.BpmSoft/`; Reqs: FR-005, FR-014; Depends: S02-001–002.
- [ ] S02-004 [US1] Add golden/metamorphic `TargetFingerprint/v1` vectors in `tests/BpmSoftSync.Domain.Tests/TargetFingerprintTests.cs`; Reqs: FR-004; Evidence: no raw-value preimage.

## Phase 3 — S03: full lookup data

- [ ] S03-001 [US1] Add registry and multipage lookup fixtures for every supported type, null/empty/value/reference and nonstandard columns in `tests/fixtures/read-only/lookup-*`; Reqs: FR-003, FR-006.
- [ ] S03-002 [US1] Add ordered-reader adversarial tests for registry and each lookup collection in `tests/BpmSoftSync.Domain.Tests/CatalogReaderPagingTests.cs`; Reqs: FR-003; Evidence: named paging blocker/no hang.
- [ ] S03-003 [US1] Implement canonical explicit-column `SelectQuery` reader, registry discovery and normalized lookup values in `src/BpmSoftSync.Application/` and `src/BpmSoftSync.Adapters.BpmSoft/`; Reqs: FR-003, FR-006; Depends: S03-001–002.
- [ ] S03-004 [US1] Verify full lookup scope is lossless or scoped-blocked with source/value fingerprints in `tests/BpmSoftSync.Adapters.BpmSoft.Tests/LookupCatalogSourceTests.cs`; Reqs: FR-006.

## Phase 4 — S04: two-pass snapshot and evidence

- [ ] S04-001 [US1] Add source-read-counter and reconciliation tests proving full independent A/B, sealed scope and no A cache reuse in `tests/BpmSoftSync.Application.Tests/CatalogQualificationServiceTests.cs`; Reqs: FR-004, FR-007.
- [ ] S04-002 [US1] Add terminal target-change, paging/shape and no-Pass-C/no-retry tests in `tests/BpmSoftSync.Domain.Tests/CatalogQualificationTests.cs`; Reqs: FR-004; Evidence: `RetryCount=0`.
- [ ] S04-003 [US2] Implement `ScopeDescriptor`, `CatalogPass`, reconciliation and `QualifiedCatalogSnapshot/v1` in `src/BpmSoftSync.Domain/` and `src/BpmSoftSync.Application/`; Reqs: FR-004, FR-007; Depends: S04-001–002.
- [ ] S04-004 [US3] Implement append-only run lifecycle, schema validator and scan-before-write/seal in `src/BpmSoftSync.Adapters.FileSystem/` with canary/collision tests; Reqs: FR-009, FR-010.

## Phase 5 — S05: Excel materialization

- [ ] S05-001 [US2] Add contract tests for required Model/Lookup sheets, headers, order and `LookupValues` normalization in `tests/BpmSoftSync.Adapters.Excel.Tests/WorkbookContractTests.cs`; Reqs: FR-008.
- [ ] S05-002 [US2] Add exact 1:1 projection, pair/manifest, read-back/OOXML closure and external/VBA/formula guard tests in `tests/BpmSoftSync.Adapters.Excel.Tests/WorkbookValidationTests.cs`; Reqs: FR-008, FR-009.
- [ ] S05-003 [US2] Implement isolated Excel/OOXML adapter and in-memory snapshot projection in `src/BpmSoftSync.Adapters.Excel/`; Reqs: FR-007, FR-008; Depends: S05-001–002.
- [ ] S05-004 [US2] Implement unique staging, fault-injected validation and atomic pair publication under run `output/` in `src/BpmSoftSync.Adapters.FileSystem/`; Reqs: FR-008, FR-010.
- [ ] S05-005 [US3] Prove raw lookup values remain in workbook only and are absent from safe outputs in `tests/BpmSoftSync.Adapters.FileSystem.Tests/OutputEvidenceBoundaryTests.cs`; Reqs: FR-009.

## Phase 6 — S06: production CLI and offline E2E

- [ ] S06-001 [US1] Wire one production composition root for fixture/fake HTTP/full snapshot/Excel/evidence in `src/BpmSoftSync.Cli/Program.cs` and command handlers; Reqs: FR-011.
- [ ] S06-002 [US3] Preserve and test compatible `catalog validate-offline`, `catalog qualify`, `catalog diagnose` contracts and safe output-root validation in `tests/BpmSoftSync.Cli.Tests/`; Reqs: FR-010, FR-011.
- [ ] S06-003 [US2] Add production-main fake E2E and adversarial regression suite in `tests/BpmSoftSync.Cli.Tests/ReadOnlyQualificationE2ETests.cs`; Reqs: FR-001–FR-011, FR-014.
- [ ] S06-004 [US3] Run Release build and every custom test executable; create fresh safe offline report with exact commands/exits, fixture SHA-256 and output-tree hashes in `specs/001-read-only-catalog-qualification/verification/`; Reqs: FR-011, FR-013.

## Phase 7 — S07: opt-in live integration

- [ ] S07-001 [US1] Implement separately opt-in `--live --manual` harness and test that default/CI paths never reach it in `src/BpmSoftSync.Cli/` and `tests/BpmSoftSync.Cli.Tests/LiveAdmissionTests.cs`; Reqs: FR-012.
- [ ] S07-002 [US2] After accepted S06 and explicit current-chat «стенд запущен», execute exactly one terminal-only live run; save only safe aggregate evidence and pair validation in `specs/001-read-only-catalog-qualification/verification/mvp-slices/S07/`; Reqs: FR-012; Stop: any blocker/no retry.

## Phase 8 — S08: final docs and handoff

- [ ] S08-001 [US3] Update `docs/read-only-handoff/`, CLI help and `specs/001-read-only-catalog-qualification/verification/live-mvp-verification-prompt.md` from accepted facts; Reqs: FR-013.
- [ ] S08-002 [US3] Produce final safe evidence index, acceptance report and concise current `HANDOFF.md` in `specs/001-read-only-catalog-qualification/verification/mvp-slices/S08/`; Reqs: FR-013.
- [ ] S08-003 [US3] Present the resulting pair paths, commands/results, blockers and human decision point without declaring acceptance in `specs/001-read-only-catalog-qualification/HANDOFF.md`; Reqs: FR-013.

## Requirement → stage → task mapping

| Requirement | Stage | Tasks |
| --- | --- | --- |
| FR-001 | S01/S06 | S01-002–003; S06-003 |
| FR-002 | S01/S06 | S01-001, S01-003–004; S06-003 |
| FR-003 | S03/S06 | S03-001–003; S06-003 |
| FR-004 | S02/S04/S06 | S02-004; S04-001–003; S06-003 |
| FR-005 | S02/S06 | S02-001–003; S06-003 |
| FR-006 | S03/S06 | S03-001–004; S06-003 |
| FR-007 | S04/S05/S06 | S04-001, S04-003; S05-003; S06-003 |
| FR-008 | S05/S06 | S05-001–004; S06-003 |
| FR-009 | S04/S05/S06 | S04-004; S05-002, S05-005; S06-003 |
| FR-010 | S04/S05/S06 | S04-004; S05-004; S06-002–003 |
| FR-011 | S06 | S06-001–004 |
| FR-012 | S07 | S07-001–002 |
| FR-013 | S06/S08 | S06-004; S08-001–003 |
| FR-014 | S01/S02/S06 | S01-003–004; S02-003; S06-003 |

## Dependency order and checkpoints

`S01 → S02 → S03 → S04 → S05 → S06 → S07 → S08`. S01–S06 are offline-only. Each arrow requires prior independent reviewer `Pass` plus actual acceptance/handoff files; S07 additionally requires explicit user confirmation. Existing implementation and historical test output do not check these tasks.
