# Cycle 1 — независимая validation durable safe evidence

**Дата:** 2026-09-15
**Роль:** independent test/validation worker
**Модель:** `gpt-5.6-terra` / `high`
**Вердикт:** **Fail — новый live diagnostic не допускается.**

Этот документ не является review, acceptance или разрешением на live attempt.

## Граница и метод

Проверены `AGENTS.md`, active feature в `.specify/feature.json`, Feature 001
`HANDOFF.md`, `spec.md`, `plan.md`, `tasks.md`, CLI contract, `test-plan.md`,
live-verification prompt, текущие Cycle 1 reports, diff и production/test
surface bounded diagnostic. Active feature подтверждён:
`001-read-only-catalog-qualification`.

Не выполнялись live BPMSoft/network/auth/browser calls, credential use,
`Write`, `Manage`, `Compare`, `Apply`, SQL mutation, delete, commit или push.
В этом отчёте нет URL, login, password, cookie, CSRF, login response либо иных
секретов.

## Выполненные offline checks

| Команда | Результат |
| --- | --- |
| `dotnet build BpmSoftSync.sln -c Release --no-restore` | exit `0`; warnings `0`, errors `0`. |
| `dotnet run --project tests/BpmSoftSync.Domain.Tests/BpmSoftSync.Domain.Tests.csproj -c Release --no-build` | exit `0`. |
| `dotnet run --project tests/BpmSoftSync.Application.Tests/BpmSoftSync.Application.Tests.csproj -c Release --no-build` | exit `0`. |
| `dotnet run --project tests/BpmSoftSync.Adapters.BpmSoft.Tests/BpmSoftSync.Adapters.BpmSoft.Tests.csproj -c Release --no-build` | exit `0`. |
| `dotnet run --project tests/BpmSoftSync.Adapters.FileSystem.Tests/BpmSoftSync.Adapters.FileSystem.Tests.csproj -c Release --no-build` | exit `0`. |
| `dotnet run --project tests/BpmSoftSync.Adapters.Excel.Tests/BpmSoftSync.Adapters.Excel.Tests.csproj -c Release --no-build` | exit `0`. |
| `dotnet run --project tests/BpmSoftSync.Cli.Tests/BpmSoftSync.Cli.Tests.csproj -c Release --no-build` | exit `0`. |
| `git diff --check` | exit `0`; whitespace errors отсутствуют. |

## Подтверждено

- `SchemaDiagnosticTerminalEvidence/v1` имеет закрытый exact allowlist из
  `schema`, `targetAlias`, `route`, `outcome`, `failedShape`. Валидатор
  отклоняет дополнительные поля, неизвестные enum и невалидный alias до
  durable write. В record нет свободного reason/recovery text, URL, response,
  identity, value или session fields.
- `AppendOnlySchemaDiagnosticEvidenceStore` serializes only этот record,
  выполняет scanner и schema validation до write, затем read-back JSON,
  validation и SHA-256 seal. Targeted FileSystem test подтверждает unique
  opaque 40-hex roots, ровно `diagnostic-terminal.json` и `.sealed`, а также
  rejection add-field/raw-value and secret canaries и tampered seal.
- CLI принимает только `catalog diagnose-schema --target <safe-alias> --manual
  --live [--evidence-root <absolute-user-local-path>]`; secrets, retry и
  `--output-root` отклоняются до runner. Default и lexical explicit path
  проверяются как nested under the user profile; relative, workspace,
  temporary, Windows/system и volume-root paths covered by tests are rejected.
- Runtime graph retains one bounded source call. Static architecture test plus
  source inspection exclude lookup, `SELECT_QUERY`, qualification,
  reconciliation, snapshot, Excel, normal `AppendOnlyRunStore`, Pass B/Pass C,
  retry and publication. `CompletedWithoutBlocker` remains a non-success.
- Evidence store is reachable only from `CatalogSchemaDiagnoseCommand` after
  bounded runner terminal result; it has no credential, transport or normal
  output-root parameter. Existing full qualification code remains a separate
  surface.

## Admission blockers

### B-01 — user-local root can escape through a reparse point

`SchemaDiagnosticEvidenceRoot.TryResolve` relies on `Path.GetFullPath` and
string-prefix checks. It neither rejects nor resolves Windows reparse
points/junctions/symlinks for the selected root or its existing ancestors.
Therefore an apparently user-local path can point physically to workspace,
temporary or system storage and pass the lexical `IsNested(candidate, user)`
check. This defeats the documented policy that evidence root is outside those
locations. Current root tests cover only literal paths; no test covers a
reparse-point escape.

Required remediation proof: fail closed on any selected-root/ancestor reparse
point, or resolve every existing link target before applying the user-local and
deny-root policy; add an offline junction/symlink characterization test. The
test must not use a live target or credentials.

### B-02 — persistence can leave an unsealed partial terminal record

`SealTerminalAsync` creates the final root and writes
`diagnostic-terminal.json` before creating `.sealed`. If read-back or seal
write throws/cancels, it returns/propagates failure but does not remove or
quarantine the already durable terminal record. No staging/publish protocol or
fault-injection test proves that an unsuccessful persistence call leaves no
visible unsealed diagnostic tree. Consequently the claimed invariant of a
unique **sealed** evidence root with exactly two files is not established for
failure paths.

Required remediation proof: use a recoverable staged artifact with atomic
publish, or an equivalent fail-closed protocol that prevents a final visible
unsealed root; add fault/cancellation tests after terminal write and before or
during seal write. Preserve the two-file final shape and no-delete constraint
for existing evidence.

## Limitations

- The scanner and exact schema make raw data leakage through the current record
  unlikely; this is offline structural proof, not proof of production BPMSoft
  response semantics.
- Target alias is caller-supplied and constrained, not a raw server identity.
- This validation did not run a real process or live admission, so it does not
  claim a live attempt, a qualified catalog, Pass A/B or Excel output.

## Decision

The durable safe-evidence implementation has substantial positive offline
coverage, but B-01 and B-02 prevent it from meeting the user-local sealed
evidence boundary. A next independent gate reviewer **must not** consider even
one live diagnostic attempt yet. After an implementation worker resolves both
blockers and a fresh independent validation passes, a separate reviewer may
consider exactly one human-authorized bounded live diagnostic; it remains
non-qualifying and may not invoke lookup, `SELECT_QUERY`, Pass B/Pass C,
reconciliation, snapshot, Excel, retry/rerun or any write-oriented operation.
