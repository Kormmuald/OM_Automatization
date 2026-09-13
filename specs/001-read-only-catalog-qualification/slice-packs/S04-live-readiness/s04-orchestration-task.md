# Bounded orchestration task — S04 implementation

Use this task only after canonical planning/tasks analysis has completed.  It is a
coordination brief, not a substitute for `/SpecKit Implement`.

## Assigned outcome

Bring the corrective S04 requirements into a tested, production-composition offline
workflow.  Use `s04-implementation-prompt.md` as the detailed execution brief and
canonical `../../tasks.md` as the sole formal task tracker.

## Required checkpoints

1. Record the preflight state and exact canonical task mapping before source edits.
2. Complete Phase A, then run focused contract/fixture tests before Phase B.
3. Complete Phase B, prove exactly two reads/no retry and per-RunId evidence lifecycle,
   then run focused qualification/evidence tests.
4. Complete Phase C exclusively through sanitized fixtures and fake HTTP/session tests;
   prove the CLI composition root—not hand-wired helpers—drives the workflow.
5. Perform Phase D only with freshly generated offline evidence. Re-evaluate T025–T042
   individually, then use S04-023 only as the preparatory/control checkpoint. Run actual
   T043/T044 once each and produce T045 as the sole package. Leave T046 open.
6. Produce a precise implementation handoff, including completed canonical IDs, modified
   files, fixture IDs/SHA-256, commands/exit codes, result artifacts, blockers, and any
   untouched dependencies.  Run feature-wide Converge/Verify/Archive only if every
   canonical Feature 001 task is genuinely complete and mandatory L2 hooks apply.

## Independence and safety

Do not dispatch hidden live work, request credentials, or ask another agent to bypass a
gate.  If a correction is required after review, assign that narrow defect to a fresh
agent and preserve already accepted design decisions.  Use `ManualTerminal` and
`DirectCurrentChatRequest` as safe enum values only; never store chat text or secrets.
