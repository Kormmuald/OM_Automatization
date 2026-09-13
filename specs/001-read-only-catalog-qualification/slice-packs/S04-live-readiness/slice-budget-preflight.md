# S04 budget and readiness preflight

## Scope budget

S04 is corrective and deliberately bounded to the 23 canonical S04 tasks
(`S04-001`…`S04-023`) mapped in `../../tasks.md`; the supplemental list is a crosswalk,
not a second tracker:

| Dependency phase | Supplemental tasks | Deliverable checkpoint |
|---|---|---|
| A — contracts/foundation | S04-001…S04-005 | exact allowlist, typed source/transport boundary, canonical fingerprint, reader semantics, full fixture manifest |
| B — qualification/evidence | S04-006…S04-014 | actual two-pass use case, reconciliation/forecast, invocation policy, one-run evidence lifecycle, safe diagnosis |
| C — offline live readiness | S04-015…S04-021 | fake-handler HTTP/session, shared composition root, runbook, boundary and E2E/adversarial regression |
| D — evidence/review | S04-022…S04-023 | re-evaluate original T-tasks; S04-023 controls the protocol, then actual T043/T044 reruns create the sole T045 package; T046 is external human-gated and excluded |

The implementation agent works in dependency order and may stop at a clean task boundary
with a precise handoff if its context is exhausted.  It must not pre-create parallel
execution branches merely to split the budget.

## Entry gates

- [ ] `plan.md` and canonical `tasks.md` have been updated/reviewed by the required
  Plan → Tasks → Analyze lifecycle to map S04 obligations; supplemental S04 IDs alone
  are not a formal implementation authorization.
- [ ] `speckit.class.gate` for `/SpecKit Implement` passes for exact Feature 001.
- [ ] `check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` confirms the exact
  target and the implementation agent has read every checklist status.
- [ ] Existing dirty changes are inventoried and preserved; the agent scopes its own
  changes without reset/checkout or attribution guesses.
- [ ] Test foundation can be built/run locally with only sanitized fixtures and fakes.

## Completion gates

- [ ] Each canonically mapped task has task-level source/test/evidence; no bulk `[X]`.
- [ ] T043 and T044 are rerun against the completed production composition root with
  fixtures/fake HTTP; T045 has new traceable safe evidence.
- [ ] `acceptance-report-template.md` has been filled only with actual commands, exit
  codes, hashes, test outputs and named blockers.
- [ ] T046 is explicitly still open unless a separately authorized manual/direct-current
  request occurs after G-1.  It is never an automated test action.

## Stop and escalation conditions

Stop and hand off—not workaround—if a requirement needs real target access, credentials,
an unclassified endpoint, arbitrary HTTP, raw values/secrets, Write/Manage capability,
Excel/compare/Apply/browser/Git work, an undocumented endpoint expansion, Pass C/retry,
or completion evidence that cannot be reproduced.  Report a contradiction between
canonical artifacts and this pack to the orchestration owner before changing either.
