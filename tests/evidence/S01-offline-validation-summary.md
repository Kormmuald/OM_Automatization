# S01 offline validation summary

- Scope: `catalog validate-offline` using only `tests/fixtures/read-only/catalog-valid.json`.
- Fixture: `catalog-valid-v1`; classification `sanitized`; SHA-256 `2babf9f0a77e6456fa926829409761a325514a0a768ac4bc312bdbf0ec048501`.
- Allowlist: all five exact `ReadEndpointAllowlist/v1` entries passed; malformed method, path and body shapes returned `ENDPOINT_NOT_ALLOWLISTED` before any transport capture.
- Capture: five allowed captures with endpoint ID/method/path only; rejected sends `0`; write calls `0`; no HTTP I/O.
- Session: password, cookie and CSRF remain private in-memory state and are cleared on disposal; no serialization surface exists.
- CLI: output contains only reason, scope, recovery and next action. Successful offline outcome is `HUMAN_REVIEW_REQUIRED`, not live authorization or Apply permission.
