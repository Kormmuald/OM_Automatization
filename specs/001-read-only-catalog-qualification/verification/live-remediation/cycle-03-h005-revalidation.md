# Cycle 3 — независимая повторная validation H-005 (gaps B/C)

## Scope and independence

Проверка выполнена после добавления gap-тестов B/C и отдельно от их реализации.
Проверялись только рабочая копия и offline fixture suites. Не выполнялись live
BPMSoft/network requests, реальная аутентификация или использование учётных
данных, запуск CLI against a target, Excel publication, mutation routes либо
Git write operations. Этот отчёт не является live admission.

## What was revalidated

- `schema.package.id` принимается исключительно как non-empty opaque string;
  `schema.package.uId` остаётся обязательным GUID и единственным primary package
  identity. `PrimaryIdentityKey` не включает raw opaque value.
- Parser matrix закрывает missing/null/whitespace/number, а новые случаи JSON
  `object`, `array` и `boolean` fail-closed на `SchemaPackageId` с
  `RequiredString`; object/array canaries не появляются в closed diagnostic.
- Новая executable collision proof создаёт duplicate
  `(schema.uId, package.uId)` при корректной read-attestation. Результат —
  terminal `PACKAGE_PRIMARY_IDENTITY_UNQUALIFIED`, `RetryCount=0`, только Pass A,
  без Pass B, snapshot или workbook output.
- Изменение opaque provenance между Pass A/B меняет safe fingerprint и приводит к
  `TARGET_STATE_CHANGED_DURING_QUALIFICATION`, без retry или Pass C. Raw canary
  отсутствует из durable evidence.
- Domain canonicalization использует только GUID primary identity для stable
  schema/package identities; opaque provenance участвует только через SHA-256
  digest в component change detection. Excel projection выводит `package.uId`,
  но тест явно подтверждает отсутствие opaque values в cells. Workflow вызывает
  publisher только при qualified result с non-null snapshot, которого collision
  path не создаёт.

## Executed evidence

All commands completed with exit `0`:

```text
dotnet build BpmSoftSync.sln -c Release --no-restore
dotnet run -c Release --no-build --project tests/BpmSoftSync.Domain.Tests/BpmSoftSync.Domain.Tests.csproj
dotnet run -c Release --no-build --project tests/BpmSoftSync.Application.Tests/BpmSoftSync.Application.Tests.csproj
dotnet run -c Release --no-build --project tests/BpmSoftSync.Adapters.BpmSoft.Tests/BpmSoftSync.Adapters.BpmSoft.Tests.csproj
dotnet run -c Release --no-build --project tests/BpmSoftSync.Adapters.FileSystem.Tests/BpmSoftSync.Adapters.FileSystem.Tests.csproj
dotnet run -c Release --no-build --project tests/BpmSoftSync.Adapters.Excel.Tests/BpmSoftSync.Adapters.Excel.Tests.csproj
dotnet run -c Release --no-build --project tests/BpmSoftSync.Cli.Tests/BpmSoftSync.Cli.Tests.csproj
git diff --check
```

Release build reported `0` warnings and `0` errors. `git diff --check` emitted
only existing CRLF conversion warnings, with no whitespace errors.

## Verdict

**Pass for handoff to a fresh independent gate reviewer.** The two blockers from
`cycle-03-h005-validation.md` are now covered by executable offline tests, and
the semantic safety, fail-closed parsing, zero-retry/no-output boundary and
Excel privacy properties revalidated successfully.

This is not authorization for a controlled full live qualification/export. A
gate reviewer must separately decide that next action and preserve the existing
explicit live-admission controls.
