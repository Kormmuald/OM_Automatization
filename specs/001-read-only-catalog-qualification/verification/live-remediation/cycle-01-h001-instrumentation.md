# Cycle 1 — H-001: safe failed-shape envelope

**Дата:** 2026-09-15
**Worker:** `gpt-5.6-terra` / `high`
**Статус:** implementation worker record; не является acceptance, review или
разрешением на live attempt.

## Изменённая граница

Только первый failed required path строгого schema parser получает
`FailedShapeDiagnostic`. Его closed fields: `FailedShapePath`,
`ExpectedShapeCategory`, `ObservedJsonKind`, `ArrayCardinalityBucket` и optional
ordinal. У типов нет полей для raw JSON, property/scalar values, display names,
GUID values, URL или session data.

Эта информация попадает в fail-closed `Blocker`, terminal-safe CLI renderer и
optional `failedShape` внутри `EvidenceEnvelope/v1`. Validator допускает только
значения закрытых enum и bounded non-negative ordinal; v1 evidence прежних run
без этого поля сохраняет compatibility. Unknown/malformed external shape не
принимается и не обходит blocker.

## Characterization и границы доказательства

Новые fake-only tests покрывают valid strict shape и failed root schema,
`schema.id`, package, `columns`, `inheritedColumns`, parent, index object и
index-member `columnUId`. Они подтверждают, что каждая ветвь остаётся terminal
и что diagnostic не содержит sanitized scalar/display-name canaries.

Тесты не доказывают BPMSoft endpoint semantics или приемлемость неизвестной
production shape. Они не выполняют HTTP к BPMSoft, browser action, login,
credential use, `SELECT_QUERY`, Pass B, workbook generation или Git action.

## Разрешённый следующий live boundary

Только после отдельной validation и independent reviewer `Pass` может быть
предложена одна current-chat human-authorized diagnostic attempt:
`AUTH_LOGIN` → `WORKSPACE_ITEMS` → bounded `SCHEMA_GET` до первого blocker.
Она не является retry S07 и не включает Pass B, Pass C, `SELECT_QUERY`, Excel,
Write, Manage, Compare, Apply, browser write, SQL mutation или Git push.

## Offline команды worker

| Команда | Результат |
| --- | --- |
| `dotnet build BpmSoftSync.sln -c Release --no-restore` | exit `0`; 0 warnings, 0 errors. |
| `dotnet run --project tests/BpmSoftSync.Adapters.BpmSoft.Tests/BpmSoftSync.Adapters.BpmSoft.Tests.csproj -c Release --no-build` | exit `0`; включая characterization H-001. |
| `dotnet run --project tests/BpmSoftSync.Adapters.FileSystem.Tests/BpmSoftSync.Adapters.FileSystem.Tests.csproj -c Release --no-build` | exit `0`; `FailedShapeEvidenceAcceptsOnlyClosedEnums` и существующие S04/S05 tests passed. |
| `dotnet run --project tests/BpmSoftSync.Cli.Tests/BpmSoftSync.Cli.Tests.csproj -c Release --no-build` | exit `0`; terminal renderer regression passed. |

Worker не выполняет self-review и не совершает commit.
