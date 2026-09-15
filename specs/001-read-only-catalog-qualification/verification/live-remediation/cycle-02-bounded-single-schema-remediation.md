# Cycle 2 — remediation bounded single-schema source

**Дата:** 2026-09-15
**Роль:** implementation worker (`gpt-5.6-terra` / `high`).
**Статус:** implementation complete; independent validation/review и live-admission не выполнялись.

## Исправленный gate blocker

`BpmSoftSchemaDiagnosticSource` ранее делегировал в
`WorkspaceInventoryAdapter.ReadFullAsync`; вследствие этого valid first schema
response мог продолжить full traversal. Source теперь выполняет отдельный
bounded path: один `WORKSPACE_ITEMS`, deterministic selection minimum typed GUID
среди typed `EntitySchema`, затем ровно один `SCHEMA_GET`. Local response parser
не инициирует request; valid response возвращает `null` только в
`BoundedSchemaDiagnosticWorkflow`, который выдаёт explicit non-qualifying
terminal `SCHEMA_DIAGNOSTIC_COMPLETED_WITHOUT_BLOCKER`.

Если typed candidate отсутствует, source возвращает closed terminal blocker
`SCHEMA_DIAGNOSTIC_NO_SCHEMA_CANDIDATE` и не вызывает `SCHEMA_GET`. Новый
`NoSchemaCandidate` terminal outcome разрешён только в sealed
`SchemaDiagnosticTerminalEvidence/v1`; он не содержит `failedShape`, names,
identifiers, raw payload, URL либо credential/session data.

## Изменённые файлы

- `src/BpmSoftSync.Adapters.BpmSoft/BpmSoftSchemaDiagnosticSource.cs`
- `src/BpmSoftSync.Adapters.BpmSoft/WorkspaceInventoryAdapter.cs`
- `src/BpmSoftSync.Domain/SafetyContracts.cs`
- `src/BpmSoftSync.Adapters.FileSystem/AppendOnlySchemaDiagnosticEvidenceStore.cs`
- `tests/BpmSoftSync.Adapters.BpmSoft.Tests/WorkspaceInventoryAdapterTests.cs`
- `tests/BpmSoftSync.Adapters.BpmSoft.Tests/Program.cs`
- `tests/BpmSoftSync.Adapters.FileSystem.Tests/SchemaDiagnosticEvidenceTests.cs`
- `specs/001-read-only-catalog-qualification/contracts/cli-contract.md`
- `specs/001-read-only-catalog-qualification/test-plan.md`
- `specs/001-read-only-catalog-qualification/verification/live-remediation/README.md`

## Новая offline characterization

- Перевёрнутый multiple-item workspace: exact request sequence
  `AUTH_LOGIN` → `WORKSPACE_ITEMS` → `SCHEMA_GET`; third request body содержит
  только minimum typed schema GUID; first schema response valid; fourth request
  отсутствует.
- Workspace без typed schema candidate: exact sequence только
  `AUTH_LOGIN` → `WORKSPACE_ITEMS`; terminal blocker is
  `SCHEMA_DIAGNOSTIC_NO_SCHEMA_CANDIDATE`; `SCHEMA_GET` отсутствует.
- Sealed evidence regression: closed no-candidate outcome writes and reads back
  with `failedShape = null`; arbitrary terminal text remains rejected.

## Выполненные offline команды

```text
dotnet build BpmSoftSync.sln -c Release --no-restore
dotnet run --project tests/BpmSoftSync.Adapters.BpmSoft.Tests/BpmSoftSync.Adapters.BpmSoft.Tests.csproj -c Release --no-build
dotnet run --project tests/BpmSoftSync.Adapters.FileSystem.Tests/BpmSoftSync.Adapters.FileSystem.Tests.csproj -c Release --no-build
dotnet run --project tests/BpmSoftSync.Application.Tests/BpmSoftSync.Application.Tests.csproj -c Release --no-build
dotnet run --project tests/BpmSoftSync.Cli.Tests/BpmSoftSync.Cli.Tests.csproj -c Release --no-build
git diff --check
```

Все завершились `exit 0`. Этот worker не выполнял live/network/auth/browser
actions, не использовал credentials и не выполнял Write/Manage/Compare/Apply,
SQL mutation, delete, Git commit/push или Excel publication.
