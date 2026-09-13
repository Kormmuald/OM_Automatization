# Clarify outcome — S04 live-readiness

**Date:** 2026-09-14
**Feature:** `001-read-only-catalog-qualification`
**Scope:** corrective Slice S04 only
**Immutable source read:** `preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md`

## Result

No critical ambiguities detected worth formal clarification.  No new owner answer was
requested or invented, and `spec.md` was not changed.  The prior Clarifications sessions
already settle the only owner-level live-read admission rule: a manually started
interactive `catalog qualify` or a direct current user request; no persisted
`AuthorizationReference`, no autonomous run, and no write permission.

## Coverage and resolved interpretation

| Area | Status | Clarify result |
|---|---|---|
| Functional scope and role boundary | Clear | S04 remains a Feature 001 corrective slice. It implements a single shared read → qualification/reconciliation → evidence workflow and does not create a separate feature or reader. |
| Live-read admission and credentials | Clear | Only `ManualTerminal` or `DirectCurrentChatRequest` may reach a terminal-only, in-memory credential prompt. Offline/automatic paths have zero prompt and zero send. This is not a permission for Write/Manage, browser, Excel, compare, Apply, or Git. |
| Two-pass behavior and target mutation | Clear | Exactly Pass A and Pass B. `TARGET_STATE_CHANGED_DURING_QUALIFICATION` is terminal with `RetryCount = 0`; no Pass C, retry, or automatic new double pass. |
| Endpoint allowlist discrepancy | Resolved from existing authority | `research.md` Decision 1 is the active contract: `AUTH_LOGIN`, `WORKSPACE_ITEMS`, `SCHEMA_GET`, and `SELECT_QUERY` only. The current code/test-only `GET_PACKAGES` entry is a documented S04-001 corrective defect, not an open product decision; it must be removed unless an owner-approved change first updates the authoritative research contract. |
| Typed source, fingerprint, paging, and evidence lifecycle | Clear as required outcome | S04-002…S04-014 specify the missing production behavior and its executable offline evidence. Current source gaps do not alter the approved requirements or permit a weaker path. |
| External response/login/CSRF shape | Deferred technical validation | No sanitized authoritative capture proves those shapes. S04 may use only bounded sanitized fixtures and fake handler tests; unknown shapes or an unclassified endpoint must fail closed and be escalated, not guessed or expanded during implementation. |
| Canonical task governance | Reconciled after this historical Clarify stage | The supplemental `S04-*` list and slice pack are not the formal tracker. The required Plan → Tasks reconciliation has since mapped S04-001…S04-023 into canonical `tasks.md`; it remains the sole formal tracker for implementation. |
| Completion/evidence and T046 | Clear | S04 implementation cannot close T046. T025–T042 require individual evidence review; T043/T044 must be rerun and T045 recreated after the new composition-root workflow. |

## Checks performed

- Read active context, constitution, L2 workflow/overlay, hooks, immutable common vision,
  Feature 001 spec/plan/research/data model/CLI contract/test plan/quickstart/tasks, S04
  slice/tasks, repo-native pack, handoff, and relevant source/regression tests.
- Ran `check-prerequisites.ps1 -Json -PathsOnly` with a process-level
  `ExecutionPolicy Bypass` because the local script is unsigned; it resolved active Feature
  001 and its spec/plan/tasks paths.
- Applied the mandatory class-gate rules for `/SpecKit Clarify`: `PASS` — class `L2`, profile
  and overlay `l2-pilot`, ratified constitution, and visible Feature 001 artifacts.
- Performed a targeted consistency scan. It confirms the documented corrective gaps:
  code/test currently allow `GET_PACKAGES`, `ICatalogSource` is fixture-validation-only,
  qualification is synthetic-pass based, the fingerprint is delimiter-based, the run store
  creates a root per `StoreAsync`, and `catalog qualify` still fails closed through the
  historical authorization gate. These are implementation gaps intentionally covered by S04,
  not evidence of completed work.
- Revalidated `checklists/requirements.md`: `16/16` items remain passing; no checkbox state
  changed because `spec.md` was not modified.

## Constraints for the next stage

1. Do not reopen the settled manual/direct-request rule or reintroduce
   `AuthorizationReference`.
2. The project-local `Plan → Tasks` reconciliation has completed after this Clarify
   snapshot; its canonical S04-001…S04-023 mapping in `tasks.md` is the input to Analyze
   and Implement. Do not treat this report or the supplemental S04 list as implementation
   authorization.
3. Preserve pre-existing dirty production changes. No real BPMSoft, credentials, real HTTP,
   Write/Manage, browser, Excel, compare, Apply, Git action, retry, or Pass C is allowed by
   this clarification outcome.
4. Treat unresolved response/login/CSRF shape as a fail-closed fixture/test boundary. A later
   need to add an endpoint or relax origin restrictions requires authoritative evidence and
   the appropriate owner/governance change, not a local S04 assumption.
