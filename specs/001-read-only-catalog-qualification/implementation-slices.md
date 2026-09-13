# Срезы реализации: безопасное чтение и qualification каталога BPMSoft

**Статус: APPROVED — HUMAN REVIEW RECORDED.**

## Запись согласования человека

- **Дата:** 2026-09-09 (+03:00).
- **Источник решения:** явное сообщение пользователя в текущем Codex-чате: «явно утверждаю slices».
- **Объект согласования:** весь данный `implementation-slices.md`, срезы S01, S02 и S03,
  только для exact active target `specs/001-read-only-catalog-qualification`.
- **Граница решения:** согласование разрешает декомпозицию в `tasks.md`; оно не является
  командой на автоматический live run, не снимает `FULL_CATALOG_NOT_QUALIFIED` или
  `INDEX_SYNC_UNRESOLVED`, не даёт Write/Manage, не разрешает Excel/compare/Apply/browser/Git
  и не допускает Pass C, retry или automatic new double pass.

**Целевая feature:** `specs/001-read-only-catalog-qualification`.

Документ декомпозирует только будущую автономную реализацию Feature 001. Это не
`tasks.md`, не разрешение на реализацию, реальный BPMSoft, запрос учётных данных,
действие в browser/Excel/Git и не закрытие контрольного ограничения. Основания: `spec.md`, `plan.md`, `research.md`,
`data-model.md`, `contracts/cli-contract.md`, `quickstart.md`, `test-plan.md`; общий
`preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md` неизменяем.

## Общие неизменяемые ограничения для каждого среза

- `FULL_CATALOG_NOT_QUALIFIED` сохраняется до завершения отдельного ручного run.
  Сначала допустимы лишь санитизированные фикстуры и имитационный транспорт. После их
  успешных проверок оператор вручную запускает `catalog qualify` для конкретных target
  и read scope либо агент действует по прямой текущей просьбе пользователя. `AuthorizationReference`
  не требуется; реальный run не запускается автоматически.
- `INDEX_SYNC_UNRESOLVED` сохраняется. Нет Excel, compare, Apply, изменения индексов,
  Write/Manage, browser или Git actions.
- `TARGET_STATE_CHANGED_DURING_QUALIFICATION` — конечный blocker: автоматический
  повтор двойного прохода, Pass C и циклы повторов запрещены; новый run требует нового
  ручного запуска либо новой прямой текущей просьбы пользователя.
- Доказательства безопасны и разрешены только схемой: без password, cookie, CSRF,
  login body, raw lookup values и secret. Точка решения человека не является
  acceptance или разрешением.

## Карта и порядок

| Порядок | Срез | Наблюдаемый и отдельно проверяемый результат | Зависит от |
|---|---|---|---|
| 1 | S01 — Безопасный автономный вход в чтение | `catalog validate-offline` с тестовой фикстурой и имитационным транспортом отклоняет запрещённую конечную точку до отправки; захват вызовов доказывает отсутствие вызовов записи. | Утверждённые Spec/Plan/Test Plan |
| 2 | S02 — Полный инвентарь без потерь с контролируемой постраничностью | У каждого элемента рабочей области в фикстуре один статус поддержки; считыватель даёт манифест либо именованный blocker постраничности/инвентаря без пропусков или зависания. | S01 |
| 3 | S03 — Двухпроходная qualification и безопасное решение | Ровно два автономных прохода дают reconciliation и `HUMAN_REVIEW_REQUIRED` либо конечный blocker; доказательства и диагностика не перезаписываются. | S01, S02 |

## S01 — Безопасный автономный вход в чтение

**Порядок:** 1.
**Наблюдаемый результат:** автономный путь принимает только тестовую фикстуру или
имитационный транспорт; классификатор допускает лишь `ReadEndpointAllowlist/v1`, а
запрещённые метод, путь или тело запроса отклоняются до отправки. Захват вызовов
подтверждает отсутствие отклонённых отправок и вызовов записи.

**Зависимости и входы:** активный `spec.md`; решения 1 и 6 из `research.md`;
`cli-contract.md`; строка «Безопасная сессия и классификатор» в `test-plan.md`;
предыдущих срезов нет.

**Связи:** US1; FR-001, FR-002, FR-015, FR-018, FR-019; SC-001, SC-007.

**В рамках среза:** автономная команда с тестовой фикстурой, классификация конечных
точек, граница сессии только в памяти, имитационный транспорт и захват вызовов,
безопасный результат CLI, архитектурная граница.

**Вне рамок среза:** фактический вход, реальный BPMSoft, учётные данные и полная
квалификация; Excel, compare, Apply, browser, Git; изменение индексов; права
Write/Manage.

**Будущие задачи (не создаются):** FT-S01-01 — подготовка решения и набора тестовых
фикстур; FT-S01-02 — контрактные тесты allowlist и отклонённых отправок; FT-S01-03 —
типизированные порты и контракт автономного CLI; FT-S01-04 — тест архитектурной
границы и границы skills.

**Проверки и ожидаемые безопасные доказательства:** `ReadEndpointAllowlistTests.cs` и
`SessionTests.cs` формируют точную матрицу allowlist и возвращают
`ENDPOINT_NOT_ALLOWLISTED` до сетевого ввода-вывода. Имитационный захват хранит
только ID конечной точки, метод и путь и подтверждает отсутствие записи. Тест CLI
показывает безопасные причину, область, способ восстановления и следующее разрешённое
действие; secrets отсутствуют в аргументах, выводе и артефактах.

**Условия остановки:** расхождение со списком разрешённых конечных точек, обход
классификатора, запрос на фактический вход, учётные данные или разрешение, действие
browser/Excel/Git/Write/Manage.
Нужно остановиться и сообщить о проблеме.

**Выход в следующий срез:** проверенная автономная граница, санитизированные
фикстуры и безопасный контракт захвата для S02; разрешение на реальный запуск не возникает.

**Сохранённые контрольные ограничения человека:** все общие ограничения, в частности
`FULL_CATALOG_NOT_QUALIFIED`, отсутствие автоматического реального run,
`INDEX_SYNC_UNRESOLVED`, отсутствие Write/Manage и запрет автоматической
повторной квалификации после `TARGET_STATE_CHANGED_DURING_QUALIFICATION`.

## S02 — Полный инвентарь без потерь с контролируемой постраничностью

**Порядок:** 2.
**Наблюдаемый результат:** автономный считыватель создаёт детерминированный манифест
страниц и `WorkspaceInventory`, где у каждого элемента тестовой фикстуры ровно один
безопасный статус поддержки. Дубликат, перекрытие, пропуск, цикл, ошибочное завершение,
превышение числа страниц или небезопасная неизвестная форма возвращают явный blocker,
но не ложный PASS, скрытое пропущенное значение или зависание.

**Зависимости и входы:** граница и фикстуры S01; решения 2–4 и 6 из `research.md`;
`data-model.md`; строки «Упорядоченный считыватель» и «Идентичность и инвентарь» в
`test-plan.md`.

**Связи:** US1; FR-003–FR-009, FR-011, FR-017–FR-019; SC-002–SC-004.

**В рамках среза:** контракты коллекций, защиты и телеметрия страниц, идентичность и
слой пакета, структурная диагностика без потерь, отношение индексов только для чтения,
отпечаток тестовой фикстуры и манифест классификации legacy-поведения.

**Вне рамок среза:** реальный полный каталог, третий проход или повтор; план, загрузка либо
Apply индексов; пара Excel-книг, compare, Apply, browser/Git, разрешение/учётные данные.

**Будущие задачи (не создаются):** FT-S02-01 — доменные тесты идентичности и статусов;
FT-S02-02 — фикстуры `paging-*` и тесты считывателя; FT-S02-03 — адаптер инвентаря и
схемы и тесты неизвестных форм; FT-S02-04 — эталонные и метаморфические тесты
отпечатка; FT-S02-05 — проверка классификации legacy-поведения.

**Проверки и ожидаемые безопасные доказательства:** `CatalogReaderPagingTests.cs` создаёт
только канонические поля манифеста, а каждая отрицательная фикстура возвращает
`CATALOG_ORDER_OR_PAGING_UNQUALIFIED`. `WorkspaceInventoryTests.cs` и
`UnknownShapeTests.cs` доказывают 100% статусов, разделение слоёв и структурную
оболочку либо блокер. `TargetFingerprintTests.cs` проверяет детерминизм при изменении порядка
свойств JSON. Доказательства по индексам остаются только инвентарными; артефакта
намерения изменить индекс нет.

**Условия остановки:** соединение идентичностей по Name/Code, подстановка значения по
умолчанию или потеря неизвестной формы, нестабильная/неограниченная постраничность,
исходное значение в структурной оболочке либо предложение реальной квалификации.
Следует сохранить именованный блокер.

**Выход в следующий срез:** типизированные данные прохода, статус инвентаря, манифесты
страниц и безопасные компоненты отпечатка для согласования в S03.

**Сохранённые контрольные ограничения человека:** все общие ограничения, в частности
`FULL_CATALOG_NOT_QUALIFIED`, отсутствие автоматического реального run,
`INDEX_SYNC_UNRESOLVED`, отсутствие Write/Manage и конечная обработка без повторов для
`TARGET_STATE_CHANGED_DURING_QUALIFICATION`.

## S03 — Двухпроходная квалификация и безопасное решение

**Порядок:** 3.
**Наблюдаемый результат:** автономная квалификация выполняет Pass A и Pass B ровно
по одному разу, запечатывает безопасный неперезаписываемый корень run и возвращает
согласованный `HUMAN_REVIEW_REQUIRED` либо именованный конечный блокер с безопасной
диагностикой. Изменение target приводит к
`TARGET_STATE_CHANGED_DURING_QUALIFICATION` без Pass C и повторов.

**Зависимости и входы:** безопасная командная граница S01; детерминированные данные
прохода и инвентарь S02; решения 2, 3, 5, 6 из `research.md`; строки Run/Evidence и
CLI/skills в `test-plan.md`; автономный путь из `quickstart.md`.

**Связи:** US1, US2, US3; FR-004, FR-010–FR-019; SC-002, SC-005–SC-007.

**В рамках среза:** согласование двух проходов и конечные блокеры; неперезаписываемый
корень запуска; валидация и сканер `EvidenceEnvelope/v1`; безопасная диагностика,
ранняя автономная передача контекста, сквозная проверка с имитационным транспортом и
условная ручная проверка чтения на реальном стенде по ручному запуску оператора либо прямой текущей просьбе пользователя.

**Вне рамок среза:** реальная приёмка, изменение состояния target, учётные данные,
автоматический повтор; Excel/compare/Apply/browser/Git, изменение индексов,
Write/Manage, финальная приемка проекта.

**Будущие задачи (не создаются):**

- FT-S03-01 — процесс квалификации, фикстура изменения target и безопасная сводка
  полного qualification: counts, ordered IDs, hashes, счётчик повторов `0`,
  gaps/duplicates, unsupported list, duration, response-size buckets и
  диагностический прогноз масштаба workbook без создания или изменения Excel.
- FT-S03-02 — тесты `RunStore`, сканера доказательств и `AuditMetadataTests.cs` для
  безопасных versions приложения/template/skills/BPMSoft, Excel tables и их
  modification times, target alias, plan hash при наличии и gate outcomes; неразрешённые
  поля отклоняются.
- FT-S03-03 — тесты диагностики CLI и handoff package по путям
  `docs/read-only-handoff/configuration-schema.md`,
  `docs/read-only-handoff/install-update-prerequisites.md`,
  `docs/read-only-handoff/operator-runbook.md`,
  `docs/read-only-handoff/failure-path.md` и
  `tests/BpmSoftSync.Cli.Tests/HandoffPackageTests.cs`.
- FT-S03-04 — чистая автономная сквозная, состязательная, метаморфическая и security
  regression.
- FT-S03-05 — обновление безопасных доказательств проверки только после выполнения
  тестов.
- FT-S03-06 — после успешных offline checks ручной read-only `catalog qualify` для
  конкретных target/read scope либо запуск по прямой текущей просьбе пользователя; безопасный
  evidence review. `ManualLiveInvocationTests.cs` доказывает zero terminal credential prompts
  и zero HTTP sends на offline/automatic paths; `AuthorizationReference` не требуется.

**Проверки и ожидаемые безопасные доказательства:** `CatalogQualificationTests.cs`
доказывает ровно два прохода либо запечатанный
`TARGET_STATE_CHANGED_DURING_QUALIFICATION` без третьего. Проверка сводки
qualification подтверждает counts, ordered IDs, hashes, `retryCount = 0`,
gaps/duplicates, unsupported list, duration, response-size buckets и только
диагностический прогноз масштаба workbook. `RunStoreTests.cs`,
`EvidenceEnvelopeTests.cs`, `SecretValueScannerTests.cs` и планируемый
`AuditMetadataTests.cs` доказывают разные корни, отклонение перезаписи, контрольные
маркеры и полный разрешённый состав audit metadata; после ошибки сканера PASS
отсутствует. `HandoffPackageTests.cs` проверяет schema настроек без secrets,
documented prerequisites, ранний runbook, failure path, отсутствие hidden local paths
и достижение `HUMAN_REVIEW_REQUIRED` только на fixture/fake transport. Имитационный CLI
E2E возвращает `HUMAN_REVIEW_REQUIRED`, а не разрешение на Apply; сохраняются только
вывод запуска, ID/digest фикстуры и сводки сканирования, схемы и захвата вызовов.

**Условия остановки:** третий проход или автоматический повтор; ошибка сканера/схемы;
небезопасное сохраняемое поле; трактовка автономных доказательств как доказательства
полного каталога; автоматический live run без прямой текущей просьбы пользователя.
Нужно запечатать безопасные доказательства блокера и остановиться.

**Выход в следующий срез:** автономно проверяемый результат Feature 001 для
проверки человеком и повторно используемые контракты запуска, аудита и доказательств;
он не даёт разрешения на реальную операцию или реализацию следующей feature.

**Сохранённые контрольные ограничения человека:** все общие ограничения, в частности
`FULL_CATALOG_NOT_QUALIFIED`, отсутствие автоматического реального run,
`INDEX_SYNC_UNRESOLVED`, отсутствие Write/Manage и запрет повторного двойного
прохода после `TARGET_STATE_CHANGED_DURING_QUALIFICATION`.

## Трассировка requirement → slice → future task → check

| Requirement | Slice | Future task | Planned check / safe evidence |
|---|---|---|---|
| US1 | S01, S02, S03 | FT-S01-02, FT-S02-02–04, FT-S03-01 | allowlist capture; inventory/paging/fingerprint fixtures; exact-two-pass blocker/reconciliation |
| US2 | S03 | FT-S03-02, FT-S03-04 | append-only run roots, schema/scanner and safe E2E evidence |
| US3 | S01, S03 | FT-S01-04, FT-S03-03 | CLI/skill architecture and clean offline handoff/diagnosis path |
| FR-001–002 | S01 | FT-S01-02, FT-S01-03 | allowlist/session contract; zero rejected sends/writes |
| FR-003–004 | S02, S03 | FT-S02-02, FT-S03-01 | paging matrix; exact-two-pass reconciliation/blocker |
| FR-005–008 | S02 | FT-S02-01, FT-S02-03 | identity/inventory/unknown-shape fixtures |
| FR-009 | S02 | FT-S02-03 | read-only index relation; `INDEX_SYNC_UNRESOLVED` retained |
| FR-010–011 | S02, S03 | FT-S02-04, FT-S03-01 | vectors; target-change no-Pass-C test |
| FR-012–014 | S03 | FT-S03-02 | append-only roots, schema/secret/value scan |
| FR-015–016 | S01, S03 | FT-S01-04, FT-S03-03 | CLI/skill boundary and offline handoff |
| FR-017 | S02, S03 | FT-S02-05, FT-S03-04 | disposition manifest; controlled rewrite evidence |
| FR-018–019 | S01, S03 | FT-S01-03, FT-S01-04, FT-S03-03 | diagnosis/architecture/E2E |
| SC-001 | S01, S03 | FT-S01-02, FT-S03-04 | capture: 100% allowlist, zero writes |
| SC-002 | S02, S03 | FT-S02-02, FT-S03-01 | manifests/hashes; terminal no-retry change |
| SC-003–004 | S02 | FT-S02-01–03 | complete status and named paging/shape blockers |
| SC-005–006 | S03 | FT-S03-02, FT-S03-04 | scanner/schema and non-overwrite evidence |
| SC-007 | S01, S03 | FT-S01-04, FT-S03-03 | clean offline path, no secrets in skill context |

All future tasks belong to a listed slice; no unassigned task is authorized.

### Детализированная матрица покрытия

| Требование | Срез | Будущая задача | Проверка / безопасное доказательство |
|---|---|---|---|
| US1 | S01, S02, S03 | FT-S01-02; FT-S02-01–04; FT-S03-01 | allowlist capture; инвентарь, постраничность, отпечаток и двухпроходная сводка |
| US2 | S03 | FT-S03-02, FT-S03-04 | корни запуска, schema/scanner/audit metadata и автономное E2E |
| US3 | S01, S03 | FT-S01-04, FT-S03-03 | архитектура CLI/skills, handoff package и чистый автономный путь |
| FR-001 | S01 | FT-S01-02, FT-S01-03 | SessionTests.cs; исключение secrets из CLI и артефактов |
| FR-002 | S01 | FT-S01-02 | ReadEndpointAllowlistTests.cs; отсутствие rejected sends и записи |
| FR-003 | S02 | FT-S02-02 | отрицательные fixtures постраничности и CATALOG_ORDER_OR_PAGING_UNQUALIFIED |
| FR-004 | S02, S03 | FT-S02-02, FT-S03-01 | манифесты страниц; два прохода или конечный blocker |
| FR-005 | S02 | FT-S02-01, FT-S02-03 | идентичности без соединения по Name/Code |
| FR-006 | S02 | FT-S02-01, FT-S02-03 | WorkspaceInventoryTests.cs; один status для каждого item |
| FR-007 | S02 | FT-S02-03 | UnknownShapeTests.cs; оболочка без потерь либо blocker |
| FR-008 | S02 | FT-S02-03 | fixtures неизвестных форм; без default/drop |
| FR-009 | S02 | FT-S02-03 | связь индексов только для чтения; INDEX_SYNC_UNRESOLVED сохранён |
| FR-010 | S03 | FT-S03-01, FT-S03-06 | offline summary; authorization-gate fixture без credential prompt/HTTP; затем ручной double pass и safe evidence review |
| FR-011 | S02, S03 | FT-S02-04, FT-S03-01 | векторы отпечатка и фикстура изменения target |
| FR-012 | S03 | FT-S03-02 | RunStoreTests.cs; разные неперезаписываемые корни |
| FR-013 | S03 | FT-S03-02 | AuditMetadataTests.cs; versions/times/alias/plan hash/gate outcomes |
| FR-014 | S03 | FT-S03-02 | EvidenceEnvelopeTests.cs; SecretValueScannerTests.cs |
| FR-015 | S01, S03 | FT-S01-04, FT-S03-03 | граница CLI/skills и безопасная диагностика |
| FR-016 | S03 | FT-S03-03 | HandoffPackageTests.cs; configuration schema, prerequisites, runbook, failure path |
| FR-017 | S02, S03 | FT-S02-05, FT-S03-04 | legacy disposition и controlled rewrite regression |
| FR-018 | S01, S03 | FT-S01-03, FT-S03-03 | безопасные причина, область, восстановление, следующее действие |
| FR-019 | S01, S03 | FT-S01-04, FT-S03-02 | архитектурный тест, audit/evidence schema и автономное E2E |
| SC-001 | S01, S03 | FT-S01-02, FT-S03-04 | 100% allowlist, zero writes |
| SC-002 | S02, S03 | FT-S02-02, FT-S03-01, FT-S03-06 | манифесты/hashes; terminal target-change без повтора; offline/automatic paths без credential prompt/HTTP |
| SC-003 | S02 | FT-S02-01, FT-S02-03 | полный status и оболочка без потерь |
| SC-004 | S02 | FT-S02-02 | blocker постраничности; без false PASS/hang |
| SC-005 | S03 | FT-S03-02, FT-S03-04 | schema/scanner canaries; без PASS после сбоя |
| SC-006 | S03 | FT-S03-02, FT-S03-04 | неперезапись корней и schema/hash regression |
| SC-007 | S03 | FT-S03-03 | handoff package без secrets/hidden paths до HUMAN_REVIEW_REQUIRED |
