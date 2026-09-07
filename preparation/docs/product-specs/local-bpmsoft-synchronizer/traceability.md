# Трассируемость: устаревшее поведение → требование → задача → валидация

Статус: **базовое состояние-кандидат для ревью владельцем**.

## 1. Неизменяемый снимок справочного устаревшего решения

Хеши повторно вычислены только для чтения по снимку текущего рабочего дерева 2026-09-06.

| Файл | Байты | SHA-256 |
|---|---:|---|
| `SyncOM/Макросы.gs` | 711 | `ed6eb47cdcbf2de3bf975f6636df42f2fcef793f77aa88f6cc01e69cf08c476d` |
| `SyncOM/DiagramDownload.html` | 417 | `005f7a7388bdcdd0f12cbc051bc2d21468516283af3eae099c8ac8a59dd12403` |
| `SyncOM/functions.gs` | 79,290 | `7fdb7cb1a5f3b8375673a7e1b4556a2c970c53b544523e6141ca6d663255258c` |
| `SyncOM/ModelFunctions.html` | 141,657 | `d641db4bb36e481ff9e4a18ca60095ae838d78ba6796aa2a39924462e3e06b9c` |
| `SyncOM/ModelFunctionsSettings.html` | 2,059 | `0b98615532af33858284837045d022ddea563459f798e7db4de841d12f1434ea` |
| `SyncOM/NginxConfigBackup.html` | 3,921 | `d42db2dfafcc396505ac330636c6ef0850b05b791f2204a8b99da52aa7f17968` |

Эти хеши идентифицируют только справочный источник для планирования. Будущая T-001 должна зафиксировать их в новом решении и завершаться ошибкой при расхождении исходного кода до ревью перечня.

## 2. Перечень устаревшего поведения и его классификация

| ID | Исторический источник/поведение | Классификация | Текущее требование | Задачи | Валидация/доказательства |
|---|---|---|---|---|---|
| L-001 | `functions.gs:1-56`: меню/настройки Google, учётные данные внедрены в HTML | `rewrite` | FR-002, FR-021, READ-001/002, OPS-012 | T-001, T-007, T-030 | V-SEC-01, V-ENDPOINT-01 |
| L-002 | `ModelFunctions.html:32-36`: кандидаты перечня рабочего пространства/пакетов | `reuse-semantics` | FR-001–005, READ-005–007 | T-001, T-009–T-012 | V-READ-01, V-CATALOG-01 |
| L-003 | `ModelFunctions.html:123,264,717`: `GetSchema`, сопоставление собственных/унаследованных/типов/ссылок | `reuse-semantics` | READ-005/007/009, WB-001, FR-011 | T-002, T-004/005, T-009 | V-MAP-01 |
| L-004 | `ModelFunctions.html:139,457,547,631-632`: `SelectQuery` с фиксированными 1000/3000/19000 строками | `rewrite` | FR-003/005, READ-003/004/010 | T-008, T-010–T-012 | V-PAGE-01, V-CATALOG-01 |
| L-005 | `ModelFunctions.html:143-220`: сравнение справочника по Id, затем Code/Name, нестрогое сравнение Boolean/ссылок | `rewrite` | FR-008/011, CMP-003/005, WB-006/007 | T-002, T-005, T-017–T-019 | V-COMPARE-01, V-IDENTITY-01 |
| L-006 | `ModelFunctions.html:94,108,133-134,186,291,298,813,848,897`: случайное поведение, зависящее от лицензии | `drop` | NFR-002/003/005 | T-001, T-006, T-019 | V-DETERMINISM-01 |
| L-007 | `ModelFunctions.html:435,502,595`: `InsertQuery`/`UpdateQuery` справочника, жёстко заданный `CreatedBy` | `defer/rewrite` | FR-013–016, APPLY-001–010 | T-022–T-025 | V-PREFLIGHT-kind, V-FAULT-01 |
| L-008 | `functions.gs:68-104`: запись возвращённых ID строк обратно в Google `!Id` | `rewrite` | FR-015/017, APPLY-008/009/011 | T-025, T-026 | V-IDMAP-01, V-WRITEBACK-01 |
| L-009 | `functions.gs:106-141`: обратная запись флагов обязательности/индекса по именам Object+Field | `drop/rewrite` | FR-011/022, WB-011/012 | T-013, T-014, T-026 | V-IDENTITY-01, V-INDEX-01 |
| L-010 | `ModelFunctions.html:629-732`: поиск/вставка реестра справочников и деструктивная замена связи схемы | `defer/drop` | FR-013–016; APPLY-001–007; без удаления | T-022, T-023; задачи удаления нет | V-ALLOWLIST-01, V-PREFLIGHT-kind |
| L-011 | `ModelFunctions.html:760-977`: поиск ссылки/родителя, создание/назначение/обновление/сохранение схемы | `defer/rewrite` | APPLY-001–010 | T-022–T-025 | V-PREFLIGHT-kind, V-STAGE-01 |
| L-012 | `ModelFunctions.html:1022-1081`: сопоставления типов схем/записей | `reuse-semantics as seed` | NFR-003/009, READ-008, WB-003, CMP-005 | T-002, T-005, T-009, T-017 | V-TYPES-01 |
| L-013 | `ModelFunctions.html:1083-1132`: общий Ajax/сессионный адаптер со смешанным доступом чтения/записи | `rewrite` | FR-002/014, READ-002, APPLY-003/006 | T-007, T-021, T-024 | V-ENDPOINT-01 |
| L-014 | `functions.gs:143-260` и формулы/таблицы Google как сериализация/входные данные | `drop/rewrite` | FR-006–008, WB-001–010 | T-013–T-016 | V-WORKBOOK-01, V-REFRESH-01 |
| L-015 | Вспомогательные средства Diagram/Drive (`functions.gs:271+`, `DiagramDownload.html`) | `drop` | Вне области действия: миграция Google/диаграмм | Только перечень T-001 | V-SCOPE-01 |

## 3. Внешние доказательства → текущие утверждения

| Доказательства | Подтверждаемое утверждение | Явное ограничение |
|---|---|---|
| [20260904T132710Z review](../../READ_ONLY_RESEARCH_RESULTS/20260904T132710Z-02-evidence-review.md) | explicit `Id` order and repeatable paging for `ActivityPriority` (3) and `Lookup` (109); selected schema/registry paths | no catalog-wide completeness; `schema.id` only candidate; bounded shapes |
| [P3-C decision](../../READ_ONLY_RESEARCH_RESULTS/20260904-P3-C-contract-change.md) | `Indexes` read membership via `columnUId`, Own/Inherited target, `IndexUId` | two simple non-auto indexes; no composite/order/write generalization |
| [G4 decision](../../READ_ONLY_RESEARCH_RESULTS/20260904-G4-owner-decision.md) | bounded workbook/tool authorization and ActualIndexed separation gate | no Write/Manage/full-catalog acceptance/index load |
| [Workbook v3 rerun](../../READ_ONLY_RESEARCH_RESULTS/20260905T191555Z-01-workbook-evidence-review-v3-rerun.md) | template v3 bounded contract passes workbook/Excel checks | Tier 2 skipped; production parser/full catalog/write not run |
| [G5 record](../../READ_ONLY_RESEARCH_RESULTS/20260906T064737Z-03-owner-g5-decision-record.md) | owner accepted canonical pair as `VerifiedBoundedBaseline` | no full catalog/write/load/MVP acceptance; index blocker retained |

## 4. Покрытие «требование → задачи → валидация»

| Группа требований | Задачи | ID валидации |
|---|---|---|
| FR-001 legacy/source precedence | T-001, T-002 | V-LEGACY-01, V-SCOPE-01 |
| FR-002–005 read/catalog | T-007–T-012 | V-SEC-01, V-ENDPOINT-01, V-PAGE-01, V-MAP-01, V-CATALOG-01 |
| FR-006–008 workbook/parser/refresh | T-013–T-016 | V-WORKBOOK-01, V-PARSER-01, V-REFRESH-01 |
| FR-009–012 compare/plan | T-017–T-021 | V-COMPARE-01, V-DETERMINISM-01, V-PLAN-01, V-STALE-01 |
| FR-013–017 preflight/Apply/writeback | T-022–T-026 | V-ALLOWLIST-01, V-PREFLIGHT-kind, V-FAULT-01, V-IDMAP-01, V-STAGE-01, V-WRITEBACK-01 |
| FR-018–021 verification/ops/skills | T-027–T-032 | V-AUDIT-01, V-BROWSER-01, V-GIT-01, V-SKILL-01, V-HANDOFF-01 |
| FR-022 indexes | T-009, T-013/014, T-018; no mutation task | V-INDEX-01 |
| NFR-001 security | T-006/007, T-027/030/031 | V-SEC-01 |
| NFR-002/005 determinism/idempotency | T-005/008/014/017/019/020/024/025 | V-DETERMINISM-01, V-IDEMPOTENCY-01 |
| NFR-003/004 fail-closed/lossless | T-006/008/009/010/013/018/021 | V-UNKNOWN-01, V-PARSER-01, V-STALE-01 |
| NFR-006 auditability | T-020, T-024–T-029 | V-TRACE-01, V-AUDIT-01 |
| NFR-007–009 packaging/usability/maintenance | T-003, T-007, T-030–T-032 | V-ARCH-01, V-HANDOFF-01 |

## 5. Определения валидации

| ID | Условие прохождения |
|---|---|
| V-LEGACY-01 | all legacy files hash-match; every material behavior has disposition |
| V-SCOPE-01 | static diff/inventory shows no legacy/Google/diagram behavior accidentally implemented |
| V-SEC-01 | credential/session/raw-value canaries absent from all persisted outputs and skill context |
| V-ENDPOINT-01 | captured calls exactly match mode allowlist; read/compare/browser paths have zero writes |
| V-PAGE-01 | ordered double pass equal; negative paging fixtures all block |
| V-MAP-01 | bounded mapping fixtures reconcile UIds/layers/registry/index relations exactly |
| V-CATALOG-01 | full-catalog two-pass report reconciles totals/hashes/skips/unsupported; independent review |
| V-TYPES-01 | every supported mapping has fixture; unknown type fails closed |
| V-IDENTITY-01 | typed IDs cannot substitute; Name/Code fallback tests fail |
| V-WORKBOOK-01 | exact contract, OOXML closure, round-trip, source projection, Excel no-recovery/usability PASS |
| V-PARSER-01 | every forbidden/tampered/invalid workbook case blocks before plan |
| V-REFRESH-01 | snapshot transaction/failure tests preserve previous snapshot and local unresolved rows |
| V-COMPARE-01 | create/update/no-op/value-state/reference fixtures match expected operations/blockers |
| V-DETERMINISM-01 | randomized input/order yields same canonical output/hash; random branches absent |
| V-PLAN-01 | report and plan operation/blocker sets identical; plan version/schema/hash valid |
| V-STALE-01 | each bound-input mutation yields zero writes and explicit stale/block reason |
| V-ALLOWLIST-01 | human and machine allowlists agree; omissions denied; delete/index absent |
| V-PREFLIGHT-kind | reviewed positive + required negative live evidence for one exact operation kind |
| V-FAULT-01 | injected failure at every position produces no later write and no Git commit |
| V-IDMAP-01 | each new ID has unique response/read-back proof; ambiguity blocks |
| V-STAGE-01 | structural read-back/compile gate prevents lookup stage on failure |
| V-WRITEBACK-01 | unchanged-hash gate and exact allowed-cell diff; zero BPMSoft calls |
| V-INDEX-01 | lossless read-only members, no ActualIndexed inference, zero index plan/endpoint |
| V-AUDIT-01 | unique append-only run artifacts contain versions/gates and pass schema/secret scans |
| V-BROWSER-01 | deterministic required sample; 100% evidence; read-only actions only |
| V-GIT-01 | success/failure/conflict/network fixtures obey commit/push/invalidation rules |
| V-SKILL-01 | skills cannot receive secrets/approve/bypass; fake-CLI dry runs follow gates |
| V-ARCH-01 | project dependency graph follows plan boundaries; clean Release zero warnings |
| V-IDEMPOTENCY-01 | new compare after achieved state yields auditable no-op |
| V-TRACE-01 | every operation key reconciles plan → request → read-back → browser/audit |
| V-HANDOFF-01 | independent clean-machine operator completes accepted scenario and owner records decision |

## 6. Трассируемость блокеров

| Блокер | Требования | Задачи, которые могут его закрыть | Контрольный этап владельца |
|---|---|---|---|
| `FULL_CATALOG_NOT_QUALIFIED` | FR-005, READ-010–012 | T-010–T-012 | read-only run authorization + accepted review |
| `WRITE_ALLOWLIST_UNAPPROVED` | FR-013/014, APPLY-001–004 | T-022 | OD-02/OD-03 |
| `WRITE_SEMANTICS_UNPROVEN` | FR-014–016 | T-023 | OD-05 + per-kind acceptance |
| `DRAFT_TOKEN_ID_UNPROVEN` | FR-011/015, APPLY-008/009 | T-023, T-025 | create-kind evidence acceptance |
| `INDEX_SYNC_UNRESOLVED` | FR-022, WB-011/012 | none in current backlog | separate proof + new explicit decision; otherwise exclusion |
| `SLICES_NOT_APPROVED` | package boundary | no task in this package | OD-01/OD-06 and future slice artifact |
