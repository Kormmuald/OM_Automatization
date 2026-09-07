# Independent P3-C Account/Test1 index evidence review

**Status:** COMPLETE  
**New evidence root:** `C:\CodingAgents\codex\projects\OM_Automatization\preparation\PrototypeReadOnlyPull\bin\Release\net10.0\probe-output\20260904T151951Z`  
**Prior successful evidence root:** `C:\CodingAgents\codex\projects\OM_Automatization\preparation\PrototypeReadOnlyPull\bin\Release\net10.0\probe-output\20260904T132710Z`

This review is strictly offline. No BPMSoft/login request, source change, evidence change, contract/SDD/handoff change, or workbook creation is permitted. This report is the sole planned modification.

## Pending checks

1. Inputs, exact file set, byte lengths, SHA-256 manifest and redaction. **Completed:** exactly nine regular files are present; manifest self-excludes and lists the other eight; names match exactly. Every listed byte length/SHA-256 matches and all eight are strict UTF-8 without BOM. Independently computed manifest SHA-256 is `dcb01e65e09cbf1e6f06be2e21b61f5147c1c8808d50865712ec11dd558870bb`. `redaction-check.json` count 9 matches inventory; independent value-only marker scan found no credential/session/raw lookup markers. Structural labels such as `caption`, `description`, `sourceCode`, `session`, and `value` occur only as property names in the redacted ledger.
2. Bounded request contract and Account/Test1 selection traceability. **Completed:** request contract records `--index-probe`, exactly one `GetWorkspaceItems` and one `GetSchema`, and no SelectQuery. Recomputed external assembly hashes/version match the two pinned 1.8.0.14107 assemblies. Workspace selection is uniquely `Account/Test1/type=3`, and its workspace UId exactly equals the persisted returned schema UId. Source control flow branches from index-probe directly to `RunIndexProbeAsync`, performs those two reads, and returns before default ordering flow; static source intent contains no write/Manage endpoint.
3. Non-empty index shapes/values; exact `Index1 → Code` and `Index2 → Name` UId joins to prior Base mapping. **Completed:** full-response structural ledger records `columns` length 0, `inheritedColumns` length 34, and `indexes` length 2, while scalar response values remain omitted. Safe index ledger contains exactly two one-member index objects: `Index1` has `isUnique=true`, `isAutoName=false`, member `columnUId=60cc5643-4ee2-4adf-b76b-06000ad0b067`; `Index2` has `isUnique=false`, `isAutoName=false`, member `columnUId=7c81a01e-f59b-47df-830c-8e830f1bf889`; both have `orderDirection=0`. Each relation exactly equals, respectively, the `Code` and `Name` `ColumnUId` in the prior hashed Base Account mapping (prior file hashes: Base `24a13b0f54da6d20444c814d6cd54054322505f9c415e6e0560786885a90d19f`; extension `312b19aaba95dd3766b17ba5049d4c957a61483ac23814bfc2e2d35aaf2ce625`). This is a bounded cross-run UId reconciliation; no target fingerprint binds the two timestamps.
4. Own-only oracle explanation and remaining semantics/case limits. **Completed:** the source’s `FindIndexProbeOwnColumn` returns no UId/indexed value whenever own-match count is not exactly one; candidate matching subsequently receives only this own-only set. Here `Code` and `Name` have own count 0 and inherited count 1, so the runtime `NOT_CONFIRMED` is a genuine failure of the stated Own/`indexed=true` oracle and a predictable false negative for an index definition that targets inherited columns. It does not refute the two explicit `schema.indexes[]` relations. Separately, current Test1 inherited `column.indexed` semantics are not evidenced: the full response ledger omits scalar values, and the persisted own-column mapping deliberately records null values. Composite indexes, auto-named indexes, order-direction enum semantics, full-catalog coverage, target fingerprint, and any mutation semantics were not examined.
5. Criterion-to-evidence decision, severity, not-run statement, owner-facing P3-C conclusion. **Completed below.**

## Commands performed (offline)

- `Get-ChildItem <new-evidence-root> -File | Sort-Object Name`, strict UTF-8/no-BOM validation, and exact-byte `Get-FileHash -Algorithm SHA256` against `artefact-manifest.sha256.json`.
- In-memory JSON parsing and structural queries against all nine new artefacts, including the 1,084,619-byte response-shape ledger; no response values were materialized in this report.
- Read-only SHA/version checks of the two local assemblies pinned by `index-probe-request-contract.json`.
- Direct UId reconciliation between `schema-Account-Test1.indexes.shape.json` and the prior Base/extension Account mappings.
- Static source inspection of `RunIndexProbeAsync`, `AnalyzeIndexProbeSchema`, `FindIndexProbeOwnColumn`, the structural-ledger redaction routine, and HTTP endpoint literals.
- Independent case-insensitive marker scan and recursive JSON value-only scan over all new evidence files.

## Exact manifest checks

| Manifest entry | Bytes | SHA-256 verified |
|---|---:|---|
| `index-probe-request-contract.json` | 2,597 | `9d5f598f91187d4ab7d5c20218b64b894ee6ba8fc61f48b367227beed35eec8e` |
| `redaction-check.json` | 389 | `3b7eef5af22de254a26e3d14d04143cead9e47bb685b4df9adfd6a8e0ea5db93` |
| `run-result.json` | 984 | `a3d706b0d04069431d5da3c3f5e99882bf14465ed3823ec231cf7856e73885a6` |
| `schema-Account-Test1.index-candidates.mapping.json` | 2,058 | `8f8c91355705d6de5babfb82d9502178087cf79e62fdc7909ec667e6f9018d5e` |
| `schema-Account-Test1.indexes.shape.json` | 5,818 | `621c172d8705bb9e9e4a6b4be05f66ca76804893548e92c142d5c3d7bdfab99f` |
| `schema-Account-Test1.own-columns.mapping.json` | 895 | `5bea564a6b90a071cc07f49109fa4e73c8e5110c1950c10e6088bae402f6f990` |
| `schema-Account-Test1.response-shape.json` | 1,084,619 | `4aafd3f76f250cb7d5524953ce90c4c89ccdc3519f4094bf6c99fa5706675d39` |
| `workspace-selection-Account-Test1.mapping.json` | 315 | `308b1d439f7aa6de70925ca05bc10acd109f1e277feaa336a6e65c5521bb900d` |

The self-excluded manifest hash is `dcb01e65e09cbf1e6f06be2e21b61f5147c1c8808d50865712ec11dd558870bb`. All nine files are present; all eight entries match byte length/hash; all are UTF-8 without BOM.

## Criterion-to-evidence matrix

| Criterion | Status | Evidence path | Gap / severity | Impact |
|---|---|---|---|---|
| Exact immutable 9-file set and byte integrity | Confirmed at review time | New evidence root; `artefact-manifest.sha256.json` | Hashes do not prove provenance before review or prevent later modification. Low. | Reproducible offline baseline. |
| No sensitive/raw response value persisted | Confirmed for saved files | `redaction-check.json`; structural response ledger; independent scans | Full source response was not retained, by design. Low. | No observed secret/session/caption/description/raw-data exposure. |
| Bounded `--index-probe` call intent | Confirmed statically and as recorded | `index-probe-request-contract.json`; `Program.cs` | Not an HTTP audit trail. Low. | One workspace read and one schema read are the only probe reads; SelectQuery is not in this branch. |
| Exact Account/Test1 selection and returned schema identity | Confirmed by source-to-artifact trace | `workspace-selection-Account-Test1.mapping.json`; own-column mapping; `RunIndexProbeAsync` | Returned raw schema UId is redacted; source compares it before writing. Low. | Package/UId selection is not name-only. |
| Non-empty `schema.indexes[]` structural form | Confirmed, bounded | `schema-Account-Test1.response-shape.json`; `schema-Account-Test1.indexes.shape.json` | One replacement-layer schema only. Medium. | Two index objects, exact member arrays, index/member UIds, names, `isUnique`, `isAutoName`, and `columnUId` paths are evidenced. |
| `Index1 → Code` and `Index2 → Name` relation | Confirmed as a bounded cross-run UId join | New index shape; prior `20260904T132710Z/schema-Account-Base.mapping.json` | No target fingerprint binds the earlier Base mapping to the later Test1 read. Medium. | Index1 is unique and targets Base `Code` UId; Index2 is non-unique and targets Base `Name` UId. |
| Original own-column oracle (`Code`/`Name` own + `indexed=true`) | Contradicted | `run-result.json`; own-column mapping; source analysis | Test1 has 0 own and 34 inherited columns; both oracle UIds/indexed values are null. Blocking for the original criterion. | Runtime correctly returns `NOT_CONFIRMED`; this claim cannot be promoted. |
| Explanation of own-only false negative | Confirmed | `Program.cs` `FindIndexProbeOwnColumn` / `CreateIndexCandidate`; own-columns and candidate mappings | It proves the prototype’s oracle limitation, not a new general API contract. Medium. | The candidate matcher cannot resolve inherited targets because it deliberately supplies only own columns. |
| Current inherited `column.indexed` semantics | Not confirmed | Full response ledger; own-column mapping | Scalars are omitted; current inherited `Code.indexed`/`Name.indexed` cannot be reconstructed. High for `Columns.ActualIndexed`; not needed for the bounded index-array relation. | Do not derive `Columns.ActualIndexed` from this test. |
| Composite/auto-name/index-order semantics/full catalog | Not run | `indexes.shape.json`; `run-result.json` | Only two simple one-member, non-auto-named examples; `orderDirection=0` is merely observed. Medium. | Exclude these cases from any proposed bounded rule. |
| Proposal-contract readiness | Partial only | All above; `WORKBOOK_CONTRACT_VISION.md` §§3.3–3.4, 8 | Current workbook contract retains `ActualIndexed` and calls index API schema unverified. Blocking for acceptance; evidence is sufficient only to inform an owner-reviewed narrowed proposal. | A proposal may model the read-only `Indexes` relation via `schema.indexes[].columns[].columnUId` and allow an Own or Inherited target, but it cannot claim original-own-oracle or inherited-flag semantics. |

## Findings

The evidence establishes a useful bounded read-only index-array fact: in the exact `Account/Test1` selection, two explicit simple indexes have one member each, their unique flags are observable, and their `columnUId` values reconcile exactly with the earlier Base Account `Code` and `Name` identities. It also establishes why the runtime verdict is `NOT_CONFIRMED`: the implemented candidate oracle requires target columns to be own and `indexed=true`, while this replacement layer inherits them.

That runtime failure must not be relabelled as a successful original P3-C contract. The source contract itself calls index fields candidates until response paths and owner oracle agree; they do not agree. The safe, source-backed conclusion is narrower: `schema.indexes[]` is suitable evidence for the two observed simple index relations, while `column.indexed` on inherited columns remains unresolved. The broader contract retains separate `Columns.ActualIndexed` semantics and does not receive evidence for composite, auto-name, index-order, scale, mutation, or target-state stability cases.

## Not run / unchanged

- No BPMSoft/login request was made by this reviewer.
- No SelectQuery, full-catalog read, target fingerprint, write/Manage, compile/save/create/update/delete, Excel, Google input, or retry occurred in the reviewed run.
- No source, prompt, contract, SDD, handoff, workbook, or immutable evidence file was modified by this reviewer.

## Precise owner question

Do you approve a narrowly scoped contract-proposal change that treats `schema.indexes[].columns[].columnUId` as the read-only `Indexes` relation and permits its target column to be `Own` or `Inherited`, while explicitly leaving `Columns.ActualIndexed`, composite/auto-name/order semantics, and all write behavior unresolved?

## Verdict

**G2 stop — DO NOT START.** Owner decision is required. The evidence may be used as input to the owner-reviewed proposal above without another BPMSoft run, but it does not itself authorize contract acceptance, implementation, full-catalog pull, Excel, or write/Manage scope.
