# S05 — independent alignment review 01

- Reviewer model/reasoning: `gpt-5.6-sol` / `high`.
- Reviewed worker result: `/root/s05_worker`.
- **Blocker** — publisher accepts constructed qualification after only `BeginRunAsync`; it must verify persisted accepted S04 pass/reconciliation/snapshot evidence and binding before staging.
- **Blocker** — pair becomes visible before evidence/seal; cancellation/error can leave output without seal or durable evidence/seal without output. Make publication/evidence/seal fail-closed and add post-move fault/cancel matrix.
- **Fix** — ActualFingerprint must include exact schema/package/column/index semantics; remove fallback digest fabrication.
- **Fix** — validate definedNames, sqref/data validation, sheet protection, relationships and content types under OOXML tamper closure.
- **Fix** — expand full-snapshot matrix: multi-package/name collision, multiple lookups, composite index, Null/reference and all supported typed kinds; assert identity-level 1:1 projection.
- **Fix** — pull timestamps are an S04→S05 contract gap, not `not-recorded`; reconcile canonical contract safely.
- **Fix** — output must follow canonical `output/*.xlsx`, not unapproved `output/pair-<PairId>/` layout.
- **Log** — LibreOffice unavailable is correctly skipped but exit code documentation is stale.
- **Pass** — Release build/six suites, static XLSX checks and raw-canary boundary passed; no live/write/S06 scope.

Verdict: Blocker.
