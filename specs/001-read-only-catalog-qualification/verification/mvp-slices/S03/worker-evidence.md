# S03 worker evidence — full lookup registry and values

- Stage: `S03` worker implementation; this file is not acceptance or handoff.
- Execution boundary: sanitized fixture plus fake `HttpMessageHandler` only; no live target, real credentials, browser, Excel, Write/Manage/Compare/Apply or Git mutation.
- Requirements: `FR-003`, `FR-006`; success evidence for `SC-003`, `SC-004` within S03 scope.

## Tests-first evidence

1. Red command: `dotnet build tests/BpmSoftSync.Adapters.BpmSoft.Tests/BpmSoftSync.Adapters.BpmSoft.Tests.csproj -c Release --no-restore`.
   Exit: `1`, with the expected missing S03 contract symbols before production implementation.
2. Green focused command: `dotnet build tests/BpmSoftSync.Adapters.BpmSoft.Tests/BpmSoftSync.Adapters.BpmSoft.Tests.csproj -c Release --no-restore` followed by `tests/BpmSoftSync.Adapters.BpmSoft.Tests/bin/Release/net10.0/BpmSoftSync.Adapters.BpmSoft.Tests.exe`.
   Exits: `0`, `0`; `PASS LookupCatalogSourceTests`.

## Fresh final validation

Command sequence:

```text
dotnet build BpmSoftSync.sln -c Release --no-restore
tests/BpmSoftSync.Domain.Tests/bin/Release/net10.0/BpmSoftSync.Domain.Tests.exe
tests/BpmSoftSync.Adapters.BpmSoft.Tests/bin/Release/net10.0/BpmSoftSync.Adapters.BpmSoft.Tests.exe
tests/BpmSoftSync.Adapters.FileSystem.Tests/bin/Release/net10.0/BpmSoftSync.Adapters.FileSystem.Tests.exe
tests/BpmSoftSync.Cli.Tests/bin/Release/net10.0/BpmSoftSync.Cli.Tests.exe
```

Final exits: build `0`; Domain `0`; Adapters.BpmSoft `0`; Adapters.FileSystem `0`; CLI `0`. Build: 0 warnings, 0 errors.

S03 behavioral matrix passed:

- full ordered registry and two nonstandard discovered lookup schemas;
- exact registry record/schema/package-layer/base-schema binding;
- 5 lookup rows and 36 normalized non-Id column values with typed/canonical forms, `Null|EmptyString|Value`, references and SHA-256 fingerprints;
- explicit columns, `allColumns=false`, ascending `Id`, bounded offset progression and two consecutive empty terminal observations;
- duplicate, overlap, detectable gap, loop, empty-middle, nonempty-after-terminal, maximum pages/rows/response bytes, timeout and cancellation;
- malformed identity/reference/value/row/select shapes and unsupported type, all fail closed without raw value diagnostics;
- deterministic registry and collection fingerprints across repeated fake reads;
- prior S01/S02, FileSystem and CLI executable regressions preserved.

## Focused correction after independent review 01

- Added a mandatory `rowsOffset` response field check. Missing, non-integer or mismatched offsets stop immediately with `LOOKUP_PAGE_OFFSET_UNQUALIFIED` and `CatalogOrderOrPagingUnqualified`.
- Added strict ordinal-ascending canonical `Id` validation within each non-empty page and across adjacent pages. Unique reordered/descending rows stop at the first observed violation with `LOOKUP_ID_ORDER_UNQUALIFIED`.
- Added three fake-HTTP adversarial cases: absent `rowsOffset`, reordered IDs inside one page and reordered IDs across two pages. Tests assert the exact blocker reason and first observable stop request.
- Focused red command: the Adapters.BpmSoft Release build passed, then its executable exited `1` with `Absent rowsOffset was not rejected at the response boundary.` before the correction.
- Focused green command: `dotnet build tests/BpmSoftSync.Adapters.BpmSoft.Tests/BpmSoftSync.Adapters.BpmSoft.Tests.csproj -c Release --no-restore` plus the Release executable; exits `0`, `0`.
- Fresh cumulative command sequence repeated after the correction; exits: build `0`, Domain `0`, Adapters.BpmSoft `0`, Adapters.FileSystem `0`, CLI `0`; 0 warnings, 0 errors.

Fixture: `lookup-full-catalog-v1`, SHA-256 `08089b636124c201be52666bcf540409b633e4d12fb5ad9ae26d087d0e7428c3`; manifest match: `true`.

Additional checks: targeted `git diff --check` exit `0`; forbidden production-scope token scan exit `0` with 0 matches.
