# S04 prompt-pack runner

This is the repo-native analogue of an all-slice pack runner.  It sequences the local
pack; it does not claim to be, or to invoke, the external `slice-generator`.

1. Verify exact Feature 001 context and read the latest handoff before all other work.
2. Read `project-specific-info.md`, then the preflight.  Stop on any unchecked entry;
   do not silently use the supplemental S04 list as a task-tracker substitute.
3. Invoke the full implementation agent with `s04-implementation-prompt.md`.  Provide it
   the latest canonical `plan.md`, `tasks.md`, Analyze outcome, and dirty-worktree
   inventory in addition to the prompt.
4. Require the agent to execute phase checkpoints and return only safe evidence.  A phase
   failing a named stop condition is not eligible to advance.
5. Use `acceptance-report-template.md` only to record verified execution. S04-023 is only
   the fresh-evidence control checkpoint; the actual T043/T044 reruns occur once each and
   T045 is their sole package. Keep T046 outside the run unless a separate
   human/manual/direct-current-request authorization exists after fresh G-1 offline evidence.
6. At full formal Feature 001 completion, follow the L2 `Converge → Verify → Archive`
   lifecycle and update project memory.  Partial S04 progress does not trigger those
   feature-wide completion hooks.
