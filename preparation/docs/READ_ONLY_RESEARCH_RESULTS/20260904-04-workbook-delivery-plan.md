# Phase 1 plan-before-code — controlled workbook delivery

**Role:** `04-workbook-delivery`  
**Date:** 2026-09-04  
**Status:** PLAN ONLY — implementation is not started  
**Gate:** G4 `APPROVED WITH LIMITS`; source: `20260904-G4-owner-decision.md`  
**Usage gate before role launch:** `PASS`, unique primary 300-minute window, remaining 88%, threshold 20%, reset 2026-09-05 02:51:45 MSK, source `codex_app_server.account/rateLimits/read`.

This phase creates no `.xlsx`, makes no BPMSoft/Google/network request, and changes no code, evidence, contract, SDD, handoff or `SyncOM`. This plan file is the only Phase 1 modification.

## 1. Delivery decision and boundary

The first pair will be a **fresh, bounded, read-only baseline**, not a false full-catalog export. A fresh bounded capture is required even though prior evidence is sufficient for G2, because one current response must bind the `Account/Test1` inherited `Code`/`Name` flags and its two `schema.indexes[]` objects to the same pull. Reusing the two older timestamps alone would leave the `ActualIndexed` check temporally partial and would not provide a truthful single `PullRunId`.

The bounded capture will read only:

1. five exact `EntitySchema` package layers: `ActivityPriority/Base`, `Lookup/Base`, `Account/Base`, `Account/Completeness`, `Account/Test1`;
2. the single `Lookup` registry relation for `ActivityPriority`;
3. all three `ActivityPriority` record IDs and only the proven text columns `Name` and `Description` for the first lookup-data example;
4. index definitions from each selected schema, with the mandatory `Account/Test1` same-response check.

The initial research pair therefore uses the manifest value `ScopeMode=VerifiedBoundedBaseline`. `AllReadableCatalog` remains the future full-catalog mode and is explicitly not claimed here. The full catalog, scale limits, composite/auto-named indexes and other lookup signatures remain later work.

No BPMSoft Write/Manage/compile/save/create/update/delete endpoint or permission check is allowed. Google rows, values and formulas are not inputs. Credentials remain interactive-only and in memory.

## 2. Exact files allowed after owner approval

### Git-ready deliverables

- `workbooks/BPMSoft.ModelCatalog.xlsx` — new.
- `workbooks/BPMSoft.LookupCatalog.xlsx` — new.

### Minimal reproducible tool

- `WorkbookDeliveryTool/BpmSoftWorkbookDelivery.csproj` — new .NET 10 console project, no external NuGet packages.
- `WorkbookDeliveryTool/Program.cs` — new single-file implementation with bounded `capture`, offline `generate`, `verify` and `self-test` commands.

The tool is necessary because pair IDs, protected cells, validations, canonical hashes and cross-workbook read-back cannot be reproduced safely by hand. It is deliberately not a production synchronizer and has no apply/write mode. The future full-catalog adapter can evolve from the same normalized baseline contract after its own gates.

For workbook creation the implementation will copy the global `minimax-xlsx/templates/minimal_xlsx` XML skeleton into a temporary directory, modify OOXML directly, and pack it with `minimax-xlsx/scripts/xlsx_pack.py`. It will not use `openpyxl` or create a workbook through a high-level writer. The skill path/version and hashes of the seven template files plus used scripts are recorded in the delivery report.

### Local run evidence, excluded from the Git-ready pair

One append-only run folder will be created under:

```text
WorkbookDeliveryTool/runs/YYYY/MM/DD/<PullRunId>/
  evidence/<UTC>-bounded-baseline.json
  evidence/<UTC>-source-manifest.sha256.json
  evidence/<UTC>-actualindexed-indexes-check.json
  audit/<UTC>-workbook-verification.json
  <UTC>-run-journal.json
```

Runtime timestamp and `PullRunId` are determined once after a successful capture; no previous run folder is overwritten. These local evidence/audit files are not part of the workbook Git commit policy.

### Durable delivery report

- `docs/READ_ONLY_RESEARCH_RESULTS/20260904-04-workbook-delivery-result.md` — new after implementation; source hashes/counts, commands, validation outcomes, `ActualIndexed` result and not-run checks.

No other project file may change in Phase 2 without a new owner decision. In particular, `SyncOM/`, `PrototypeReadOnlyPull/`, contracts, drafts, handoff and existing `probe-output` evidence remain unchanged.

## 3. Existing evidence used as immutable expectations

The fresh capture is compared with, but never overwrites, these independently reviewed sources.

| Existing source | Bytes | SHA-256 |
|---|---:|---|
| `20260904T132710Z/summary.json` | 1,623 | `3d2a76399bfd31d8bdd41d7c770c740207a1a413e8f9bf1eb8bc4205344ef2d4` |
| `20260904T132710Z/schema-ActivityPriority.mapping.json` | 3,354 | `6885b1609e66135dccfaaf19cd1de2feb6db2a9501660c7afba3ed5d7b0bdaef` |
| `20260904T132710Z/schema-Lookup.mapping.json` | 4,232 | `25329cd0368b0aff578aa74ae5787d60022ca91a26f1ef79770513ffc6142357` |
| `20260904T132710Z/schema-Account-Base.mapping.json` | 10,853 | `24a13b0f54da6d20444c814d6cd54054322505f9c415e6e0560786885a90d19f` |
| `20260904T132710Z/schema-Account-extension.mapping.json` | 11,307 | `312b19aaba95dd3766b17ba5049d4c957a61483ac23814bfc2e2d35aaf2ce625` |
| `20260904T132710Z/lookup-registry-ActivityPriority.mapping.json` | 759 | `db0afd574cdb6ce6f3083fb273e235152933b2df016fe9e1c1d49675021abf7b` |
| `20260904T132710Z/ActivityPriority.asc-pass-1.ids.txt` | 110 | `6f7f454fffab74103fb90f408dcf425440773c3a7b506717f4fc601ccf799c7f` |
| `20260904T132710Z/request-contract.json` | 3,312 | `f572d1cdaa68f5592ae31399def05aec3321b7db48390114286298fe453bd1be` |
| `20260904T132710Z/run-result.json` | 1,613 | `caa401c3b5185374fbf64bafabefbb78202e5aa99012485bb0086029901d05a0` |
| `20260904T151951Z/workspace-selection-Account-Test1.mapping.json` | 315 | `308b1d439f7aa6de70925ca05bc10acd109f1e277feaa336a6e65c5521bb900d` |
| `20260904T151951Z/schema-Account-Test1.own-columns.mapping.json` | 895 | `5bea564a6b90a071cc07f49109fa4e73c8e5110c1950c10e6088bae402f6f990` |
| `20260904T151951Z/schema-Account-Test1.indexes.shape.json` | 5,818 | `621c172d8705bb9e9e4a6b4be05f66ca76804893548e92c142d5c3d7bdfab99f` |
| `20260904T151951Z/schema-Account-Test1.response-shape.json` | 1,084,619 | `4aafd3f76f250cb7d5524953ce90c4c89ccdc3519f4094bf6c99fa5706675d39` |
| `20260904T151951Z/artefact-manifest.sha256.json` | 1,589 | `dcb01e65e09cbf1e6f06be2e21b61f5147c1c8808d50865712ec11dd558870bb` |

Reference counts are five selected schema layers, 90 fully mapped columns in the first four layers, 34 inherited columns in `Account/Test1`, two one-member indexes there, one `ActivityPriority` registry row and three `ActivityPriority` records. The fresh source manifest records actual counts and exact hashes. A delta is reported; identity ambiguity, missing required relations or unsupported index shape is a stop, while a harmless count change is not silently discarded.

## 4. Bounded capture and normalized source

The `capture` command performs, in this order:

1. verify the pinned local BPMSoft 1.8.0.14107 assembly versions/hashes and the assembly-backed order payload before login;
2. prompt URL/login/password interactively; password is hidden, never enters args/files/logs and is cleared after login; session material stays in memory;
3. call Login, one `GetWorkspaceItems`, then select the five exact `(Name, PackageName, type=3, UId)` identities; no name-only fallback;
4. call `GetSchema` exactly once for each selected UId and normalize only approved fields: schema/package/parent identifiers, own and inherited columns, raw type/requirement/indexed flags, references and generic index/member fields;
5. read `Lookup` with `allColumns=false`, explicit `Id` ascending order and unsorted `SysEntitySchemaUId`; traverse to a short terminal page and require exactly one `ActivityPriority` registry relation;
6. read `ActivityPriority` twice ascending and once descending using explicit `Id` order, with visible columns `Id`, `Name`, `Description`; require identical ascending passes, exact reverse control, unique IDs, page-ledger reconciliation and a persisted empty terminal-page assertion;
7. create a normalized UTF-8/LF JSON baseline and SHA-256 manifest only after all checks pass; no login response, password, cookie, CSRF or HTTP headers are persisted.

The normalized source carries its own field-level provenance. `GetSchema.schema.id` is retained only as `GetSchemaSchemaIdCandidate`; it is never used as `SysSchemaId`, identity, relation, fingerprint key or plan key. In the workbook `Schemas.SysSchemaId` remains blank until its exact semantics are independently proven. This preserves the diagnostic value without promoting it.

Data-type and `requirementType` values are converted only through a local assembly-backed enum mapping recorded in the source manifest. If a required enum cannot be resolved exactly, no friendly name is invented: generation stops with `DATATYPE_MAPPING_UNRESOLVED` or `REQUIREMENT_MAPPING_UNRESOLVED`. `LookupValues` is bounded to the two proven text fields `Name` and `Description`; unsupported value kinds are not fabricated.

## 5. Canonical data model and deterministic order

All strings are normalized to NFC; GUIDs are lowercase canonical strings; booleans are true/false; timestamps are UTC ISO-8601; null is explicit. Hash input uses UTF-8 without BOM and LF line endings. Property order is fixed by the tool, not by JSON/dictionary iteration.

Canonical row order, independent of Excel sorting/filter state:

- `WorkspaceInventory`: `WorkspaceItemUId` ordinal ascending;
- `Schemas`: `SchemaUId` ordinal ascending;
- `Columns`: `ParentSchemaUId`, ownership rank `Own < Inherited < System`, `ColumnUId` ordinal;
- `Indexes`: `SchemaUId`, `IndexUId`, numeric zero-based `Ordinal`;
- `LookupRegistry`: `SysEntitySchemaUId`, `LookupRecordId`;
- `LookupRows`: `SysEntitySchemaUId`, `RecordId`;
- `LookupValues`: `SchemaName`, `RecordId`, `ColumnName` ordinal.

`ActualFingerprint`/`SourceFingerprint` is SHA-256 of the canonical server-origin row fields and explicit nulls; editable `Desired*`, comments, display order and formatting are excluded. `PairBaselineHash` is SHA-256 of the labelled canonical row streams of both workbooks, excluding manifests, self-referential fields, `PullConflicts` and snapshots. `BaselineTargetFingerprint` is bounded to target alias, verified BPMSoft assembly identity, selected schema UIds, registry/record IDs and the normalized source manifest hash; it is not presented as a full-target fingerprint.

`PairId` is generated once locally and shared by both books. `PullRunId` is generated once for the successful bounded capture and shared by both. Neither is a BPMSoft ID. A second offline `generate` from the same source plus the same pair/run IDs must produce identical canonical worksheet XML hashes and the same `PairBaselineHash`; ZIP container timestamps may differ and are not used as the content identity.

## 6. Exact workbook layouts

All data ranges receive a frozen header and autofilter. Text and GUID/hash fields are Excel text; `Ordinal` and counts are integers; flags are native booleans or blank when unknown; timestamps remain ISO text to avoid locale drift. Derived/service values are literal protected baseline data produced by the tool, not user inputs. There are no calculated data cells.

### `BPMSoft.ModelCatalog.xlsx`

Initial sheet order:

1. `Readme` — protected; columns `Topic | Value`.
2. `Manifest` — protected; columns `Key | Value`; exact keys `ContractVersion`, `PairId`, `PullRunId`, `TargetAlias`, `ScopeMode`, `PullStartedUtc`, `PullCompletedUtc`, `PairBaselineHash`, `BaselineTargetFingerprint`, `TemplateVersion`.
3. `WorkspaceInventory` — protected/filterable; `WorkspaceItemUId | Name | ItemType | PackageName | PackageUId | SupportStatus | SupportReason`.
4. `Schemas` — mixed; `SchemaName | SchemaUId | SysSchemaId | SchemaKind | ParentSchemaName | ParentSchemaUId | DesiredPackageName | ActualPackageName | ActualPackageUId | DesiredState | ServerPresence | ActualFingerprint`.
5. `Columns` — mixed; `SchemaName | ParentSchemaUId | ColumnName | ColumnUId | Ownership | DataType | ReferenceSchemaName | ReferenceSchemaUId | DesiredRequired | ActualRequired | DesiredIndexed | ActualIndexed | DesiredState | ServerPresence | ActualFingerprint`.
6. `Indexes` — fully protected; `SchemaName | SchemaUId | IndexUId | IndexName | IsUnique | ColumnName | ColumnUId | Ordinal | ActualFingerprint`.
7. `ValidationLists` — `veryHidden`, protected; `DesiredState | ServerPresence | Ownership | SchemaKind | ValueState | ValueKind | BooleanChoice | SupportStatus | ScopeMode | DataType`.
8. `PullConflicts` — protected/filterable, initially empty; `ConflictCode | Workbook | Sheet | StableKey | ColumnName | BeforeValue | ServerValue | ResolutionStatus | Message`.

No `S_*` sheet is created during initial pair creation because no pre-existing working workbook is about to be overwritten. A later pull must create the single pre-pull snapshot set before replacing working sheets.

### `BPMSoft.LookupCatalog.xlsx`

Initial sheet order:

1. `Readme` — protected; `Topic | Value`.
2. `Manifest` — same protected keys and exactly the same pair/run/baseline values.
3. `LookupRegistry` — mixed; `SchemaName | SysEntitySchemaUId | LookupRecordId | BaseSchemaName | BaseSchemaUId | DesiredState | ServerPresence | ActualFingerprint`.
4. `LookupRows` — mixed; `SchemaName | SysEntitySchemaUId | RecordId | DraftRowToken | DesiredState | ServerPresence | SourceFingerprint | Comment`.
5. `LookupValues` — mixed; `SchemaName | RecordId | DraftRowToken | ColumnName | ValueState | Value | ValueKind | ReferenceRecordId | ReferenceDraftRowToken | CanonicalValue | SourceFingerprint`.
6. `ValidationLists` — same exact columns as the Model workbook; `veryHidden`, protected.
7. `PullConflicts` — same exact columns, protected/filterable and initially empty.

### Editable/protected cells

- All IDs, actual values, fingerprints, manifest/service fields, inventory, `Indexes`, validation lists and conflicts are locked.
- Existing `Schemas`: only `DesiredState` is unlocked; `DesiredPackageName` stays locked because all bounded rows are existing.
- Own existing `Columns`: `DesiredRequired`, `DesiredIndexed`, `DesiredState` are unlocked and seeded to the current no-diff state; inherited/system rows keep them blank and locked.
- `LookupRegistry`: `DesiredState` unlocked.
- `LookupRows`: `DesiredState` and `Comment` unlocked; no draft rows are generated in this delivery.
- `LookupValues`: `ValueState`, `Value`, `ReferenceRecordId`, `ReferenceDraftRowToken` unlocked; identity, kind, canonical value and fingerprint locked.
- Sheet protection is an accidental-edit guard, not cryptographic security. No secret protection password is stored; the parser remains authoritative.

Validation uses internal hidden lists for enums/booleans/data types and custom same-workbook rules for required names, UUID syntax and the ID/token XORs. Validation formulas contain no external references. Unsupported or unspecified columns are parser blockers.

## 7. Formula, external-link and secret policy

- No cell formula is generated in either workbook, especially not in editable tables.
- Cryptographic hashes are computed by the tool and written as protected service data; Excel is not asked to calculate SHA-256.
- No `externalLinks`, `connections`, macros/VBA, query tables, `IMPORTRANGE`, external named ranges or cross-workbook formulas are present.
- Plain `TargetAlias` is nonsecret; login response and session material never enter OOXML.
- Static scans cover package parts, XML text, relationship targets and shared strings for external links and secret markers.

## 8. Mandatory `ActualIndexed` versus `Indexes` test

This check is a first-class output, not a comment.

1. From the **same fresh `Account/Test1` GetSchema response**, persist inherited `Code` and `Name` `ColumnUId`/`indexed` values and all index objects.
2. Generate `Indexes` exclusively by iterating every `schema.indexes[].columns[]` member. No condition may read `Columns.ActualIndexed`.
3. Require exported member-row count to equal the source sum of member counts; compare canonical member tuples `(SchemaUId, IndexUId, IndexName, IsUnique, ColumnUId, Ordinal)` exactly.
4. Require every member `ColumnUId` to resolve exactly once to a `Columns.ColumnUId` in the same effective package layer, allowing `Own` or `Inherited`. Member `.uId`, member name and `ActualIndexed` are rejected as substitute keys.
5. Produce a separate comparison table `ColumnUId | ActualIndexed | IndexMembershipCount | Interpretation`. Agreement is informative only; disagreement must not add, remove or rewrite an `Indexes` row.
6. Run two negative fixtures: `(ActualIndexed=false, membership present)` must retain the index; `(ActualIndexed=true, membership absent)` must not fabricate one. Any derived add/drop intent is a failing defect.

Result rules:

- **PASS (export):** exact source-to-workbook index-member equality, exact joins and both negative fixtures pass.
- **PARTIAL (overall):** export passes but `column.indexed` semantics still do not explain membership or are inconsistent; read-only display may proceed, while index loading remains blocked.
- **FAIL:** any source member is lost/altered, any row is fabricated from `ActualIndexed`, or a relation is ambiguous; do not deliver workbooks.

Regardless of export PASS, future index load/apply begins in state `INDEX_SYNC_UNRESOLVED`. No `DesiredIndexed` difference is converted to an index operation, and no `Indexes` difference is converted to a column-flag operation. A later dedicated write research/gate must prove add/drop semantics and zero false operations; otherwise index loading is excluded from that version while `Indexes` stays read-only.

## 9. Build, read-back and verification commands

After plan approval the exact command arguments are recorded in the result report. Verification sequence:

1. `dotnet build WorkbookDeliveryTool/BpmSoftWorkbookDelivery.csproj -c Release` — zero errors/warnings required.
2. `dotnet run ... -- capture` — interactive credentials, bounded reads only; produces normalized source/manifest or no workbook.
3. `dotnet run ... -- generate --source <bounded-baseline.json> --model workbooks/BPMSoft.ModelCatalog.xlsx --lookup workbooks/BPMSoft.LookupCatalog.xlsx`.
4. `dotnet run ... -- verify ...` — parser read-back of both ZIP/XML packages and exact comparison with source.
5. `dotnet run ... -- self-test` — temporary fixtures for ordering, pair mismatch, duplicate schema names/package layers, inherited index join, wrong member UId, actual-index negative cases, invalid GUID/XOR, unknown columns and external-link/formula blockers.
6. `python <minimax-xlsx>/scripts/xlsx_reader.py <file> --json` and `--quality` for both files; compare sheet names, headers, row counts, cell types and a deterministic sample.
7. `python <minimax-xlsx>/scripts/formula_check.py <file> --json` for both files. Expected formula count is zero and errors are zero.
8. Unpack both files and validate: well-formed XML, relationship/content-type closure, shared-string counts, style indices, protection/unlocked-cell map, validations, hidden state, autofilters, absence of external links/macros/connections and exact pair metadata.
9. Check LibreOffice availability with `libreoffice_recalc.py --check`. If present, recalculate a temporary copy and rerun static validation; if absent, record `Tier 2: SKIPPED — LibreOffice not available`. The recalculated copy is never delivered over the original.
10. Generate a read-only preview/render when LibreOffice is available; otherwise record automated render as skipped. The owner then opens both originals in desktop Excel and confirms: no repair prompt, sheet order/visibility, frozen headers/filters, locked versus editable cells, readable widths, both Account layers, two Test1 index rows and matching pair manifests. Human confirmation is evidence for G5, not acceptance by the agent.
11. Run offline generation a second time from the same source/IDs; require identical canonical worksheet XML hashes, row counts, relations and pair hashes.

The result report contains Tier 1/Tier 2 statuses, formula count, every skipped check with reason, workbook file hashes, canonical content hashes, source counts/hashes and the separate index-safety outcome.

## 10. Stop conditions

Stop before writing `.xlsx`, or remove partial outputs, if any of the following occurs:

- source hashes/assembly contract fail, selected schema identity is missing/ambiguous, or ordered pagination cannot be reproduced;
- fresh `Account/Test1` no longer contains the two expected simple indexes, its member relation cannot be joined by `columnUId`, or a composite/auto-named/unknown index form appears;
- `ActualIndexed` influences index export, an index row is lost/fabricated, or the future-operation simulation produces false add/drop intent;
- schema/column/reference/registry/record identity is missing or inconsistent;
- local type/requirement mapping cannot be proven; a value kind would have to be guessed;
- the pair cannot share stable `PairId`, `PullRunId`, `PairBaselineHash` and truthful bounded scope metadata;
- workbook exceeds an Excel/package limit, OOXML/parser/formula/external-link/secret validation fails, or reproducibility fails;
- any BPMSoft Write/Manage endpoint, Google input, secret persistence, `SyncOM` change, contract expansion or new public architecture becomes necessary.

`INDEX_SYNC_UNRESOLVED` blocks index loading, not a correctly preserved read-only `Indexes` export. A failure of the export itself blocks workbook delivery.

## 11. Checks deliberately not run in Phase 1

- No code/build/test/generation.
- No BPMSoft/login/network/browser request.
- No `.xlsx`, normalized source or run folder created.
- No Google source used.
- No Write/Manage/compile/save/create/update/delete, full-catalog pull, index mutation or future-load research.
- No change to existing evidence, contracts, SDD drafts, handoff, prototype or `SyncOM`.

## 12. Exact approval question

**Одобряете ли вы этот Phase 2 plan, включая fresh bounded read-only capture, исследовательское значение `ScopeMode=VerifiedBoundedBaseline`, сохранение `schema.id` только вне workbook как `GetSchemaSchemaIdCandidate`, две Git-ready книги по указанным путям и обязательный статус `INDEX_SYNC_UNRESOLVED` для любой будущей загрузки индексов?**
