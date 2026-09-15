# Матрица source → target для run `20260907-165314-7f12`

## Проверка связности r002

Роль 02 проверила, что квалифицированные source references, назначенные владельцы
и предметный смысл всех 93 нумерованных требований не менялись. Изменены только
явные связи между feature в `feature-dependencies.md` и постоянные правила
накопительной разработки в `AGENTS.md`. `OPS-001`…`OPS-003` остаются механизмом
001 с consumers 002–004; `OPS-005` остаётся material responsibility 003 и
execution/acceptance responsibility 004; `OPS-004`, `OPS-006`…`OPS-011` принадлежат
004, `OPS-012` — mechanism owner 001, `OPS-013` — early owner 001 и final acceptance
004. Ни одна ранняя safety-check обязанность не зависит от позднего этапа.

Все связи имеют статус planned: в проекте нет реализации CLI, adapters, product
skills, tests, evidence schemas или live qualification. Поэтому presence spec и
checklist не является evidence реализации, verification либо approval.

Статус матрицы: `COMPLETE_FOR_ROLE_01`. Все пути-источники ниже находятся в неизменяемом `preparation/docs/product-specs/` либо указывают на отдельно неизменяемый workbook contract. `Перенесено` означает сохранение требования в specification и acceptance/check, а не реализацию или live qualification.

Сокращения целей: `001 FR-n/SC-n` = `specs/001-read-only-catalog-qualification/spec.md`; аналогично `002`, `003`, `004`. В колонке source ID всегда квалифицирован именем исходного файла.

## Общее видение: функциональные требования

| Source | Владелец / потребители → target requirement | Acceptance / check | Статус |
| --- | --- | --- | --- |
| `local-bpmsoft-synchronizer/spec.md:FR-001` | 001 FR-017; все функции следуют иерархии | 001 SC-007; legacy disposition review | Перенесено |
| `spec.md:FR-002` | 001 FR-001–002; 003 FR-001 | 001 SC-001; endpoint capture | Перенесено |
| `spec.md:FR-003` | 001 FR-003–004 | 001 SC-002/SC-004 | Перенесено |
| `spec.md:FR-004` | 001 FR-006–008 | 001 SC-003 | Перенесено |
| `spec.md:FR-005` | 001 FR-010 | 001 SC-002/SC-003; human qualification decision | Перенесено |
| `spec.md:FR-006` | 002 FR-001–003 | 002 SC-001/SC-002/SC-004 | Перенесено |
| `spec.md:FR-007` | 002 FR-009–010 | 002 SC-005 | Перенесено |
| `spec.md:FR-008` | 002 FR-004–008; 003 FR-004 | 002 SC-003; blocker matrix | Перенесено |
| `spec.md:FR-009` | 003 FR-001–007 | 003 SC-001/SC-002 | Перенесено |
| `spec.md:FR-010` | 003 FR-008–010; 004 FR-004 | 003 SC-004; admission mutation matrix | Перенесено |
| `spec.md:FR-011` | 001 FR-005; 002 FR-006–008; 003 FR-003; 004 FR-008–009 | identity fixtures; 004 SC-003 | Перенесено |
| `spec.md:FR-012` | 003 FR-006/FR-008/FR-009 | fixed `RunContext`; permutations при одном context дают одинаковые canonical bytes/`PlanHash`/order; новый context намеренно создаёт новый plan instance | Перенесено; RF-003 исправлено решением владельца 2026-09-08 |
| `spec.md:FR-013` | 004 FR-003–004 | 004 SC-001; human records | Перенесено |
| `spec.md:FR-014` | 004 FR-005–006 | 004 SC-002; fault injection | Перенесено |
| `spec.md:FR-015` | 004 FR-008–009 | 004 SC-003; ID proof fixtures | Перенесено |
| `spec.md:FR-016` | 004 FR-007/FR-010 | 004 SC-003; staged/full read-back | Перенесено |
| `spec.md:FR-017` | 002 FR-019 contract; 004 FR-011 execution | 004 SC-005; cell diff/0 BPMSoft calls; writeback разрешён после successful CLI read-back при `VERIFICATION_PENDING`, но не разрешает Git | Перенесено; RF-004 уточнено решением владельца 2026-09-08 |
| `spec.md:FR-018` | 003 FR-014 material; 004 FR-013–016 execution | 004 SC-004 | Перенесено |
| `spec.md:FR-019` | 001 FR-012–014 owner; 002 FR-016, 003 FR-015, 004 FR-023 consumers | 001 SC-005/SC-006; per-run schema/scan | Перенесено |
| `spec.md:FR-020` | 004 FR-017–020 | 004 SC-006/SC-007 | Перенесено |
| `spec.md:FR-021` | 001 FR-015 owner; 002 FR-017, 003 FR-016, 004 FR-021 consumers | fake-CLI/static skill checks; 004 SC-009 | Перенесено |
| `spec.md:FR-022` | 001 FR-009 owner; 002 FR-011–012, 003 FR-004, 004 FR-006 consumers | 002 SC-006; zero index operation/endpoint | Перенесено |

## Общее видение: нефункциональные требования

| Source | Владелец / потребители → target requirement | Acceptance / check | Статус |
| --- | --- | --- | --- |
| `spec.md:NFR-001` | 001 FR-001/FR-014 owner; все функции используют scanner | 001 SC-005; 004 SC-008 | Перенесено |
| `spec.md:NFR-002` | 001 FR-003–004; 002 FR-003; 003 FR-006–009/FR-014 | 003 SC-003/SC-006 | Перенесено |
| `spec.md:NFR-003` | 001 FR-008/FR-018; 002 FR-005; 003 FR-010–013; 004 FR-004–012 | negative fixtures; 004 SC-002 | Перенесено |
| `spec.md:NFR-004` | 001 FR-006–008; 002 FR-011; 003 FR-012 | 001 SC-003; 002 SC-001 | Перенесено |
| `spec.md:NFR-005` | 001 FR-004; 003 FR-011; 004 FR-012 | repeat/no-op fixtures | Перенесено |
| `spec.md:NFR-006` | 001 FR-012–014 owner; 003 FR-015; 004 FR-023–024 | plan→read-back→browser/Git reconciliation | Перенесено |
| `spec.md:NFR-007` | 001 FR-016 early; 004 FR-022 final | 004 SC-009 clean-machine acceptance | Перенесено |
| `spec.md:NFR-008` | 001 FR-018; 002 FR-017; 003 FR-016; 004 FR-021–022 | diagnostic/skill/runbook checks | Перенесено |
| `spec.md:NFR-009` | 001 FR-019 owns boundary from start; 002 FR-003, 003 FR-019, 004 FR-024 consume | dependency-direction/static architecture check | Перенесено |

## Этап 01: чтение и каталог

| Source | Target | Acceptance / check | Статус |
| --- | --- | --- | --- |
| `01-read-only-catalog-spec.md:READ-001` | 001 FR-001 | 001 SC-005 | Перенесено |
| `01-read-only-catalog-spec.md:READ-002` | 001 FR-002 | 001 SC-001 | Перенесено |
| `01-read-only-catalog-spec.md:READ-003` | 001 FR-003 | 001 SC-004 | Перенесено |
| `01-read-only-catalog-spec.md:READ-004` | 001 FR-004 | 001 SC-002 | Перенесено |
| `01-read-only-catalog-spec.md:READ-005` | 001 FR-005 | identity/layer fixtures | Перенесено |
| `01-read-only-catalog-spec.md:READ-006` | 001 FR-006 | 001 SC-003 | Перенесено |
| `01-read-only-catalog-spec.md:READ-007` | 001 FR-007 | supported-scope inventory review | Перенесено |
| `01-read-only-catalog-spec.md:READ-008` | 001 FR-008 | unknown-shape fixtures | Перенесено |
| `01-read-only-catalog-spec.md:READ-009` | 001 FR-009 | index relation fixtures | Перенесено |
| `01-read-only-catalog-spec.md:READ-010` | 001 FR-010 | 001 SC-002–004 | Перенесено |
| `01-read-only-catalog-spec.md:READ-011` | 001 FR-008 | bounded blocker/research decision | Перенесено |
| `01-read-only-catalog-spec.md:READ-012` | 001 FR-011 | fingerprint/staleness review | Перенесено |

## Этап 02: workbook pair

| Source | Target | Acceptance / check | Статус |
| --- | --- | --- | --- |
| `02-workbook-pair-spec.md:WB-001` | 002 FR-001/FR-014–015 | exact contract + 002 SC-004 | Перенесено |
| `02-workbook-pair-spec.md:WB-002` | 002 FR-002 | 002 SC-002 | Перенесено |
| `02-workbook-pair-spec.md:WB-003` | 002 FR-003 | deterministic round-trip | Перенесено |
| `02-workbook-pair-spec.md:WB-004` | 002 FR-004 | protection/tamper fixtures | Перенесено |
| `02-workbook-pair-spec.md:WB-005` | 002 FR-005 | 002 SC-003 | Перенесено |
| `02-workbook-pair-spec.md:WB-006` | 002 FR-006 | group/identity fixtures | Перенесено |
| `02-workbook-pair-spec.md:WB-007` | 002 FR-007 | reference precedence fixtures | Перенесено |
| `02-workbook-pair-spec.md:WB-008` | 002 FR-008 | token creation/tamper fixtures | Перенесено |
| `02-workbook-pair-spec.md:WB-009` | 002 FR-009 | 002 SC-005 | Перенесено |
| `02-workbook-pair-spec.md:WB-010` | 002 FR-010 | refresh conflict fixtures | Перенесено |
| `02-workbook-pair-spec.md:WB-011` | 002 FR-011 | 002 SC-006 | Перенесено |
| `02-workbook-pair-spec.md:WB-012` | 002 FR-012 | zero index plan/load/apply | Перенесено |
| `02-workbook-pair-spec.md:WB-013` | 002 FR-013 | 002 SC-001/SC-004 | Перенесено |

## Этап 03: compare и plan

| Source | Target | Acceptance / check | Статус |
| --- | --- | --- | --- |
| `03-compare-plan-spec.md:CMP-001` | 003 FR-001 | 003 SC-001 | Перенесено |
| `03-compare-plan-spec.md:CMP-002` | 003 FR-002 | field/no-op fixtures | Перенесено |
| `03-compare-plan-spec.md:CMP-003` | 003 FR-003 | identity fixtures | Перенесено |
| `03-compare-plan-spec.md:CMP-004` | 003 FR-004/FR-013 | blocker matrix | Перенесено |
| `03-compare-plan-spec.md:CMP-005` | 003 FR-005 | typed value fixtures | Перенесено |
| `03-compare-plan-spec.md:CMP-006` | 003 FR-006 | 003 SC-003 | Перенесено |
| `03-compare-plan-spec.md:CMP-007` | 003 FR-007 | 003 SC-002 | Перенесено |
| `03-compare-plan-spec.md:CMP-008` | 003 FR-008/FR-014 | fixed `RunContext`, binding/sample validation | Перенесено; RF-003 исправлено решением владельца 2026-09-08 |
| `03-compare-plan-spec.md:CMP-009` | 003 FR-009; 004 FR-004 | one canonical serialization; `PlanHash` covers fixed context + complete semantic content except its own field; no product `PlanArtifactHash` | Перенесено; RF-003 исправлено решением владельца 2026-09-08 |
| `03-compare-plan-spec.md:CMP-010` | 003 FR-010 | 003 SC-004 | Перенесено |
| `03-compare-plan-spec.md:CMP-011` | 003 FR-011 | explicit no-op fixture | Перенесено |
| `03-compare-plan-spec.md:CMP-012` | 003 FR-012 | 003 SC-007 | Перенесено |

## Этап 04: Apply

| Source | Target | Acceptance / check | Статус |
| --- | --- | --- | --- |
| `04-gated-apply-spec.md:APPLY-001` | 004 FR-001 | per-kind package review | Перенесено |
| `04-gated-apply-spec.md:APPLY-002` | 004 FR-002 | required outcome matrix | Перенесено |
| `04-gated-apply-spec.md:APPLY-003` | 004 FR-003 | 004 SC-001 | Перенесено |
| `04-gated-apply-spec.md:APPLY-004` | 004 FR-004 | admission mutation checks | Перенесено |
| `04-gated-apply-spec.md:APPLY-005` | 004 FR-005 | 004 SC-002 | Перенесено |
| `04-gated-apply-spec.md:APPLY-006` | 004 FR-006 | endpoint inventory | Перенесено |
| `04-gated-apply-spec.md:APPLY-007` | 004 FR-007 | stage failure fixtures | Перенесено |
| `04-gated-apply-spec.md:APPLY-008` | 004 FR-008 | 004 SC-003 | Перенесено |
| `04-gated-apply-spec.md:APPLY-009` | 004 FR-009 | ID mapping fixtures | Перенесено |
| `04-gated-apply-spec.md:APPLY-010` | 004 FR-010 | full affected-state reconciliation | Перенесено |
| `04-gated-apply-spec.md:APPLY-011` | 004 FR-011 | 004 SC-005 | Перенесено |
| `04-gated-apply-spec.md:APPLY-012` | 004 FR-012 | retry/new-plan fixtures | Перенесено |

## Сквозной этап 05, распределённый между 001–004

| Source | Основной владелец / потребители | Первое применение / финальная проверка | Статус |
| --- | --- | --- | --- |
| `05-verification-operations-spec.md:OPS-001` | 001 FR-012; 002 FR-016, 003 FR-015, 004 FR-023 | первый read run / clean-machine full run | Перенесено |
| `05-verification-operations-spec.md:OPS-002` | 001 FR-013; 002/003/004 audit consumers | первый read run / 004 evidence matrix | Перенесено |
| `05-verification-operations-spec.md:OPS-003` | 001 FR-014; все consumers | первый persisted artifact / 004 SC-008 | Перенесено |
| `05-verification-operations-spec.md:OPS-004` | 004 FR-013 | после full CLI read-back / browser session review | Перенесено |
| `05-verification-operations-spec.md:OPS-005` | 003 FR-014 material owner; 004 FR-014 execution | two-stage `SamplingBasisHash` from canonical plan payload without sample → rank by basis + canonical kind + stable key → stored sample → final `PlanHash`; 004 validates and uses exact keys without resampling | Перенесено; RF-002 исправлено решением владельца 2026-09-08 |
| `05-verification-operations-spec.md:OPS-006` | 004 FR-015 | tooling/login/session inability → `VERIFICATION_PENDING` + помощь/ручная проверка; подтверждённый UI mismatch → `VERIFICATION_FAILED`; только `VERIFIED` + последующее human confirmation разрешают Git; auto rollback отсутствует | Перенесено; RF-004 исправлено решением владельца 2026-09-08 |
| `05-verification-operations-spec.md:OPS-007` | 004 FR-016 | CLI read-back до browser / Git gate | Перенесено |
| `05-verification-operations-spec.md:OPS-008` | 004 FR-017 | Git setup / credentials scan | Перенесено |
| `05-verification-operations-spec.md:OPS-009` | 004 FR-018 | final success / 004 SC-006 | Перенесено |
| `05-verification-operations-spec.md:OPS-010` | 004 FR-019 | conflict/network path / 004 SC-007 | Перенесено |
| `05-verification-operations-spec.md:OPS-011` | 004 FR-020 | partial result / zero commit | Перенесено |
| `05-verification-operations-spec.md:OPS-012` | 001 FR-015 mechanism owner; 002 FR-017, 003 FR-016, 004 FR-021 | early setup/read / complete skill suite | Перенесено |
| `05-verification-operations-spec.md:OPS-013` | 001 FR-016 early, 002 FR-018, 003 FR-017, 004 FR-022 final | early handoff / clean-machine acceptance | Перенесено |

## Ненумерованные требования, риски, ошибки, exclusions и приёмка

| Source section | Target / смысл | Check / статус |
| --- | --- | --- |
| `spec.md §§2–5` users, end-to-end flow, scope/out-of-scope | sequence и boundaries распределены 001→002→003→004; общий цикл заканчивается в 004 | Все четыре specs; Перенесено |
| `spec.md §9` восемь MVP criteria | 004 FR-025 + cumulative SC; full catalog остаётся обязанностью 001/002 | Перенесено, не выполнено |
| `01-read-only-catalog-spec.md` error states | 001 edge cases: paging, layer ambiguity, unknown shape, workbook scale | Negative fixtures; Перенесено |
| `02-workbook-pair-spec.md` acceptance/out-of-scope | 002 scenarios/SC/assumptions, включая no Google/delete/write | Contract/parser/Excel checks; Перенесено |
| `03-compare-plan-spec.md` named blockers/out-of-scope | 003 FR-013, edge cases, no Apply/partial approval | Blocker matrix; Перенесено |
| `04-gated-apply-spec.md` prerequisites/candidate kinds/out-of-scope | 004 stories 1–3, edge cases, default `DENIED` statuses | Allowlist/preflight/fault gates; Перенесено, не одобрено |
| `05-verification-operations-spec.md` acceptance/out-of-scope | 003 sample material; 004 browser/Git/clean-machine; no agent acceptance | Property/Git/secret/human tests; Перенесено |
| `WORKBOOK_CONTRACT_VISION.md §§1–7` exact pair/sheets/identity/pull/compare/writeback | 002 FR-001–015/FR-019; 003 bindings; 004 writeback | Exact contract + round-trip/cell diff; Перенесено |
| `WORKBOOK_CONTRACT_VISION.md §8` open risks | exact API, indexes, scale, snapshot UX, fingerprint, logs, write semantics сохранены как gates/assumptions | Clarify/Plan и human decisions; Перенесено, не закрыто |
| `clarify-review.md §§2–7` decisions/assumptions/blockers | актуальные decisions сохранены; исторический `SLICES_NOT_APPROVED` не переносится как активный, так как constitution 2.0.0 явно разрешила development lifecycle | Source hierarchy applied; finding recorded |
| `.specify/memory/constitution.md` human gates | 001 live read; 004 credentials/backup/plan/allowlist/Git/constitution decisions | Human records required; Перенесено |
| `.specify/project.yml` L2 boundary | pilot scope/success/stop/fallback/human outcome отражены во всех specs | Checklists PASS; Перенесено |

## Итог смысловой сверки

- Все 93 нумерованных source requirements (`FR`/`NFR`/`READ`/`WB`/`CMP`/`APPLY`/`OPS`) имеют target owner, acceptance/check и статус.
- Full-catalog reader qualification и Excel quality/scale остаются обязательствами 001 и 002; bounded evidence не подменяет их.
- `OPS-001…003` имеют одного владельца механизма (001) и consumers 002–004; `OPS-005` разделён на material owner 003 и executor/acceptance 004.
- Никакой отдельной 005 не создано; browser/Git/clean-machine завершение находится в 004.
- Write kinds, endpoints, permissions, fingerprints и evidence не выдуманы; неизвестные live facts представлены gates/default deny.
