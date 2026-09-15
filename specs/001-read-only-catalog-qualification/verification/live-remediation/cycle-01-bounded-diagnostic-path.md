# Cycle 1 — bounded diagnostic path H-001

**Дата:** 2026-09-15
**Worker:** `gpt-5.6-terra` / `high`
**Статус:** реализация worker; не является review, acceptance или разрешением на live attempt.

## Точная граница

Добавлена отдельная interactive-only команда:

`catalog diagnose-schema --target <safe-alias> --manual --live`

В production composition она допускает только последовательность
`AUTH_LOGIN` → `WORKSPACE_ITEMS` → bounded `SCHEMA_GET` до первого terminal
blocker. Повторный запуск, retry, Pass B/Pass C, full qualification,
`LookupCatalogSource`, `SELECT_QUERY`, reconciliation, snapshot, Excel-pair и
run/output publication не имеют ни аргумента, ни порта, ни зависимости на этом
пути.

Если все schema reads завершились без blocker, команда возвращает terminal
non-success `SCHEMA_DIAGNOSTIC_COMPLETED_WITHOUT_BLOCKER`. Это не qualification
success и не разрешение на full run.

## Первоначальный terminal-only контракт

Команда не принимает URL, login, password, cookie, CSRF или authorization в
arguments/configuration и не принимает output-root. URL/login/password читаются
исключительно `TerminalCredentialPrompt` в памяти процесса. Результат содержит
только обычные safe terminal fields и уже существующий closed
`FailedShapeDiagnostic`: path enum, expected enum, observed JSON-kind enum,
array-cardinality enum и bounded ordinal. Этот маршрут не создаёт durable
artifacts; поэтому response/name/value/GUID/URL/cookie/CSRF/login response/secret
не могли попасть в journal, evidence, output или Excel через diagnostic path.

## Дополнение: durable safe evidence boundary

После отдельного admission finding первоначальный terminal-only контракт заменён
узкой дополнительной границей. `catalog diagnose-schema` принимает только
опциональный `--evidence-root <absolute-user-local-path>`; relative,
workspace, temporary, Windows/system и volume-root пути отклоняются до prompt
и runner. Без аргумента используется user-local default.

Для каждого terminal result создаётся unique root
`diagnostic-runs/yyyy/MM/dd/<opaque-token>/` только с
`diagnostic-terminal.json` и `.sealed`. Record
`SchemaDiagnosticTerminalEvidence/v1` содержит exact allowlisted поля: schema,
safe alias, fixed route, closed outcome и optional closed `failedShape`. Перед
write выполняются scanner и schema validation, затем exact read-back и SHA-256
seal. В record запрещены raw JSON, response/data names и values, GUID, URL,
credentials, cookies, CSRF, login material, arbitrary terminal text и workbook
contents. Это не normal qualification run: не создаются audit/evidence/output,
journal, staging или Excel.

Offline regression покрывает accepted user-local default/explicit root, rejected
relative/workspace/temp/system/volume roots до runner, sealed read-back, exact
allowlist, canary rejection и `--output-root` rejection. Дополнение не меняет
single-read boundary и не authorizes live attempt без отдельной validation/review.

## Изменённые поверхности

- `src/BpmSoftSync.Application/Ports.cs` и
  `BoundedSchemaDiagnosticWorkflow.cs`: single-read port и terminal-only
  non-qualification result.
- `src/BpmSoftSync.Adapters.BpmSoft/BpmSoftSchemaDiagnosticSource.cs`: изоляция
  workspace/schema traversal без lookup reader.
- `src/BpmSoftSync.Cli/Commands/CatalogSchemaDiagnoseCommand.cs` и
  `LiveSchemaDiagnosticRunner.cs`, `Program.cs`: explicit CLI admission и
  terminal-only production composition.
- Application/CLI targeted tests: first-blocker/no-blocker, one read/no retry,
  secret/retry argument rejection, no output/publication parameter и static
  architecture guard against lookup/qualification/snapshot/workbook/run-store
  dependencies.

## Неподтверждённое

Изменение не доказывает endpoint semantics или легитимность любой production
shape и не подтверждает H-001. Новый live diagnostic допускается только после
отдельных validation и independent reviewer `Pass`, а затем отдельного current
human decision. Никакие credentials или live/network actions этим worker не
использовались.
