# Handoff: discovery локального синхронизатора ОМ BPMSoft

> **Актуальная точка возобновления — раздел 16 (2026-09-06).** Разделы 0–15 сохраняют историю решений и evidence. Их старые формулировки «следующий шаг» и прежние stop conditions не применяются, если противоречат разделу 16.

Дата актуализации: 2026-09-04  
Статус: создан первый owner-reviewed draft package для будущего SDD / GitHub Spec Kit. Это не canonical Spec Kit и не разрешение на реализацию, новый BPMSoft probe, `.xlsx` или write. Отдельный strictly read-only `PrototypeReadOnlyPull` по-прежнему является единственным техническим evidence.

## 0. Курс и выбранная последовательность SDD / GitHub Spec Kit — 2026-09-03

### Где искать первоисточник

Канонический локальный курс находится вне этого репозитория: `C:\CodingAgents\codex\projects\VibeCeH\docs\pilot-course\lessons`. Это справочный источник метода; его примеры, учебные результаты и шаблоны не являются артефактами текущего проекта и не должны копироваться или создаваться здесь без отдельного решения владельца.

- Урок 1 — операционная постановка, границы и проверяемый результат: `C:\CodingAgents\codex\projects\VibeCeH\docs\pilot-course\lessons\lesson-01-codex-operational-start\23-lesson-1-confluence-WC_act.md`.
- Урок 2 — SDD, выбор GitHub Spec Kit, `constitution`, `spec`, `clarify`, `plan`, `tasks`, `analyze` и DoD спецификации: `C:\CodingAgents\codex\projects\VibeCeH\docs\pilot-course\lessons\lesson-02-lightweight-spec-and-spec-review\02-lesson-2-confluence-text.md`; сокращённые тематические части: `02a-lesson-2-sdd-framework-and-spec-choice-confluence-text.md` и `02b-lesson-2-spec-kit-and-spec-review-confluence-text.md` в той же папке.
- Урок 3 — технический plan, implementation slices, tasks, plan-before-code, review, verification и handoff: `C:\CodingAgents\codex\projects\VibeCeH\docs\pilot-course\lessons\lesson-03-managed-implementation-by-spec\03-lesson-3A.md` и `05-lesson-3B.md`.
- Урок 4 — orchestration, control gates, evidence и acceptance: `C:\CodingAgents\codex\projects\VibeCeH\docs\pilot-course\lessons\lesson-04-orchestration-control-gates-and-acceptance\05-lesson-4A.md` и `06-lesson-4B_V2.md`.
- `slice-generator` — только последующая упаковка уже утверждённых slices в orchestration workflow: `C:\CodingAgents\codex\projects\VibeCeH\docs\pilot-course\lessons\slice-generator\`. Ключевые файлы: `project-specific-info.md`, `slice-budget-preflight.md`, `next-slice-prompt-pack-generator.md`, `all-slice-prompt-pack-runner.md`, `orchestration-task-template.md`, `acceptance-report-template.md`.

### Применимая последовательность

Курс задаёт не «постепенно кодировать инструмент», а следующую цепочку:

```text
проверяемый контекст и ограничения
-> constitution (устойчивые правила проекта)
-> spec (поведение, scope/out of scope, acceptance)
-> clarify + review / DoD spec
-> implementation plan
-> slices -> tasks -> bounded agent tasks
-> plan-before-code -> implementation -> review -> verification
-> human acceptance report -> handoff
```

`analyze` проверяет согласованность артефактов до реализации. `slice-generator` появляется только после устойчивых `spec`, plan, slices и tasks: он создаёт workflow/prompt-pack, но не реализует и не принимает slice. Для execution-ready Slice 2+ ему необходимы фактические acceptance report и handoff предыдущего slice; они не могут быть придуманы заранее.

Для ОМ сейчас допустим предшествующий инструменту этап: после поочерёдного обсуждения с владельцем создать и совместно править repo-native draft-версии будущих project-wide rules и feature spec. Их нужно хранить отдельно от рабочего layout Spec Kit в `docs\SDD_DRAFTS\`, с явным статусом `DRAFT — NOT YET SPEC KIT CANONICAL`:

- `constitution.draft.md` — только устойчивые правила проекта;
- `first-feature-spec.draft.md` — поведение, scope/out of scope, requirements и acceptance первой feature;
- `clarify-review.draft.md` — вопросы, решения, evidence, assumptions, DoD и блокеры перехода к plan.

Использовать нужно уже подтверждённые discovery facts и contract, явно отделяя их от assumptions и открытых вопросов. Эти drafts не являются `.specify/`, `specs/` или готовыми canonical Spec Kit artifacts; не создавать пока plan/tasks/slices, orchestration packs, acceptance reports и не изменять production-код. После совместного owner-approved review drafts отдельным решением будет определено, переносить ли их в GitHub Spec Kit и начинать `constitution -> spec -> clarify -> plan` workflow.

## 0. Завершённый read-only prototype probe — 2026-09-03

### Что запускалось

Владелец трижды запускал `dotnet run --project C:\CodingAgents\codex\projects\OM_Automatization\PrototypeReadOnlyPull\BpmSoftReadOnlyPull.csproj -c Release` локально. URL и login вводятся интерактивно; login имеет несекретный default `Supervisor`; password запрашивается скрыто и не передавался в чат, файлы, аргументы или PowerShell-команды. Исходный password очищается после login; cookies и CSRF существуют только в памяти завершившегося процесса.

Финальный успешный run: `2026-09-03T12:48:29Z`. Собирался `Release` без warning/error. Статическая проверка исходника подтверждает отсутствие `.xlsx`, `InsertQuery`, `UpdateQuery`, `DeleteQuery`, create/update/delete/save schema endpoints и Manage/Write checks.

### Подтверждённые read-only endpoints и JSON forms

Все HTTP POST ниже являются read/login calls; write API не вызывался:

- `/ServiceModel/AuthService.svc/Login` — login успешен; сам response не сохраняется.
- `/ServiceModel/PackageService.svc/GetPackages` — root: `packages`, `success`, `errorInfo`; 97 packages. Item включает `id`, `uId`, `name`, `version`, `isReadOnly`, dates и package metadata.
- `/ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems` — root: `items`, `success`, `errorInfo`; 9 157 items. Item включает `id`, `uId`, `name`, `title`, `packageUId`, `packageName`, numeric `type`, `modifiedOn`, `isChanged`, `isLocked`, `isReadOnly`, `status`, `isAvailableAsSection`. Local UI `ClientApp/#/WorkspaceExplorer` показал те же 97 packages и согласованный item `AcademyURL` (`URL`, type `3`/«Объект», package `Base`, same modified time). `type=3` подтверждён только для «Объект»; все остальные значения enum пока не отображаются в contract mapping.
- `/ServiceModel/EntitySchemaDesignerService.svc/GetSchema` с `{ "schemaUId": "..." }` — root: `schema`, `success`, `errorInfo`. `schema` содержит как минимум `caption`, `columns`, `inheritedColumns`, `indexes`, parent/reference schema objects, column `uId`/`name`/`type`/`requirementType`/`usageType`/`indexed` и lookup flags. `AcademyURL`: 0 own, 8 inherited, 0 indexes; `ActivityPriority`: 1 own, 8 inherited, 0 indexes.
- `/DataService/json/SyncReply/SelectQuery` с `rootSchemaName`, `rowCount`, `rowsOffset`, `isPageable=true`, `allColumns=true`, `useLocalization=true` — root: `rowConfig`, `rows`, `notFoundColumns`, `rowsAffected`, `nextPrcElReady`, `success`. `rowConfig` описывает `dataValueType` и lookup metadata; a row includes server `Id`, timestamps, lookup wrapper `value`/`displayValue`/`primaryImageValue`, `Name`, `Description`, `ProcessListeners`, `Img`.

`ActivityPriority` с page size 2: offset 0 вернул 2 rows, offset 2 — 1 row, shared server `Id` count = 0. Предыдущий probe без `isPageable=true` вернул повторную первую страницу; это диагностировало и подтвердило обязательный flag. Этот sample подтверждает offset behavior, но не full stable ordering: явный sorting parameter для local 1.8 ещё не verified.

### Безопасный evidence

Финальный evidence: `C:\CodingAgents\codex\projects\OM_Automatization\PrototypeReadOnlyPull\bin\Release\net10.0\probe-output\20260903T124828Z`.

- `packages.response.json` и `workspace-items.response.json` — read-only inventory responses.
- `schema-*.response-shape.json` и `lookup-*.response-shape.json` сохраняют только property names, JSON kinds and array lengths; scalar values lookup rows redacted.
- `summary.json` содержит только target, time, counts, schema counts and pagination metrics.
- Нет login response, password, cookie, CSRF value или raw lookup-row value. Marker scan нашёл `Password`/`UserPassword` только как names workspace items и слово «password» в safety note summary, не credential values.

### Изменения prototype

- Добавлен in-memory `BPMCSRF` header from session cookie after successful login; без него `GetPackages` returned 403.
- Ошибки read request теперь называют endpoint and HTTP status, без response body/secrets.
- Login prompt defaults to `Supervisor`; password remains hidden and has no default.
- Добавлены read-only schema probes (`AcademyURL`, `ActivityPriority`) and redacted `ActivityPriority` data response shapes.
- Добавлен `isPageable=true` to `SelectQuery`; raw data values are not persisted.

### Что не проверено и технический stop condition

Не подтверждены: enum mapping всех workspace `type`; full-catalog traversal and scale; explicit stable sort and a full repeatable pagination proof; schema mapping для всех entity/lookup variants; `Lookup` registry (`LookupRecordId` / `SysEntitySchemaUId`); full lookup value normalization; target fingerprint; workbook generation/validation; Manage/Write permissions, any write semantics, backup, compile, idempotency and `DraftRowToken -> RecordId`.

**Stop condition:** не начинать full-catalog pull, не создавать `.xlsx` и не открывать write scope, пока read-only prototype не подтвердит explicit deterministic ordering and complete pagination (no skips/duplicates across a repeatable full representative data set), а также exact schema/lookup registry mapping required by the approved contract.

Технический следующий шаг при отдельном будущем решении: развивать только `PrototypeReadOnlyPull` для read-only проверки explicit sort/pagination contract and representative schema/lookup-registry/data shapes. Сначала получить local 1.8 evidence for an explicit deterministic order (not guessed) and use it to traverse a representative lookup completely twice; затем сравнить IDs/counts and only reassess readiness for full-catalog schema/data pull. No `.xlsx` before those gates.

## 1. Обязательный режим следующей сессии

- Следующая сессия начинает с совместного формирования входа для GitHub Spec Kit по разделу 0, а не с реализации или нового BPMSoft probe. Задавать не более одного вопроса за раз.
- Не инициализировать GitHub Spec Kit и не создавать/изменять `.specify/`, `specs/`, canonical `constitution.md`, `spec.md`, `plan.md`, `tasks.md`, `implementation-slices.md`, `docs/orchestration/`, prompt-packs, acceptance reports или новые handoff-файлы без нового явного решения владельца. Разрешены только три draft-файла в `docs\SDD_DRAFTS\` из раздела 0 после обсуждения с владельцем.
- Не выполнять `/speckit.*`, не писать production-код и не менять старые исходники в `SyncOM/`. Новый technical probe разрешён только после отдельного решения владельца, только в `PrototypeReadOnlyPull`, только strictly read-only и без `.xlsx` до прохождения API gates.
- Этот файл является передачей контекста, а не canonical спецификацией или планом.
- Текущая цель следующей сессии: на базе этого handoff, `WORKBOOK_CONTRACT_VISION.md` и источников курса совместно сформировать, затем создать и править три draft-файла в `docs\SDD_DRAFTS\`: цель, пользователи и сценарии, границы, out of scope, устойчивые правила, requirements, acceptance criteria, факты/evidence, assumptions и вопросы. Если для этого не хватает конкретного факта, остановиться на одном исследовательском вопросе; не подменять его реализацией.

## 2. Текущая цель владельца

Первая версия — надёжный локальный синхронизатор между локальной `.xlsx`-книгой и локально развёрнутым стендом BPMSoft.

Ближайший проверочный сценарий:

1. Локальный стенд `http://localhost:8002/` намеренно чистый.
2. Google-книги ОМ и справочников изучены только как источник существующего пользовательского формата и предметной логики. Их данные не относятся к локальному стенду и не должны загружаться на него.
3. После согласования Excel-контракта нужно получить из локального стенда его фактические ОМ и строки справочников и сформировать локальную книгу или книги, логически аналогичные Google-книгам, но не обязанные повторять их пиксель-в-пиксель.
4. Владелец укажет минимальные изменения ОМ и справочников.
5. Эти изменения будут внесены сначала в локальный Excel.
6. Затем на основе Excel нужно провести строго read-only compare, показать неизменяемый change plan и только после отдельного подтверждения попробовать минимальный apply на выделенном локальном стенде.

До явного подтверждения логического Excel-контракта нельзя создавать `.xlsx`, ранжировать финальную карту delivery slices или начинать реализацию.

## 3. Зафиксированные продуктовые решения

### 3.1. Технологическое направление

Согласована стартовая архитектура: **.NET 10 LTS / C# + guided CLI + локальный HTML-отчёт**.

- Один self-contained Windows executable предпочтителен для первого выпуска.
- CLI ведёт пользователя по сценарию; HTML служит только для просмотра отчёта и не имеет кнопки Apply.
- WPF/desktop UI возможен позже поверх того же прикладного ядра.
- `.NET 8` как клиент не выбран из-за близкого завершения поддержки; локальный BPMSoft работает на .NET 8, но это не требует той же версии runtime от клиента.
- Python CLI и Electron/Node обсуждались, но не выбраны: C# лучше соответствует Windows-развёртыванию, типизированным контрактам и будущему desktop UI.

### 3.2. Основной безопасный workflow

```text
read/validate workbook
  -> deterministic strictly read-only compare
  -> human-readable report + saved machine-readable immutable change plan
  -> whole-plan approve or reject
  -> explicit apply of that exact unchanged plan
  -> read-back verification
  -> optional Excel writeback
  -> audit
```

- Пользователь принимает или отклоняет план целиком. Выбора отдельных операций внутри плана нет.
- План связывается с хэшем книги, версией шаблона, целевым стендом и отпечатком прочитанного состояния. Изменение книги, плана или затронутого состояния стенда инвалидирует план и требует нового compare.
- Push и pull — отдельные направленные планы. Скрытого двустороннего merge нет.
- Compare всегда read-only; Apply не должен молча пересчитывать diff.
- Удаления объектов, полей и строк BPMSoft запрещены в MVP.
- Все чтения коллекций обязаны использовать проверенную пагинацию и стабильный порядок.
- Повторный запуск должен быть идемпотентным: уже успешно достигнутое состояние не создаёт повторных изменений.

### 3.3. Ручной backup и восстановление

- Резервное копирование и восстановление БД в первой версии выполняются человеком вручную.
- Синхронизатор только напоминает сделать backup и ждёт подтверждения «backup сделан»; он не проверяет наличие/валидность копии и сам backup не запускает.
- Один целиком утверждённый plan может выполняться этапами:
  - `B0` перед изменениями ОМ;
  - применение и read-back verification ОМ;
  - `B1` перед загрузкой строк справочников;
  - применение и read-back verification строк справочников.
- Граница этапов: сначала схемы/поля и определения справочников, после успешного применения/компиляции/проверки — данные справочников.
- При ошибке выполнение останавливается, Excel writeback не выполняется, сохраняется incident bundle и состояние `FAILED / AWAITING ROLLBACK DECISION`.
- Решение принимает человек после диагностики: восстановить `B1` и оставить ОМ, восстановить `B0` и откатить всё либо пока не восстанавливать. Инструмент не выбирает backup автоматически.
- После ручного восстановления нужен отдельный read-only verify. Старый план нельзя автоматически продолжать.
- Архивы исходного кода в рабочей папке не являются доказательством backup БД.

### 3.4. Credentials

- Пароль вводится при каждом запуске через скрытый prompt.
- Для непрерывной сессии `compare -> review -> apply -> verify -> optional writeback` достаточно одного ввода, пока серверная сессия действительна.
- После login исходный пароль очищается; cookies и CSRF живут только в памяти процесса.
- При перезапуске, истечении сессии или новом плане пароль вводится снова.
- Пароль не хранится в книге, исходниках, конфигурации, командной строке или логах.
- URL стенда и login могут храниться как несекретная локальная настройка.

### 3.5. Обратное обновление Excel из BPMSoft

Excel не является единственным источником истины: небольшие изменения ОМ могут выполняться человеком прямо в BPMSoft, после чего локальную книгу нужно обновить по реальному состоянию стенда.

- Pull должен охватывать структуру ОМ, метаданные справочников и строки справочников.
- Перед pull создаются полные снимки всех затронутых рабочих листов внутри книги с сохранением значений, формул, форматирования, validation, скрытых строк/колонок, фильтров и сортировки.
- Текущее состояние стенда записывается в соответствующие рабочие листы.
- Конфликтующие ячейки выделяются цветом в снимках/представлении конфликта. Точные имена служебных листов, цвета и retention ещё не согласованы.
- Локальные элементы, отсутствующие на стенде, остаются в рабочих листах и помечаются как **потенциально удалённые**. Статус `Removed` автоматически не выставляется.
- Пользователь сам решает, когда поставить `Removed`.
- Неразрешённые метки не блокируют весь push, но такие элементы исключаются из push-plan до явного решения пользователя; формируется предупреждение и audit-запись.
- Статус `Removed` в Excel не вызывает delete/update в BPMSoft; такие строки исключаются из push, а расхождение может показываться информационно.
- Пользователь сам отвечает за объём и очистку накопленных snapshot-листов.

### 3.6. Writeback после Apply

Поведение должно быть аналогично старому инструменту, где результат не записывался в Google Sheets автоматически как часть apply.

- После успешного Apply и read-back verification программа спрашивает, нужно ли выполнить отдельный Excel writeback.
- Тот же writeback можно запустить вручную позже.
- В scope writeback: заполнение `!Id` у строк справочников и только отдельно доказанное обновление флагов Required/Index по фактическому состоянию BPMSoft. Index membership из `schema.indexes[]` не является автоматическим oracle для исторического column `Index`/`ActualIndexed` flag.
- Writeback — отдельная workbook-only операция; она не пишет в BPMSoft.
- Если книга изменилась после apply и её хэш не совпадает, программа ничего не записывает и показывает уведомление. Пользователь сам запускает новый цикл; merge не выполняется.
- В audit фиксируются хэши до/после и список изменённых ячеек без секретов.

### 3.7. Scope первой пробной выгрузки

- Владелец выбрал весь доступный каталог чистого стенда, а не минимальный пакет.
- `WorkspaceInventory` должен фиксировать все доступные workspace items, включая неподдерживаемые для структурной синхронизации; последние не должны исчезать молча.
- Точная граница типов, для которых v1 извлекает детальную структуру помимо `EntitySchema`, ещё не согласована.

### 3.8. Связь двух Excel-книг и отпечатки

- Предварительно рекомендованы две локальные книги: ОМ и справочники. Внешние Excel-формулы и `IMPORTRANGE` не используются: их кэш, пересчёт и путь к файлу не дают детерминированного входа для compare.
- `PairId` связывает две книги как одну пару; `PullRunId` отмечает одну конкретную выгрузку. Оба значения должны совпадать в manifests обеих книг.
- Связь модели со справочником должна быть явной в таблицах, а не вычисляться формулой. После уточнения identity она строится по BPMSoft `UId` схемы, а до создания новой схемы — по валидируемому `SchemaName` только внутри неизменного plan.
- В manifest допустим `PairBaselineHash` канонического server-origin состояния и baseline `TargetFingerprint`. Хэш текущего содержимого книги и fingerprint шаблона считаются перед compare и повторно перед apply, хранятся в plan/audit, а не внутри самой книги: полный file hash нельзя безопасно записать в тот же файл без самоссылки.
- Владелец утвердил две физические книги как обязательную логическую пару; несовпадение `PairId` или `PullRunId` блокирует pull/compare/writeback.

### 3.9. Идентификаторы и связи BPMSoft для Excel-contract v1

Решение владельца: **BPMSoft сам генерирует GUID для новых схем, колонок, записей реестра справочников и строк справочников. Excel не генерирует BPMSoft GUID и получает их только через успешный read-back/writeback.**

- Для связей configuration metadata используется `SchemaUId` / `SysSchema.UId`, а не `SysSchema.Id`. `SysSchema.Id` может быть зафиксирован как наблюдаемое derived metadata, но не является ключом связи в Excel.
- Existing schema: `SchemaUId` обязателен. Existing column: `ColumnUId` плюс принадлежность схеме. Lookup-column хранит `ReferenceSchemaUId`; связь наследования — `ParentSchemaUId`.
- Запись системного объекта `Lookup` имеет собственный `LookupRecordId` и связывается со схемой через `SysEntitySchemaUId`. Это отличается от схемы справочника и от строк его данных.
- Строка данных справочника идентифицируется её серверным `Id`. Значение lookup-колонки в данных — `...Id` целевой записи, не `SchemaUId` целевой схемы.
- Для новых schema/column/lookup registry rows соответствующие server GUID-поля пусты до read-back. Их временная связь в declared desired state задаётся строго валидируемыми кодами `SchemaName`, `ColumnName` и `ReferenceSchemaName`; эти коды не являются fallback identity существующих объектов.
- Для новой строки справочника без server `Id` используется `DraftRowToken`: это локальный, неизменяемый корреляционный маркер Excel, не BPMSoft GUID и не часть запросов к серверу. Он нужен, чтобы writeback мог безопасно заменить именно эту строку на server `Id`, даже если `Code`/`Name` пусты или неуникальны.
- После успешного staged apply и read-back workbook-only writeback заполняет server IDs, заменяет связанные `DraftRowToken` в том же workbook context и фиксирует mapping в audit. Если API не возвращает или не позволяет однозначно восстановить `Id` созданной неоднозначной строки, её создание — stop condition, а не повод сопоставлять по `Name`.
- Публичная документация BPMSoft подтверждает UUID для полей «Справочник» и «Уникальный идентификатор», а также использование `SysSchema.UId` в configuration-связях. Точные имена полей и семантика ответов API локальной версии 1.8 должны быть read-only подтверждены до реализации.

### 3.10. Утверждённый workbook contract v1

- В `docs/WORKBOOK_CONTRACT_VISION.md` зафиксирован подтверждённый logical contract v1: две физические книги (`BPMSoft.ModelCatalog.xlsx` и `BPMSoft.LookupCatalog.xlsx`) как одна логическая пара, exact sheets/columns, editable/derived rules, lifecycle pull/compare/writeback и реальные открытые риски.
- Это утверждённый вход для следующего strictly read-only шага, но не разрешение на production-код, write API или изменение `SyncOM/`.

### 3.11. Принятые уточнения contract и отказанные варианты

- Full inventory содержит только лёгкие поля `WorkspaceItemUId`, `Name`, `ItemType`, `PackageName`, `PackageUId`, `SupportStatus`, `SupportReason`; детально читаются только `EntitySchema`, включая lookup schemas. Клиентские схемы, исходный код и иные items не читаются как структура и не изменяются.
- В `ModelCatalog` приняты sheets `Readme`, `Manifest`, `WorkspaceInventory`, `Schemas`, `Columns`, `Indexes`, `ValidationLists`, `PullConflicts` и один набор `S_*`. В `LookupCatalog` — control sheets, `LookupRegistry` и единый `LookupValues`; отдельные `LookupRows` и `LookupColumns` отвергнуты как дублирование структуры.
- Snapshots не являются историческим архивом: в каждой книге ровно один полный пред-pull набор; новый заменяет старый только после успешной записи и проверки нового набора.
- Captions и rename schemas/columns/lookups исключены; попытка даёт `RENAME_NOT_SUPPORTED` с указанием менять вручную в BPMSoft. Изменение `DataType` existing column исключено (`TYPE_CHANGE_NOT_SUPPORTED`).
- У existing own column разрешены изменения Required в обе стороны; simple-index add остаётся лишь будущей возможностью после dedicated field-level write preflight, а сейчас не разрешён. Снятие индекса — `INDEX_DROP_NOT_SUPPORTED`, вручную через БД. `UsageType`, `IsSimpleLookup`, `IntegrityMode`, `CascadeMode` исключены как неподтверждённые и ненужные v1 properties.
- Lookup registry может быть создан для новой lookup schema либо для existing `EntitySchema`, но второй сценарий не начинается без read-only API evidence. Package — единственный source в `ModelCatalog.Schemas`; base schema fields registry — read-only context.
- `LookupValues` — единая нормализованная source-of-truth таблица для metadata записи и значений её колонок. Metadata повторяется только внутри группы значений одной записи и обязана быть согласованной; отдельного `LookupRows` нет. `DraftRowToken` выдаётся только явной workbook-only командой, ручной ввод запрещён; без доказуемого `DraftRowToken -> RecordId` новая строка не создаётся.
- Excel validation: enumerations states, Boolean Required/Indexed, UUID input syntax, mandatory names, XOR ID/token; formulas в editable tables, external links и неописанные колонки — parser blocker.
- Журнал запусков и пакет ошибки находятся вне книг, только с добавлением, без секретов и полных lookup values по умолчанию. Путь/формат/срок хранения решаются перед первым write-capable шагом.

## 4. Что изучено в реальных Google-книгах (read-only)

Источники:

- OM: https://docs.google.com/spreadsheets/d/1djyYw7oV-N_-V9JI3naCD8RQ6yH__vq8Wsf3aZUOge4/edit
- Lookups: https://docs.google.com/spreadsheets/d/1WS1Tjc0JNjYy1ew6CMmcEJxwkB_DR_gqLtb31ZmvAB0/edit?gid=1908152953#gid=1908152953
- Wiki: https://wiki.nobilis.team/pages/viewpage.action?pageId=132220966

Книги были изучены фактически, включая экспорт `.xlsx`; выводы не основаны только на wiki или Apps Script. Экспорты использовались как временные read-only материалы и не добавлялись в проект.

### 4.1. Книга объектной модели

Пять листов:

| Лист | Видимость/диапазон | Назначение |
|---|---|---|
| `Fields` | visible, `A1:AG2680` | Поля объектов, пользовательские атрибуты и производные данные для sync/diagram |
| `Objects` | visible, `A1:U313` | Объекты/разделы, подписи, типы и diagram-поля |
| `Lookups` | visible, `A1:O988` | Импорт реестра из второй Google-книги |
| `SheetInfo` | visible, `A1:I76` | Статусы, типы, пакеты, defaults, ссылка на lookup-книгу |
| `SheetInfo2` | hidden, `A1:BN1000` | Формульные helper-диапазоны для типов/lookup values |

`Fields`, 33 колонки:

```text
Object | Name EN | Name RU | Type | Link | Lookup | Group | Status |
CreatedOn | Jira Link | Comment | Глобал. поиск | Логирование |
Индекс (Инверт.) | Обяз. | Ссылка Активна | Скрыть на Диаграмме |
Пакет | Индекс (Авто) | Ссылка Активна(Авто) |
Скрыть на Диаграмме(Авто) | Draw-ElementRU+Type | Draw-Link |
!Draw-Group | RowNumber | RowNumber Object | Draw-Element FillTopLine |
Object GroupCnt | Draw-Element PositionY | Draw-Element Type |
Draw-Link Id | Json | Draw-Link Points
```

- Человеческие/script inputs: `Object`, `Name EN`, `Name RU`, `Type`, `Lookup`, `Group`, `Status`, `Jira Link`, `Comment`, `Глобал. поиск`, `Логирование`, `Индекс (Инверт.)`, `Обяз.`, `Ссылка Активна`, `Скрыть на Диаграмме`, `Пакет`.
- `CreatedOn` заполнялся скриптом; `Link`, `Индекс (Авто)` и большая часть `S:AF` производные/формульные; `AG` использовался диаграммным скриптом.
- `V:AG` скрыты; 45 строк скрыты; заморожена первая строка.
- Validation: `Type -> SheetInfo!B2:B17`, `Status -> SheetInfo!A2:A9`, `Пакет -> SheetInfo!A17:A42`.
- Filter `A1:K2547`; сохранённая сортировка `CreatedOn desc`, затем `Name EN asc`, `Object asc`, что расходится с описанной в wiki сортировкой.
- Условного форматирования нет. Named ranges имеют только технический характер фильтра.

Наблюдаемые данные: 544 поля, 36 объектов. Статусы: 480 `Done`, 45 `Removed`, 9 `Developer`, 8 `Analytic`, 2 пустых. Lookup-полей 240. Дубликатов по `Object + Name EN` не найдено. Есть два активных поля с пустым статусом, опечатка типа `String (Inifinity)` и значения с лишними пробелами — это материал для validation tests.

`Objects`:

```text
[link/derived] | Name EN | Name RU | Type | hide other fields |
position/color/height and other diagram fields | Глобал. поиск | Логирование
```

- Пользовательски значимы прежде всего `Name EN`, `Name RU`, `Type`, флаг скрытия, глобальный поиск и логирование.
- `F:S` в основном скрыты и относятся к диаграмме; диаграмма вне MVP.
- 37 строк: 26 объектов и 11 разделов; дубликатов имён нет.
- Validation есть для имени объекта и типа; фильтра нет.

`Lookups!A1` использует `IMPORTRANGE(SheetInfo!G16,"Lookups!A1:P1000")`, связывая две Google-книги. В локальном Excel эта Google-зависимость не должна переноситься автоматически.

`SheetInfo` задаёт статусы полей `0.New`…`7.Removed`, типы `Lookup`, строки разных размеров, `DateTime`, `Date`, `Boolean`, `Integer`, варианты `Double`, `Guid`; default package `Nobilis.Base` и список пакетов `Nobilis.Base/Integration/Common/Logging`.

### 4.2. Книга справочников

73 листа: `Lookups`, `SheetInfo`, `Template` и 70 листов данных справочников.

Реестр `Lookups`, 15 колонок:

```text
Name RU | Name EN | Link | Есть значения | Отображ. Списком | Status |
Jira | Базовый объект | Пакет | Comment | ListId | Json |
Row Cnt | Json Rows | Lookup Values
```

- Редактируемые человеком: `Name RU`, `Name EN`, `Отображ. Списком`, `Status`, `Jira`, `Базовый объект`, `Пакет`, `Comment`.
- `ListId` — gid листа; `Link`, `Есть значения`, `Json`, `Row Cnt`, `Json Rows`, `Lookup Values` производные.
- Validation: `Status`, `Пакет`, `Базовый объект`; первая строка заморожена.
- Формула `Link` содержит старый жёстко заданный ID другой Google-книги — исторический дефект.
- `Json` строится только для статусов `Developer`/`Done`; `Json Rows` сериализует данные листа.
- 73 записи: 49 `Done`, 7 `Analytic`, 6 `Business`, 6 `Developer`, 5 `Removed`.
- Базовый объект: 43 `SmBaseCodeLookup`, 29 пустых/default, 1 `BaseEntity`; package во всех строках пустой и брался из default `Nobilis.Base`.
- Пять строк без `ListId`. Есть дубликат `Name EN = SmAccountPromocodeSorry` и подозрительная подпись `AccountType = SmLeadOrigin`.

`SheetInfo` задаёт статусы `0.New..5.Removed`, варианты базового объекта (`SmBaseCodeLookup`, `BaseCodeLookup`, `BaseLookup`, `BaseEntity`), default base `SmBaseCodeLookup` и default package `Nobilis.Base`.

`Template`:

```text
Name (Name - String250) | Код (Code - String250) |
Внешний код (SmExternalCode - String500) |
Описание (Description - StringInfinity) | !Comment
```

Формат колонок data-листа: `Человеческая подпись (BPMColumn - Type)`. Колонки с `!` старый инструмент не синхронизирует как обычные поля; `!Id` — GUID записи BPMSoft.

Среди 70 data-листов найдено 16 разных сигнатур; только 54 следуют стандартному шаблону. Значимые исключения:

- `OpportunityTag` — единственный лист с `!Id`, значения пусты.
- Несколько листов содержат ссылки на другие справочники и нестандартные поля; встречаются Boolean и Integer.
- `SmTeamRole` имеет скрытые старые колонки `B:E` и новый дублирующий набор `J:M`; смысл неоднозначен.
- Есть заполненные колонки без заголовка. Новый parser не должен молча сопоставлять их по позиции.
- Excel ограничивает имя листа 31 символом; два Google-листа длиннее и при экспорте усечены: `SmQualityControlOpportunityLosingResoluton` и `SmRestaurantReactivationRejectReason`. Контракт «один справочник = лист с точным именем схемы» нельзя принять без mapping.
- В `SmTestLookup`, `SmQualityControlChecklistResult`, `ActivityType` есть строки без пригодной identity.
- `Name` не уникален во всех листах: дубликаты есть в `SmLeadOrigin` и особенно в `ActivityCategoryResultEntry`.

Всего обнаружено 859 строк данных. Старая эвристика identity: сначала `Id`, затем `Code`, затем `Name`; для нового контракта её ещё нужно формально утвердить и определить поведение при пустых/дублирующихся значениях.

Технические свойства: VBA/macros, external workbook links, connections и Excel comments не найдены; named range `TriggerLookupChange` технический; все data-листы видимы; `SmTeamRole` скрывает `B:E`; фильтры есть у `ActivityResult` и `OpportunityStage`; большинство листов фиксируют первую строку.

## 5. Wiki и старый код

Confluence page: `Синхронизатор ОМ и генератор ЕР-диаграммы V1.0`, page ID `132220966`, обновлена 2025-04-30.

Старая архитектура:

```text
Google Sheets -> Apps Script -> HTML/JavaScript dialog
  -> localhost nginx for CORS/cookies/CSRF -> BPMSoft internal services
```

| Путь | Назначение | Состояние |
|---|---|---|
| `SyncOM/functions.gs` | Меню, чтение Sheets, JSON, diagram/writeback | Изучен; не менять |
| `SyncOM/ModelFunctions.html` | Login, compare/apply ОМ и справочников | Изучен; не архитектурный образец |
| `SyncOM/ModelFunctionsSettings.html` | URL/credentials/mode | Изучен |
| `SyncOM/NginxConfigBackup.html` | CORS/cookie proxy | Изучен; вне MVP |
| `SyncOM/DiagramDownload.html` | Повреждённая копия downloader | Не использовать |
| `BPMSyncCodeBackup-Nopass/BPMSyncCodeBackup/DiagramDownload.txt` | Исправная версия downloader | Сопоставлена |
| `SyncOM.7z` | Нешифрованный архив исходников | Проверен |
| `BPMSyncCodeBackup.7z` | Шифрованный архив | Пароль не предоставлен |
| `ChatExport_2026-09-02/messages.html` | История передачи | Изучена |

Используемые старым клиентом endpoints:

- `/ServiceModel/AuthService.svc/Login`
- `/ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems`
- `/ServiceModel/PackageService.svc/GetPackages`
- `/ServiceModel/EntitySchemaDesignerService.svc/GetSchema`
- `/DataService/json/SyncReply/SelectQuery`
- DataService insert/update;
- EntitySchemaDesignerService create/update schema/columns.

Исторические дефекты, которые должны стать test cases: `Math.random()` в license-ветках; credentials в browser JS и defaults `Supervisor/Supervisor2`; фиксированные лимиты вместо пагинации; `indexOf(callback)`; `fields.length` на object; `isRequire/requireType` typo; отсутствие saved plan/audit/idempotency; разрушительное предварительное удаление `_SyncData`.

Старые writeback-команды: `Fill lookup IDs` записывает `!Id`; `Update table flags` записывает Required/Index; после lookup apply инструмент только напоминает обновить IDs. Создание/изменение `_SyncData` исключено из текущего MVP без нового явного решения.

## 6. Что проверено на локальном BPMSoft

Стенд: `http://localhost:8002/`.

- Стенд намеренно чистый и не соответствует данным Google-книг.
- Путь установки: `C:\Creatio\BpmSoftStandSetup\Constructor_1.8.0.14107_Net8_PostgreSQL`.
- Версия/дистрибутив: `1.8.0.14107`, BPMSoft/BPMStudio, `BPMSoft.WebHost.dll` на Kestrel, PostgreSQL.
- Anonymous request перенаправляет на `/Login/Login.html`.
- `window.isNtlmLoginVisible=false`; использован обычный username/password login.
- `/swagger/index.html` вернул 404; публичный Swagger не обнаружен.
- Конфигурационные файлы с возможными секретами не читались.

Read-only inspection assemblies подтвердил маршруты `/ServiceModel/` и `/0/ServiceModel/`, JSON POST для package/workspace/schema services, отдельные `CanView`/`CanManage`, свойства пагинации `SelectQuery` и наличие own/inherited columns/indexes в `GetSchema`.

Живой read-only probe выполнен после интерактивного ввода credentials во временном окне. Credentials отправлялись только на localhost, не сохранялись и не логировались.

| Проверка | Результат |
|---|---|
| Login | Успешно |
| `GetPackages` | `success=true`, 97 пакетов, `Nobilis.*` нет |
| `GetWorkspaceItems` | `success=true`, 9 157 workspace items |
| Чтение схемы | `AcademyURL`, package `Base`, 0 own columns, 8 inherited columns, 0 indexes |
| `SelectQuery` | `ActivityPriority`: page size 2, offset 0 — 2 строки, offset 2 — 1 строка, overlap 0 |

`SmLead` отсутствует, поэтому использовалась `AcademyURL`. Текущая учётная запись имеет права просмотра конфигурации и строк `ActivityPriority`. Права управления/записи не проверялись. Две страницы подтверждают `RowsOffset`, но не полную стабильную пагинацию: нужны stable sort и проверка отсутствия пропусков/дубликатов на полном наборе.

## 7. Матрица «проверено / ещё предстоит»

| Область | Уже проверено | Ещё не проверено / stop condition |
|---|---|---|
| Google as-is | Реальные книги, листы, формулы, validations, скрытые области, фильтры, связи и исключения | Повторять без конкретной причины не нужно |
| Excel to-be | Logical Excel contract v1, physical pair, sheets, exact columns, validations, identity rules и реальные risks утверждены в `WORKBOOK_CONTRACT_VISION.md` | До следующего строго read-only шага `.xlsx` ещё не создавалась; exact API/read pagination и scale требуют проверки |
| BPMSoft identity/version | URL, дистрибутив, login flow, read endpoints | Нужен target fingerprint для plan |
| Packages/workspace | Читаются 97 packages и 9 157 items; scope выбран: весь доступный каталог | Уточнить поддерживаемые типы для детальной структурной выгрузки, сохраняя полный inventory |
| Schema read | Прочитана одна простая схема | Types, localization, own/inherited columns, indexes, extensions, ownership на репрезентативных схемах |
| Lookup rows | Одна таблица прочитана двумя страницами | Полная stable pagination, references, nulls, system columns, большие наборы |
| Permissions | View/read подтверждены | `CanManage` и write endpoints не проверялись; до контрактов и backup-confirmation писать нельзя |
| Compare/plan | Согласована safety-модель; приняты `UId`/`Id`/`DraftRowToken` rules | Реализации/fixtures нет; normalization, exact API response fields и correlation created rows требуют проверки |
| Apply/idempotency | Согласованы safety rules | Write API, compile, read-back и failure semantics не проверялись |
| Backup/rollback | Согласован ручной B0/B1 | Реальный backup/restore БД не выполнялся |
| Pull/conflicts | Server-wins, full snapshots, potential deletion marker | Не утверждены layout, цвет, sheet names, retention, очистка markers |
| Excel writeback | `!Id`, Required/Index, отдельный запуск | Не проверены locking, hash race, preservation и atomic replacement |
| Audit | Состав согласован | Не утверждены формат, storage, retention, incident bundle |
| Tests | Нужны unit + dedicated-stand integration | Test matrix/fixtures и isolated write-test entities не согласованы |

## 8. Обязательные свойства MVP

- Локальное чтение и валидация `.xlsx`.
- Детерминированный strictly read-only compare.
- Читаемый отчёт и машиночитаемый immutable change plan.
- Apply только целиком подтверждённого и неизменённого плана.
- Audit: источник, хэш/версия книги, target fingerprint, plan, stage, операции, ответы, ошибки и rollback decision без секретов.
- Пагинация; dry-run; идемпотентность; read-back verification; отсутствие скрытых/случайных изменений.
- Автотесты Excel parsing, diff, request building; integration tests только на выделенном стенде.
- Секреты не хранятся в книге, исходниках, командной строке или логах.
- Удаления и `_SyncData` не входят в текущий MVP.

Вне MVP: ER/draw.io, Google Drive/Sheets, Apps Script, nginx/CORS proxy, license binding, obfuscation, автоматический backup/restore и автоматический production run.

## 9. Следующая рекомендуемая последовательность discovery

1. Не пересматривать scope или contract без нового конкретного возражения. После каждого сообщения кратко фиксировать факты, решения, допущения и один следующий вопрос.
2. Сначала запустить собранный `PrototypeReadOnlyPull` с интерактивным скрытым вводом credentials: он проверяет login, packages и workspace inventory, сохраняет только безопасные JSON responses и не создаёт `.xlsx`.
3. На реальной форме JSON ограниченно развить этот prototype для read-only exact API fields, pagination/stable ordering, полного scope и масштаба файла/snapshot. При несоответствии не фабриковать mapping и не начинать write.
4. Проверить созданную пару parser/read-back: `PairId`/`PullRunId`, sheets, exact columns, validations, ID relations, отсутствие внешних links/formulas и сохранность единственного snapshot-набора.
5. Затем получить от владельца минимальный набор Excel-изменений и выполнить strictly read-only compare с immutable plan.
6. Перед любым write отдельно выполнить write preflight: Manage/Write, create/save/read-back response, compile, failure semantics, `DraftRowToken -> RecordId`, журнал/пакет ошибки и ручной `B0`; после этого запросить отдельное явное согласие владельца.
7. Только затем возможен staged apply целиком принятого plan. Без нового явного согласия write-вызовы запрещены.

## 10. Открытые вопросы

Это не вопросы о contract, а точные технические открытые вопросы, которые нельзя закрывать предположением:

1. Exact JSON fields/semantics BPMSoft 1.8 для `SchemaUId`, `SysSchemaId`, `ColumnUId`, `ReferenceSchemaUId`, `LookupRecordId`, `SysEntitySchemaUId`, `RecordId`, indexes и required flags.
2. Stable ordering, pagination, target fingerprint и условие целостности full-catalog read.
3. Измеренный размер/скорость full-catalog и единственного snapshot-набора; technical sheet names/colours conflict view.
4. Exact API semantics Required/add-index, регистрации existing `EntitySchema` как lookup, create/save response, compile и atomicity.
5. Доказуемый путь `DraftRowToken -> RecordId` без matching по `Code`/`Name`.
6. Путь, формат и retention журнала запусков/пакета ошибки.

## 10.1. G3: P3-C bounded index-resolution — 2026-09-04

Владелец явно одобрил минимальную contract correction только для P3-C; это **не G4** и не разрешение на `.xlsx`, production tool, full-catalog pull, Manage/Write или BPMSoft write. Однократно разрешённое игнорирование usage-gate `PAUSE` относится к этой документной правке и не расширяет технический scope.

Источник решения и evidence:

- `docs\READ_ONLY_RESEARCH_RESULTS\20260904T151951Z-Account-Test1-index-research.md`;
- `docs\READ_ONLY_RESEARCH_RESULTS\20260904T151951Z-Account-Test1-index-evidence-review.md`;
- `PrototypeReadOnlyPull\bin\Release\net10.0\probe-output\20260904T151951Z\schema-Account-Test1.indexes.shape.json`;
- previous safe mappings `...\20260904T132710Z\schema-Account-Base.mapping.json` and `schema-Account-extension.mapping.json`.

Bounded G2 resolution: `Account/Test1` had two explicit simple one-member index objects. The evidence proves `schema.indexes[].uId`, `.name`, `.isUnique`, and member `.columns[].columnUId`; `columnUId` resolves to `Code`/`Name` `ColumnUId`. The index definition is in the Test1 package layer, while the target columns are inherited there; requiring own targets and `indexed=true` was a false-negative prototype oracle. Therefore `Indexes` is a protected read-only table with one row per member, `IndexUId` is retained, and member `.uId` is explicitly not `ColumnUId`. `Columns.ActualIndexed` remains separate historical-Google compatibility context, not the membership source and not automatically derived from the index array. Historical Google `bpmColumn.indexed` was a coarse/lossy flag and is not a new source of truth; Google data are not imported.

Still open: composite indexes, auto-name, broader `orderDirection` enum/semantics, full-catalog generalisation, target fingerprint across the two bounded reads, current inherited `column.indexed` semantics, and all add/change/drop API semantics.

## 10.2. G4: controlled workbook delivery — 2026-09-04

Владелец принял read-only evidence с перечисленными ограничениями, счёл G2 закрытым для workbook v1, принял исправленный contract и отдельно разрешил G4: контролируемую read-only выгрузку, создание и проверку двух `.xlsx` и минимального локального инструмента наполнения. BPMSoft Write/Manage, Google input и изменение `SyncOM` не разрешены.

Полный каталог не является acceptance condition текущего исследования или первой workbook delivery. Сейчас разрешён verified bounded baseline; попытку полного каталога должен выполнять уже разработанный инструмент с сохранением ordering/pagination и evidence controls.

Обязательный следующий контроль: во время выгрузки и workbook verification отдельно доказать, что `Columns.ActualIndexed`, который не выводится автоматически из `schema.indexes[]`, не скрывает и не искажает фактический состав `Indexes`. Перед любым будущим index load/apply требуется новый самостоятельный gate: доказать, что разделение не создаёт ложные add/drop операции. При неоднозначности или неверном плане применяется `INDEX_SYNC_UNRESOLVED`, работа останавливается для решения владельца; допустимый результат — исключить загрузку индексов из текущей версии и оставить их только как read-only evidence.

## 11. Оговорки для следующего исполнителя

- Не принимать Google data за desired state локального стенда.
- Не считать успешный login доказательством write permissions.
- Не считать две страницы `ActivityPriority` полной проверкой пагинации.
- Не смешивать `SysSchema.Id`, `SysSchema.UId`, идентификатор записи данных `Id` и `UId` колонки: это разные уровни модели. Для Excel-контракта metadata links строятся по `UId`, data links — по `Id`.
- Не генерировать client-side BPMSoft GUID для новых схем, колонок, lookup registry records или data rows. Пустой server ID до apply — нормальное состояние; заполнение разрешено только read-back/writeback.
- `DraftRowToken` — не server identity и не payload BPMSoft. Он допустим только как локальный корреляционный маркер новой неоднозначной строки справочника.
- Не считать публичную документацию доказательством exact JSON contract локальной версии 1.8: имена полей и create/save response должны быть verified read-only до реализации.
- Не считать HTML-отчёт интерфейсом Apply.
- Не называть staged apply транзакцией: backend atomicity не подтверждена, компенсация ручная через backup/restore.
- Не фабриковать acceptance evidence предыдущих slices. Для будущего Slice 2+ реальный acceptance report/handoff предыдущего slice будет execution-time входом.
- Рабочая папка на момент актуализации не является Git repository; `git status` недоступен.

## 12. SDD drafts и следующий review — 2026-09-04

### Пути и статус

- `docs\SDD_DRAFTS\constitution.draft.md` — **DRAFT — NOT YET SPEC KIT CANONICAL**; устойчивые project-wide rules.
- `docs\SDD_DRAFTS\first-feature-spec.draft.md` — **DRAFT — NOT YET SPEC KIT CANONICAL**; верхнеуровневая `spec of specs` полного MVP synchronizer.
- `docs\SDD_DRAFTS\clarify-review.draft.md` — **DRAFT — NOT YET SPEC KIT CANONICAL**; evidence, decisions, assumptions, DoD и blockers.
- Review status: owner review зафиксировал safety, verification, audit и Git decisions; дальнейшая работа передана в отдельный task. Drafts не готовы к canonical Spec Kit и не готовы к `plan`.

### Зафиксированные owner decisions

- MVP описывается как полный synchronizer из пяти будущих child specs: workbook foundation; read-only pull/refresh; compare/immutable plan; gated apply/read-back/writeback; verification/evidence/Codex boundary.
- Будущими самостоятельными операторами являются BPMSoft developers на Windows с desktop Excel и local BPMSoft. CLI владеет hard gates; skills обязательны, но не получают secrets и не обходят CLI.
- Только человек вводит credentials, подтверждает backup и approve/reject whole plan. Browser verification обязательна, strictly read-only и использует сохранённую deterministic sample.
- New lookup rows требуют `DraftRowToken -> RecordId`; matching по `Code`/`Name` запрещён. MVP не удаляет BPMSoft entities.
- Изменение workbook или affected target state инвалидирует plan. Apply останавливается на первой ошибке без auto-rollback; затем требуются read-back и human decision.

### Незакрытые вопросы и exact stop condition

- Browser sample rule закрыт решением владельца: все structural changes; lookup rows — 10% каждого типа (минимум 3, максимум 10) с new/updated coverage, выбор по plan hash/устойчивому ключу, 100% acceptance.
- Blocking research question: какой exact `SelectQuery` payload в local BPMSoft 1.8 даёт explicit deterministic order и позволяет дважды подтвердить complete pagination без skips/duplicates?
- **Exact stop condition:** до подтверждения этого order/pagination contract и exact schema/lookup-registry mapping не начинать full-catalog pull, не создавать `.xlsx`, не открывать write scope, не проверять Manage/Write и не менять production-код.

### Следующий один шаг

Передать работу в отдельный task по разделу 13; в текущем task не запускать техническое исследование.

## 13. Передача в отдельный task: read-only research и conditional workbook workflow — 2026-09-04

### Актуальные SDD drafts

- `docs\SDD_DRAFTS\constitution.draft.md`
- `docs\SDD_DRAFTS\first-feature-spec.draft.md`
- `docs\SDD_DRAFTS\clarify-review.draft.md`

Draft package отражает весь MVP synchronizer как `spec of specs`; это по-прежнему не canonical Spec Kit и не plan/tasks/slices.

### Решения, добавленные в review

- Browser verification обязательна после полного CLI read-back: проверяются все structural changes; lookup rows — 10% каждого типа, минимум 3 и максимум 10, с new/updated coverage. Выбор вычисляется из plan hash и устойчивого ключа операции до Apply; acceptance — 100% sample.
- Audit, evidence и run journal локальны: отдельная versioned folder каждого запуска в date-based catalogue, раздельные audit/evidence folders, timestamped files. Retention/cleanup — ответственность пользователя. Metadata содержит версии приложения, workbook template, Codex skills, BPMSoft и Excel tables с modification time.
- Compare блокируется при повреждённом workbook; mandatory skills объясняют violation, сравнивают обе Excel-книги с предыдущими Git versions и предлагают correction без automatic edit.
- Обе Excel-книги ведутся в отдельном remote Git repository ОМ. Пользователь задаёт provider/repository URL/branch в non-secret settings artifact; credentials остаются в user credential manager. Только после successful Apply, read-back, browser verification и explicit human confirmation skill делает один atomic commit обеих книг с run ID/plan hash и pushes его при отсутствии conflict. Conflict решает пользователь; после него требуется новый validation/compare. Failed push сохраняет local commit и ждёт explicit retry. Audit/evidence/journal в Git не попадают.
- Separate strictly read-only research обязателен до первой implementation-ready child spec. Его результаты могут выявить несостоятельность contract/assumptions; тогда работа останавливается для решения владельца.

### Следующий task и stop condition

- Prompt package для отдельного task: `docs\READ_ONLY_RESEARCH_PROMPTS\00-orchestrator.md` и последующие numbered prompts.
- Фактическая рабочая директория нового task: `C:\CodingAgents\codex\projects\OM_Automatization\preparation`; все пути `docs/...` и `PrototypeReadOnlyPull/...` в prompt package относительны к ней.
- **Exact stop condition:** до positive evidence explicit deterministic ordering + complete repeatable pagination + exact schema/lookup-registry mapping запрещены full-catalog pull, `.xlsx`, production tool, Manage/Write check и BPMSoft write.
- Рекомендуемый порядок двух task: сначала отдельный orchestration task по `00-orchestrator.md` возвращает role split/gates/risks; затем его итоговый output передаётся в task, запущенный по `NEXT_AGENT_PLANNING_PROMPT.md`. Этот следующий task первым делом запрашивает и сверяет orchestration output, а затем задаёт единственный G1-вопрос на запуск strictly read-only researcher.

## 14. Актуальная передача продолжения workbook-orchestrator — 2026-09-05

### 14.1. Где остановилась цепочка

Завершены роли прежнего пакета:

1. `01-read-only-researcher` — собрал строго read-only evidence локального BPMSoft;
2. `02-evidence-reviewer` — независимо проверил evidence и выявил ложный own-only oracle для индексов;
3. `03-contract-decision-preparer` — подготовил и после G3 внёс одобренное исправление contract;
4. `04-workbook-delivery` — выполнил разрешённый G4 bounded capture, создал минимальный локальный инструмент и пару Excel.

Следующий старый номер роли — `05-verification-and-report`, но для отдельного task подготовлен сокращённый новый пакет только по оставшимся шагам: `docs/WORKBOOK_CONTINUATION_PROMPTS/`.

### 14.2. Закрытые gates и решения владельца

- **G1 закрыт:** владелец разрешил strictly read-only исследование локального стенда; credentials вводились человеком и не сохранялись.
- **G2 закрыт для workbook v1 с ограничениями:** explicit deterministic order и repeatable pagination доказаны на bounded `Lookup`/`ActivityPriority`; exact mapping доказан для выбранных schema layers и lookup registry. Full catalog не является acceptance condition этой delivery.
- **G3 закрыт:** identity схем/колонок строится по server UId; непроверенный `GetSchema schema.id` хранится только как diagnostic candidate. `Indexes` строится только из `schema.indexes[]`, member relation — `columnUId -> Columns.ColumnUId`, target может быть Own или Inherited. `IndexUId` сохраняется; member `.uId` не является `ColumnUId`. `ActualIndexed` сохранён отдельно и не выводится из membership.
- **G4 закрыт:** разрешены bounded read-only capture, две `.xlsx` и минимальный локальный инструмент. BPMSoft Write/Manage, Google input и изменение `SyncOM/` не разрешены.
- **G5 не закрыт:** агент обязан подготовить criterion-to-evidence matrix и рекомендацию, но итог принимает только владелец.

Старое разовое исключение из usage gate уже использовано и не может применяться снова. Перед каждым новым субагентом обязателен `codex-five-hour-usage-gate`, unique 300-minute window, threshold 20% и `gate_result=PASS`.

### 14.3. Главные доказанные результаты

- Deterministic pagination/order evidence: `PrototypeReadOnlyPull/bin/Release/net10.0/probe-output/20260904T132710Z/`.
- Reviewer report: `docs/READ_ONLY_RESEARCH_RESULTS/20260904T132710Z-02-evidence-review.md`.
- Индексный P3-C evidence: `PrototypeReadOnlyPull/bin/Release/net10.0/probe-output/20260904T151951Z/` и два отчёта `20260904T151951Z-Account-Test1-index-*.md`.
- Owner G4 decision: `docs/READ_ONLY_RESEARCH_RESULTS/20260904-G4-owner-decision.md`.
- Immutable bounded source: `WorkbookDeliveryTool/runs/2026/09/04/80fac4ee-e388-4f25-adf0-a8305950c3c6/evidence/20260904T212051Z-bounded-baseline.json`.
- PullRunId: `80fac4ee-e388-4f25-adf0-a8305950c3c6`; PairId: `cb3722e8-ee9e-4f47-a299-723769ce7bbf`; PairBaselineHash: `f2480cdbdaa036856ebedbbbae45fe6dd55b919a4156bb6af78833a602e4f65a`.
- Bounded source содержит 5 schema layers, 124 columns, 3 lookup records/6 lookup values и 2 index members.
- Экспорт `Indexes` сохраняет ровно два Test1 members (`Code` unique и `Name` non-unique), несмотря на 64 значения `ActualIndexed=true`. Отрицательные fixtures доказывают, что одно поле не выводится из другого.
- Это не доказывает безопасную загрузку индексов. До отдельного future load/apply research действует blocker `INDEX_SYNC_UNRESOLVED`; допустимо исключить index loading из первой версии и оставить read-only display.

### 14.4. Workbook incident и исправления

Первая пара workbook была отозвана: Excel предложил recovery и после восстановления показывал пустые листы. Recovery log доказал неверный порядок OOXML worksheet children. Повреждённые версии и recovery evidence сохранены append-only в `.../audit/rejected/`.

После исправления `sheetProtection -> autoFilter -> dataValidations` desktop Excel открыл обе книги без recovery. Владелец подтвердил, что содержимое выглядит нормально, но выявил два usability/contract замечания:

1. нельзя было менять ширину колонок и критерии фильтров;
2. `LookupRows` и `LookupValues` дублировали record metadata и должны быть одним листом.

По явному решению владельца подготовлен template v2:

- protection сохраняет блокировку protected/derived cells, но разрешает `formatColumns`, existing `autoFilter`, selection и presentation sorting;
- отдельный `LookupRows` удалён;
- `LookupValues` содержит ровно 15 колонок: `SchemaName`, `SysEntitySchemaUId`, `RecordId`, `DraftRowToken`, `DesiredState`, `ServerPresence`, `SourceFingerprint`, `Comment`, `ColumnName`, `ValueState`, `Value`, `ValueKind`, `ReferenceRecordId`, `ReferenceDraftRowToken`, `CanonicalValue`;
- metadata одной записи повторяется только внутри её нескольких value rows и должна быть согласованной;
- `TemplateVersion=2-bounded-research`; исходный bounded source и `PairBaselineHash` не менялись.

Проверенные template-v2 outputs установлены в canonical paths:

- `workbooks/BPMSoft.ModelCatalog.xlsx` — SHA-256 `e42ccd1187c744c846805ad6c4ee59d1b75a50c1bb19a252f45f0bd534129ca0`;
- `workbooks/BPMSoft.LookupCatalog.xlsx` — SHA-256 `9ebe5369cef454a6e0cdfb135da9c5ac862af0950c2cf7dbc976c9ccebc1b2e1`.

После закрытия книг обе canonical версии заменены вместе сохранённой проверенной парой. Tool verify, SHA-256, formula/style checks и desktop Excel COM checks повторены уже по canonical paths. Final append-only audit: `WorkbookDeliveryTool/runs/2026/09/04/80fac4ee-e388-4f25-adf0-a8305950c3c6/audit/20260905T114516Z-workbook-template-v2-canonical-verification.json`.

Предыдущая структурно исправная, но неудобная template-v1 пара сохранена как superseded evidence в `.../audit/rejected/20260905T113437Z-*.superseded-template-v1.xlsx`.

### 14.5. Что проверено для template v2

- Release build: PASS, 0 warnings/errors.
- `self-test`: PASS, включая merged LookupValues guard и прежние negative fixtures.
- tool `verify`: PASS; Model — 8 sheets/165 data rows, Lookup — 6 sheets/36 data rows; `LookupRows` отсутствует, `LookupValues` содержит 6 value rows.
- Формулы: 0; external/VBA/connections: 0; OOXML closure, exact sheets/headers/rows, pair metadata, hidden validation list, validations и protection permission attributes: PASS.
- `formula_check.py`, `style_audit.py`, `xlsx_reader.py --quality`: PASS с ожидаемыми предупреждениями о пустых draft/reference/description fields.
- Desktop Excel COM подтверждает `Protection.AllowFiltering=True`, `AllowFormattingColumns=True`, `AllowSorting=True`; изменение ширины колонки в памяти проходит. Программное применение критерия через COM-метод `Range.AutoFilter` на защищённом листе не является валидной имитацией dropdown UI и возвращает запрет даже для созданного самим Excel reference file; поэтому реальная смена filter criteria остаётся обязательной ручной проверкой владельца.
- Повторная ручная visual/usability проверка именно template v2 пока не является human acceptance и должна быть отражена как `not run`, пока владелец её явно не подтвердит. Автоматически подтверждено: обе книги открываются без recovery, expected sheets на месте, column resize проходит, protected cell write блокируется, editable cell write разрешён, Excel сообщает `AllowFiltering=True`.

### 14.6. Что остаётся сделать

1. Независимому reviewer повторить offline build/self-test/verify и Excel checks по prompt `01-workbook-evidence-reviewer.md` нового пакета.
2. Владельцу открыть canonical template-v2 книги и подтвердить: no recovery; данные/листы на месте; ширина колонок меняется; dropdown-фильтр применяется/снимается; protected cells нельзя изменить, editable cells доступны.
3. Роли `02-g5-decision-preparer` собрать criterion-to-evidence matrix, not-run checks, gaps и одну рекомендацию.
4. Остановиться на G5 и получить явное решение владельца. Не принимать результат вместо него.
5. Только после решения владельца роль `03-owner-decision-recorder` может обновить согласованные handoff/contract/SDD строки.

Остаются вне этой цепочки и не могут быть объявлены доказанными: full-catalog scale/generalisation; composite/auto-name/index order semantics; inherited `column.indexed` semantics; любая BPMSoft mutation/write/Manage; DraftRowToken-to-RecordId через create/read-back; safe index add/drop planning/loading; LibreOffice Tier 2/render.

### 14.7. Новый prompt package и рекомендуемые настройки

Путь: `docs/WORKBOOK_CONTINUATION_PROMPTS/`.

- `00-orchestrator-continuation.md` — `gpt-5.6-sol`, reasoning `high`;
- `01-workbook-evidence-reviewer.md` — `gpt-5.6-sol`, reasoning `high`;
- `02-g5-decision-preparer.md` — `gpt-5.6-sol`, reasoning `high`;
- `03-owner-decision-recorder.md` — `gpt-5.6-terra`, reasoning `high`.

Ни одна роль пакета не использует Astra или reasoning выше `high`.

## 15. Owner correction и canonical template v3 — 2026-09-05

### 15.1. Решение владельца

После independent reviewer FAIL владелец явно отменил workbook-level XOR для `ReferenceRecordId` / `ReferenceDraftRowToken` и cell-level protection mixed-листов. Новый contract:

- оба reference fields могут быть заполнены одновременно;
- будущая загрузка использует валидный непустой `ReferenceRecordId`; иначе однозначно разрешённый `ReferenceDraftRowToken`; иначе пустую ссылку;
- некорректный непустой `ReferenceRecordId` блокирует загрузку;
- protection применяется только к полностью read-only листам;
- `Schemas`, `Columns`, `LookupRegistry`, `LookupValues` доступны для редактирования целиком, а parser/compare обязан блокировать недопустимые изменения.

Decision record: `docs/READ_ONLY_RESEARCH_RESULTS/20260905-owner-workbook-editability-reference-precedence.md`. Это не G5 acceptance и не разрешение BPMSoft Write/Manage.

### 15.2. Canonical template v3

`WorkbookDeliveryTool` и contract/SDD drafts обновлены. Из того же immutable bounded source создана и вместе установлена canonical пара `TemplateVersion=3-bounded-research`:

- `workbooks/BPMSoft.ModelCatalog.xlsx` — SHA-256 `7fb01fd5f4af039beb45e7e77819385d9c25beac6762616cf6212e6ac32a849f`;
- `workbooks/BPMSoft.LookupCatalog.xlsx` — SHA-256 `48719140d6ecf4a7b21c61fc381eccb8aa95f2b8e32f437c360ed14e96f07b2d`.

Template-v2 canonical pair сохранена в `.../audit/rejected/20260905T122311Z-*.superseded-template-v2.xlsx`. Template-v3 audit: `.../audit/20260905T122353Z-workbook-template-v3-canonical-verification.json`.

Local verification: Release build PASS (0 warnings/errors), self-test PASS, tool verify PASS, Tier 1 PASS (0 formulas/errors), style audit PASS, Excel 16.0 opened both canonical files without recovery, mixed sheets are unprotected/editable, read-only sheets are protected and allow resize/filter/sort. Tier 2/render SKIPPED: LibreOffice unavailable. `INDEX_SYNC_UNRESOLVED` unchanged.

### 15.3. Следующий шаг

По разовому owner exception допускается один запуск independent workbook reviewer при usage ниже 20%; исключение расходуется только на этот запуск. Reviewer должен проверить template v3 и новый contract. При PASS затем нужен новый обычный usage gate перед `02-g5-decision-preparer`; при отсутствии PASS цепочка снова останавливается. G5 принимает только владелец.

### 15.4. Independent template-v3 review — FAIL на protected-sheet sort

Reviewer report: `docs/READ_ONLY_RESEARCH_RESULTS/20260905T183700Z-01-workbook-evidence-review-v3.md`.

Все проверки template v3, кроме реальной сортировки защищённых read-only листов, получили PASS/PASS WITH EXPECTED WARNINGS. Excel 16 сообщает `AllowSorting=True`, но `Range.Sort` блокируется, а встроенная UI-команда sort не меняет порядок, потому что сортируемые cells locked. Контроль тем же harness на unprotected mixed-листах проходит. Resize и real context-menu filter на protected sheets проходят.

Текущие требования конфликтуют в статической `.xlsx`: нельзя одновременно запретить редактирование locked cells и разрешить Excel переставлять их при сортировке. До owner decision `02-g5-decision-preparer` не запускается. Владелец должен выбрать: сохранить protection read-only листов и снять обязательность manual sort для них либо снять protection и полагаться на parser/compare. Разовое usage exception не израсходовано: фактический gate перед reviewer был `PASS`, remaining 98%.

### 15.5. Owner resolution protected-sheet sort — 2026-09-05

Владелец явно выбрал сохранение Excel protection полностью read-only листов и подтвердил, что фактическая сортировка их locked ranges не является обязательной. Обязательными остаются: отсутствие recovery, блокировка записи на read-only листах, полная редактируемость mixed-листов, изменение ширины колонок и применение существующих фильтров. Сортировка unprotected mixed sheets работает штатно; `AllowSorting=True` на protected sheets, если присутствует, является только best-effort permission metadata и не доказывает реальную сортировку.

Это решение снимает единственный blocker отчёта `20260905T183700Z-01-workbook-evidence-review-v3.md`. Canonical `.xlsx` не пересоздаются: их данные, protection map и hashes не меняются. Contract/SDD/prompt criteria и verifier приведены к owner decision; после локальных build/self-test/verify требуется независимый evidence rerun, затем при PASS разрешён переход к `02-g5-decision-preparer`. `INDEX_SYNC_UNRESOLVED`, Tier 2 skip и все write/full-catalog gaps сохраняются.

### 15.6. Independent rerun после owner resolution — PASS

Новый report: `docs/READ_ONLY_RESEARCH_RESULTS/20260905T191555Z-01-workbook-evidence-review-v3-rerun.md`, SHA-256 `b45f598dd7aeb1fdbbd7a3da13f33a1956f822a5a0c8ab883e0f3478040bc6ca`.

Reviewer независимо повторил clean Release build, self-test, tool verify, Tier 1, style/read-back, OOXML/source/audit/hash/secret/index checks и Excel 16 operations. Обе canonical книги открылись без recovery; все 10 fully read-only sheets блокируют запись; все 4 mixed sheets приняли запись во всех used cells; resize и existing-filter operations прошли; simultaneous `LookupValues!M2`/`N2` input прошёл; mixed-sheet sorting прошла. Canonical `.xlsx` не сохранялись и их hashes не изменились. Verdict: `PASS` по обновлённому bounded contract.

Tier 2/render остаётся `SKIPPED` из-за отсутствия LibreOffice. G5 не закрыт, runtime loader/reference precedence не реализован, `INDEX_SYNC_UNRESOLVED` остаётся future gate. Следующий разрешённый шаг — после нового usage `PASS` запустить `02-g5-decision-preparer`; его рекомендация не является решением владельца.

## 16. G5 human acceptance — 2026-09-06

### 16.1. Exact owner decision and status

Владелец дал дословный ответ G5: **«принимаю»** — в ответ на точный вопрос из `docs/READ_ONLY_RESEARCH_RESULTS/20260906T064102Z-02-g5-decision-package.md`:

> «Подтверждаете ли вы после личного открытия обеих canonical template-v3 книг в desktop Excel отсутствие recovery, видимость ожидаемых данных и листов, работу resize и existing filters, блокировку protected cells и редактируемость mixed sheets, а также решение G5 `accept with limits` строго для `VerifiedBoundedBaseline` без разрешения full-catalog/write/load и со сохранением `INDEX_SYNC_UNRESOLVED`?»

Это **human G5 acceptance: `accept with limits`**. Владелец подтвердил перечисленные manual desktop-Excel checks и принимает canonical template-v3 pair исключительно как `VerifiedBoundedBaseline` bounded read/export workbook delivery. Это закрывает G5 только в явно названных границах, а не принимает весь MVP synchronizer или какие-либо future operations.

### 16.2. Preserved limits and blockers

- Не приняты и не закрыты: full-catalog completeness/scale/generalisation; LibreOffice Tier 2/render; composite/auto-name/broader index-order semantics; inherited `column.indexed` semantics; production parser/compare/immutable plan; runtime reference precedence/token resolution/invalid-GUID loader fixture; Git success workflow.
- Не разрешены: BPMSoft Write/Manage, create/update/delete/compile/save, loader, load/apply/writeback, safe index add/drop planning/loading, Google input и изменения `SyncOM/`.
- `INDEX_SYNC_UNRESOLVED` сохраняется. `Indexes` остаётся только read-only evidence; любой index load/apply требует отдельного future safety proof и нового явного owner decision.

### 16.3. Next safe step and changed files

Следующий безопасный шаг — сохранить принятую pair как bounded reference без её изменения и остановиться на этой delivery. Любая дальнейшая работа требует отдельного нового owner authorization; она не может выводить разрешение на full catalog, BPMSoft write/load/apply или index load из G5 acceptance.

Decision record: `docs/READ_ONLY_RESEARCH_RESULTS/20260906T064737Z-03-owner-g5-decision-record.md`.

Изменены только `docs/PROJECT_HANDOFF.md` и этот новый decision record. `docs/WORKBOOK_CONTRACT_VISION.md` и все `docs/SDD_DRAFTS/*.draft.md` не менялись: их существующие safety limits уже согласованы с данным ограниченным G5 решением.
