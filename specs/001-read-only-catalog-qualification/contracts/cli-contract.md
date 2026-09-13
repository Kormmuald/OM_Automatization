# CLI contract: feature 001

This is a planned public CLI contract. Commands below do not imply that their implementation exists yet. A live command is started only manually by an operator or by an agent after a direct current user request in an available chat; it needs no persisted authorization reference.

| Command | Inputs | Permitted result | Prohibited behaviour |
|---|---|---|---|
| `catalog validate-offline --fixture <sanitized-fixture>` | local fixture only | fixture validation, safe test run root | network, credential prompt, workbook/Git changes |
| `catalog qualify` | interactive local target URL/login and declared scope; password only through terminal prompt | two-pass read-only qualification and safe artifacts | automatic agent start without a direct current request; secrets in args/config/artifacts; endpoint outside allowlist; pass C/retry; write/manage/browser/Git actions |
| `catalog diagnose --run <RunId>` | existing run ID | safe blocker/recovery/next-action view | raw response, secret/value reveal, state mutation |

Password input is terminal-masked and in-memory only. No command accepts `--password`, cookie, CSRF, Authorization header, response body or a Write/Manage switch.

| Outcome | CLI behaviour |
|---|---|
| success evidence ready | exit `0`, but output explicitly says `HUMAN_REVIEW_REQUIRED`; it is not Apply authorization. |
| `FULL_CATALOG_NOT_QUALIFIED` | nonzero only as a qualification outcome; it is not a precondition that blocks a manual live invocation. |
| `TARGET_STATE_CHANGED_DURING_QUALIFICATION` | nonzero, seal artifacts; state that a new manual invocation or direct current user request is required, with no automatic retry. |
| any paging/inventory/evidence/allowlist blocker | nonzero, safe recovery and next permitted action. |

`IReadOnlyTransport` accepts only validated `EndpointClassification`; `ICatalogSource` returns typed data; `IRunStore` persists only validated/scanned `EvidenceEnvelope`. Domain code never accepts `HttpClient`, browser, Excel or Git types.
