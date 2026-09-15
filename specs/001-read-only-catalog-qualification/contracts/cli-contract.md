# CLI contract: Feature 001 full-catalog MVP

This is the planned contract; no command implies its implementation or acceptance exists.

| Command | Allowed inputs/result | Prohibited behaviour |
| --- | --- | --- |
| `catalog validate-offline --fixture <sanitized-fixture>` | fixture/fake only; safe test run and result | network, credential prompt, live assertion, workbook/Git mutation |
| `catalog qualify --target <safe-alias> --scope full --manual [--output-root <path>]` | terminal prompts for exact URL, login and masked password; one read-only full workflow and fresh pair only after qualified A/B | credentials in args/config/files; non-allowlisted request; Pass C/retry; overwrite; Write/Manage/Compare/Apply/browser/Git |
| `catalog qualify --target <safe-alias> --scope full --manual --live` | S07 only: accepted S06 plus explicit current user confirmation; terminal-only credentials | default/CI/unattended execution or automatic retry |
| `catalog diagnose --run <RunId>` | safe status/scope/recovery/next action from validated records | workbook cells, raw responses/secrets or mutation |

`--output-root` is normalized and rejects source-template/prototype roots; default is documented user-local root, never `Path.GetTempPath()`. Success exit `0` explicitly says `HUMAN_REVIEW_REQUIRED`; target-change/other blocker is nonzero and tells the operator the next permitted action. Evidence only carries relative workbook paths and hashes.
