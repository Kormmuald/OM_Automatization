# P3-C contract change — bounded index evidence

**Date:** 2026-09-04  
**Status:** COMPLETE — G3-approved documentation correction only.  
**Boundary:** This is not G4. It authorizes neither `.xlsx`/tool work nor full-catalog pull, Manage/Write, BPMSoft mutation, compile, or a Git commit.

## Decision recorded

The owner approved the P3-C correction after the independent index-evidence review. `Indexes` is now a protected read-only evidence table populated from `GetSchema.schema.indexes[]`, with one row per member. `IndexUId` is included because the bounded local 1.8 evidence proves the exact `schema.indexes[].uId` path and GUID value shape.

The member-to-column relation is exclusively:

```text
schema.indexes[].columns[].columnUId -> Columns.ColumnUId
```

The target column may be `Own` or `Inherited` in the selected schema/package layer. `schema.indexes[].columns[].uId` is the index-member object identity, not a `ColumnUId`, and must never be substituted for it. `Columns.ActualIndexed` remains a separate compatibility flag for historical Google `bpmColumn.indexed`; that coarse/lossy mechanism is not the new source of truth, and Google data are not imported. It is not index-membership evidence and is not automatically derived from `schema.indexes[]`.

## Changed files

- `docs/WORKBOOK_CONTRACT_VISION.md` — makes `Indexes` derived/protected/read-only; adds `IndexUId`; defines exact paths, member relation, package-layer resolution, zero-based ordinal, and `ActualIndexed` separation.
- `docs/PROJECT_HANDOFF.md` — records the G3 decision, bounded G2 resolution, unchanged stop conditions, and removes automatic Index-flag implication from writeback wording.
- `docs/SDD_DRAFTS/clarify-review.draft.md` — records bounded evidence, G3 decision, and open gaps.
- `docs/SDD_DRAFTS/constitution.draft.md` — adds the durable project safety rule for read-only index evidence.
- `docs/SDD_DRAFTS/first-feature-spec.draft.md` — adds the parser/verification requirement (`FR-20`) and preserves the write gate.
- `docs/READ_ONLY_RESEARCH_PROMPTS/01-read-only-researcher.md` — replaces the failed own+`indexed=true` oracle with the evidence-backed Own-or-Inherited `columnUId` rule and marks the sample complete.
- `docs/READ_ONLY_RESEARCH_PROMPTS/02-evidence-reviewer.md` — updates independent review criteria to the same rule and explicit limits.

No prototype, SyncOM source, evidence file, workbook, or `.xlsx` was changed.

## Before / after semantics

| Subject | Before | After |
|---|---|---|
| `Indexes` source | Read-only intent, but exact non-empty API schema was unverified. | `schema.indexes[]` is the evidence source for membership. |
| Index identity | `IndexUId` excluded. | Protected derived `IndexUId <- schema.indexes[].uId`. |
| Member relation | Contract did not have an evidenced non-empty relation. | `columns[].columnUId` joins the member to `Columns.ColumnUId`; member `.uId` is explicitly different. |
| Target ownership | Earlier P3-C prompt wrongly required `Own` `Code`/`Name`. | Target may be `Own` or `Inherited` in the current package layer. |
| `ActualIndexed` | Could be read as an index-membership/automatic writeback flag. | Retained only as a separate legacy-compatibility flag; no automatic derivation from membership. |
| Mutation | Field-level gate stated but index context was ambiguous. | Add/change/drop remains prohibited pending a dedicated write preflight. |

## Criterion-to-evidence paths

| Contract criterion | Evidence | Result / limit |
|---|---|---|
| Non-empty index collection | `.../20260904T151951Z/schema-Account-Test1.response-shape.json` (`/schema/indexes` length 2) and `schema-Account-Test1.indexes.shape.json` | Confirmed for one Account/Test1 layer only. |
| `IndexUId`, name, uniqueness | `schema-Account-Test1.indexes.shape.json`: `/schema/indexes/{0,1}/uId`, `/name`, `/isUnique` | Confirmed for two simple, non-auto-named indexes. |
| Member relation | Same file: `/schema/indexes/{0,1}/columns/0/columnUId` | Confirmed: values exactly reconcile to `Code`/`Name` `ColumnUId` in `.../20260904T132710Z/schema-Account-Base.mapping.json`. |
| Inherited target is valid in current layer | `.../20260904T151951Z/schema-Account-Test1.response-shape.json` (own 0, inherited 34), `schema-Account-Test1.own-columns.mapping.json`, and prior Base/extension mappings | Confirmed for this bounded Test1 scenario; old own-only runtime oracle is a false negative. |
| Member `uId` differs from relation key | `schema-Account-Test1.indexes.shape.json` contains distinct `columns/0/uId` and `columns/0/columnUId` values for both indexes | Confirmed for the sample. |
| Inherited `column.indexed` semantics | Full response values are redacted; own-column mapping records null current flags | Not confirmed; excluded from the membership rule. |

Primary interpretive sources: `20260904T151951Z-Account-Test1-index-research.md` and `20260904T151951Z-Account-Test1-index-evidence-review.md`.

## Unresolved gaps and not run

- Composite indexes, auto-named indexes, broader `orderDirection` enum/semantics, full-catalog index representation/generalisation, and cross-run target fingerprint are not evidenced.
- Current inherited `column.indexed` semantics are not evidenced.
- No write/Manage/add/change/drop-index endpoint, compile/save/create/update/delete, `SelectQuery`, full-catalog pull, Excel generation, Google input, or Git commit was run.
- The G2 stop condition remains: no full catalog, `.xlsx`, tool, Manage/Write, or BPMSoft write until all required ordering/pagination and exact mapping gates are positively resolved at their intended scope.
