# Handoff: discovery локального синхронизатора ОМ BPMSoft

Дата актуализации: 2026-09-03  
Статус: logical Excel contract v1 и перечень оставшихся рисков явно подтверждены владельцем 2026-09-03. Production-код и Spec Kit artifacts не создавались; отдельный strictly read-only `PrototypeReadOnlyPull` успешно подтвердил login, packages/workspace inventory, две schema shapes и paging shape lookup. `.xlsx` и BPMSoft write-вызовы не выполнялись. Владелец остановил дальнейшее техническое развитие prototype: следующий этап — совместно подготовить вход для SDD / GitHub Spec Kit по локальному курсу, а не реализовывать следующие read-only slices.

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
- В scope writeback: заполнение `!Id` у строк справочников и обновление флагов Required/Index по фактическому состоянию BPMSoft.
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
- В `ModelCatalog` приняты sheets `Readme`, `Manifest`, `WorkspaceInventory`, `Schemas`, `Columns`, `Indexes`, `ValidationLists`, `PullConflicts` и один набор `S_*`. В `LookupCatalog` — те же control sheets и `LookupRegistry`, `LookupRows`, `LookupValues`; `LookupColumns` отвергнут как дублирование `ModelCatalog.Columns`.
- Snapshots не являются историческим архивом: в каждой книге ровно один полный пред-pull набор; новый заменяет старый только после успешной записи и проверки нового набора.
- Captions и rename schemas/columns/lookups исключены; попытка даёт `RENAME_NOT_SUPPORTED` с указанием менять вручную в BPMSoft. Изменение `DataType` existing column исключено (`TYPE_CHANGE_NOT_SUPPORTED`).
- У existing own column разрешены изменения Required в обе стороны и только добавление simple index; снятие индекса — `INDEX_DROP_NOT_SUPPORTED`, вручную через БД. `UsageType`, `IsSimpleLookup`, `IntegrityMode`, `CascadeMode` исключены как неподтверждённые и ненужные v1 properties.
- Lookup registry может быть создан для новой lookup schema либо для existing `EntitySchema`, но второй сценарий не начинается без read-only API evidence. Package — единственный source в `ModelCatalog.Schemas`; base schema fields registry — read-only context.
- `LookupRows` и `LookupValues` — нормализованная source-of-truth пара для строк/значений. `DraftRowToken` выдаётся только явной workbook-only командой, ручной ввод запрещён; без доказуемого `DraftRowToken -> RecordId` новая строка не создаётся.
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
