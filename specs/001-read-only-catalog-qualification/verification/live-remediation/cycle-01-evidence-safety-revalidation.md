# Cycle 1 — независимая повторная validation B-01/B-02

**Дата:** 2026-09-15
**Роль:** independent test/validation worker (`gpt-5.6-terra`, `high`), отдельный от implementation worker B-01/B-02.
**Граница:** только offline read/inspection и test execution. Не выполнялись live/network/auth/browser actions, не использовались credentials, не выполнялись Write/Manage/Compare/Apply, SQL mutation, delete или Git commit/push.

## Прочитанные источники и исходное состояние

- Подтверждён active feature: `.specify/feature.json` указывает `specs/001-read-only-catalog-qualification`.
- Прочитаны `AGENTS.md`, `.specify/project.yml`, `.specify/extensions.yml`, актуальный `HANDOFF.md`, Feature 001 `spec.md`, `plan.md`, `tasks.md`, `contracts/cli-contract.md`, `test-plan.md`, `verification/live-mvp-verification-prompt.md`, весь текущий `verification/live-remediation/` и все 40 S00–S08 acceptance/handoff/review artifacts.
- Прочитаны текущий relevant production/test code и diff. До моей документационной записи worktree уже был dirty с изменениями нескольких владельцев и generated `bin/`/`obj/`; это не приписывается B-01/B-02.
- Исторический S07 остаётся единственным live result: `SCHEMA_INVENTORY_UNQUALIFIED` / `UNKNOWN_SHAPE_UNQUALIFIED`, без Pass B, reconciliation или Excel output. Он не является доказательством production defect.

## B-01 — physical containment user-local evidence root

**Pass.** `SchemaDiagnosticEvidenceRoot.TryResolve` допускает только абсолютный descendant dedicated `%LOCALAPPDATA%/BpmSoftSync`, отклоняет workspace, temporary, Windows/system, volume root и file path. До runner обходятся existing ancestors: file, `ReparsePoint` или `LinkTarget` дают fail-closed result. `AppendOnlySchemaDiagnosticEvidenceStore` повторно создаёт/проверяет каждый physical directory segment до staging/final path.

Targeted CLI test создаёт Windows symlink, при недоступности symlink использует junction fallback, и доказывает, что user-local lexical path через link в temporary target завершается `DIAGNOSTIC_EVIDENCE_ROOT_NOT_ALLOWED` до runner. Этот test прошёл в CLI suite.

## B-02 — staging, seal, read-back и atomic publication

**Pass.** Terminal JSON записывается только в sibling `.staging-<token>`. До publish выполняются closed-schema validation/scanner, exact read-back validation, SHA-256 `.sealed` и exact two-file shape validation. Final `<token>` создаётся только через `Directory.Move` из того же date parent; existing final root не перезаписывается. Ошибка `IOException`/`UnauthorizedAccessException` возвращает terminal persistence failure; cancellation propagates; `finally` best-effort удаляет только physical staging directory.

Targeted FileSystem tests прошли для fault immediately after terminal write и cancellation before seal: нет final 40-hex root и нет stale `.staging-*`. Normal read-back принимает ровно `.sealed` + `diagnostic-terminal.json` и отвергает tampered seal.

## Privacy, route and terminal boundaries

**Pass.** Durable `SchemaDiagnosticTerminalEvidence/v1` имеет exact five-field allowlist (`schema`, `targetAlias`, `route`, `outcome`, `failedShape`). `failedShape` состоит только из closed enum/bucket fields; URL, credentials, cookies, CSRF, login response, raw schema/lookup data, identifiers и arbitrary blocker/recovery strings в serializable contract не передаются. Scanner выполняется до write; validator выполняется перед write и на read-back.

**Pass.** `CatalogSchemaDiagnoseCommand` принимает только `--target <safe-alias> --manual --live [--evidence-root <absolute-user-local-path>]`; secret-bearing arguments, retry/rerun и `--output-root` не допускаются до runner. Bounded source вызывает ровно одну schema traversal и завершает completed traversal non-success `SCHEMA_DIAGNOSTIC_COMPLETED_WITHOUT_BLOCKER`. Статический architecture test и inspection не нашли достижимых lookup/`SELECT_QUERY`, Pass B/Pass C, reconciliation, `QualifiedCatalogSnapshot`, Excel/workbook, normal run store/pair publisher или publication path. Full qualification safeguards остаются в отдельном production route.

## Команды и результаты

```powershell
dotnet build src/BpmSoftSync.Cli/BpmSoftSync.Cli.csproj -c Release --no-restore
```

Result: exit `0`, warnings `0`, errors `0`.

```powershell
dotnet run --project tests/BpmSoftSync.Domain.Tests/BpmSoftSync.Domain.Tests.csproj -c Release --no-build
dotnet run --project tests/BpmSoftSync.Application.Tests/BpmSoftSync.Application.Tests.csproj -c Release --no-build
dotnet run --project tests/BpmSoftSync.Adapters.BpmSoft.Tests/BpmSoftSync.Adapters.BpmSoft.Tests.csproj -c Release --no-build
dotnet run --project tests/BpmSoftSync.Adapters.FileSystem.Tests/BpmSoftSync.Adapters.FileSystem.Tests.csproj -c Release --no-build
dotnet run --project tests/BpmSoftSync.Adapters.Excel.Tests/BpmSoftSync.Adapters.Excel.Tests.csproj -c Release --no-build
dotnet run --project tests/BpmSoftSync.Cli.Tests/BpmSoftSync.Cli.Tests.csproj -c Release --no-build
git diff --check
```

Result: все шесть executable suites завершились exit `0`; `git diff --check` exit `0`. FileSystem suite включает closed-schema/secret scanner/read-back/seal и injected fault/cancellation test; CLI suite включает exact admission, path/root rejection и symlink/junction escape test. Excel suite прошёл только на synthetic/offline input; он не создавал live output.

## Остаточное ограничение

Проверка физического пути является fail-closed для наблюдаемого filesystem state, однако .NET path API и `Directory.Move` не дают отдельного OS-level no-follow handle для adversarial concurrent local filesystem actor. Между ancestor check и create/move остаётся обычное TOCTOU окно локального hostile actor. В рамках stated user-local, non-hostile operator model это не блокирует bounded diagnostic; не следует представлять его как криптографическую или kernel-enforced гарантию.

## Вердикт и следующий gate

**Pass для передачи новому независимому reviewer.** B-01 и B-02 закрывают именно ранее зафиксированные admission blockers; offline evidence достаточна, чтобы новый reviewer мог рассмотреть и, при собственном Pass, разрешить **ровно один** controlled live bounded schema diagnostic.

Это не является самопринятием и не разрешает full qualification, retry, Pass B/Pass C, lookup/`SELECT_QUERY`, reconciliation, snapshot или Excel publication. Новый reviewer обязан отдельно подтвердить spec/plan/tasks/current hypothesis и ограничение одного запуска; только затем оркестратор может выполнить live probe с уже предоставленными пользователем credentials в interactive process.
