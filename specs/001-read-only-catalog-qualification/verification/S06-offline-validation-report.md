# S06 offline validation report

Date: 2026-09-14. Scope: production CLI composition and offline fixture/fake-handler validation only. No BPMSoft target, browser, credential, Compare, Apply, Write, Manage, Git or live harness was invoked.

## Exact completed commands

| Command | Exit |
| --- | ---: |
| `dotnet build BpmSoftSync.sln -c Release --no-restore` | 0; 0 warnings, 0 errors |
| `dotnet run --project tests/BpmSoftSync.Domain.Tests/BpmSoftSync.Domain.Tests.csproj -c Release` | 0 |
| `dotnet run --project tests/BpmSoftSync.Adapters.BpmSoft.Tests/BpmSoftSync.Adapters.BpmSoft.Tests.csproj -c Release` | 0 |
| `dotnet run --project tests/BpmSoftSync.Application.Tests/BpmSoftSync.Application.Tests.csproj -c Release` | 0 |
| `dotnet run --project tests/BpmSoftSync.Adapters.FileSystem.Tests/BpmSoftSync.Adapters.FileSystem.Tests.csproj -c Release` | 0 |
| `dotnet run --project tests/BpmSoftSync.Adapters.Excel.Tests/BpmSoftSync.Adapters.Excel.Tests.csproj -c Release` | 0 |
| `dotnet run --project tests/BpmSoftSync.Cli.Tests/BpmSoftSync.Cli.Tests.csproj -c Release --no-build` | 0 |

## Safe fixture and E2E facts

- Sanitized fixture manifest: `tests/fixtures/read-only/fixture-manifest.json`, SHA-256 `91a83a62aa5e238278342431e99879e6ccc29839d76cc8f2fa0694992d4a7177`.
- New full process fixture: `tests/fixtures/read-only/s06-full-cli-fake.json`, SHA-256 `7e21b3443732ba4f9b6b58af764b93c81f6b59efa02b05b0f777be943ec5746d`.
- `ProgramMainOfflineCompositionTests.ProductionMainPublishesFixturePairWithoutLiveAdmissionAsync` calls production `Program.Main` with `catalog qualify-offline --fixture … --output-root …`; it creates the fake session, production `BpmSoftFullCatalogSource`, shared `CatalogQualificationWorkflow`, canonical pair and S05 seal. No test injects a command, workflow or publisher manually.
- `ProgramMainOfflineCompositionTests.ProductionMainRejectsWorkspaceAndNestedRootsBeforeLivePromptAsync` proves both the workspace/repository root and a nested `src/` path are rejected with `OUTPUT_ROOT_NOT_ALLOWED` before a live terminal prompt or HTTP can occur.
- `ProgramMainOfflineCompositionTests.ProductionMainRejectsBareTempRootBeforeRunnerPromptOrHttpAsync` passes bare `%TEMP%` without its trailing separator to production `Program.Main`; the runner is not reached (a throwing console reader remains unread), and the command returns `OUTPUT_ROOT_NOT_ALLOWED` with exit `2`.
- `ProductionWorkflowE2ETests.FullCompositionPublishesExactAcceptedPairAsync` used an in-process sanitized fake source, completed exactly two independent reads and verified S04 qualification through S05 atomic pair publication.
- Fresh ephemeral output tree SHA-256: `9a90bde8691d9e9fa86a46aebe89212c8756a07bfbc1acd91c08406cd41a0b49`.
- Model workbook SHA-256: `14e7591bb371067af64f420a87741be66ddd8ccbd3512f866d663b7d1010304f`.
- Lookup workbook SHA-256: `feed6fe8367fbcc538a8285edad73c81bcf9c8b732f76ef03bb5b196e062a283`.
- The E2E scanner proved that the lookup-value and secret canaries are absent outside `output/`; the temporary run tree was deleted after validation.

## Boundary result

The opt-in live route is assembled as `catalog qualify --target <safe-alias> --scope full --manual --live [--output-root <path>]`. It normalizes candidate and forbidden roots symmetrically, then rejects absent `--live`, secret-bearing arguments, the repository root, every nested repository path and `%TEMP%` (including bare no-trailing-separator form) before terminal credentials or HTTP. This report does not authorize a live run and is not an acceptance report.
