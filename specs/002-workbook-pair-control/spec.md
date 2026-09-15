# Спецификация функции: управляемая логическая пара Excel-книг

**Ветка функции**: `002-workbook-pair-control`

**Создано**: 2026-09-07

**Статус**: Черновик

**Вводные**: Вторая функция четырёхэтапного L2-пилота материализует допущенное состояние из 001 в логическую пару `BPMSoft.ModelCatalog.xlsx` + `BPMSoft.LookupCatalog.xlsx`, валидирует пользовательское намерение и выполняет безопасный workbook pull/refresh. Общее видение: [source](../../preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md); этапный source: [02](../../preparation/docs/product-specs/local-bpmsoft-synchronizer/02-workbook-pair-spec.md); сквозной source: [05](../../preparation/docs/product-specs/local-bpmsoft-synchronizer/05-verification-operations-spec.md); точный контракт: [WORKBOOK_CONTRACT_VISION.md](../../preparation/docs/WORKBOOK_CONTRACT_VISION.md). Все sources неизменяемы.

## Пользовательские сценарии и проверка *(обязательно)*

### Пользовательская история 1 — Создать согласованную пару полного каталога (Приоритет: P1)

Оператор получает две физические книги как одну логическую пару из одного qualified baseline, с точным составом листов, полей и общей identity.

**Почему этот приоритет**: compare не может безопасно работать с неполной, смешанной или неоднозначной парой.

**Независимая проверка**: source projection 1:1, OOXML closure, parser round-trip и открытие обеих книг в desktop Excel без recovery подтверждают пару либо возвращают именованный blocker.

**Сценарии приемки**:

1. **Дано** принятый результат full-catalog qualification 001, **Когда** пара создаётся дважды из одного baseline, **Тогда** pair metadata совпадают, а канонические hashes рабочих листов одинаковы.
2. **Дано** полный catalog source, **Когда** выполняется независимая сверка, **Тогда** нет пропущенных/лишних строк, а unsupported items представлены явно.

### Пользовательская история 2 — Проверить пользовательское намерение fail-closed (Приоритет: P1)

Оператор редактирует только разрешённые desired/value поля, а parser обнаруживает любое вмешательство в identity, derived/control fields, структуры, ссылки или запрещённые намерения до compare.

**Почему этот приоритет**: Excel protection не является security boundary, особенно на mixed sheets.

**Независимая проверка**: отрицательные fixtures для tamper/formula/external/VBA/unknown/reference/pair mismatch дают blocker до формирования plan и без BPMSoft calls.

**Сценарии приемки**:

1. **Дано** изменённый `SchemaUId`, `RecordId`, fingerprint или manifest, **Когда** parser проверяет пару, **Тогда** обработка блокируется независимо от Excel protection.
2. **Дано** одновременно заполненные reference fields, **Когда** `ReferenceRecordId` — валидный GUID, **Тогда** он имеет приоритет; невалидный непустой GUID блокирует обработку, не превращаясь в empty reference.
3. **Дано** новая lookup row, **Когда** она создана не workbook-only командой либо токен повторён/подменён, **Тогда** compare запрещён.

### Пользовательская история 3 — Обновить пару с сохранением локального намерения (Приоритет: P2)

Оператор выполняет pull/refresh, получая server-priority actual state и полный снимок прежнего рабочего состояния без автоматического удаления локальных строк.

**Почему этот приоритет**: актуализация книг не должна уничтожать нерешённое пользовательское намерение.

**Независимая проверка**: fault injection на каждой границе snapshot/write доказывает сохранение предыдущего `S_*`, а успешный refresh показывает exact conflict mapping и `PotentiallyDeleted` без `Removed`.

**Сценарии приемки**:

1. **Дано** валидная пара, **Когда** готовится новый полный `S_*` и подготовка завершается ошибкой, **Тогда** прежний snapshot и рабочие листы сохраняются без частичной замены.
2. **Дано** элемент только в локальной книге, **Когда** server state обновляет пару, **Тогда** элемент остаётся `PotentiallyDeleted`, а delete intent не создаётся.

### Пользовательская история 4 — Получить безопасные workbook evidence и операторские шаги (Приоритет: P2)

Reviewer связывает workbook generation/validation/refresh с общим run/audit/evidence из 001, а оператор использует навыки выгрузки и валидации только как оркестрацию CLI.

**Почему этот приоритет**: качество пары должно быть доказуемым и переносимым, но skills не должны дублировать parser rules.

**Независимая проверка**: каждый workbook run имеет уникальный root, версии/хеши/результаты checks, проходит secret scan и воспроизводится fake-CLI dry run без передачи credentials.

**Сценарии приемки**:

1. **Дано** workbook generation/refresh, **Когда** сохраняются evidence, **Тогда** они используют механизм 001, не содержат raw lookup values и не перезаписывают предыдущий run.
2. **Дано** handoff operator, **Когда** он выполняет документированные pull/validate steps, **Тогда** skill не принимает решения о qualification и не обходит blocker.

### Граничные случаи

- Любое несовпадение `PairId`, `PullRunId`, `PairBaselineHash`, `BaselineTargetFingerprint`, `TemplateVersion` или состава пары даёт `PAIR_MISMATCH`.
- Формулы в editable tables, external links, VBA, connections, неизвестные columns или headers, повреждённые validations/manifest блокируют обработку.
- `LookupValues` с конфликтующими групповыми metadata, отсутствующими либо двойными `RecordId|DraftRowToken` блокируются.
- Некорректные типы, ссылки, duplicate token, неразрешённая inheritance/package identity не исправляются автоматически.
- `Indexes` отражают members losslessly; `ActualIndexed` не является membership source; `INDEX_SYNC_UNRESOLVED` запрещает desired index plan/load/apply.
- Full-catalog scale вне принятых Excel limits даёт `WORKBOOK_SCALE_DECISION_REQUIRED`; bounded template v3 не заменяет этот gate.
- Изменение книги между hash check и workbook-only writeback в 004 останавливает writeback без merge.

### Ожидания к формальным проверкам *(обязательно)*

- **Проверки инкрементов**: будущий `test-plan.md` ДОЛЖЕН включить exact contract, negative parser matrix, deterministic rendering, snapshot transaction/failure, source projection, OOXML closure, parser round-trip и desktop Excel usability/scale.
- **Проверки итогового решения**: workbook pair ДОЛЖНА регрессионно проверяться перед compare, Apply validation, writeback и Git; 002 потребляет run/audit/evidence 001 и передаёт validated pair/baseline/snapshot results в 003.
- **Ожидания к доказательствам**: hashes, exact sheet/header inventories, OOXML checks, parser reports, desktop Excel no-recovery evidence, source reconciliation, conflict map и secret scan.

## Требования *(обязательно)*

### Функциональные требования

- **FR-001 — точный workbook contract** (`02-workbook-pair-spec.md:WB-001`; общий `spec.md:FR-006`): обе книги ОБЯЗАНЫ соответствовать утверждённым sheets, headers, columns, editable/derived semantics и reference precedence из `WORKBOOK_CONTRACT_VISION.md`.
- **FR-002 — атомарная identity пары** (`WB-002`): книги создаются/заменяются только вместе из одного baseline с общими `PairId`, `PullRunId`, `PairBaselineHash`, `BaselineTargetFingerprint`, `TemplateVersion`.
- **FR-003 — канонические ячейки** (`WB-003`; общие `NFR-002`, `NFR-009`): renderer/parser ОБЯЗАНЫ использовать typed cell semantics, canonical serialization и explicit row keys; Excel presentation order не влияет на смысл.
- **FR-004 — защита и parser boundary** (`WB-004`; общий `FR-008`): read-only sheets защищены, mixed sheets не защищаются; parser ОБЯЗАН обнаруживать tamper каждого identity/derived/service field независимо от UI protection.
- **FR-005 — запрещённое содержимое** (`WB-005`): formula, external link, VBA, connection, unknown column, invalid header или повреждённые validation/manifest/pair metadata блокируют compare.
- **FR-006 — identity lookup record** (`WB-006`; общий `FR-011`): каждая группа `LookupValues` имеет согласованные metadata и ровно один `RecordId|DraftRowToken`.
- **FR-007 — reference precedence** (`WB-007`): валидный `ReferenceRecordId` имеет приоритет; невалидный непустой GUID блокирует; затем допустим только однозначный mapping `ReferenceDraftRowToken -> RecordId`, иначе ссылка пуста.
- **FR-008 — создание draft row** (`WB-008`): уникальный immutable `DraftRowToken` создаётся только workbook-only командой и никогда не является BPMSoft ID/server payload; ручная подстановка блокируется.
- **FR-009 — transactional snapshot** (`WB-009`; общий `FR-007`): до pull создаётся полный `S_*` одного поколения для всех затронутых sheets, включая values/formulas/formatting/validation/hidden/filter/sort state; старый набор заменяется только после полной проверки нового.
- **FR-010 — server-priority refresh** (`WB-010`; общий `FR-007`): actual state обновляется от сервера; local-only item сохраняется `PotentiallyDeleted`, без автоматического `Removed` или delete intent.
- **FR-011 — lossless read-only indexes** (`WB-011`; общий `FR-022`): `Indexes` ОБЯЗАН хранить `IndexUId`, schema layer, member `ColumnUId` и ordinal без вывода membership из `ActualIndexed` или member `.uId`.
- **FR-012 — индексный запрет** (`WB-012`): `INDEX_SYNC_UNRESOLVED` блокирует любой desired index plan/load/apply; add/change/drop index не входят в эту функцию.
- **FR-013 — full-catalog workbook qualification** (`WB-013`): пара полного каталога ОБЯЗАНА пройти OOXML closure, parser cycle, 1:1 source projection, desktop Excel no-recovery/usability и measured scale; bounded `VerifiedBoundedBaseline` — только regression fixture.
- **FR-014 — точный Model Catalog** (`WORKBOOK_CONTRACT_VISION.md §§3–3.4`): `Readme`, `Manifest`, `WorkspaceInventory`, `Schemas`, `Columns`, `Indexes`, `ValidationLists`, `PullConflicts`, `S_*` и их утверждённая семантика ОБЯЗАНЫ сохраняться без добавления caption/index mutation contracts.
- **FR-015 — точный Lookup Catalog** (`WORKBOOK_CONTRACT_VISION.md §§4–4.2`): `Readme`, `Manifest`, `LookupRegistry`, `LookupValues`, `ValidationLists`, `PullConflicts`, `S_*` и нормализованная value model `Null|EmptyString|Value` ОБЯЗАНЫ сохраняться; отдельных `LookupRows`/`LookupColumns` нет.
- **FR-016 — audit/evidence consumer** (`05-verification-operations-spec.md:OPS-001`, `OPS-002`, `OPS-003`; общий `FR-019`): generation, validation и refresh runs ОБЯЗАНЫ использовать общий механизм 001, фиксировать workbook/table versions, modification times, hashes и gates, запрещать raw values/secrets и не перезаписывать runs.
- **FR-017 — skills выгрузки и валидации** (`OPS-012`; общий `FR-021`): skills ОБЯЗАНЫ вызывать документированные workbook CLI contracts, объяснять blocker и обновлять операторские шаги без parallel business logic, credentials или approval decisions.
- **FR-018 — handoff contribution** (`OPS-013`): операторский пакет ОБЯЗАН документировать установку workbook prerequisites, создание/проверку пары, snapshot/refresh failure handling и full-catalog qualification boundary; окончательная clean-machine acceptance выполняется в 004.
- **FR-019 — workbook-only writeback contract** (общий `FR-017`): 002 определяет допустимые target cells и hash precondition для будущей команды 004, но не выполняет writeback и не вызывает BPMSoft.

### Ключевые сущности *(включайте, если функция связана с данными)*

- **WorkbookPair / Manifest**: две книги и общая identity/baseline/version metadata.
- **SchemaRow / ColumnRow / IndexMemberRow**: configuration state с package-layer identity и editable/derived boundaries.
- **LookupRegistryRow / LookupValueGroup**: registry identity и normalized typed values.
- **DraftRowToken**: immutable local correlation token до доказанного server `RecordId`.
- **SnapshotGeneration / PullConflict**: полное pre-pull состояние и cell-level conflict record.
- **WorkbookEvidence**: consumer общего Run/Audit/Evidence contract из 001.

## Критерии успеха *(обязательно)*

### Измеримые результаты

- **SC-001**: 100% строк qualified source представлены в паре ровно один раз либо в explicit unsupported inventory; missing/extra rows равны 0.
- **SC-002**: двукратная generation из одного baseline даёт 100% одинаковые canonical worksheet hashes и общие pair fields.
- **SC-003**: 100% negative parser fixtures блокируются до compare/BPMSoft calls; ложный plan равен 0.
- **SC-004**: обе книги проходят OOXML closure, parser round-trip и открытие/сохранение в desktop Excel без recovery; обязательные filters/resize доступны.
- **SC-005**: при fault injection до проверки нового snapshot прежний `S_*` сохраняется в 100% точек; partial replacement равен 0.
- **SC-006**: проекция `Indexes` содержит 100% доказанных members и 0 inferred members из `ActualIndexed`; index operations равны 0.
- **SC-007**: workbook artifacts каждого run проходят schema/secret scan и связаны с 001; collisions/overwrites предыдущих runs равны 0.
- **SC-008**: независимый оператор по обновлённым шагам создаёт и валидирует пару без устных знаний владельца; решение о full-catalog acceptance остаётся у человека.

## Допущения

- 001 предоставила accepted qualification result либо явно принятый ограниченный список; без этого промышленная full-catalog pair остаётся заблокированной.
- `WORKBOOK_CONTRACT_VISION.md` v1 и bounded template v3 используются как authority/fixture соответственно; конкретные colours и точные имена `S_*` могут быть уточнены без изменения transactional semantics.
- Desktop Excel на Windows — обязательная среда pilot; LibreOffice и non-Windows не входят.
- Не входят: compare/immutable plan, BPMSoft write, delete, index mutation, browser verification, Git finalization и автоматическое разрешение conflicts.
