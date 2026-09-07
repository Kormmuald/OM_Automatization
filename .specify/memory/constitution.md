<!--
Sync Impact Report
- Version change: scaffold (unversioned) -> 1.0.0-candidate
- Modified principles: none; the resolved scaffold was populated from the canonical source.
- Added sections: Additional Constraints; Development Workflow.
- Removed sections: none.
- Follow-up TODOs: RATIFICATION_DATE awaits explicit owner ratification. This candidate does
  not authorize implementation, a full-catalog run, or BPMSoft Write/Manage operations.
-->

# Конституция проекта локального синхронизатора BPMSoft

Статус: **SDD candidate v1 — подготовлено к owner review; не разрешает реализацию,
full-catalog запуск или BPMSoft Write/Manage**.

Эта конституция задаёт обязательные правила для локального Windows-комплекта,
синхронизирующего объектную модель и данные справочников между локальным BPMSoft и
логической парой Excel-книг. Она применяется к спецификациям, планам, задачам,
будущим slices, production-коду, Codex skills, evidence и operator workflow. Сам пакет SDD
не открывает реализацию: переход к slices и коду требует отдельного owner review согласно
[feature package](../../preparation/docs/product-specs/local-bpmsoft-synchronizer/README.md).

## Core Principles

### I. Доказательная иерархия источников

При конфликте MUST применяться следующий порядок: (1) текущее явное решение владельца и
актуальный workbook contract; (2) воспроизводимое evidence локального BPMSoft и независимые
reviews; (3) characterization исторического `SyncOM`; (4) статическое чтение `SyncOM` и
внешняя документация. Исторический `SyncOM` является reference implementation предметной
логики и API-кандидатов, но не доказывает безопасность, полноту каталога, identity,
idempotency, обработку ошибок или write semantics. Любой перенос MUST быть controlled rewrite,
а не port-by-copy.

### II. Безопасность и человеческий контроль — без исключений

Credentials вводит только человек в интерактивном CLI или отдельной browser-сессии. Пароли,
cookies, CSRF, login response и Git credentials MUST NOT попадать в skills, arguments,
workbook, plan, logs, evidence или Git. Read, compare и browser verification MUST быть
read-only и иметь отдельный endpoint allowlist; отсутствие HTTP write calls MUST проверяться
контрактными тестами. Только человек подтверждает ручной backup и принимает либо отклоняет
plan целиком; skills не одобряют plan и не обходят CLI gates. Delete, SQL delete,
automatic backup/restore, browser write actions и скрытый bidirectional merge не входят в MVP.

### III. Plan-before-apply и fail-closed execution

Compare MUST формировать human-readable report и machine-readable immutable plan из одной
canonical model; Apply MUST NOT пересчитывать diff. Apply разрешён только для точного plan,
связанного с hashes обеих книг, template version, pair baseline, target fingerprint и
affected-state fingerprint. Любое изменение input или live state MUST инвалидировать plan до
write calls. Apply MUST остановиться после первой неуспешной операции, MUST NOT выполнять
automatic rollback и MUST NOT продолжать последующие writes.

### IV. Строгая identity и lossless обработка

Server `Id`, schema `UId`, column `UId`, lookup registry `Id` и локальный `DraftRowToken` MUST
считаться разными невзаимозаменяемыми типами identity. Existing configuration MUST связываться
по доказанной BPMSoft identity и package-layer context; `Name` и `Code` допустимы только как
display/diagnostic hint. BPMSoft MUST создавать GUID для новых schema, columns, registry records
и lookup rows. Новый lookup row MUST использовать неизменяемый `DraftRowToken` до доказуемого
create-response или однозначного read-back server `RecordId`; иначе операция блокируется.
Неизвестный type/shape MUST сохраняться losslessly как inventory/unsupported diagnostics и
закрывать затронутую write-capable возможность явным blocker. Collection read MUST иметь
explicit ordering, pageable reader, duplicate/skip detection, termination guards и повторяемые
hashes/counts.

### V. Workbook contract — проверяемая граница

Source of truth для workbook v1 —
[WORKBOOK_CONTRACT_VISION.md](../../preparation/docs/WORKBOOK_CONTRACT_VISION.md); accepted
template v3 — только bounded `VerifiedBoundedBaseline`, не full-catalog и не production
acceptance. Model Catalog и Lookup Catalog MUST валидироваться как единая пара по `PairId`,
`PullRunId`, baseline и template metadata. Parser/compare, а не Excel protection, MUST быть
security boundary для IDs, fingerprints, actual/derived fields, formulas, validations и
`Desired*` values. Mixed sheets редактируемы на уровне Excel. Fully read-only sheets MUST быть
защищены; resize и existing filters обязательны, а фактическая сортировка locked ranges не
является acceptance criterion. `Indexes` MUST строиться только из `schema.indexes[]`, а member relation — из
`columns[].columnUId -> Columns.ColumnUId`; `ActualIndexed` остаётся отдельным compatibility
signal. Lookup reference MUST применять precedence: валидный `ReferenceRecordId`, затем
однозначно resolved `ReferenceDraftRowToken`, иначе empty reference; некорректный непустой GUID
блокирует операцию.

## Additional Constraints

Основная технология MVP — .NET 10/C#, guided CLI и локальный HTML report без Apply controls.
Поддерживаемая среда: Windows, desktop Excel и local BPMSoft. Domain logic, adapters, workbook
I/O, compare/plan, apply executor и verification/audit MUST иметь отдельные ответственности;
Google UI/formulas и Apps Script MUST NOT переноситься. Детерминизм, idempotency, fail-closed
validation, stable operation ordering и secret scanning MUST проверяться тестами до live use.
Characterization tests фиксируют полезную legacy-семантику, но expected behavior MUST
соответствовать текущему contract.

Production-код MUST NOT изменяться до согласованных spec, clarify/review, plan, tasks,
consistency report, будущих slices и owner gate.

`INDEX_SYNC_UNRESOLVED` запрещает index plan/load/apply. `WRITE_ALLOWLIST_UNAPPROVED` запрещает
write preflight и Apply для operation kinds без owner allowlist. `WRITE_SEMANTICS_UNPROVEN`
запрещает включение operation kind в Apply до version-specific positive/negative evidence.
`FULL_CATALOG_NOT_QUALIFIED` запрещает считать bounded evidence доказательством catalog
completeness, scale или generalisation. `SLICES_NOT_APPROVED` запрещает implementation execution
до отдельной owner-approved slice decomposition.

## Development Workflow

CLI MUST владеть детерминированными security checks, fingerprints, plan validity и read-back;
skills могут только оркестрировать CLI, объяснять safe diagnostics и выполнять разрешённую
read-only browser verification. Каждый run MUST иметь отдельную date-based versioned folder с
раздельными audit/evidence, run journal и timestamped filenames. Артефакты MUST быть append-only
в рамках run и MUST NOT добавляться в workbook Git repository. Evidence по умолчанию MUST NOT
содержать raw lookup values; исключение требует отдельного минимизированного schema и owner
decision.

Success MUST включать полный CLI read-back и 100% browser acceptance выбранных checks.
Structural operations проверяются полностью; lookup rows — детерминированной выборкой 10% каждого
operation kind, минимум 3 и максимум 10, с new/update coverage при наличии. После success gates
и явного подтверждения человека обе книги MUST фиксироваться одним Git commit с run ID и plan
hash; push MUST NOT выполнять overwrite или automatic conflict resolution. Failed/partial cycle
MUST NOT создавать workbook commit. При network/auth push failure локальный commit сохраняется;
conflict требует manual resolution, полной повторной validation и нового compare.

Широкое исследование MUST NOT создаваться, если поведение уже классифицировано в legacy
inventory, покрыто sanitized fixture/evidence и не пересекает новую safety boundary.
Дополнительный probe допустим только как bounded preflight конкретной операции или blocking shape
с заранее заданными endpoint, payload, invariant, PASS/FAIL и запрещёнными вызовами.
Full-catalog pull является единым qualification gate generalized reader. Write research допустимо
только как operation-level bounded preflight после owner-approved allowlist; недоказанный
operation kind остаётся закрытым и MUST NOT блокировать независимый безопасный subset.

## Governance

Эта конституция имеет приоритет над проектными практиками и future artifacts. Любое изменение
MUST содержать причину, затронутые requirements и миграционные последствия, пройти owner review
и обновить Sync Impact Report. Owner decision MUST NOT выводиться из agent recommendation или
PASS automated checks. Assumption MUST быть маркировано и MUST NOT становиться
confirmed/owner decision без источника. Исключения безопасности MUST NOT наследоваться между
gates и MUST NOT автоматически расширять scope.

Версии следуют SemVer: MAJOR — несовместимое удаление или переопределение governance; MINOR —
добавление принципа или существенное расширение требований; PATCH — уточнение без изменения
смысла. До owner ratification используется prerelease-суффикс `-candidate`. Каждый будущий
artifact или diff MUST показать применимые requirement IDs, соблюдённые blockers/Out of Scope,
проверки, evidence path и решения человека. Несоответствие конституции является blocker, а не
advisory warning.

**Version**: 1.0.0-candidate | **Ratified**: TODO(RATIFICATION_DATE): awaits explicit owner
ratification | **Last Amended**: 2026-09-07
