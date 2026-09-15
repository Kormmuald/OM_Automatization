# Test plan: full-catalog qualification and Excel pair

## Evidence rules

S01–S06 use sanitized fixtures and fake `HttpMessageHandler` only. Evidence records command, exit code, fixture ID/SHA-256, run-tree/output hashes, counts/digests and scanner/schema result; it contains no credential, session material, raw response or lookup cell value. S07 is distinct, opt-in and records safe aggregates only.

## Stage test matrix

| Stage | Requirements | Mandatory proof |
| --- | --- | --- |
| S01 | FR-001, FR-002, FR-014; SC-001, SC-004 | exact endpoint/body/origin matrix, fake login/cookie/CSRF lifecycle, no secret persistence and zero rejected sends/writes |
| S02 | FR-005, FR-014; SC-003, SC-004 | full workspace/schema fixtures, package-layer collision, own/inherited/reference/index ordinal and unknown-shape tests |
| S03 | FR-003, FR-006; SC-003, SC-004 | registry plus multipage lookup collections, null/empty/value/reference/nonstandard column, paging/limits/terminal tests |
| S04 | FR-004, FR-007, FR-009, FR-010; SC-002, SC-004, SC-006 | source-read counter exactly two; equality/mutation/no-Pass-C; snapshot gate; evidence/journal collision, schema and canary scans |
| S05 | FR-008, FR-009; SC-005, SC-006 | 1:1 model/lookup projection, sheets/headers/order, pair/manifest, read-back/OOXML/formula/external/VBA guards, fault-injected atomic publication |
| S06 | FR-011, FR-013, FR-014; SC-001–SC-007 | production `Program.Main` fixture/fake E2E, compatibility regression, Release build/all custom executables and fresh safe offline report |
| S07 | FR-012; SC-001, SC-002, SC-005, SC-008 | explicit opt-in harness only: full live A/B, pair verification, capture zero writes or one terminal blocker/no retry |
| S08 | FR-013; SC-007, SC-008 | docs/handoff facts, stage evidence references and user-decision prompt; no fabricated acceptance |

## Final exit criteria

No later stage begins without prior accepted reviewer evidence. A passing offline suite cannot close `FULL_CATALOG_NOT_QUALIFIED`. `WORKBOOK_SCALE_DECISION_REQUIRED` is fail-closed and `TARGET_STATE_CHANGED_DURING_QUALIFICATION` remains terminal with no partial pair.
