# S01 handoff

- Accepted stage: `S01`.
- Следующий stage: `S02`.
- Readiness следующего stage: `execution-ready` — S01 independently accepted in `acceptance-report.md`.

## Что фактически изменено

- `BpmSoftReadTransport` and contracts — sole redirect-denying, bounded, typed HTTP read boundary.
- `ReadEndpointAllowlist` and credential/session components — exact read allowlist and terminal-only ephemeral session policy.
- S01 tests — fake-handler coverage for request denial, session lifecycle, redirect, delayed stream timeout, response bounds and secret safety.

## Стабильные входы следующего stage

- `BpmSoftReadTransport.LoginAsync`, `GetWorkspaceItemsAsync`, `GetSchemaAsync`, `WorkspaceItemsResponse`, `SchemaResponse`, `BpmSoftHttpOptions` and injected fake handler seam.
- S02 must map full object model through that boundary; it must not create a second `HttpClient`, use synthetic production pages or begin lookup/Pass A/B/Excel work.

## Проверки и evidence

- Acceptance report: `acceptance-report.md`.
- Independent reviews: `review-01.md` (resolved), `review-02.md` (Pass).
- Commands/results: Release build and four custom test executables passed; no live run.

## Открытые вопросы и запреты

- `FULL_CATALOG_NOT_QUALIFIED` and `INDEX_SYNC_UNRESOLVED` remain open.
- S02 is fixtures/fakes only; no real BPMSoft, credentials, Write/Manage/Apply/Compare/browser write/Git actions.
