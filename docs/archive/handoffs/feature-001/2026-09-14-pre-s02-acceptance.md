# Актуальный handoff — Feature 001

**Дата обновления:** 2026-09-14  
**Источник предыдущего handoff:** `docs/archive/handoffs/feature-001-handoff-2026-09-14-pre-s00-reconciliation.md`.

## Текущее состояние

Active feature подтверждена: `specs/001-read-only-catalog-qualification`. S00 reconciled canonical planning artifacts with the approved MVP full read-only live pull and fresh Excel-pair output. Это подготовка к реализации, не implementation evidence и не acceptance.

Canonical tracker is `tasks.md`: every unchecked implementation task belongs exactly to S01–S08. S01 is the first execution candidate, but becomes `execution-ready` only after independent S00 reviewer `Pass` and creation of its stage acceptance/handoff by the orchestrator.

## Зафиксированный контракт

- Production path is one full `Pass A → Pass B → reconciliation → QualifiedCatalogSnapshot/v1 → Excel pair → safe evidence` pipeline.
- A/B are independent reads of the same sealed full workspace/schema/lookup scope. Mismatch is terminal `TARGET_STATE_CHANGED_DURING_QUALIFICATION`, `RetryCount=0`, with no Pass C/retry/partial output.
- Exact read transport is limited to Login, GetWorkspaceItems, GetSchema and SelectQuery. No Write/Manage/Compare/Apply/browser/Git/index mutation is in scope.
- Excel pair is a Feature 001 output adapter after equality only; books can contain lookup values, evidence/audit/CLI/diagnostics cannot.
- S01–S06 use fixtures/fake HTTP only. S07 requires accepted S06, a current explicit user reply «стенд запущен», and terminal-only credentials; it runs exactly once.

## Evidence and prohibitions

Historical build/test results and partial code remain characterization only. No stage task is checked. `FULL_CATALOG_NOT_QUALIFIED` and `INDEX_SYNC_UNRESOLVED` remain open. Do not use real BPMSoft, credentials, browser writes, Excel mutation outside the planned adapter tests, Compare, Apply or Git before the respective stage gates.

## Next allowed action

Orchestrator sends this S00 result to a new independent reviewer using `prompts/alignment-reviewer.md`. Only its `Pass` permits creation of S00 `acceptance-report.md` and stage `handoff.md`, then dispatch of S01.
