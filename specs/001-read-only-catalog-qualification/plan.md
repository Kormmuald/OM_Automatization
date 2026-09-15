# План реализации: full-catalog read-only qualification и Excel output

**Feature**: `001-read-only-catalog-qualification` | **Class**: `L2 / l2-pilot` | **Дата reconciliation**: 2026-09-14.

## Summary

Единый production path: `manual CLI admission → ephemeral BPMSoft session → exact transport → full catalog source → ordered reader → Pass A → Pass B → reconciliation → QualifiedCatalogSnapshot/v1 → Excel adapter → safe evidence seal`. Fixture и fake HTTP проходят этот же use case. Excel является узким output consumer snapshot; сравнение, Apply и write capabilities не создаются.

## Technical context

- .NET 10/C#, `src/BpmSoftSync.Cli`, Domain, Application, BPMSoft/FileSystem adapters; новый Excel adapter остаётся вне Domain/Application.
- Canonical sources are immutable: common vision, MVP reference and workbook contract under `preparation/**`. Legacy semantics are characterized, then dispositioned; no runtime dependency or port-by-copy.
- Exact transport: POST Login, GetWorkspaceItems, GetSchema(one validated `schemaUId`), SelectQuery(canonical reader payload); redirects disabled, exact origin enforced.
- Each run uses unique `<output-root>/runs/yyyy/MM/dd/<RunId>/` with audit/evidence/output/journal. Workbooks can contain lookup values; safe evidence cannot.

## Constitution check

| Gate | Плановое соблюдение |
| --- | --- |
| Read-only | `ReadEndpointAllowlist/v1`, deny-before-send and captured zero writes; no Write/Manage/index mutation. |
| Secrets | interactive terminal only; ephemeral session; scanner before durable safe records. |
| Completeness | full workspace + schemas + lookup registry/collections; lossless envelope or scoped blocker. |
| Reconciliation | exactly Pass A then B of sealed identical scope; mismatch terminal, `RetryCount=0`. |
| Output | B-only in-memory snapshot after equality; atomic new pair; OOXML/read-back/1:1 verification. |
| Human gates | S07 only after accepted S06 and explicit current-chat «стенд запущен»; human decides result. |

Result: PASS for planning only. It neither runs live BPMSoft nor accepts implementation.

## Delivery sequence and stage ownership

1. **S01**: session/HTTP boundary and exact request matrix through fake handler.
2. **S02**: lossless full workspace/object-model inventory.
3. **S03**: Lookup registry plus every supported lookup schema/value under common ordered reader.
4. **S04**: sealed full Pass A/B, terminal reconciliation, snapshot and safe evidence.
5. **S05**: Excel/OOXML adapter, contract projection and atomic pair publication.
6. **S06**: production CLI composition root, compatibility, Release/offline E2E evidence.
7. **S07**: explicit opt-in one-time real integration only after user confirmation.
8. **S08**: documentation, final factual handoff and acceptance material for human decision.

No implementation task may span or be attributed to more than one stage. Stage N+1 needs prior accepted reviewer evidence; S00 is planning-only and creates no acceptance report/handoff.

## Planned artifact and change impact

`research.md` records exact decisions; `data-model.md` defines full-snapshot types; `contracts/cli-contract.md` constrains entrypoints; `test-plan.md` maps requirements to offline/live proof; `tasks.md` is the sole executable tracker. Existing partial S01–S04 code is neither discarded nor accepted: S01–S06 workers assess/refactor it only against their assigned tests and current contract.

Feature 001 owns the one reader/identity/qualification/snapshot pipeline. Feature 002 is a future dependent consumer of its read-only materialization semantics, not permission for this plan to implement workbook-pair control, compare or any write workflow. S06/S08 cumulative regression must preserve existing CLI/offline probes while adding the new production path.

## Risks and stop conditions

- Unknown server shape, endpoint/body/origin mismatch, nonconforming paging, or failed evidence scanner stops the run; no fallback path.
- Scale beyond proven Excel limit yields `WORKBOOK_SCALE_DECISION_REQUIRED`, not truncation.
- Target mutation stops at `TARGET_STATE_CHANGED_DURING_QUALIFICATION`; no retry, Pass C or partial pair.
- Offline test, build or stage review failure blocks later stages. S07 never starts without user confirmation and is not a prerequisite for developing S01–S06.

## Validation strategy

Tests precede implementation per stage. S01–S06 use sanitized fixtures/fake handler only; S06 executes Release build and all custom executables through production composition root. S07 has its separate explicit live suite. S08 reports exact evidence and does not substitute human acceptance.
