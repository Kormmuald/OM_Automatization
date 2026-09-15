# Quickstart: factual validation path for Feature 001

1. S01–S06 have accepted offline evidence under `verification/mvp-slices/<SXX>/`.
   Review the S06 Release/offline report: custom executables, fixture SHA-256,
   run-tree hashes and zero-write boundary evidence.
2. One S07 live run was authorized after `стенд запущен`, then stopped terminally:
   `exit 2`, `SCHEMA_INVENTORY_UNQUALIFIED` / `UNKNOWN_SHAPE_UNQUALIFIED`,
   `RetryCount=0`.
3. No Excel files, pair or `review-only-seal.json` exist for that run; Pass A did not
   complete, and Pass B/successful reconciliation were not reached.
4. Do not retry, start Pass C, or run `catalog qualify` automatically. The next step
   is human review of safe blocker evidence and separately authorized remediation.
5. Any future live command must be a new terminal-only manual admission; the operator
   enters URL/login/password only in terminal prompts.
6. Feature 001 never invokes Write, Manage, Compare, Apply, browser write, Git or
   index mutation.
