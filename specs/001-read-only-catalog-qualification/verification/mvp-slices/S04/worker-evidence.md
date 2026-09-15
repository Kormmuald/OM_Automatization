# S04 worker evidence — exact-two-pass snapshot and safe evidence

- Stage: `S04` worker implementation; this file is not acceptance or handoff.
- Preconditions: active feature `001`; accepted S03 evidence is `../S03/acceptance-report.md`, `../S03/handoff.md`, `../S03/review-02.md` (`Pass`).
- Execution boundary: in-memory full-catalog fake source and local temporary run roots only. No live BPMSoft, credentials, browser, Excel, Write/Manage/Compare/Apply or Git mutation was used.
- Requirements covered: `FR-004`, `FR-007`, `FR-009`, `FR-010`; S04 evidence for `SC-002`, `SC-004`, `SC-006`.

## Tests-first evidence

1. After adding the S04 application tests and restoring the new project, the RED command was:
   `dotnet build tests/BpmSoftSync.Application.Tests/BpmSoftSync.Application.Tests.csproj -c Release --no-restore`.
   Exit `1`: the compiler reported the expected absent S04 contracts (`IFullCatalogSource`, `ScopeDescriptor`, `CatalogReadRequest`, `FullCatalogRead`).
2. Focused GREEN command after production implementation:
   `dotnet build tests/BpmSoftSync.Application.Tests/BpmSoftSync.Application.Tests.csproj -c Release --no-restore` followed by `tests/BpmSoftSync.Application.Tests/bin/Release/net10.0/BpmSoftSync.Application.Tests.exe`.
   Exits `0`, `0`; result `PASS CatalogQualificationServiceTests`.
3. Focused correction after `review-01.md` was also tests-first. Application RED:
   `dotnet build tests/BpmSoftSync.Application.Tests/BpmSoftSync.Application.Tests.csproj -c Release --no-restore` exited `1` because the newly required registry-derived `CatalogScopePolicy`/unsealed-A contract did not yet exist. FileSystem RED: `dotnet tests/BpmSoftSync.Adapters.FileSystem.Tests/bin/Release/net10.0/BpmSoftSync.Adapters.FileSystem.Tests.dll` exited `1` with `A second store did not observe the filesystem stable-key claim.` before filesystem persistence was implemented.
4. Correction GREEN is the fresh cumulative validation below. It covers all five review findings without starting S05.
5. Final focused correction after `review-02.md` added two more RED gates. First FileSystem run exited `1` with `An unmarked ordinary lookup value crossed strict field-level validation.` for alphanumeric `NorthwindCustomer`. After closing the contract formats, the next run exited `1` with `RunId uniqueness was not enforced globally across date partitions and store instances.` The focused test then passed after the global claim was implemented.

Fake package identity: `fake:s04-full-catalog-v1`. Final SHA-256 of its typed fake/fault matrix source `tests/BpmSoftSync.Application.Tests/CatalogQualificationServiceTests.cs`: `7c75b5f657cec74793adf31646761146f9c1f9a9fda877d22859144e47928017`.

## Behavioral evidence

- Pass A receives no sealed lookup list. Its complete registry establishes the lookup set; each registry record must resolve to exactly one returned collection. Only then is one exact `ScopeDescriptor/v1` sealed and used to validate A and request B. It includes ordered workspace, schema, lookup-registry and registry-derived lookup contracts plus exact query/order/limit/version identifiers.
- Pass requests have distinct non-empty independent-read IDs. The service performs exactly two complete fake reads; counters are `full=2`, `workspace=2`, `schema=2`, `lookup=2`.
- Top-level and nested Pass-A material reuse are rejected as `PASS_B_INDEPENDENCE_UNQUALIFIED`. A deep-cloned Pass-A cache with a fresh outer ID is also rejected because its workspace/schema/registry/lookup read attestations overlap; its subread counters remain `1/1/1`, proving it did not independently reread. There is no loop, retry or Pass C.
- Reconciliation covers source identity, scope, target/version evidence, full ordered identities, counts, page manifests, response-size buckets, schema/index/reference/lookup-value component hashes, unsupported diagnostics and `TargetFingerprint/v1`.
- Equal passes create exactly one in-memory `QualifiedCatalogSnapshot/v1` from Pass B. The snapshot carries source/run/pair identities, full workspace/schema/index and lookup registry/value graphs, pass/component/fingerprint digests, unsupported diagnostics, counts and `WorkbookScaleForecast/v1`.
- Fake snapshot counts: workspace items `1`, schemas `1`, columns `2`, indexes `1`, index members `1`, lookup registry `1`, lookup collections `1`, lookup rows `1`, normalized lookup values `2`, page manifests `2`.
- A normalized lookup value/content mutation with unchanged version evidence becomes terminal `TARGET_STATE_CHANGED_DURING_QUALIFICATION`; the domain matrix separately covers target-version evidence and every other reconciliation component. A Pass-B paging failure preserves `LOOKUP_PAGE_OFFSET_UNQUALIFIED`; both return `RetryCount=0`, no snapshot and safe failure evidence.
- A projected worksheet count beyond the declared limit becomes `WORKBOOK_SCALE_DECISION_REQUIRED`, with no snapshot or truncation.

## Run/evidence boundary

- A successful full fake qualification creates one unique `<root>/runs/yyyy/MM/dd/<RunId>/` containing `audit/`, `evidence/`, `output/` and `run-journal.json`; no workbook is created in S04.
- Typed safe records for a successful S04 gate are `pass-a`, `pass-b`, `reconciliation` and `qualified-snapshot`. The RunId deliberately remains open for S05 to append pair evidence and perform the single final success seal; blocked S04 flow ends immediately with `blocked-terminal`.
- Evidence contains only strictly typed/allowlisted target alias, IDs, non-negative counts, fixed buckets/statuses, lowercase SHA-256 digests and explicit sealed scope contracts. Dynamic lookup collection IDs are emitted only as `lookup-sha256:<64 lowercase hex>`. Workspace/schema/registry/lookup contract kinds each have one closed field-specific `CollectionId`/`OrderKeyId`/`QueryContractId`/limits tuple; arbitrary technical-looking strings cannot satisfy it. Pass/reconciliation/snapshot evidence attests the safe tuple, while the original exact contracts participate in the pass reconciliation digest and exact A/B comparison.
- The actual normalized lookup value remains in snapshot memory and is absent from evidence and journal. Field-level negative tests inject ordinary unmarked alphanumeric `NorthwindCustomer` into payload digest, digest value, outcome, duration/scale/response buckets, gate outcome, collection ID, order key and query contract; every variant is rejected without relying on spaces or canary markers. A raw blocker reason is mapped to a fixed blocker-code token before terminal evidence. Marker scanning remains defense in depth, not the evidence safety model.
- Same-RunId collision is rejected for sequential, concurrent same-store, concurrent separate-store and different-date/separate-store attempts. A filesystem `CreateNew` claim under `runs/.run-id-claims/<RunId>` establishes global uniqueness before the date-partitioned root is created. Stable-key evidence uses a separate filesystem `CreateNew` under a per-run cross-instance lock. A persistent `.sealed` claim is created before terminal evidence; a second store cannot overwrite a stable key or append after seal.

## Fresh cumulative validation

Commands:

```text
dotnet build BpmSoftSync.sln -c Release --no-restore
dotnet tests/BpmSoftSync.Domain.Tests/bin/Release/net10.0/BpmSoftSync.Domain.Tests.dll
dotnet tests/BpmSoftSync.Application.Tests/bin/Release/net10.0/BpmSoftSync.Application.Tests.dll
dotnet tests/BpmSoftSync.Adapters.BpmSoft.Tests/bin/Release/net10.0/BpmSoftSync.Adapters.BpmSoft.Tests.dll
dotnet tests/BpmSoftSync.Adapters.FileSystem.Tests/bin/Release/net10.0/BpmSoftSync.Adapters.FileSystem.Tests.dll
dotnet tests/BpmSoftSync.Cli.Tests/bin/Release/net10.0/BpmSoftSync.Cli.Tests.dll
```

Final exits: build `0`; Domain `0`; Application `0`; Adapters.BpmSoft `0`; Adapters.FileSystem `0`; CLI `0`. Build result: 0 warnings, 0 errors.

Production-source SHA-256 anchors after validation:

- `src/BpmSoftSync.Domain/CatalogSnapshot.cs`: `448b081829384442864bc95fb994ab71972e1496796a8844581ad6d57e8991e1`;
- `src/BpmSoftSync.Application/CatalogQualificationService.cs`: `3e25956da5b4ee154b252afd9b4635855aa5e608227329d839e4e6e6aa831b06`;
- `src/BpmSoftSync.Adapters.FileSystem/AppendOnlyRunStore.cs`: `d889c9cc7140b10e7b5f56787e88fa0119b2ffd28997eea9dd3f5682cca021a3`;
- `src/BpmSoftSync.Adapters.FileSystem/EvidenceEnvelopeValidator.cs`: `52035d9d538e954ea7fd3a4675ecd2e202a87aeebb8b7d39f01acd1af7ce1e64`.

## Contract handed to S05

S05 may accept only `CatalogQualification.Snapshot` when `IsQualified=true`, `Result.Reason=HUMAN_REVIEW_REQUIRED`, `Snapshot.Schema=QualifiedCatalogSnapshot/v1` and `Snapshot.Scale.Status=DiagnosticOnly`. It must project the full Pass-B `Workspace` and `Lookups` graphs without rereading BPMSoft or reimplementing identity/reconciliation logic. `RunId` binds the still-open existing append-only root; `PairId` binds both new workbooks. Pair/manifest evidence must reference `PassADigest`, `PassBDigest`, `TargetFingerprint`, `ComponentDigests`, `Counts`, `Scope.Digest` and `SourceIdentity` only. Actual `NormalizedLookupValue` content is allowed solely in the local Lookup workbook output; it must never be copied into audit/evidence/journal/diagnostics. A null snapshot or non-diagnostic scale status is terminal input rejection for S05. S05 owns the next append(s), atomic pair publication and the single final success seal; it must not create a second RunId.

## Worker-owned change surface

- Domain/application: `CatalogSnapshot.cs`, `CatalogQualification.cs`, `WorkbookScaleForecast.cs`, `SafetyContracts.cs`, `Ports.cs`, `CatalogQualificationService.cs`.
- FileSystem evidence lifecycle: `AppendOnlyRunStore.cs`, `EvidenceEnvelopeValidator.cs`, `SecretValueScanner.cs`.
- Tests: new `tests/BpmSoftSync.Application.Tests/`; S04 extensions to Domain/FileSystem tests; one CLI regression assertion narrowed to count files under `evidence/` because S04 now correctly creates canonical `run-journal.json`.
- Solution registration: `BpmSoftSync.sln` adds the S04 application test executable.
- Stage evidence: this file only. No `acceptance-report.md` or `handoff.md` was created by the worker.
