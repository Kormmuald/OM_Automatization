# Конституция проекта локального синхронизатора BPMSoft

Статус: **SDD candidate v1 — подготовлено к owner review; не разрешает реализацию, full-catalog запуск или BPMSoft Write/Manage**.

Версия: `1.0-candidate`  
Дата: 2026-09-06

## 1. Назначение и область действия

Эта конституция задаёт устойчивые правила для локального Windows-комплекта, который синхронизирует объектную модель и данные справочников между локальным BPMSoft и логической парой Excel-книг. Правила действуют для спецификаций, планов, задач, будущих slices, production-кода, Codex skills, evidence и operator workflow.

Пакет SDD сам по себе не открывает реализацию. Переход к slices и коду требует отдельного owner review в соответствии с [feature package](local-bpmsoft-synchronizer/README.md).

## 2. Иерархия источников

При конфликте применяется следующий порядок:

1. текущее явное решение владельца и актуальный workbook contract;
2. воспроизводимое evidence локального BPMSoft и независимые reviews;
3. characterization исторического `SyncOM`;
4. статическое чтение `SyncOM` и внешняя документация.

Исторический `SyncOM` считается работавшим reference implementation предметной логики и API-кандидатов. Он сокращает domain discovery, но не доказывает безопасность, полноту каталога, identity, idempotency, обработку ошибок или write semantics нового решения. Перенос выполняется как controlled rewrite, а не как port-by-copy.

## 3. Неизменяемые границы безопасности

- Credentials вводит только человек в интерактивном CLI или отдельной browser-сессии. Пароли, cookies, CSRF, login response и Git credentials не передаются skills и не сохраняются в arguments, workbook, plan, logs, evidence или Git.
- Read, compare и browser verification строго read-only. У них отдельный endpoint allowlist; отсутствие HTTP write calls проверяется контрактными тестами.
- Compare формирует human-readable report и machine-readable immutable plan из одной canonical model. Apply не пересчитывает diff.
- Apply допустим только для точного plan, связанного с hashes обеих книг, template version, pair baseline, target fingerprint и affected-state fingerprint.
- Изменение связанного input или live state инвалидирует plan до любых write calls.
- Только человек подтверждает ручной backup и принимает либо отклоняет plan целиком. Skills не одобряют plan и не обходят CLI gates.
- Apply останавливается после первой неуспешной операции, не делает automatic rollback и не продолжает последующие writes.
- Delete, SQL delete, automatic backup/restore, browser write actions и скрытый bidirectional merge не входят в MVP.

## 4. Identity и lossless processing

- Server `Id`, schema `UId`, column `UId`, lookup registry `Id` и локальный `DraftRowToken` — разные типы identity и не взаимозаменяются.
- Existing configuration связывается по доказанной BPMSoft identity и package-layer context. `Name`/`Code` не являются fallback identity; они допустимы только как display/diagnostic hint.
- BPMSoft создаёт GUID новых schema, columns, registry records и lookup rows. Клиент их не фабрикует.
- New lookup row использует неизменяемый локальный `DraftRowToken`. Переход к server `RecordId` допустим только через доказуемый create-response или однозначный read-back; иначе операция блокируется.
- Неизвестный type/shape не угадывается и не отбрасывается. Доступная безопасная структура сохраняется losslessly как inventory/unsupported diagnostics, а затронутая write-capable возможность закрывается явным blocker.
- Полнота collection read требует explicit ordering, pageable reader, duplicate/skip detection, termination guards и повторяемые hashes/counts.

## 5. Workbook contract как проверяемая граница

- Source of truth для workbook v1 — [WORKBOOK_CONTRACT_VISION.md](../WORKBOOK_CONTRACT_VISION.md); accepted template v3 — только bounded `VerifiedBoundedBaseline`, не full-catalog или production acceptance.
- Model Catalog и Lookup Catalog всегда валидируются как единая пара по `PairId`, `PullRunId`, baseline и template metadata.
- Mixed sheets редактируемы на уровне Excel; parser/compare, а не protection, является security boundary для IDs, fingerprints, actual/derived fields, formulas, validations и `Desired*` values.
- Fully read-only sheets защищены. Resize и existing filters обязательны; фактическая сортировка locked ranges не является acceptance criterion.
- `Indexes` строится только из `schema.indexes[]`; member relation — `columns[].columnUId -> Columns.ColumnUId`. `ActualIndexed` остаётся отдельным compatibility signal и не выводится из membership.
- Любое index planning/loading/apply закрыто под `INDEX_SYNC_UNRESOLVED` до отдельного proof и нового owner decision.
- Для lookup reference действует precedence: валидный `ReferenceRecordId`, иначе однозначно resolved `ReferenceDraftRowToken`, иначе empty reference. Некорректный непустой GUID блокирует операцию.

## 6. Evidence, audit и human gates

- CLI владеет детерминированными security checks, fingerprints, plan validity и read-back. Skills только оркестрируют CLI, объясняют safe diagnostics и выполняют разрешённую read-only browser verification.
- Каждый run имеет отдельную date-based versioned folder с раздельными audit/evidence, run journal и timestamped filenames. Артефакты append-only в рамках run и не добавляются в workbook Git repository.
- Evidence по умолчанию не содержит raw lookup values. Любое исключение требует отдельного минимизированного schema и owner decision.
- Success требует полного CLI read-back и 100% browser acceptance выбранных checks. Structural operations проверяются полностью; lookup rows — детерминированная выборка 10% каждого operation kind, минимум 3 и максимум 10, с new/update coverage при наличии.
- После success gates и явного подтверждения человека обе книги фиксируются одним Git commit с run ID и plan hash. Push не выполняет overwrite или automatic conflict resolution.
- Failed/partial cycle не создаёт workbook commit. Network/auth push failure сохраняет локальный commit; conflict требует manual resolution, полной повторной validation и нового compare.

## 7. Исследования и qualification

- Широкое исследование не создаётся, если поведение уже классифицировано в legacy inventory, покрыто sanitized fixture/evidence и не пересекает новую safety boundary.
- Дополнительный probe допускается только как bounded preflight конкретной операции или blocking shape: endpoint, payload, invariant, PASS/FAIL и запрещённые вызовы задаются заранее.
- Full-catalog pull — единый qualification gate generalized reader, а не серия предварительных probes по каждому типу.
- Write research — только operation-level bounded preflight после owner-approved allowlist. Недоказанный operation kind остаётся закрытым и не должен блокировать независимый безопасный subset.

## 8. Инженерные правила

- Основная технология: .NET 10/C#, guided CLI, локальный HTML report без Apply controls; Windows + desktop Excel + local BPMSoft являются supported environment MVP.
- Domain logic, adapters, workbook I/O, compare/plan, apply executor и verification/audit имеют отдельные ответственности. Google UI/formulas и Apps Script не переносятся.
- Детерминизм, idempotency, fail-closed validation, stable operation ordering и secret scanning проверяются тестами до live use.
- Characterization tests фиксируют полезную семантику legacy, но expected behavior всегда приводится к текущему contract.
- Production-код не изменяется до согласованных spec, clarify/review, plan, tasks, consistency report, будущих slices и owner gate.

## 9. Управление изменениями

- Любое изменение constitution требует явной записи причины, затронутых requirements и миграционных последствий.
- Owner decision не выводится из agent recommendation или PASS automated checks.
- Assumption всегда маркируется и не повышается до confirmed/owner decision без источника.
- Исключения безопасности не наследуются между gates и не расширяют scope автоматически.

## 10. Текущие обязательные blockers

- `INDEX_SYNC_UNRESOLVED`: запрещены index plan/load/apply.
- `WRITE_ALLOWLIST_UNAPPROVED`: запрещены write preflight и Apply для operation kinds без owner allowlist.
- `WRITE_SEMANTICS_UNPROVEN`: запрещено включать operation kind в Apply до version-specific positive/negative evidence.
- `FULL_CATALOG_NOT_QUALIFIED`: запрещено считать bounded evidence доказательством catalog completeness/scale/generalisation.
- `SLICES_NOT_APPROVED`: запрещена implementation execution до отдельной owner-approved slice decomposition.

## 11. Compliance gate

Любой будущий artifact или diff должен показать: применимые requirement IDs, соблюдённые blockers/Out of Scope, проверки, evidence path и решения человека. Несоответствие конституции является blocker, а не advisory warning.
