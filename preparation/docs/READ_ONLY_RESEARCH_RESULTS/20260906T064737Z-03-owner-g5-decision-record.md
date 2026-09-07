# G5 owner decision record — canonical template v3

**Recorded (UTC):** 2026-09-06 06:47:37  
**Role:** `03-owner-decision-recorder`  
**Decision type:** human G5 acceptance  
**Status:** **accepted with limits**

## Exact owner decision

**«принимаю»**

The owner gave this exact reply to the following exact G5 question from [the G5 decision package](20260906T064102Z-02-g5-decision-package.md):

> «Подтверждаете ли вы после личного открытия обеих canonical template-v3 книг в desktop Excel отсутствие recovery, видимость ожидаемых данных и листов, работу resize и existing filters, блокировку protected cells и редактируемость mixed sheets, а также решение G5 `accept with limits` строго для `VerifiedBoundedBaseline` без разрешения full-catalog/write/load и со сохранением `INDEX_SYNC_UNRESOLVED`?»

Accordingly, this is a **human G5 acceptance: `accept with limits`**, not an agent recommendation. It records the owner’s stated personal desktop-Excel confirmation of no recovery, visible expected data and sheets, resize and existing-filter behaviour, protected-cell write blocking, and mixed-sheet editability.

## Accepted bounded scope

Accepted only: canonical template v3 as a bounded read/export workbook delivery for `VerifiedBoundedBaseline`.

| Canonical workbook | SHA-256 |
|---|---|
| `workbooks/BPMSoft.ModelCatalog.xlsx` | `7fb01fd5f4af039beb45e7e77819385d9c25beac6762616cf6212e6ac32a849f` |
| `workbooks/BPMSoft.LookupCatalog.xlsx` | `48719140d6ecf4a7b21c61fc381eccb8aa95f2b8e32f437c360ed14e96f07b2d` |

The accepted pair remains `TemplateVersion=3-bounded-research`, `PairId=cb3722e8-ee9e-4f47-a299-723769ce7bbf`, `PullRunId=80fac4ee-e388-4f25-adf0-a8305950c3c6`, and `PairBaselineHash=f2480cdbdaa036856ebedbbbae45fe6dd55b919a4156bb6af78833a602e4f65a`.

## Limits not accepted or closed

- Full-catalog completeness, scale, and generalisation.
- LibreOffice Tier 2/render.
- Composite/auto-name/broader index-order semantics and inherited `column.indexed` semantics.
- Production parser/compare, immutable plan, runtime reference precedence, `DraftRowToken -> RecordId`, and invalid-GUID runtime fixture.
- Any BPMSoft Write/Manage capability, create/update/delete/compile/save, loader, load/apply/writeback, or safe index add/drop planning/loading.
- Google input, `SyncOM/` changes, Git success workflow, and full MVP-synchronizer acceptance.

## Mandatory retained blocker

`INDEX_SYNC_UNRESOLVED` remains in force. The accepted `Indexes` export is read-only evidence only. No index load/apply may start until a separate future safety gate proves that the separation of `ActualIndexed` from `Indexes` cannot create false add/drop actions and the owner explicitly accepts that result.

## Next safe step

Retain the accepted canonical pair unchanged as the `VerifiedBoundedBaseline` reference and stop this delivery chain. Any further work needs a separate new owner authorization and cannot infer permission for full-catalog work, BPMSoft write/load/apply, or index loading from this G5 acceptance.

## Changed files

- `docs/PROJECT_HANDOFF.md` — appended G5 human-acceptance status, retained limits/blocker, next safe step, and changed-file statement.
- `docs/READ_ONLY_RESEARCH_RESULTS/20260906T064737Z-03-owner-g5-decision-record.md` — this timestamped decision record.

`docs/WORKBOOK_CONTRACT_VISION.md` and all three `docs/SDD_DRAFTS/*.draft.md` were not changed because their current agreed safety limits already match this bounded G5 acceptance.
