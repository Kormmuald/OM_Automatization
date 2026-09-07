# Бэклог задач реализации до декомпозиции

Статус: **бэклог-кандидат; спецификация пригодна для исполнения, но выполнение запрещено до появления одобренных владельцем частей работы**.

Этот файл нужен для оценки полноты плана и зависимостей. В отличие от обычного `tasks.md` Spec Kit, задачи пока не связаны с частями реализации, поскольку текущим поручением запрещено их создавать. После отдельного контрольного этапа владельца будущая декомпозиция должна назначить каждую задачу ровно одной части работы либо явно вернуть бэклог на ревью.

## Условные обозначения

- Планируемый корень исходного кода: `preparation/LocalBpmSoftSynchronizer/`.
- `[P]` означает техническую возможность параллельного выполнения после указанных зависимостей, но не разрешение запускать агента.
- Задачи в рабочей среде дополнительно требуют именованных полномочий владельца; завершение задачи кода их не заменяет.
- Каждая задача считается завершённой только по указанным приёмке/доказательствам, а не по факту изменения файлов.

## A. Характеристика и основа

### T-001 — Зафиксировать перечень исходного кода устаревшего решения

- Зависимости: none.
- Изменение: создать `docs/legacy-behavior-inventory.md` в новом solution root; записать six file hashes, behaviors, endpoints, identities, mutations, failure paths и disposition `reuse-semantics|rewrite|drop|defer`.
- Не изменять: `preparation/SyncOM/`.
- Приёмка: hashes совпадают с [traceability baseline](traceability.md); все `L-*` entries классифицированы; reviewer не находит implicit behavior.
- Валидация: SHA-256 recompute + inventory completeness checklist.

### T-002 — Создать очищенные фикстуры характеристики

- Зависимости: T-001.
- Изменение: `fixtures/legacy/` с types, own/inherited, lookup values/references, create/update/no-op, missing/ambiguous identity и dependency cases.
- Приёмка: fixture schema documented; no credentials/raw production lookup values; every fixture links to legacy behavior and current requirement.
- Валидация: JSON/schema parse, secret scan, traceability check.

### T-003 — Создать каркас решения

- Зависимости: owner-approved slice package; T-001.
- Изменение: create planned solution/project/test layout, dependency direction and build configuration; no functional adapter behavior.
- Приёмка: clean Release build with zero warnings; forbidden reverse references absent; no modification of reference implementations.
- Валидация: `dotnet build -c Release`, project-reference graph check, git diff scope check.

### T-004 — Реализовать типизированную идентичность и доменные записи [P]

- Зависимости: T-003.
- Изменение: `BpmSoftSync.Domain` identities and immutable records for package/schema layer/column/index/member/lookup registry/record/value/draft token.
- Приёмка: compile-time type separation prevents substituting schema/column/record/member IDs; duplicate schema names remain distinct by layer.
- Валидация: domain unit tests for equality, parsing and invalid IDs.

### T-005 — Реализовать канонические значения и сопоставление типов [P]

- Зависимости: T-002, T-003.
- Изменение: typed `Null|EmptyString|Value`, value kinds, reference precedence and supported type map.
- Приёмка: all fixtures produce deterministic canonical values; unknown type returns blocker, never fallback value.
- Валидация: golden/unit/property tests including invalid GUID and simultaneous reference fields.

### T-006 — Реализовать контракты блокеров и неизвестных структур [P]

- Зависимости: T-003.
- Изменение: named blocker taxonomy, redacted lossless structural envelope and safe diagnostic schema.
- Приёмка: unknown structure is representable with path/kind/cardinality/safe metadata; no unknown input can produce silent success.
- Валидация: fuzz/property fixtures + secret canaries.

## B. Адаптер чтения и допуск

### T-007 — Реализовать сессию в памяти и список разрешённых конечных точек чтения

- Зависимости: T-003, T-006.
- Изменение: `BpmSoftSync.BpmSoft` interactive auth/session and deny-by-default endpoint registry.
- Приёмка: secret values are absent from args/files/logs/exceptions; non-allowlisted endpoint cannot be called by read client.
- Валидация: contract tests with fake transport, process-argument/log/evidence scans.

### T-008 — Реализовать обобщённый постраничный считыватель

- Зависимости: T-004, T-007.
- Изменение: explicit-order paging, duplicate/skip/termination/max-page guards and telemetry.
- Приёмка: bounded ordering fixtures reproduce expected IDs/hashes; every negative paging fixture fails with named blocker.
- Валидация: contract and property tests for repeated/overlap/missing/terminal pages.

### T-009 — Реализовать парсеры пакетов/рабочего пространства/схем/справочников

- Зависимости: T-004–T-008.
- Изменение: typed parsers and inventory status mapping, including package layers, registry relation and index member rule.
- Приёмка: bounded evidence fixtures reconcile; duplicate `Account` layers stay distinct; unknown shapes remain lossless/unsupported.
- Валидация: golden fixture tests and exact source-to-domain counts.

### T-010 — Создать стенд допуска полного каталога

- Зависимости: T-008, T-009.
- Изменение: offline-capable double-pass orchestration, canonical hashes/counts, scale metrics and report schema.
- Приёмка: fake catalog scenarios produce PASS or finite blocker list; no write endpoint exists in harness dependency graph.
- Валидация: deterministic reruns, endpoint capture, injected novel shapes and size thresholds.

### T-011 — Выполнить допуск полного каталога (РАБОЧАЯ СРЕДА, ТОЛЬКО ЧТЕНИЕ)

- Зависимости: T-010; explicit owner authorization for this run.
- Изменение: no code; run generalized reader twice against unchanged local target and persist safe evidence.
- Приёмка: complete inventory and two-pass reconciliation, or explicit finite blocker list; no silent omission/write call/raw values.
- Валидация: independent evidence review, HTTP allowlist reconciliation, secret scan.

### T-012 — Принять результат допуска или направить его дальше

- Зависимости: T-011.
- Изменение: qualification decision record; create only bounded research tickets for exact blocking shapes.
- Приёмка: owner accepts PASS/limits or keeps affected capability blocked; full-catalog readiness is not inferred by agent.
- Валидация: decision-to-evidence matrix.

## C. Промышленный путь Excel-книг

### T-013 — Реализовать парсер Excel-книг/валидатор контракта

- Зависимости: T-004–T-006, T-003.
- Изменение: `BpmSoftSync.Workbooks` parser for exact pair/sheets/headers/types/relations/desired fields and blocker matrix.
- Приёмка: template-v3 bounded fixture passes; all tamper/formula/external/unknown/reference negative fixtures block.
- Валидация: contract tests, parser round-trip, secret scan.

### T-014 — Реализовать детерминированный отрисовщик пары

- Зависимости: T-005, T-009, T-013, T-012 PASS or accepted limits.
- Изменение: atomic two-book renderer with pair metadata, exact protection map, validations and `Indexes` projection.
- Приёмка: same baseline produces same canonical worksheet hashes; source rows reconcile; no `LookupRows`, formulas/external/VBA/connections.
- Валидация: generation twice, OOXML closure, bounded template regression, ActualIndexed/Indexes negative fixtures.

### T-015 — Реализовать снимок и контролируемое обновление

- Зависимости: T-013, T-014.
- Изменение: single-generation `S_*` transaction and server-wins refresh preserving unresolved local rows.
- Приёмка: failed snapshot leaves previous generation intact; successful refresh has complete snapshot/conflict mapping; no auto delete/Removed.
- Валидация: fault tests at each write boundary and before/after workbook reconciliation.

### T-016 — Допустить пару Excel-книг полного каталога

- Зависимости: T-012 accepted, T-014, T-015; explicit owner authorization for read-only/temporary Excel checks.
- Изменение: generate from qualified source in non-canonical output, independently verify, then promote only by explicit acceptance.
- Приёмка: source projection 1:1, no recovery, exact sheets/data, protected/unprotected behavior, resize/filter and scale thresholds.
- Валидация: tool verify, OOXML, parser round-trip, desktop Excel temporary-copy checks, independent report.

## D. Сравнение и неизменяемый план

### T-017 — Реализовать правила сравнения по полям [P]

- Зависимости: T-005, T-013.
- Изменение: `BpmSoftSync.Planning` pure comparisons for allowed schema/column/registry/lookup fields and no-op semantics.
- Приёмка: create/update/no-op and Null/EmptyString/reference cases match specs; no Name/Code identity fallback.
- Валидация: golden/unit/property tests.

### T-018 — Реализовать блокеры парсера/сравнения [P]

- Зависимости: T-006, T-013.
- Изменение: exact blockers for tamper, rename/type/package/delete/index/reference/stale/unsupported states.
- Приёмка: each named blocker has positive and negative fixture; blocked input yields no operations.
- Валидация: blocker matrix test.

### T-019 — Реализовать граф зависимостей и стабильный порядок операций

- Зависимости: T-004, T-017, T-018.
- Изменение: stable operation keys, producer/dependency rules, cycle/duplicate detection and deterministic total order.
- Приёмка: row/input enumeration permutations produce identical ordered operations; cycles block.
- Валидация: property/metamorphic tests.

### T-020 — Генерировать неизменяемый план и HTML-отчёт

- Зависимости: T-019, T-016.
- Изменение: canonical versioned plan, plan hash/bindings/browser sample and read-only HTML renderer from one model.
- Приёмка: report/plan operation sets match exactly; identical inputs stable; altered input changes/invalidates binding.
- Валидация: golden serialization, report reconciliation, CSP/no-Apply UI inspection.

### T-021 — Реализовать автономный валидатор перед Apply

- Зависимости: T-020.
- Изменение: validation-only component in `BpmSoftSync.Apply` that reads plan and live/read fingerprints without diff recomputation.
- Приёмка: mutation of every bound input, plan byte/schema/allowlist mismatch or stale state yields zero writes.
- Валидация: mutation matrix with fake write transport asserting no calls.

## E. Доказательства записи и Apply (заблокированы решениями владельца)

### T-022 — Зафиксировать список разрешённых операций владельца

- Зависимости: OD-02, OD-03; T-020.
- Изменение: versioned decision record listing exact operation kinds/fields; default-deny all omissions.
- Приёмка: machine-readable allowlist and human record agree; indexes/deletes absent.
- Валидация: schema and decision comparison.

### T-023 — Подготовить и выполнить ограниченную предварительную проверку для каждого вида операции (ЗАПИСЬ В РАБОЧЕЙ СРЕДЕ)

- Зависимости: T-022, OD-05; disposable/local target and explicit per-run authorization.
- Изменение: one separately reviewable evidence package per kind with exact request/response/read-back and required failures.
- Приёмка: positive + permission/stale/ambiguous/timeout/malformed/read-back mismatch outcomes recorded; no cross-kind inference.
- Валидация: independent evidence review and owner accept/reject per kind.

### T-024 — Реализовать исполнитель разрешённых операций

- Зависимости: accepted T-023 kinds, T-021.
- Изменение: endpoint registry/executor only for accepted kinds, backup/approval gates, per-operation audit and stop-first-error.
- Приёмка: unlisted kind cannot dispatch; injected failure prevents all later writes; delete/index endpoints unavailable.
- Валидация: fake transport/fault injection and endpoint inventory.

### T-025 — Реализовать поэтапное повторное чтение и сопоставление ID

- Зависимости: T-024 and relevant accepted identity evidence.
- Изменение: structural stage gate, lookup stage gate, full affected-state reconciliation and exact server ID mappings.
- Приёмка: ambiguous/missing ID or mismatch stops; `DraftRowToken -> RecordId` never uses Name/Code.
- Валидация: integration fixtures for success and every ambiguity/failure.

### T-026 — Реализовать защищённую обратную запись только в Excel-книги

- Зависимости: T-013, T-025.
- Изменение: local-only command for proven IDs/actual fields/reference replacement with content-hash and cell mapping audit.
- Приёмка: workbook change blocks; command makes zero BPMSoft calls; only allowlisted cells change.
- Валидация: before/after hashes, cell diff allowlist, endpoint capture.

## F. Проверка, эксплуатация и передача

### T-027 — Реализовать журнал запуска, схемы аудита/доказательств и сканирование секретов [P]

- Зависимости: T-006, T-003.
- Изменение: `BpmSoftSync` artifact schemas, version metadata, append-only paths and scanners.
- Приёмка: two runs never collide; secret canaries absent; raw lookup values denied by default; previous artifacts retained.
- Валидация: schema tests, path collision tests, secret/value scans.

### T-028 — Реализовать контракт детерминированной проверки в браузере

- Зависимости: T-020, T-025, T-027.
- Изменение: checklist/sample generation and evidence ingestion; browser actions remain read-only.
- Приёмка: sample policy exact and reproducible; missing/failed check blocks success; no credentials in skill context.
- Валидация: boundary/property tests and read-only action review.

### T-029 — Реализовать процесс финализации Git

- Зависимости: T-013, T-027, T-028.
- Изменение: non-secret settings validation, atomic two-book commit, conflict/network/auth paths and retry command.
- Приёмка: full success + confirmation commits both books; failed/partial does not; conflict requires new validation/compare; run artifacts excluded.
- Валидация: temporary repositories for success/conflict/failure/dirty state.

### T-030 — Создать обязательные навыки Codex

- Зависимости: stable CLI contracts T-007–T-029; separate skill authoring review.
- Изменение: setup, pull, compare/review, apply accompaniment, browser verify, blocked-input diagnosis and Git finalize skills.
- Приёмка: skills only call documented CLI/read-only browser/Git flows; cannot accept plan, receive secret or bypass blocker.
- Валидация: static instruction review and safe dry runs with fake CLI.

### T-031 — Упаковать и задокументировать рабочий процесс оператора [P]

- Зависимости: T-029, T-030.
- Изменение: self-contained Windows package, settings schema, install/update, runbook, failure playbook and evidence guide.
- Приёмка: package contains no secret/local absolute path and documents every gate/blocker/recovery action.
- Валидация: clean extraction, checksum/SBOM/dependency scan and documentation link check.

### T-032 — Независимая приёмка на чистой машине

- Зависимости: all applicable tasks and owner-approved acceptance protocol.
- Изменение: no product code; run complete supported scenario with a BPMSoft developer and record human acceptance.
- Приёмка: operator completes without oral owner knowledge; every MVP criterion has evidence; limitations explicit.
- Валидация: criterion-to-evidence matrix + exact owner decision record.

## Сводка контрольных этапов

| Контрольный этап | Заблокированные задачи |
|---|---|
| Одобренная владельцем декомпозиция | T-003 и далее |
| Полномочия/результат допуска полного каталога | T-011, T-012, заявления о промышленном применении T-014/T-016 |
| Список разрешённых операций записи и полномочия на предварительную проверку | T-022–T-026 |
| `INDEX_SYNC_UNRESOLVED` | все задачи изменения индексов; в этом бэклоге они отсутствуют |
| Решения человека об успехе | продвижение, Git commit/push и финальная приёмка |
