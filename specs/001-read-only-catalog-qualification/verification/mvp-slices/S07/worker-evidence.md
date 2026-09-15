# S07 worker evidence — single opt-in live run

Date: 2026-09-14. This is worker evidence only; it does not claim acceptance.

## Gates and scope

- Active feature: `specs/001-read-only-catalog-qualification`.
- Prior gate: S06 has independent reviewer `Pass` and `ACCEPTED` evidence.
- Current human gate: the orchestrator reported the operator's current-chat confirmation `стенд запущен`.
- Target alias: `local-stand`. URL, login, password, cookie, CSRF and authorization data were entered only in the visible terminal and are not recorded here.
- Prohibited actions were not invoked: no retry, Pass C, Compare, Apply, Write, Manage, browser write or Git mutation.

## Preflight

The following commands completed before the live terminal was opened:

| Command | Result |
| --- | --- |
| `dotnet build BpmSoftSync.sln -c Release --no-restore` | exit `0`; 0 warnings; 0 errors |
| `dotnet run --project tests/BpmSoftSync.Domain.Tests/BpmSoftSync.Domain.Tests.csproj -c Release` | exit `0` |
| `dotnet run --project tests/BpmSoftSync.Adapters.BpmSoft.Tests/BpmSoftSync.Adapters.BpmSoft.Tests.csproj -c Release` | exit `0` |
| `dotnet run --project tests/BpmSoftSync.Application.Tests/BpmSoftSync.Application.Tests.csproj -c Release` | exit `0` |
| `dotnet run --project tests/BpmSoftSync.Adapters.FileSystem.Tests/BpmSoftSync.Adapters.FileSystem.Tests.csproj -c Release` | exit `0` |
| `dotnet run --project tests/BpmSoftSync.Adapters.Excel.Tests/BpmSoftSync.Adapters.Excel.Tests.csproj -c Release` | exit `0` |
| `dotnet run --project tests/BpmSoftSync.Cli.Tests/BpmSoftSync.Cli.Tests.csproj -c Release --no-build` | exit `0` |

Fresh CLI-suite aggregate: `S06_E2E outputTreeSha256=640a98b5ac90a8988dd12f11c04e8b9aa6892c1be296493158d140d792299ad6 modelSha256=3b0e047013a7293177bb489d1cc2dbbc02f67f4c8d5abecf2f0a8178573bde49 lookupSha256=90937c0a3723f8b7d10ad4cf3ce1a438f595e5ba24282eb344e243718df35eb3`.

## Interactive execution

The safe command form was:

`BpmSoftSync.Cli catalog qualify --target local-stand --scope full --manual --live --output-root C:\Users\Evgenii_2\Documents\BpmSoftSync\S07-live-20260914-001`

An initial Codex-terminal process reached only `BPMSoft origin:` but the operator could not see that panel. It received no terminal input, created no output root or run tree, and was terminated pre-auth with exit `1`. The operator then explicitly requested the same command in a separate visible PowerShell window. That replacement produced the only credentialed/HTTP live run; it was not an automatic retry.

Single live result:

- exit: `2`;
- `reason=SCHEMA_INVENTORY_UNQUALIFIED`;
- `scope=schema`;
- `recovery=Capture a safe structural envelope or resolve the typed source contract.`;
- `nextAction=Stop this qualification run.`
- no rerun or retry was performed.

## Safe run evidence

- RunId: `007b6fd7-e66b-471a-b262-7a24d32832bc`.
- Run root: `C:\Users\Evgenii_2\Documents\BpmSoftSync\S07-live-20260914-001\runs\2026\09\14\007b6fd7-e66b-471a-b262-7a24d32832bc`.
- `evidence/reconciliation.json`: schema-valid `EvidenceEnvelope/v1`; outcome `UNKNOWN_SHAPE_UNQUALIFIED`; `RetryCount=0`; SHA-256 `88e85ffae6dfe2130ebdea313974a45cdbb86d62803d053d2fc25cc00b245d38`.
- `evidence/blocked-terminal.json`: schema-valid `EvidenceEnvelope/v1`; outcome `UNKNOWN_SHAPE_UNQUALIFIED`; `RetryCount=0`; SHA-256 `7d6bf4eb9bbacdf1cf4951414833079e7efd4f33fa472671eccff01229b5a91a`.
- `run-journal.json`: 2 non-empty records; SHA-256 `6bbb7d87de121e09c9d59baf90be0b12d4084550b3f8494064a48580b2b2d46c`.
- `.sealed` contains `blocked-terminal`; there is no `review-only-seal.json`.
- Production `EvidenceEnvelopeValidator` accepted both evidence envelopes; its normal validation path includes production `SecretValueScanner` checks for those two envelope payloads.
- A separate independent read-only marker scan covered all 5 durable text artifacts and found 0 forbidden markers. This supplementary five-file result is not claimed as production `SecretValueScanner` execution over the whole run tree.

## Pair and endpoint result

- `output/` exists and contains 0 entries.
- Excel paths: none; `.xlsx` count is 0. OOXML/read-back/formula/1:1 projection checks are therefore not applicable to this blocked run.
- Pass A did not complete; Pass B, reconciliation equality and workbook publication were not reached.
- The live artifact schema does not persist endpoint request counters or `writeCallCount`. Therefore a target-specific runtime endpoint matrix and numeric `writeCallCount=0` cannot be claimed from this run. The preflight does prove the accepted `ReadEndpointAllowlist/v1` boundary and absence of write-capable production endpoints, but that is static/offline evidence rather than live capture.

## Classification and decision gate

`SCHEMA_INVENTORY_UNQUALIFIED` is the implemented and offline-tested fail-closed outcome for a `GetSchema` response that cannot be adapted to the qualified typed schema contract. The safe live evidence does not contain raw response shape and therefore does not establish a concrete production-code defect. Classification: expected unresolved target/schema qualification blocker; investigation would require an owning implementation stage to capture or derive a safe structural envelope before any separately authorized new run.

S07 did not reach `HUMAN_REVIEW_REQUIRED`. No human approval was made. `FULL_CATALOG_NOT_QUALIFIED` remains open, and `INDEX_SYNC_UNRESOLVED` remains unchanged.
