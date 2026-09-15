# S07 manual live-verification prompt

Use this only after S06 has an independent reviewer `Pass`, a human has decided the
current blocker is resolved, and the user has explicitly replied in the current chat:
`стенд запущен`.

1. Open an interactive local terminal. Do not put a URL, login, password, cookie, CSRF value or authorization data in chat, CLI arguments, configuration, files, logs or evidence.
2. Confirm the command is exactly:

   `BpmSoftSync.Cli catalog qualify --target <safe-alias> --scope full --manual --live [--output-root <new-user-local-directory>]`

3. Enter URL, login and password only at the terminal prompts. The command performs the allowlisted read-only sequence and two complete independent passes. No retry, Pass C, Compare, Apply, Write, Manage, browser write or Git action is permitted.
4. Stop immediately on any blocker. Do not rerun automatically. Preserve only the safe RunId, safe result, counts, hashes and relative pair paths for review; do not copy lookup values or raw responses into evidence.
5. On `HUMAN_REVIEW_REQUIRED`, inspect the sealed safe evidence and the two local output workbooks. A human decides whether a later operation is permitted; this command never applies a change.

Default tests/CI do not invoke this route. Do not run it as part of S06.

## Current historical result — do not reinterpret as PASS

The authorized S07 run on 2026-09-14 ended `exit 2` with
`SCHEMA_INVENTORY_UNQUALIFIED` / `UNKNOWN_SHAPE_UNQUALIFIED` and `RetryCount=0`.
It produced no Excel files (`output/` empty, `.xlsx=0`) and no
`review-only-seal.json`; `HUMAN_REVIEW_REQUIRED` was not reached. This prompt does
not authorize a retry. A future run needs a separate human decision after the target
schema blocker is addressed.
