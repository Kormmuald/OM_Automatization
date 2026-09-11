<!--
Sync Impact Report
- Version change: 1.0.1-candidate -> 2.0.0
- Modified principles: the delivery baseline, scope, anti-scope, human gates, acceptance model,
  and governance now permit implementation of the CLI and Codex skills; all write-safety gates
  remain in force.
- Added sections: none.
- Removed sections: the `SLICES_NOT_APPROVED` blocker and the blanket prohibition on
  implementation. The former Additional Constraints and Development Workflow content remains
  redistributed into the required corporate sections.
- Follow-up TODOs: timeline, budget, communications, and the remaining business-case facts are
  unknown. Full-catalog operation and any BPMSoft write operation remain gated by their specific
  qualification, allowlist, and evidence requirements.
-->

# Конституция проекта локального синхронизатора BPMSoft

## Назначение

Конституция задаёт обязательные правила для локального Windows-комплекта,
синхронизирующего объектную модель и данные справочников между локальным BPMSoft и
логической парой Excel-книг. Она применяется к спецификациям, планам, задачам, будущим
slices, production-коду, Codex skills, evidence и operator workflow.

Разработка CLI-приложения и Codex skills разрешена по корпоративному жизненному циклу после
согласованных спецификации, плана и задач. Реальные операции записи в BPMSoft остаются
допустимыми только в границах отдельных safety gates этой конституции.

## Статус согласования

Согласовано. Разрешена разработка CLI-приложения и Codex skills для обновления данных локального
BPMSoft из локальных Excel-книг объектной модели и справочников. Full-catalog operation и
операции записи в BPMSoft допускаются только после выполнения применимых qualification,
allowlist и evidence gates.

## Рамка проекта

### Название проекта

Локальный синхронизатор BPMSoft.

### Код и название Jira / файла

Файл: `.specify/memory/constitution.md`; Jira-ключ не задан.

### Владелец результата

Евгений Харитонов.

### Краткая цель

Получить работающий инструмент: комбинацию CLI-приложения и Codex skills для управляемого
обновления данных локального BPMSoft из локальных Excel-книг объектной модели и справочников,
формат которых уже зафиксирован. Историческое решение не переносится копированием, а
доказательные и человеческие контрольные точки не обходятся.

### Ожидаемый результат

Работающий локальный Windows-инструмент из CLI-приложения и набора Codex skills, реализующий
обновление данных локального BPMSoft из зафиксированной пары Excel-книг объектной модели и
справочников. Реализация следует корпоративному lifecycle; full-catalog operation и каждый
operation kind записи в BPMSoft разрешаются только после выполнения их отдельных safety gates.

## Основные принципы

### I. Доказательная иерархия источников

При конфликте ОБЯЗАН применяться следующий порядок: (1) текущее явное решение владельца и
актуальный workbook contract; (2) воспроизводимое evidence локального BPMSoft и независимые
reviews; (3) characterization исторического `SyncOM`; (4) статическое чтение `SyncOM` и
внешняя документация. Исторический `SyncOM` является reference implementation предметной
логики и API-кандидатов, но не доказывает безопасность, полноту каталога, identity,
idempotency, обработку ошибок или write semantics. Любой перенос ОБЯЗАН быть controlled
rewrite, а не port-by-copy.

### II. Безопасность и человеческий контроль — без исключений

Credentials вводит только человек в интерактивном CLI или отдельной browser-сессии. Пароли,
cookies, CSRF, login response и Git credentials НЕ ДОЛЖНЫ попадать в skills, arguments,
workbook, plan, logs, evidence или Git. Read, compare и browser verification ОБЯЗАНЫ быть
read-only и иметь отдельный endpoint allowlist; отсутствие HTTP write calls ОБЯЗАНО
проверяться контрактными тестами. Только человек подтверждает ручной backup и принимает либо
отклоняет plan целиком; skills не одобряют plan и не обходят CLI gates. Delete, SQL delete,
automatic backup/restore, browser write actions и скрытый bidirectional merge не входят в MVP.

### III. Plan-before-apply и fail-closed execution

Compare ОБЯЗАН формировать human-readable report и machine-readable immutable plan из одной
canonical model; Apply НЕ ДОЛЖЕН пересчитывать diff. Apply разрешён только для точного plan,
связанного с hashes обеих книг, template version, pair baseline, target fingerprint и
affected-state fingerprint. Любое изменение input или live state ОБЯЗАНО инвалидировать plan до
write calls. Apply ОБЯЗАН остановиться после первой неуспешной операции, НЕ ДОЛЖЕН выполнять
automatic rollback и НЕ ДОЛЖЕН продолжать последующие writes.

### IV. Строгая identity и lossless обработка

Server `Id`, schema `UId`, column `UId`, lookup registry `Id` и локальный `DraftRowToken`
ОБЯЗАНЫ считаться разными невзаимозаменяемыми типами identity. Existing configuration
ОБЯЗАНА связываться по доказанной BPMSoft identity и package-layer context; `Name` и `Code`
допустимы только как display/diagnostic hint. BPMSoft ОБЯЗАН создавать GUID для новых schema,
columns, registry records и lookup rows. Новый lookup row ОБЯЗАН использовать неизменяемый
`DraftRowToken` до доказуемого create-response или однозначного read-back server `RecordId`;
иначе операция блокируется. Неизвестный type/shape ОБЯЗАН сохраняться losslessly как
inventory/unsupported diagnostics и закрывать затронутую write-capable возможность явным
blocker. Collection read ОБЯЗАН иметь explicit ordering, pageable reader, duplicate/skip
detection, termination guards и повторяемые hashes/counts.

### V. Workbook contract — проверяемая граница

Source of truth для workbook v1 —
[WORKBOOK_CONTRACT_VISION.md](../../preparation/docs/WORKBOOK_CONTRACT_VISION.md); accepted
template v3 — только bounded `VerifiedBoundedBaseline`, не full-catalog и не production
acceptance. Model Catalog и Lookup Catalog ОБЯЗАНЫ валидироваться как единая пара по `PairId`,
`PullRunId`, baseline и template metadata. Parser/compare, а не Excel protection, ОБЯЗАН быть
security boundary для IDs, fingerprints, actual/derived fields, formulas, validations и
`Desired*` values. Mixed sheets редактируемы на уровне Excel. Fully read-only sheets ОБЯЗАНЫ
быть защищены; resize и existing filters обязательны, а фактическая сортировка locked ranges не
является acceptance criterion. `Indexes` ОБЯЗАН строиться только из `schema.indexes[]`, а member
relation — из `columns[].columnUId -> Columns.ColumnUId`; `ActualIndexed` остаётся отдельным
compatibility signal. Lookup reference ОБЯЗАН применять precedence: валидный
`ReferenceRecordId`, затем однозначно resolved `ReferenceDraftRowToken`, иначе empty reference;
некорректный непустой GUID блокирует операцию.

## Базовая форма поставки

Основная технология MVP — .NET 10/C#, guided CLI, Codex skills и локальный HTML report.
Поддерживаемая среда — Windows, desktop Excel и local BPMSoft. Базовой формой результата
является работающий локальный Windows-комплект с раздельными domain logic, adapters, workbook
I/O, compare/plan, apply executor и verification/audit. Apply controls допускаются только при
соблюдении точного plan, qualification, allowlist и evidence gates этой конституции.

## Границы работ

- Управляемая синхронизация и обновление объектной модели и данных справочников между
  локальным BPMSoft и логической парой Excel-книг зафиксированного формата.
- Разработка CLI-приложения, Codex skills, спецификаций, планов, задач, production-кода,
  evidence и operator workflow в пределах правил этой конституции.
- Детерминизм, idempotency, fail-closed validation, stable operation ordering и secret scanning,
  проверяемые тестами до live use.
- Characterization tests для полезной legacy-семантики с приведением expected behavior к
  текущему contract.

## Анти-границы

- Google UI/formulas и Apps Script не переносятся.
- Delete, SQL delete, automatic backup/restore, browser write actions и скрытый bidirectional
  merge не входят в MVP.
- Full-catalog operation не допускается без qualification; операция записи в BPMSoft не
  допускается без owner allowlist и version-specific evidence.

## Безопасные допущения

- Исторический `SyncOM` допустим только как reference implementation предметной логики и
  API-кандидатов, а не как доказательство безопасности, полноты, identity, idempotency,
  обработки ошибок или write semantics.
- Accepted template v3 является bounded `VerifiedBoundedBaseline`; он не доказывает
  full-catalog или production acceptance.
- Широкое исследование не создаётся, если поведение уже классифицировано в legacy inventory,
  покрыто sanitized fixture/evidence и не пересекает новую safety boundary.
- Недоказанный operation kind не блокирует независимый безопасный subset, но остаётся закрытым.

## Небезопасные допущения

- Недопустимо считать bounded evidence доказательством catalog completeness, scale или
  generalisation до снятия `FULL_CATALOG_NOT_QUALIFIED`.
- Недопустимо выполнять index plan/load/apply до снятия `INDEX_SYNC_UNRESOLVED`.
- Недопустимо выполнять write preflight или Apply для operation kind без owner allowlist
  (`WRITE_ALLOWLIST_UNAPPROVED`) либо без version-specific positive/negative evidence
  (`WRITE_SEMANTICS_UNPROVEN`).
- Неизвестный type/shape, некорректный непустой GUID lookup reference и недоказанный переход
  `DraftRowToken -> RecordId` не могут быть безопасно угаданы и блокируют затронутую операцию.

## Контрольные точки человека

Нужно остановиться и запросить решение человека перед:

- вводом credentials;
- подтверждением ручного backup;
- принятием или отклонением plan целиком;
- любым исключением, допускающим raw lookup values в evidence;
- owner-approved allowlist для write research и Apply;
- включением нового operation kind в write-capable Apply;
- фиксацией обеих книг одним Git commit после success gates;
- разрешением конфликта Git, после которого требуются полная повторная validation и новый
  compare;
- изменением конституции, принципов, scope, blockers или исключений безопасности.

## Требования к проверке

- Read, compare и browser verification должны быть read-only; отсутствие HTTP write calls
  проверяется контрактными тестами.
- CLI владеет детерминированными security checks, fingerprints, plan validity и read-back;
  skills только оркестрируют CLI, объясняют safe diagnostics и выполняют разрешённую read-only
  browser verification.
- Каждый run имеет отдельную date-based versioned folder с раздельными audit/evidence, run
  journal и timestamped filenames; артефакты append-only в рамках run и не добавляются в
  workbook Git repository.
- Success включает полный CLI read-back и 100% browser acceptance выбранных checks. Structural
  operations проверяются полностью; lookup rows — детерминированной выборкой 10% каждого
  operation kind, минимум 3 и максимум 10, с new/update coverage при наличии.
- Дополнительный probe допустим только как bounded preflight конкретной операции или blocking
  shape с заранее заданными endpoint, payload, invariant, PASS/FAIL и запрещёнными вызовами.
- Full-catalog pull является единым qualification gate generalized reader. Write research
  допустимо только как operation-level bounded preflight после owner-approved allowlist.

## Модель решения о приемке

Только человек принимает либо отклоняет plan целиком; skills не принимают plan и не обходят CLI
gates. Успешный цикл требует success gates, полного CLI read-back, 100% browser acceptance
выбранных checks и явного подтверждения человека перед единым Git commit обеих книг с run ID и
plan hash. Failed/partial cycle не создаёт workbook commit.

При network/auth push failure локальный commit сохраняется; conflict требует manual resolution,
полной повторной validation и нового compare. Ратификация этой конституции разрешает
реализацию CLI-приложения и Codex skills; full-catalog operation и операции записи в BPMSoft
по-прежнему требуют выполнения отдельных qualification, allowlist и evidence gates.

## Источник истины

При конфликте источников действует следующий порядок: (1) текущее явное решение владельца и
актуальный workbook contract; (2) воспроизводимое evidence локального BPMSoft и независимые
reviews; (3) characterization исторического `SyncOM`; (4) статическое чтение `SyncOM` и
внешняя документация.

Актуальная конституция хранится в `.specify/memory/constitution.md`. Источниками для её
текущего содержания служат неизменяемые черновики в `preparation/docs/product-specs/`, включая
`preparation/docs/product-specs/constitution.md` и пакет
`preparation/docs/product-specs/local-bpmsoft-synchronizer/`. Workbook contract хранится в
`preparation/docs/WORKBOOK_CONTRACT_VISION.md`; evidence, audit и run journal хранятся в
отдельных date-based versioned folders каждого run.

## Управление

Эта конституция имеет приоритет над проектными практиками и future artifacts. Любое изменение
ОБЯЗАНО содержать причину, затронутые requirements и миграционные последствия, пройти owner
review и обновить Sync Impact Report. Owner decision НЕ ДОЛЖНО выводиться из agent
recommendation или PASS automated checks. Assumption ОБЯЗАНО быть маркировано и НЕ ДОЛЖНО
становиться confirmed/owner decision без источника. Исключения безопасности НЕ ДОЛЖНЫ
наследоваться между gates и НЕ ДОЛЖНЫ автоматически расширять scope.

Версии следуют SemVer: MAJOR — несовместимое удаление или переопределение governance; MINOR —
добавление принципа или существенное расширение требований; PATCH — уточнение без изменения
смысла. Каждый будущий artifact или diff ОБЯЗАН показать применимые requirement IDs,
соблюдённые blockers/Out of Scope, проверки, evidence path и решения человека. Несоответствие
конституции является blocker, а не advisory warning.

---

## Бизнес-кейс L2

Этот раздел обязателен начиная с `L2`. Ниже сохранены известные факты; отсутствующие
owner/business facts отмечены без их выдумывания и должны быть закрыты до углубления работы на
уровне `L2+`.

### Бизнес-заказчик

Калмыков Павел.

### Заинтересованные участники

Наталья Питерская, Роман Рахманов. Дополнительно применимы роли владельца результата,
оператора и человека, подтверждающего backup и plan.

### Исходные материалы

`preparation/docs/product-specs/`, включая каноническую constitution и пакет
`local-bpmsoft-synchronizer/`; `preparation/docs/WORKBOOK_CONTRACT_VISION.md`; исторический
`SyncOM` как reference implementation; воспроизводимое evidence локального BPMSoft,
независимые reviews и актуальные явные решения владельца.

### Проблема / Возможность

Нужен контролируемый локальный контур синхронизации объектной модели и данных справочников
между BPMSoft и логической парой Excel-книг с доказательными границами безопасности, identity,
планирования и проверки.

### Описание идеи

Работающий локальный Windows-комплект на .NET 10/C# из guided CLI, Codex skills и локального
HTML report. Он обновляет данные локального BPMSoft из Excel-книг зафиксированного формата;
исторический `SyncOM` используется для controlled rewrite, а не для port-by-copy.

### Предполагаемый эффект

- `Экономия`: порядка 40% времени разработки.
- `Ускорение`: порядка 40% времени разработки.
- `Новые возможности`: управляемая синхронизация BPMSoft и Excel-книг в пределах доказанных
  и owner-approved возможностей.

### Сроки

TODO(TIMELINE): сроки не определены. Разработка разрешена; full-catalog operation и операции
записи в BPMSoft зависят от снятия применимых qualification, allowlist и evidence gates.

### Бюджет

TODO(BUDGET): бюджет и ресурсные ограничения не определены в доступных материалах.

### Риски

Риски включают неполноту и необобщаемость bounded evidence, недоказанную write semantics,
незакрытый index sync, недоказанный переход `DraftRowToken -> RecordId`, неизвестные type/shape,
риски credentials и HTTP write calls, а также нарушения identity, lossless обработки и
fail-closed execution.

### Ограничения и допущения бизнес-кейса

Поддерживаются Windows, desktop Excel и local BPMSoft. Google UI/formulas, Apps Script,
Delete, SQL delete, automatic backup/restore, browser write actions и скрытый bidirectional
merge не входят в MVP. Все ограничения и blockers этой конституции сохраняют силу.

### Коммуникации

TODO(COMMUNICATIONS): требования к коммуникациям не определены в доступных материалах.

### Критерии успеха

Для разрешённых operation kinds успех требует полного CLI read-back, 100% browser acceptance
выбранных checks, success gates и явного подтверждения человека. Критерии бизнес-эффекта не
определены и не должны выводиться без решения владельца.

### Открытые вопросы бизнес-кейса

- TODO(TIMELINE), TODO(BUDGET) и TODO(COMMUNICATIONS).
- Полнота, масштаб и обобщение full-catalog; target fingerprint для промышленного применения;
  точные разрешения и семантика Write/Manage; доказательство `DraftRowToken -> RecordId`;
  изменение/загрузка индексов; поддержка LibreOffice уровня 2 и не-Windows сред; успешный
  Git-сценарий для промышленного применения.

**Версия**: 2.0.0 | **Ратифицирована**: 2026-09-07 | **Последнее изменение**: 2026-09-07
