# Зависимости features и будущий путь выполнения

## Последовательность и передаваемые результаты

| Feature | Entry criteria | Основной результат | Exit criteria до следующей feature |
| --- | --- | --- | --- |
| 001 `read-only-catalog-qualification` | согласованные constitution/project; отдельное полномочие для live run только когда оно реально потребуется | независимые domain/identity/blocker и run/audit/evidence contracts; deny-by-default reader; qualification result | два прохода reconciled либо принятый человеком finite blocker list; zero writes; secret/schema checks |
| 002 `workbook-pair-control` | результат 001 и workbook contract authority | validated atomic pair, baseline metadata, `S_*`/conflict result, workbook evidence | source projection, OOXML/parser/Excel/scale gates PASS либо explicit blocker |
| 003 `compare-read-only-plan` | validated pair 002 + current read-only target state 001 | safe report + exact immutable plan, bindings, stable order, browser sample material | report/plan reconciliation, determinism, staleness checks, zero writes |
| 004 `gated-apply-operations` | exact plan 003; owner allowlist; per-kind accepted evidence; manual backup/whole-plan decisions | gated execution, full CLI read-back, browser outcome, optional workbook-only writeback, Git result и clean-machine acceptance | Git success только при `VERIFIED` + all applicable gates PASS + explicit human L2 outcome; `VERIFICATION_PENDING` допускает writeback, но не commit |

```text
001 qualified read + shared audit/evidence
  -> 002 validated workbook pair + snapshot/baseline
    -> 003 safe report + immutable plan + browser sample
      -> 004 gated Apply -> full CLI read-back
         -> optional workbook-only writeback (`VERIFIED` or `VERIFICATION_PENDING`)
         -> browser outcome: `VERIFIED` | `VERIFICATION_PENDING` | `VERIFICATION_FAILED`
         -> only `VERIFIED` + explicit human success confirmation -> atomic Git
```

001 не зависит от 004: общий механизм audit/evidence создаётся в 001 и повторно используется 002–004. Canonical domain core и adapters разделяются с первого этапа согласно `NFR-009`.

## Проверенная матрица связей r002

Все producer results ниже — **ожидаемые будущие контракты**, а не существующий код,
API или evidence. Сейчас реализованы только `spec.md`, checklists и run-артефакты.
Consumer не может считать связь выполненной до фактического predecessor result и
проверки его контракта.

| Source и producer | Consumer | Передаваемая capability / данные / identity | Ограничения совместимости и evidence до использования | Будущая интеграционная и регрессионная проверка | Статус |
| --- | --- | --- | --- | --- | --- |
| `001` (`FR-003`–`FR-014`, `FR-019`; `OPS-001`–`OPS-003`) | `002` (`FR-001`–`FR-003`, `FR-016`) | Qualified read result: typed inventory, package-layer identity, target fingerprint, stable ordering, `RunId` и sanitized audit/evidence root. | Только фактический qualified result или принятый finite blocker list; no raw values/secrets; full-catalog/scale не выводятся из bounded fixture. | 002 повторяет reader/identity/audit checks, сверяет source projection 1:1, pair baseline и per-run evidence; 0 BPMSoft writes. | Planned dependency; producer/consumer implementation отсутствует. |
| `001` (`FR-001`–`FR-014`, `FR-019`; `OPS-001`–`OPS-003`) | `003` (`FR-001`, `FR-015`, `FR-019`) | Current read-only target state, typed identity, target fingerprint и общий run/audit/evidence mechanism. | Read allowlist и deny-by-default сохраняются; unknown shape/qualification blocker не маскируется compare; HTML/plan не меняют domain semantics. | 003 повторяет read-only endpoint/identity/audit checks и доказывает 0 writes вместе с plan/report reconciliation. | Planned dependency; producer/consumer implementation отсутствует. |
| `002` (`FR-001`–`FR-015`, `FR-019`) | `003` (`FR-001`–`FR-005`) | Validated atomic workbook pair: `PairId`, `PullRunId`, baseline/template/fingerprint hashes, canonical rows и parser outcome. | Обе книги валидируются как одна пара; server ID, `DraftRowToken` и `RecordId` не взаимозаменяемы; tamper, mismatch или scale blocker прекращают compare. | 003 повторяет pair/parser checks, mutations bound fields invalidируют plan, permutations не меняют plan bytes/hash/order. | Planned dependency; producer/consumer implementation отсутствует. |
| `001` (`FR-012`–`FR-016`, `FR-019`; `OPS-001`–`OPS-003`, `OPS-012`–`OPS-013`) | `004` (`FR-021`–`FR-025`) | Shared run/audit/evidence and early operator-handoff mechanism, secret scan и independent domain boundary. | Не считается live/write admission; credentials остаются interactive; evidence schema не допускает secrets/raw values; 001 не зависит от 004. | 004 повторяет evidence/secret scans и связывает каждый applicable stable key с итоговым audit/read-back/browser/Git evidence. | Planned dependency; producer/consumer implementation отсутствует. |
| `002` (`FR-016`–`FR-019`) | `004` (`FR-004`, `FR-011`, `FR-023`–`FR-025`) | Pair bindings, template/baseline/content hashes и allowlisted future workbook-only cells. | Pair и hashes должны оставаться exact; workbook change invalidates admission/writeback; writeback — local-only, 0 BPMSoft calls и разрешён после successful CLI read-back при `VERIFIED` или `VERIFICATION_PENDING`; pending/failed browser блокирует Git. | 004 повторяет pair validation до Apply, tests cell diff/changed-input block, zero BPMSoft calls during writeback, writeback-under-pending и запрет commit до `VERIFIED`. | Planned dependency; producer/consumer implementation отсутствует. |
| `003` (`FR-007`–`FR-018`; `OPS-005`) | `004` (`FR-004`, `FR-014`–`FR-016`, `FR-024`) | Exact immutable canonical plan/report bytes, schema/version, fixed `RunContext`, `SamplingBasisHash`, `PlanHash`, bindings, stable operation order, blockers и stored deterministic browser sample keys. | 004 допускает только known-version canonical plan, validates context/basis/ranks/stored sample/`PlanHash`, rejects unknown/duplicate/non-canonical content, не пересчитывает diff и не выбирает новый sample; file SHA-256 is audit-only, not product identity. | Validation-only admission rejects byte/context/schema/binding/sample mutations before dispatch; full CLI read-back precedes use of the exact stored sample, 100% browser acceptance and Git success. | Planned dependency; producer/consumer implementation отсутствует. |

Матрица ациклична: все связи направлены от меньшего этапа к большему, а 001 не
имеет обратной зависимости от 002–004. `OPS-001`…`OPS-013` сохраняют владельцев,
потребителей, первое применение и финальную проверку из `traceability.md`; 005 не
создаётся.

## Накопительная регрессия

- 002 обязана повторять reader/identity/audit checks 001 для materialized source.
- 003 обязана повторять pair/parser/read-only checks 001–002 и не иметь write transport.
- 004 обязана повторять exact plan/binding/identity/audit checks 001–003 перед первым write.
- Успешный Git требует полного CLI read-back, browser status `VERIFIED` и последующего explicit human success confirmation; browser evidence не заменяет CLI. `VERIFICATION_PENDING` разрешает только optional workbook-only writeback и требует уведомить оператора/предложить ручную проверку или помощь; `VERIFICATION_FAILED` ведёт к диагностике и отдельному решению об исправлении либо откате без автоматического rollback.
- Каждая последующая feature обновляет operator runbook/skills contract без дублирования business logic.

## Будущие SDD-артефакты и gates

Для каждой feature после отдельного `/SpecKit Clarify` потребуются соответствующие L2 `/SpecKit Plan` outputs (`plan.md`, `research.md`, `data-model.md` при структурированных данных, `quickstart.md`, contracts при необходимости), затем разрешённые workflow steps. Эти файлы роль 01 не создавала.

Известный workflow blocker: `.specify/workflows/speckit/class-workflow.yml` и overlay `l2-pilot` ставят `/SpecKit Analyze` перед `/SpecKit Tasks`, но установленный `.agents/skills/speckit-analyze/SKILL.md` требует успешно созданный `tasks.md` и запускает prerequisites с `-RequireTasks`. Нельзя менять порядок или создавать фиктивный `tasks.md`; до продолжения полного lifecycle требуется явное исправление workflow/skill в отдельном решении.

## Реальное и будущее

- Реально сейчас: четыре `spec.md`, четыре `checklists/requirements.md`, traceability/dependency/handoff artifacts и Jira skipped traces.
- Будущее: CLI, skills продукта, adapters, tests, evidence schemas, workbook generator/parser, compare/plan, Apply, browser verification, packaging и Git workflow.
- Наличие specification/checklist не означает implementation, verification, live qualification или owner approval write operations.

## Project memory

Будущий `speckit.archive.run` после successful Implement/Converge/Verify обязан обновить `.specify/memory/spec.md`, `.specify/memory/plan.md` и `.specify/memory/changelog.md` полным scope. Роль 01 не выполняла Archive и не изменяла project memory.
