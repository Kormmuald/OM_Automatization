# Спецификация функции: безопасное чтение и допуск каталога BPMSoft

**Ветка функции**: `001-read-only-catalog-qualification`

**Создано**: 2026-09-07

**Статус**: Черновик

**Вводные**: Первая функция четырёхэтапного L2-пилота создаёт общий безопасный механизм run/audit/evidence, защищённое чтение BPMSoft и проверяемый допуск полного каталога. Общее видение: [неизменяемый source](../../preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md); этапный source: [01](../../preparation/docs/product-specs/local-bpmsoft-synchronizer/01-read-only-catalog-spec.md); сквозной source: [05](../../preparation/docs/product-specs/local-bpmsoft-synchronizer/05-verification-operations-spec.md).

## Clarifications

### Session 2026-09-08

- Q: Как система должна поступить при расхождении двух полных проходов из-за изменения цели, а не ошибки пагинации? → A: Завершить run blocker `TARGET_STATE_CHANGED_DURING_QUALIFICATION`; новый двойной проход — только после нового ручного запуска либо новой прямой текущей просьбы пользователя.

### Session 2026-09-13

- Q: Нужна ли отдельная фиксируемая ссылка на разрешение перед live read-only test? → A: Нет. После успешных автономных checks live test разрешён либо прямой текущей просьбой пользователя в доступном чате, либо ручным интерактивным запуском `catalog qualify` оператором. Отдельный `AuthorizationReference` не требуется и не сохраняется. Агент не запускает live run сам по себе; credentials запрашиваются только terminal-приглашением в рамках такого прямого/ручного запуска. Это не даёт разрешения на write.

## Пользовательские сценарии и проверка *(обязательно)*

### Пользовательская история 1 — Получить безопасный каталог (Приоритет: P1)

Технический оператор интерактивно подключается к локальному BPMSoft и получает полный инвентарный результат только для чтения, не передавая системе или навыкам сохраняемые секреты.

**Почему этот приоритет**: без доказанного чтения нельзя безопасно строить Excel-пару, сравнение или Apply.

**Независимая проверка**: два прохода одной утверждённой области на неизменной цели дают одинаковые упорядоченные идентичности, количества и канонические hashes либо конечный перечень именованных ограниченных blocker без скрытых пропусков и без HTTP write calls.

**Сценарии приемки**:

1. **Дано** успешно пройденные автономные проверки и прямой текущий запрос пользователя в чате либо ручной запуск `catalog qualify` оператором, **Когда** оператор вручную выполняет два полных прохода неизменной цели, **Тогда** результат сверяет counts, ordered IDs, hashes, paging telemetry, unsupported items, duration и response sizes.
2. **Дано** неизвестная структура, **Когда** она встречается при чтении, **Тогда** она сохраняется losslessly в обезличенной структурной диагностике и блокирует только зависящую возможность.
3. **Дано** endpoint вне read allowlist, **Когда** путь пытается его вызвать, **Тогда** запуск завершается fail-closed до сетевого вызова.

### Пользовательская история 2 — Проверить доказательства запуска (Приоритет: P1)

Reviewer независимо связывает результат qualification с безопасными audit/evidence, версиями и состояниями gates, не получая credentials или исходные значения справочников.

**Почему этот приоритет**: L2-пилот требует воспроизводимого решения человека, а не доверия к сообщению об успехе.

**Независимая проверка**: схема артефактов принимает полный безопасный набор, отклоняет запрещённые поля и secret canaries, а повторный run не перезаписывает предыдущий.

**Сценарии приемки**:

1. **Дано** два запуска с одинаковым временем до доступной точности, **Когда** создаются их корни, **Тогда** уникальные `RunId` предотвращают коллизию и каждый запуск имеет раздельные `audit/`, `evidence/` и журнал.
2. **Дано** password, cookie/CSRF, login response, Git secret или raw lookup value-canary, **Когда** формируется сохраняемый артефакт, **Тогда** schema/scan отклоняет его и успешный статус невозможен.

### Пользовательская история 3 — Получить безопасную диагностику и передачу раннего этапа (Приоритет: P2)

Оператор получает понятную причину остановки, допустимое следующее действие и базовые инструкции настройки/чтения, а handoff operator может воспроизвести безопасный subset без устных знаний владельца.

**Почему этот приоритет**: диагностируемость и ранний handoff нужны до подключения последующих функций.

**Независимая проверка**: fake-CLI dry run показывает, что setup/read/diagnosis skills только вызывают документированные команды и не принимают approvals, не получают secrets и не дублируют бизнес-логику.

**Сценарии приемки**:

1. **Дано** `CATALOG_ORDER_OR_PAGING_UNQUALIFIED`, **Когда** оператор открывает диагностику, **Тогда** она показывает безопасную причину, затронутую область и только разрешённый путь исправления/эскалации.
2. **Дано** новый оператор на чистой Windows-машине, **Когда** он выполняет ранние setup/read шаги пакета передачи, **Тогда** он доходит до qualification gate без устной передачи credentials или скрытых локальных путей.

### Граничные случаи

- Повторная, перекрывающаяся, пропущенная, пустая промежуточная или непустая последняя page, превышение лимита pages и немонотонное завершение дают `CATALOG_ORDER_OR_PAGING_UNQUALIFIED`.
- Одинаковые имена схем в разных package layers не объединяются; неоднозначность даёт `SCHEMA_LAYER_IDENTITY_AMBIGUOUS`.
- Неизвестная структура без lossless envelope завершает qualification критической ошибкой; корректно сохранённая unsupported shape создаёт ограниченный blocker.
- Превышение будущих ограничений Excel отмечается `WORKBOOK_SCALE_DECISION_REQUIRED`, но не подменяет результат полного каталога bounded baseline.
- `Indexes` читаются только из `schema.indexes[].columns[].columnUId`; member `.uId` и `ActualIndexed` не подменяют relation.
- Отсутствие прямой текущей просьбы пользователя в чате не блокирует разработку и автономные checks, но агент не начинает live run самостоятельно. Оператор может начать его вручную через `catalog qualify`; отдельное сохраняемое разрешение не требуется.
- Расхождение двух полных проходов, указывающее на изменение цели, завершает run fail-closed с `TARGET_STATE_CHANGED_DURING_QUALIFICATION`; автоматический новый двойной проход запрещён и требует нового ручного запуска либо новой прямой просьбы пользователя.

### Ожидания к формальным проверкам *(обязательно)*

- **Проверки инкрементов**: будущий `test-plan.md` ДОЛЖЕН формализовать endpoint capture, negative paging fixtures, metamorphic ordering, duplicate/skip detection, unknown-shape handling, schema validation и secret canaries.
- **Проверки итогового решения**: эта функция владеет общим механизмом run/audit/evidence; функции 002–004 ДОЛЖНЫ повторно применять его к своим типам запуска без обратной зависимости 001 от 004.
- **Ожидания к доказательствам**: принимаются безопасные command outputs, sanitized fixtures, schema/scan reports, hash/count reconciliation и подпись решения человека; исторические PASS не являются текущим evidence.

## Требования *(обязательно)*

### Функциональные требования

- **FR-001 — сессия и секреты** (`01-read-only-catalog-spec.md:READ-001`; общий `spec.md:FR-002`; `05-verification-operations-spec.md:OPS-003`): система ОБЯЗАНА принимать URL и вход интерактивно, хранить password/cookies/CSRF только в памяти и исключать их из arguments, skills и сохраняемых данных.
- **FR-002 — deny-by-default read allowlist** (`READ-002`; общий `FR-002`): система ОБЯЗАНА разрешать только явно классифицированные endpoints чтения и сохранять вид endpoint и безопасные metadata без response body с raw lookup values.
- **FR-003 — полный постраничный считыватель** (`READ-003`; общий `FR-003`): каждая collection ОБЯЗАНА иметь explicit stable ordering, paging, duplicate/overlap/skip checks, termination/max-page guards и telemetry.
- **FR-004 — воспроизводимое чтение** (`READ-004`): два прохода неизменной области ДОЛЖНЫ давать одинаковые ordered identities, counts и canonical hashes; direction check выполняется там, где доказан контрактом.
- **FR-005 — package-layer identity** (`READ-005`; общий `FR-011`): одинаковые display names НЕ ДОЛЖНЫ объединять server `Id`, schema `UId`, column `UId`, registry `Id` или package layer.
- **FR-006 — полный inventory** (`READ-006`): `WorkspaceInventory` ОБЯЗАН содержать каждый доступный item со статусом `Structured|InventoryOnly|Unreadable|Unsupported` и безопасной причиной.
- **FR-007 — ограничение детального разбора** (`READ-007`): детальный разбор разрешён только для подтверждённых EntitySchema/lookup contracts; иные items остаются lossless inventory.
- **FR-008 — неизвестные структуры** (`READ-008`, `READ-011`; общий `FR-004`): неизвестный type/property/shape НЕ ДОЛЖЕН заменяться default или пропускаться; допустимы lossless representation, explicit unsupported или bounded research item с точным вопросом.
- **FR-009 — индексы только для чтения** (`READ-009`; общий `FR-022`): member relation ОБЯЗАН строиться только по `schema.indexes[].columns[].columnUId`; `INDEX_SYNC_UNRESOLVED` исключает index plan/load/apply.
- **FR-010 — full-catalog qualification** (`READ-010`; общий `FR-005`): после успешных автономных verification checks оператор может вручную начать один read-only `catalog qualify` либо агент может начать его только по прямой текущей просьбе пользователя в доступном чате. Отдельный `AuthorizationReference` не требуется. CLI запрашивает credentials исключительно интерактивно в terminal и не сохраняет их. Run ОБЯЗАН фиксировать counts, ordered IDs, hashes, retries, gaps/duplicates, unsupported list, duration, response sizes и прогнозируемый workbook scale. Расхождение, указывающее на изменение цели, ОБЯЗАНО завершать run `TARGET_STATE_CHANGED_DURING_QUALIFICATION`; автоматический повтор двойного прохода запрещён, новый run требует нового ручного запуска либо новой прямой просьбы пользователя.
- **FR-011 — target fingerprint** (`READ-012`; общий `FR-010`): fingerprint ОБЯЗАН использовать только доказанные read data и version metadata; collision/staleness assumptions документируются и до qualification не считаются промышленно доказанными.
- **FR-012 — корень запуска** (`05-verification-operations-spec.md:OPS-001`; общий `FR-019`): каждый run ОБЯЗАН иметь date hierarchy + unique `RunId`, отдельные `audit/`, `evidence/` и журнал с timestamps; прежние runs не перезаписываются.
- **FR-013 — метаданные запуска** (`OPS-002`): audit ОБЯЗАН фиксировать версии приложения, workbook template, skills, BPMSoft, Excel tables и их modification times, target alias, plan hash при наличии и gate outcomes.
- **FR-014 — схема доказательств и scan** (`OPS-003`; общий `NFR-001`): evidence schema ОБЯЗАНА запрещать поля по умолчанию и автоматически искать secret/value markers до успешного статуса.
- **FR-015 — ранние skills** (`OPS-012`): setup, read/pull и blocked-input diagnosis skills ОБЯЗАНЫ только оркестрировать CLI, объяснять безопасную диагностику и не реализовывать параллельную domain logic.
- **FR-016 — ранний handoff** (`OPS-013`): пакет передачи ОБЯЗАН включать schema настроек без secrets, install/update prerequisites, ранний operator runbook, failure path и границы допуска на чистой машине; окончательная clean-machine acceptance принадлежит 004.
- **FR-017 — приоритет источников** (общий `FR-001`): legacy-поведение ОБЯЗАНО иметь disposition `reuse-semantics|rewrite|drop|defer`; port-by-copy запрещён.
- **FR-018 — диагностический результат** (общие `NFR-003`, `NFR-008`): каждая остановка ОБЯЗАНА возвращать безопасную причину, затронутую область, способ устранения и следующее разрешённое действие без расширения полномочий.
- **FR-019 — архитектурная граница с первого этапа** (общий `NFR-009`): canonical domain/identity/blocker/audit contracts НЕ ДОЛЖНЫ зависеть от BPMSoft, Excel, browser или Git adapters; последующие функции являются потребителями, а не владельцами дублирующей логики.

### Ключевые сущности *(включайте, если функция связана с данными)*

- **Run**: уникальный запуск, его тип, timestamps, версии, target alias и gate outcomes.
- **AuditEvent / EvidenceEnvelope**: безопасное решение или доказательство со schema version, stable key и запретами на secrets/raw values.
- **WorkspaceInventoryItem**: доступный item, package-layer identity, support status и reason.
- **CatalogQualification**: два прохода, reconciliation, telemetry, unsupported list и решение человека.
- **TargetFingerprint**: доказанные read-only признаки состояния и явные staleness/collision assumptions.

## Критерии успеха *(обязательно)*

### Измеримые результаты

- **SC-001**: 100% captured calls автономных и авторизованных qualification scenarios принадлежат read allowlist; write calls равны 0.
- **SC-002**: автономная проверка доказывает этот путь на fixture/fake transport, а отдельно разрешённый владельцем ручной run на неизменной цели даёт 100% совпадение ordered identities, counts и canonical hashes либо конечный explicit blocker list без скрытых omissions; при признаке изменения цели результатом является `TARGET_STATE_CHANGED_DURING_QUALIFICATION`, а не PASS или автоматический новый двойной проход.
- **SC-003**: 100% workspace items представлены одним из четырёх support statuses; неизвестные shapes без lossless diagnostic равны 0.
- **SC-004**: все negative paging fixtures завершаются именованным blocker и ни одна не имеет ложного PASS или бесконечного выполнения.
- **SC-005**: secret/value canaries обнаруживаются в 100% запрещённых полей; ни один сохраняемый PASS artifact не содержит их.
- **SC-006**: два последовательных runs сохраняют раздельные неперезаписанные roots и проходят schema/hash validation.
- **SC-007**: независимый оператор по раннему runbook достигает qualification decision point без устных инструкций и без передачи secrets в skill context.

## Допущения

- Инициатива остаётся `L2 / l2-pilot`; полномочия на разработку не являются полномочием на live full-catalog run.
- Условный ручной live run следует только после успешных автономных проверок и начинается вручную оператором либо по прямой текущей просьбе пользователя в рабочем чате; отдельный `AuthorizationReference` не используется. Credentials не сохраняются.
- Ограниченные evidence для `ActivityPriority`, `Lookup` и двух simple indexes служат fixtures, но не доказывают full-catalog completeness или scale.
- Точный endpoint allowlist, fingerprint algorithm и redaction schema уточняются на будущих Clarify/Plan без ослабления deny-by-default contracts.
- Qualification может завершиться PASS либо принятым человеком конечным limited blocker list; агент не принимает это решение.
- Не входят: генерация Excel-книг, compare/plan, Apply, browser verification, Git, deletion и index mutation.
