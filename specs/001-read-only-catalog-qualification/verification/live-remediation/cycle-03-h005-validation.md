# Cycle 3 — независимая validation H-005 semantic rewrite

**Дата:** 2026-09-15
**Роль:** independent validation; без реализации, commit, live/network/auth,
credentials или Excel publication.
**Вердикт:** **Fail — fresh reviewer пока не может открыть gate для controlled
full live qualification/export.**

## Проверенная граница

Прочитаны `AGENTS.md`, active Feature 001 (`.specify/feature.json`), canonical
`spec.md`, `plan.md`, `tasks.md`, `HANDOFF.md`, весь текущий
`verification/live-remediation/`, текущий diff, production code и tests. Source
drafts под `preparation/docs/product-specs/` не изменялись.

Проверка относится только к H-005: `schema.package.id` должен быть required
non-empty opaque string, `schema.package.uId` — единственной GUID primary
identity, opaque scalar — только in-memory provenance, а его односторонний
digest должен участвовать в change detection без raw leakage.

## Выполненные команды

```text
dotnet build BpmSoftSync.sln -c Release --no-restore
dotnet run --project tests/BpmSoftSync.Domain.Tests/BpmSoftSync.Domain.Tests.csproj -c Release --no-build
dotnet run --project tests/BpmSoftSync.Application.Tests/BpmSoftSync.Application.Tests.csproj -c Release --no-build
dotnet run --project tests/BpmSoftSync.Adapters.BpmSoft.Tests/BpmSoftSync.Adapters.BpmSoft.Tests.csproj -c Release --no-build
dotnet run --project tests/BpmSoftSync.Adapters.FileSystem.Tests/BpmSoftSync.Adapters.FileSystem.Tests.csproj -c Release --no-build
dotnet run --project tests/BpmSoftSync.Adapters.Excel.Tests/BpmSoftSync.Adapters.Excel.Tests.csproj -c Release --no-build
dotnet run --project tests/BpmSoftSync.Cli.Tests/BpmSoftSync.Cli.Tests.csproj -c Release --no-build
git diff --check
```

Все команды завершились с exit `0`. Release build: `0` warnings, `0` errors.
CLI suite создала только synthetic S06 fixture output и удалила его; это не
live run и не target qualification.

## Подтверждённое inspection и tests

- Parser в `WorkspaceInventoryAdapter.TryAdaptSchema` принимает `package.id`
  только через `TryRequiredString`, затем требует GUID `package.uId`.
  Existing matrix доказывает acceptance opaque и GUID-looking string, а также
  rejection missing/null/whitespace/number and invalid `uId` без scalar в
  diagnostic.
- `PackageLayerIdentity.PrimaryIdentityKey` строится только из `package.uId`.
  `CatalogPassBuilder` требует non-empty provenance и non-null GUID до Pass A
  или B, а duplicate pair check находится в
  `PrimaryPackageIdentitiesAreUnambiguous`.
- Raw opaque scalar не входит в stable identity, `TargetFingerprint` input,
  safe evidence, CLI renderer или projected cells. Component content включает
  только SHA-256 provenance digest. Application test доказывает, что change
  provenance between A/B changes fingerprint, returns
  `TARGET_STATE_CHANGED_DURING_QUALIFICATION`, creates no snapshot and makes
  exactly two reads with `RetryCount=0`. FileSystem test scans durable content;
  Excel test checks both workbook projections for opaque canaries.
- Existing full-read, allowlist, evidence, atomic pair and CLI regressions
  remain green; inspection did not find a new retry, Pass C, reuse of A in B,
  write endpoint or live admission expansion.

## Blocking test gaps

H-005's own approved hypothesis requires collision vectors. Current source has
the intended duplicate guard, but no test constructs two schemas with the same
`(schema.uId, package.uId)` and asserts fail-closed
`PACKAGE_PRIMARY_IDENTITY_UNQUALIFIED`, `RetryCount=0`, no Pass B and no output.
The existing same-name Excel collision test has different schema/package GUIDs,
so it does not exercise this guard.

The adapter negative matrix also omits `package.id` as JSON `object`, `array`
and `boolean`. `TryRequiredString` appears to reject them, but this is not a
tested H-005 contract at the parser boundary.

These are small, focused offline tests, but they protect the exact
identity/collision and fail-closed claims required before sending credentials to
a full live workflow. No implementation change is requested by this validation.

## Gate result

**Fail.** A fresh reviewer must not gate a controlled full live
qualification/export until an implementation worker adds the two focused test
groups above and a new independent validation reruns the same six suites.
This verdict does not alter `FULL_CATALOG_NOT_QUALIFIED`, does not authorize a
retry/rerun, and does not infer any BPMSoft target defect.
