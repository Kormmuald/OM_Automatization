# S05 worker evidence — atomic Excel materialization

- Stage: `S05` worker implementation; this document is not acceptance or handoff.
- Preconditions: active feature `001`; accepted S04 evidence is `../S04/acceptance-report.md`, `../S04/review-03.md` (`Pass`) and `../S04/handoff.md`.
- Boundary: synthetic `QualifiedCatalogSnapshot/v1` only, local filesystem only. No BPMSoft/source reread, Pass A/B, credentials, browser, CLI composition, live target, Compare, Apply, Write, Manage, index mutation or Git mutation was used.
- Requirements covered: `FR-007`, `FR-008`, `FR-009`, `FR-010`; S05 evidence for `SC-005`, `SC-006`.

## Tests-first evidence

The RED command after adding the S05 executable contract tests was:

```text
dotnet build tests/BpmSoftSync.Adapters.Excel.Tests/BpmSoftSync.Adapters.Excel.Tests.csproj -c Release
```

Exit `1`. The project and required S05 contracts did not exist; compilation failed for `WorkbookProjection`, `WorkbookPublicationCheckpoint` and `IWorkbookPublicationFaultInjector`.

Focused GREEN after production implementation:

```text
dotnet build tests/BpmSoftSync.Adapters.Excel.Tests/BpmSoftSync.Adapters.Excel.Tests.csproj -c Release --no-restore
dotnet tests/BpmSoftSync.Adapters.Excel.Tests/bin/Release/net10.0/BpmSoftSync.Adapters.Excel.Tests.dll
```

Exits `0`, `0`; result `PASS Excel S05 tests`.

Review-01 correction RED was captured after adding the persisted-evidence, post-publication rollback, canonical-layout, deep-tamper, full-snapshot and timestamp assertions:

```text
dotnet build tests/BpmSoftSync.Adapters.Excel.Tests/BpmSoftSync.Adapters.Excel.Tests.csproj -c Release
```

Exit `1`: the five-argument qualification constructor and the `AfterPublication`, `AfterPairEvidence`, `AfterSeal` checkpoints did not exist. After the production correction, the same build and executable test command exit `0`.

Review-02 focused tests were added before the corresponding corrections for: post-acceptance nested Pass-B mutation, `InvalidOperationException` at every post-move checkpoint, a durable-marker/crash-window orphan with truncated S05 journal tail, header-inclusive scale limit, protected raw-value staging permissions, `cellXfs` protection and exact `autoFilter` tampering, closed `SourceIdentity`, and inherited-column identity/editability. The final Release build and Excel/Domain executables exit `0`.

Review-03 correction adds a Manifest-only boundary: all business worksheets fit the declared limit (`17` rows including headers), while each Manifest requires `44` rows. Qualification returns `WORKBOOK_SCALE_DECISION_REQUIRED`, and a forged stale `DiagnosticOnly` snapshot is independently rejected by the materializer before a staging directory or XLSX file exists.

## Implemented behavior

- `AtomicWorkbookPairPublisher` rejects before staging unless the existing RunId contains schema-valid, exact and journal-backed `pass-a`, `pass-b`, `reconciliation` and `qualified-snapshot` S04 records. Their payloads, counts, digests, sealed scope, target, Pass A/B, pull-window digest and snapshot identity must exactly match the supplied successful qualification. Immediately before staging it independently rederives ordered identities, counts, manifests, component/unsupported digests, target fingerprint and reconciliation digest from the actual current Pass-B workspace/lookup object graph. A post-acceptance nested lookup-value mutation and a `BeginRunAsync`-only constructed qualification are both explicitly rejected without staging.
- `WorkbookPairMaterializer` additionally requires both real independent Pass A/B objects, their exact snapshot bindings, B object references, component/identity/count equality, a successful zero-retry `QualifiedCatalogSnapshot/v1`, sealed scope, non-empty pair identity, `DiagnosticOnly` scale and a valid captured pull interval.
- Projection uses only the accepted snapshot workspace and lookup graphs. It makes no source call and contains no A/B or identity-reconciliation implementation.
- Model sheet order is exactly `Readme`, `Manifest`, `WorkspaceInventory`, `Schemas`, `Columns`, `Indexes`, `ValidationLists`, `PullConflicts`.
- Lookup sheet order is exactly `Readme`, `Manifest`, `LookupRegistry`, `LookupValues`, `ValidationLists`, `PullConflicts`.
- Business headers match workbook contract v1. `ValidationLists` is hidden/protected; fully derived sheets are protected; mixed sheets are not sheet-protected. Cell styles distinguish locked derived cells from the exact editable columns. Local defined-name list validations cover the declared state/type/boolean/data-type fields through row 500.
- Every workspace item, schema, own/inherited column, index member, lookup registry record and normalized lookup value becomes exactly one corresponding row. All projection orders are explicit and stable. Inherited columns preserve actual required/indexed values while all desired/editable cells remain blank and locked.
- `LookupValues` preserves `Null|EmptyString|Value`, typed kind, canonical value and reference identity. Actual lookup content is written only into the local Lookup workbook.
- `ActualIndexed` comes directly from the column model. `Indexes` comes only from `schema.Indexes[].Members[].ColumnUId` with the source ordinal. Neither is inferred from the other; the synthetic negative matrix covers membership-present/flag-false and membership-absent/flag-true. Schema fingerprints are exact qualified component digests resolved by the full schema/package-layer stable identity; missing/ambiguous components fail. Column fingerprints contain the complete schema/package identity and column semantics; each index member row carries the digest of the complete index, including its ordered composite membership. No fallback digest exists.
- Both manifests bind the same `RunId`, `PairId`, baseline hash, target fingerprint, Pass A/B digests, scope digest, closed source identity, component digest and counts digest. `SourceIdentity` is normalized then emitted only as `sha256:<64 lowercase hex>`; raw source identity text never enters either workbook or safe evidence.
- The writer produces an owned, versioned, sanitized OOXML package directly; there is no dependency on `preparation/**` and no prototype/template file was changed.
- Package validation checks exact root/workbook relationship IDs, types and targets; exact content-type defaults and per-part MIME overrides; the complete defined-name set and ranges; exact data-validation names, attributes and `sqref`; exact sheet protection; exact four `cellXfs` including `applyProtection`, locked/unlocked semantics and alignment; and the exact per-sheet `autoFilter` range. Tampering tests cover formulas, external relations, VBA, defined names, validation `sqref`, validation formula, sheet protection, editable-style protection, auto-filter extent, workbook relationship type and content type.
- ZIP entry timestamps and part order are fixed; the same snapshot produced identical Model/Lookup file SHA-256 and pair digest across distinct staging roots.
- S04 now captures the real qualification start/completion instants through an injectable clock, carries them in the qualified snapshot, and binds them into persisted reconciliation/snapshot evidence with `pullWindow`. S05 writes those UTC instants into both manifests; it does not synthesize or substitute a placeholder time.
- Workbook scale forecasting now models all 14 actual projected worksheets in the Model/Lookup pair, including fixed Readme/ValidationLists/PullConflicts rows, every header, and Manifest's 17 fixed rows plus every count and component-digest row. The materializer constructs the in-memory projection, requires its exact per-sheet row map to equal the domain forecast, then enforces the declared limit before creating staging or invoking either XLSX writer.
- `AtomicWorkbookPairPublisher` stages both files in a unique sibling directory with inherited ACLs removed on Windows (current identity only) or `0700` directory/`0600` files on Unix, then atomically replaces the known-empty `output/` directory with that complete directory, yielding canonical `output/BPMSoft.ModelCatalog.xlsx` and `output/BPMSoft.LookupCatalog.xlsx` without a pair subdirectory.
- The commit holds the cross-instance run lock, revalidates accepted S04 evidence and writes a safe durable pending marker before visibility. Every `Exception`, including `InvalidOperationException` and `OperationCanceledException`, at/after move rolls back output, pair evidence, seal evidence, seal marker and journal. A later invocation deterministically removes bounded `.pair-staging-*` raw-value orphans and uses the four persisted S04 evidence files to reconstruct the exact journal even if an S05 append was truncated; orphan `output/*.xlsx` is removed before restaging. Successful publication removes the pending marker and leaves the canonical pair, one safe `workbook-pair` record and the single `review-only-seal`. The publisher never calls `BeginRunAsync`.

## Task coverage

| Task | Evidence | Result |
| --- | --- | --- |
| `S05-001` | `WorkbookContractTests.cs`: exact sheets/headers/order plus identity-level 1:1 assertions over multi-package same-name schemas, own/inherited columns, two lookups, composite indexes, Null/EmptyString/reference and all nine typed kinds | implemented; awaits independent review |
| `S05-002` | `WorkbookValidationTests.cs`: deterministic hashes, pair binding, exact 14-sheet scale map and Manifest-only pre-writer overflow boundary, deep OOXML closure, ACL/mode restriction, exact `cellXfs`/protection/autoFilter/validations and eleven tamper classes | implemented; awaits independent review |
| `S05-003` | `src/BpmSoftSync.Adapters.Excel/`: isolated projection/writer/reader/inspector/materializer | implemented; awaits independent review |
| `S05-004` | `AtomicWorkbookPairPublisher.cs` + run-store commit: current-graph rederivation over persisted S04 binding, canonical atomic directory publication, all-exception rollback and crash-window/startup recovery | implemented; awaits independent review |
| `S05-005` | `OutputEvidenceBoundaryTests.cs`: workbook-only canary and zero canary in non-XLSX durable files | implemented; awaits independent review |

## Generated synthetic pair

Current review pair: `generated-fake-data-pair-current/`.

- RunId: `10000000-0000-0000-0000-000000000001`.
- PairId: `10000000-0000-0000-0000-000000000002`.
- Model SHA-256: `1053c58cf0bdba039fd756ea494243ba2b6ad5e3e29d4e357eeb222ca7703032`.
- Lookup SHA-256: `e114073e915f55b985f9ee84b7ff4ba6b11b0324436ed717f45c7d84979b9105`.
- Pair digest: `e9a512454fb55c5aafebf21e5d5028407bafde5f510b739fc17b143ed308f416`.
- Formula validation: see `formula-validation-report.md`; Tier 1 PASS for both workbooks, Tier 2 explicitly SKIPPED because LibreOffice is unavailable.

Earlier pre-final generated pairs were moved intact out of the workspace under `C:/Users/Evgenii_2/AppData/Local/Temp/BpmSoftSync-S05-obsolete-*`; they are recoverable there and are not current evidence.

## Fresh cumulative validation

```text
dotnet build BpmSoftSync.sln -c Release --no-restore
dotnet tests/BpmSoftSync.Domain.Tests/bin/Release/net10.0/BpmSoftSync.Domain.Tests.dll
dotnet tests/BpmSoftSync.Application.Tests/bin/Release/net10.0/BpmSoftSync.Application.Tests.dll
dotnet tests/BpmSoftSync.Adapters.BpmSoft.Tests/bin/Release/net10.0/BpmSoftSync.Adapters.BpmSoft.Tests.dll
dotnet tests/BpmSoftSync.Adapters.FileSystem.Tests/bin/Release/net10.0/BpmSoftSync.Adapters.FileSystem.Tests.dll
dotnet tests/BpmSoftSync.Adapters.Excel.Tests/bin/Release/net10.0/BpmSoftSync.Adapters.Excel.Tests.dll
dotnet tests/BpmSoftSync.Cli.Tests/bin/Release/net10.0/BpmSoftSync.Cli.Tests.dll
```

The focused and cumulative runs completed with exit `0`; Release build reported 0 warnings and 0 errors. Existing Domain/Application/BPMSoft/FileSystem/CLI regressions remained green, and the new Excel executable reported `PASS Excel S05 tests`.

Additional independent workbook checks:

```text
python -X utf8 C:/Users/Evgenii_2/.codex/skills/minimax-xlsx/scripts/formula_check.py BPMSoft.ModelCatalog.xlsx --json   # actual exit 0; 8 sheets; 0 formulas; 0 errors
python -X utf8 C:/Users/Evgenii_2/.codex/skills/minimax-xlsx/scripts/formula_check.py BPMSoft.LookupCatalog.xlsx --json  # actual exit 0; 6 sheets; 0 formulas; 0 errors
python -X utf8 C:/Users/Evgenii_2/.codex/skills/minimax-xlsx/scripts/xlsx_reader.py BPMSoft.ModelCatalog.xlsx            # actual exit 0; exact 8-sheet inventory
python -X utf8 C:/Users/Evgenii_2/.codex/skills/minimax-xlsx/scripts/xlsx_reader.py BPMSoft.LookupCatalog.xlsx           # actual exit 0; exact 6-sheet inventory
python -X utf8 C:/Users/Evgenii_2/.codex/skills/minimax-xlsx/scripts/libreoffice_recalc.py --check                        # actual exit 2; `LibreOffice NOT available`; Tier 2 SKIPPED
```

Selected production SHA-256 anchors:

- `CatalogSnapshot.cs`: `229782f8e187a9b20d7cc32f3445607a4846af16844ec27642f1796ac9a2416f`.
- `WorkbookScaleForecast.cs`: `eaf4697da025c4f51d19743b15b57d1fd18ecd298a53d9e271eb8db5bc856091`.
- `CatalogQualification.cs`: `1d34e72c97a7bd922f63dd4625c9ae0920fbdf64175633b77da691bb44326e44`.
- `CatalogQualificationService.cs`: `e9064ec6862d1b08be1968e244f271b11addec9e6f3e9f83cc01fa9048a72f8f`.
- `WorkbookContract.cs`: `11c4ee15969573f4835975acf94b55175dd3dac9a8a375318a4a347a761e6535`.
- `WorkbookProjection.cs`: `0a978f2d3a78df9d7fd2a0581826d9982583374516faaff77c573293ff7e3bc2`.
- `WorkbookPackageWriter.cs`: `445a02cf3d8a45d2159e2d5dbe3cb67dd0b9661fa3427874695b645a0f54c8c3`.
- `WorkbookPackageInspector.cs`: `9ccbe0fb257dfac7691a70b4fb13592b22ad1f1aa776c8b76f821b588eb15eb5`.
- `WorkbookPairMaterializer.cs`: `ff95e226497ba1a35db29953782393dc30968f890fc955007ecf616bd1da36b8`.
- `PrivateWorkbookStaging.cs`: `03b6d20c93c79eaaca5a8e160cb65d672c91ad1e6470b7d2d01ee0a8db01967a`.
- `AppendOnlyRunStore.cs`: `413cf03d2530abc64ac4f4cfd7e48c404bd57baf0d77fa6f05c648cf014bc2e7`.
- `AtomicWorkbookPairPublisher.cs`: `2544b16313613aedb19515d60271df35ecb3794ce028f6ca0fb192b85691d21e`.

## Worker-owned change surface

- New adapter project: `src/BpmSoftSync.Adapters.Excel/`.
- New executable tests: `tests/BpmSoftSync.Adapters.Excel.Tests/`.
- New atomic publisher: `src/BpmSoftSync.Adapters.FileSystem/AtomicWorkbookPairPublisher.cs`.
- Minimal S04→S05 contract bridge: real pull-window timestamps on `QualifiedCatalogSnapshot`, qualification-service clock capture and safe `pullWindow` evidence digest. FileSystem adds exact accepted-chain validation plus transactional pair/evidence/seal commit.
- Solution registration: the new Excel adapter and test executable.
- S05 worker evidence, formula report and generated synthetic pair only. No acceptance report or handoff was created.

## Contract handed to S06

S06 may compose this stage only after independent S05 `Pass` and accepted handoff. It must pass the exact successful, persisted S04 `CatalogQualification` to one `AtomicWorkbookPairPublisher` for the same run; startup publication recovery is owned by that publisher and S06 must not manipulate staging/output/evidence itself. It must not call `BeginRunAsync`, reread BPMSoft, rerun Pass A/B, generate a second PairId, or write files directly. On success the pair is exactly `<run>/output/BPMSoft.ModelCatalog.xlsx` plus `<run>/output/BPMSoft.LookupCatalog.xlsx`; `workbook-pair.json` and `review-only-seal.json` exist and that RunId accepts no further evidence. S06 may display only returned relative paths, safe hashes/counts and `HUMAN_REVIEW_REQUIRED`; it must never read or print `LookupValues` or raw `SourceIdentity`. Fixture/fake E2E remains offline-only. No live admission, Compare, Apply, Write or Manage capability is provided by S05.
