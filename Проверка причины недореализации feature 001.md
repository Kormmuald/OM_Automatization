# Цель исходного запроса:

> **Историческая оговорка (2026-09-13):** этот аудит описывает состояние и правила на
> дату проверки. Его выводы о необходимости `AuthorizationReference` и отдельного
> owner authorization заменены текущим решением в `specs/001-read-only-catalog-qualification/spec.md`:
> для live read-only test достаточно ручного запуска CLI оператором либо прямой текущей
> просьбы пользователя; автоматический запуск и write по-прежнему запрещены.
установить, почему в проекте одновременно существуют:
- S03 offline implementation и проходящие BCL-only harness-проверки;
- verification/offline-validation.md с заявлением PASS;
- unchecked T025–T046 в tasks.md;
- отсутствие реализованного live read-only пути для T046;
- handoff, который описывает только fixture-only путь;
- ожидание, что после S03 можно будет выполнить реальную выгрузку BPMSoft.

# Результат:
## 1. Краткий вывод
Расхождение вызвано несколькими независимыми причинами, а не одной незакрытой галочкой:
1. Feature 001 действительно должна предоставить работающее чтение BPMSoft. Это прямо следует из spec, Plan и решения владельца от 9 сентября.
2. S01–S03 декомпозированы преимущественно как offline-реализация. Перед T046 отсутствуют явные задачи, превращающие подготовленные компоненты в исполнимый live read-only путь.
3. Даже offline S03 реализован не полностью: production CLI не соединён с reader, reconciliation и сохранением полного evidence.
4. Harness PASS подтверждает выполнение имеющихся assertions, но их недостаточно для T043/T044.
5. offline-validation.md не удовлетворяет evidence-требованиям T045.
6. T025–T046 формально не закрыты. Первоначально это было осознанным отражением незавершённости; последующее добавление тестовых файлов не сопровождалось доказательной сверкой completion.
T046 сейчас нельзя выполнить положительно даже при будущем разрешении владельца: технический путь отсутствует. Но отсутствие разрешения также остаётся самостоятельным gate.
Аудит выполнен с применением test-reviewer. Новые сборки и harness-запуски не выполнялись; исторические результаты сверены с текущими исходниками. Read-only пересчёт SHA-256 подтвердил все 12 зарегистрированных fixtures. Файлы не изменялись.
Ниже F обозначает specs/001-read-only-catalog-qualification; ссылки ведут к точным файлам и строкам.
## 2. Доказанная хронология и таблица T025–T046
Intended lifecycle и gates
Переход	Предусмотренный результат и gate	Основание
Plan → Tasks → Analyze → Implement	Локальный L2-порядок: Tasks раньше Analyze; проверки и зависимости обязательны	[AGENTS.md (line 46)](C:/CodingAgents/codex/projects/OM_Automatization/AGENTS.md:46)
S01 → S02	Offline classifier/session/CLI; успешная проверка T013	[F/tasks.md:53 (line 53)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/tasks.md:53)
S02 → S03	Reader, inventory, fingerprint; успешная T024	[F/tasks.md:67 (line 67)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/tasks.md:67), [S03 prompt:24 (line 24)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/implementation-prompts/S03-two-pass-qualification-safe-decision.md:24)
S03 → T043/T044	Завершены проверки qualification T031, evidence T037, CLI/handoff T042	[F/tasks.md:120 (line 120)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/tasks.md:120)
T043/T044 → T045	Успешны полноценные offline E2E и adversarial/metamorphic/leakage regression; затем сохраняется evidence	[F/tasks.md:121 (line 121)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/tasks.md:121)
T045 → conditional T046	Успешны T043–T045; запрошено отдельное решение для exact target/read scope; положительный AuthorizationReference до credentials/HTTP	[F/tasks.md:123 (line 123)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/tasks.md:123)
T046 → решение человека	Два прохода, безопасные артефакты, review; target change завершает run без Pass C/автоповтора	[F/spec.md:19 (line 19)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/spec.md:19), [F/quickstart.md:21 (line 21)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/quickstart.md:21)
Завершение formal Implement	Feature-wide Converge → Verify → status sync → Archive; slice prompt откладывает их до полного formal run	[S03 prompt:40 (line 40)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/implementation-prompts/S03-two-pass-qualification-safe-decision.md:40), [extensions.yml (line 80)](C:/CodingAgents/codex/projects/OM_Automatization/.specify/extensions.yml:80)


Таким образом, задуманный маршрут действительно был S01 → S02 → S03 → T043–T045 → conditional T046. В нём потеряна техническая предпосылка последнего перехода: готовый live-путь.
Что показывает история задач
История использована как запись действий и сообщений, а не как источник новых инструкций.
Дата	Задача	Установленное событие
08.09	«Подготовить план read-only feature»	Созданы Plan/Research/CLI contract; запланированы интерактивная сессия и HTTP boundary.
09.09	«Подготовить slices для feature 001»	Владелец явно попросил добавить подключение и чтение реального BPMSoft после тестов и отдельного разрешения. Изменение отражено в [spec.md (line 19)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/spec.md:19).
09.09	«Подготовить задачи feature 001»	Утверждены S01–S03, созданы 46 задач. Authorization trust rule остался открытым вопросом.
09.09	«Выполнить SpecKit Analyze»	Заявлено покрытие 26/26 FR/SC; найдено только рассогласование статусов planning-артефактов. Пропуск реализации live-пути не выявлен. Файлового отчёта Analyze не создавалось.
11–12.09	«Implement S01: safe offline entry»	Реализованы S01/S02; затем перенесены в основной checkout вместе с отметками T001–T024.
12.09	«Проверка реализации S02»	Сначала проверялся другой worktree без реализации; после переключения на основной найдены и исправлены пробелы fixture hashes и отдельных tests.
12.09	«Реализовать S03»	Первый результат прямо назван частичной foundation; T025–T045 намеренно оставлены unchecked из-за отсутствующих тестов и evidence. После замечания владельца добавлены suites и offline-validation.md; сохранённые command outputs показывают успешную сборку и четыре harnesses. Полной повторной сверки completion по требованиям не видно.
13.09	«Провести offline-тестирование S03»	Повторено утверждение об успешном offline-тестировании. По текущему коду часть приписанного ему покрытия отсутствует. Проверочный prompt запрещал менять tasks.md.


T025–T046: что существует и что доказано
Формальный статус каждой строки ниже — [ ]. Это непосредственно видно в [tasks.md (line 71)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/tasks.md:71), [tasks.md (line 89)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/tasks.md:89), [tasks.md (line 106)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/tasks.md:106), [tasks.md (line 120)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/tasks.md:120).
Обозначения evidence:
- H — исторический успешный harness output найден; это не новый запуск текущего аудита.
- D — утверждение в offline-validation.md.
- Полного пакета — требуемых fixture-linked outputs, scan/schema/reconciliation результатов — для этих задач нет.
Task	Реализация	Тест и фактически проверяемое поведение	Отсутствующее покрытие	Evidence	Можно считать completed?
T025	Reconciliation есть	CatalogQualificationTests: одинаковые/разные строки digest → review/blocker, RetryCount=0	Вызовы pass reader, process boundary, fixture, seal, запрет Pass C по счётчику	H, D	Нет
T026	Forecast есть	Три assertions: известный лимит, отсутствующий лимит, превышение	Bucket vectors, границы, отрицательные counts/limits, полнота входных данных	H, D	Нет
T027	CLI refusal есть	Один сценарий без reference → blocker; вывод не содержит password	Отдельные refusal/no-response cases, prompt/send spies, trust/mismatch cases	H, D	Нет
T028	Service вызывает delegate дважды; reconcile сравнивает digest	Unit comparison и clean synthetic service call	Реальный reader pipeline, сверка manifests/unsupported, ошибки прохода, sealing	H, D	Нет
T029	Diagnostic-only forecast реализован без Excel I/O	Те же три проверки T026	Полный контракт/vectors T026; не завершены dependencies	H, D	Компонент есть; task completion не доказан
T030	Refusal wired; Evaluate существует отдельно	CLI всегда отказывает	Проверка доверенности reference; Evaluate принимает любую непустую строку при совпадении alias/scope	H, D	Нет
T031	Есть запуск соответствующих методов harness	Узкие T025/T026/T027 assertions	Требуемый отчёт с измеренными pass/prompt/send counts и terminal evidence	H, D	Нет
T032	Run roots существуют	Два разных root при одном времени; повтор того же RunId отклонён	Journal, сохранность содержимого, overwrite evidence, конкурентные коллизии	H, D	Нет
T033	Validator/scanner есть	Unknown envelope field не записывается; один комбинированный canary даёт finding	Полная canary matrix, все durable paths, pre-seal, отсутствие PASS после ошибки	H, D	Нет
T034	Metadata whitelist есть	Один JSON принимается; переименование targetAlias в credential отклоняется	Типы/обязательность/вложенные поля, корректность hashes/timestamps, raw values внутри разрешённых полей	H, D	Нет
T035	Root/store/journal helpers есть	Тестируется в основном создание root	Единый run lifecycle, безопасный journal, атомарный non-overwrite, связь с qualification	H, D	Нет
T036	Часть schema/pre-write validation есть	Узкие проверки T033/T034	StoreAsync обходит validator/scanner; нет pre-seal/state gate; schema поверхностна	H, D	Нет
T037	FileSystem harness запускается	Четыре узких теста	Полный security report, hash validation, canary→failed run→no PASS	H, D	Нет
T038	Diagnostic renderer и архитектурная граница частично есть	Наличие четырёх подстрок; поиск трёх слов в .cs	Project/assembly dependencies, вложенные исходники, Excel dependency, skill dry-run, вся blocker matrix	H, D	Нет
T039	Четыре документа есть	Наличие файлов, двух status strings, отсутствие C:\	Исполнимость runbook, schema semantics, полный secret/path scan, независимый operator flow	H, D	Нет
T040	Renderer/helper есть	Проверяется только отказ qualify	catalog diagnose --run не подключён к Main; нет чтения run и paging/scan diagnostics tests	H, D	Нет
T041	Минимальные offline-документы созданы	Проходят existence/string checks	Достаточная конфигурация и воспроизводимый handoff; положительная процедура, на которую ссылается T046	H, D	Нет
T042	CLI/handoff проверки вызываются	Проверки строк и файлов	Сквозной fake-CLI dry run по инструкции, skill orchestration, безопасный run result	H, D	Нет
T043	Отдельные компоненты вручную соединены в тесте	Synthetic inventory, фиксированный digest, запись safe.json, review result	Production CLI workflow, фактический reader, полнота inventory, независимый fingerprint, полный evidence	H, D	Нет
T044	Один unit-тест	Два заданных digest → mutation blocker и нулевой retry field	Paging/shape/endpoint/schema/canary matrix, metamorphic runs, leakage scan, process termination	H, D	Нет
T045	Файл summary создан	Автоматической проверки полноты evidence нет	Fixture IDs/digests, ссылки на outputs, schema/scan summaries, traceable results	D	Нет
T046	Положительный live-путь отсутствует	Есть только synthetic negative T027	Готовая интеграция, authorization rule/input, ручной run и его evidence	Нет	Нет; не запускалась


Это не означает отсутствия полезной реализации. Это означает, что существование компонента и прохождение его узкого теста не дают достаточного основания закрыть целую задачу с более широким acceptance scope.
## 3. Root-cause analysis
### 3.1. HIGH: между approved scope и декомпозицией пропущен live implementation
Spec требует интерактивный вход, полный reader и авторизованный live double pass: [spec.md (line 83)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/spec.md:83), [spec.md (line 92)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/spec.md:92).
Plan прямо включает:
- интерактивную read-only сессию и HTTP boundary;
- HttpClient с fake HTTP handler для тестов;
- interactive prompt, in-memory cookie container/CSRF;
- два прохода одной authorized scope.
Основание: [plan.md (line 7)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/plan.md:7), [plan.md (line 15)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/plan.md:15), [plan.md (line 73)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/plan.md:73).
Но Tasks назначают:
- T010 — capture-only transport;
- T012 — fixture-only CLI;
- T030 — offline refusal path, причём live transport указан как stop condition;
- T046 — уже ручное использование catalog qualify.
Основание: [tasks.md (line 50)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/tasks.md:50), [tasks.md (line 76)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/tasks.md:76), [tasks.md (line 123)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/tasks.md:123).
Пропущен переход «компоненты проверены на fake transport» → «тот же production workflow имеет проверенный реальный adapter». Запрет обращаться к стенду во время разработки фактически превратился в отсутствие задачи реализовать сам adapter с offline-тестированием.
S03 одновременно заявляет offline qualification и conditional manual verification, но не выделяет подготовку положительного пути: [implementation-slices.md (line 158)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/implementation-slices.md:158), [implementation-slices.md (line 187)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/implementation-slices.md:187).
### 3.2. HIGH: отсутствует даже production-связка offline S03
catalog validate-offline:
1. проверяет аргументы, наличие файла, fixtureId и classification;
2. создаёт один GET_PACKAGES classification;
3. вызывает capture transport;
4. возвращает HUMAN_REVIEW_REQUIRED с hash файла.
Он не разбирает paging/target-change semantics, не вызывает qualification service и не сохраняет run evidence. Это видно в полном пути [CatalogValidateOfflineCommand.cs (line 18)](C:/CodingAgents/codex/projects/OM_Automatization/src/BpmSoftSync.Cli/Commands/CatalogValidateOfflineCommand.cs:18).
Следовательно, по текущему исходнику target-state-change.json не приведёт через эту команду к ожидаемому mutation blocker: её содержимое после общих metadata не интерпретируется. Это статический вывод; команду в аудите не запускал.
Quickstart требует противоположное: mutation blocker, sealed root и отсутствие третьего прохода через эту же offline-команду — [quickstart.md (line 15)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/quickstart.md:15).
Это не только отсутствие будущего live adapter, но и неполное выполнение существующего offline scope.
### 3.3. HIGH: T043 не является заявленным сквозным тестом
В [ReadOnlyQualificationE2ETests.cs (line 11)](C:/CodingAgents/codex/projects/OM_Automatization/tests/BpmSoftSync.Cli.Tests/ReadOnlyQualificationE2ETests.cs:11):
- CLI validation выполняется отдельно;
- inventory создаётся вручную из одного item;
- fingerprint задаётся литералом "fixture-digest";
- pass manifests и unsupported lists пусты;
- service получает delegate с заранее готовым результатом;
- evidence записывается отдельно с "payloadDigest":"abc";
- проверяется один captured request;
- временный run root удаляется.
Этот тест проверяет совместимость нескольких helpers на подготовленных значениях. Он не доказывает, что production CLI получает из fixture полный catalog, проводит два реальных прохода reader и сохраняет связанное evidence.
Счётчик вызовов pass reader отсутствует. Поэтому даже требование «ровно два прохода» здесь не проверяется assertion непосредственно.
### 3.4. HIGH: T044 сведена к одному unit assertion
Весь [ReadOnlyQualificationSecurityRegressionTests.cs (line 5)](C:/CodingAgents/codex/projects/OM_Automatization/tests/BpmSoftSync.Cli.Tests/ReadOnlyQualificationSecurityRegressionTests.cs:5) — вызов CatalogQualification.Reconcile для "one" и "two".
Там нет:
- запуска production CLI;
- paging/endpoint/unknown-shape/evidence fixtures;
- перестановок входов;
- scan stdout/stderr/run tree;
- fault injection;
- наблюдения количества проходов.
Часть нужных unit-проверок существует в S01/S02, но она не заменяет требуемую финальную проверку соединённого workflow. Именно отдельный cross-increment test package требует методика: [SPEC_KIT_CUSTOMIZATIONS.md (line 1251)](C:/CodingAgents/codex/projects/OM_Automatization/SPEC_KIT_CUSTOMIZATIONS.md:1251).
### 3.5. HIGH: evidence boundary не обеспечивает заявленные гарантии
Найдено несколько конкретных разрывов:
- IRunStore.StoreAsync записывает данные без schema/scanner и создаёт новый root на каждый вызов: [AppendOnlyRunStore.cs (line 23)](C:/CodingAgents/codex/projects/OM_Automatization/src/BpmSoftSync.Adapters.FileSystem/AppendOnlyRunStore.cs:23).
- WriteEvidenceAsync валидирует один JSON, но не ведёт failed/sealed state всего run; отказ одной записи не запрещает последующую успешную запись: [AppendOnlyRunStore.cs (line 30)](C:/CodingAgents/codex/projects/OM_Automatization/src/BpmSoftSync.Adapters.FileSystem/AppendOnlyRunStore.cs:30).
- Validator проверяет верхние имена полей и metadata names, но не требует корректные stableKey/payloadDigest, не валидирует типы и вложенные значения: [EvidenceEnvelopeValidator.cs (line 19)](C:/CodingAgents/codex/projects/OM_Automatization/src/BpmSoftSync.Adapters.FileSystem/EvidenceEnvelopeValidator.cs:19).
- Journal принимает произвольную непустую строку digest и пишет её без scan/форматной проверки: [RunJournalWriter.cs (line 5)](C:/CodingAgents/codex/projects/OM_Automatization/src/BpmSoftSync.Adapters.FileSystem/RunJournalWriter.cs:5).
- Pre-success sealing как операция жизненного цикла отсутствует.
Особенно показателен canary-тест: строка "password=RAW_LOOKUP_CANARY" считается обнаруженной при любом finding. Scanner найдёт password, но его marker rawlookup не совпадает с RAW_LOOKUP_CANARY. Поэтому тест не доказывает обнаружение raw-value canary отдельно: [SecretValueScannerTests.cs (line 8)](C:/CodingAgents/codex/projects/OM_Automatization/tests/BpmSoftSync.Adapters.FileSystem.Tests/SecretValueScannerTests.cs:8), [SecretValueScanner.cs (line 10)](C:/CodingAgents/codex/projects/OM_Automatization/src/BpmSoftSync.Adapters.FileSystem/SecretValueScanner.cs:10).
Таким образом, утверждение «PASS после scanner/schema failure невозможен» сейчас шире реализации и проверок.
### 3.6. HIGH: граница T046 не готова по нескольким компонентам
Компонент	Фактическое состояние
Positive AuthorizationReference input	В CLI отсутствует. qualify всегда вызывает RefuseWithoutAuthorization — [CatalogQualifyCommand.cs (line 11)](C:/CodingAgents/codex/projects/OM_Automatization/src/BpmSoftSync.Cli/Commands/CatalogQualifyCommand.cs:11).
Правило доверенности reference	Не определено в проектном решении. Evaluate проверяет только непустую строку и равенство alias/scope, после чего возвращает IsSuccess; CLI этот метод не использует — [AuthorizationGate.cs (line 9)](C:/CodingAgents/codex/projects/OM_Automatization/src/BpmSoftSync.Application/AuthorizationGate.cs:9).
Credential boundary	Есть контейнер private password/cookie/CSRF с disposal. Нет terminal-masked input, login workflow и привязки к HTTP session — [InMemoryReadOnlySession.cs (line 3)](C:/CodingAgents/codex/projects/OM_Automatization/src/BpmSoftSync.Adapters.BpmSoft/InMemoryReadOnlySession.cs:3).
Live read-only transport	Отсутствует. IReadOnlyTransport возвращает SafeResult; ICatalogSource умеет только ValidateOfflineFixtureAsync, а не выдачу catalog data — [Ports.cs (line 5)](C:/CodingAgents/codex/projects/OM_Automatization/src/BpmSoftSync.Application/Ports.cs:5).
Endpoint policy	Classifier существует, но capture transport проверяет только AllowlistVersion; публичный classification можно создать без classifier. WriteCallCount production capture константно равен 0 — [CapturedReadOnlyTransport.cs (line 16)](C:/CodingAgents/codex/projects/OM_Automatization/src/BpmSoftSync.Adapters.BpmSoft/CapturedReadOnlyTransport.cs:16), [CapturedReadOnlyTransport.cs (line 25)](C:/CodingAgents/codex/projects/OM_Automatization/src/BpmSoftSync.Adapters.BpmSoft/CapturedReadOnlyTransport.cs:25).
Target/scope configuration	Нет validated target origin, привязки alias к target, registry collection contracts и полной конфигурации ограничений. Документ schema — один абзац — [configuration-schema.md (line 3)](C:/CodingAgents/codex/projects/OM_Automatization/docs/read-only-handoff/configuration-schema.md:3).
Two-pass reader	Есть delegate, вызываемый дважды. Reader принимает уже готовый список pages; fetching/query builder отсутствует — [CatalogQualificationService.cs (line 5)](C:/CodingAgents/codex/projects/OM_Automatization/src/BpmSoftSync.Application/CatalogQualificationService.cs:5), [CatalogReader.cs (line 18)](C:/CodingAgents/codex/projects/OM_Automatization/src/BpmSoftSync.Domain/CatalogReader.cs:18).
Reconciliation	Сравнивается только переданная строка fingerprint. Переданные manifests и unsupported lists непосредственно не сверяются и не проверяются на согласованность с digest — [CatalogQualification.cs (line 7)](C:/CodingAgents/codex/projects/OM_Automatization/src/BpmSoftSync.Domain/CatalogQualification.cs:7).
Append-only evidence	Helpers имеются, но нет единого связанного, просканированного и запечатанного qualification run.
Terminal recovery	Есть текст blocker с запретом retry. Нет integrated обработки ошибок чтения, interruption, seal и последующей диагностики сохранённого run.


Отсутствие live authorization объясняет запрет запуска, но не объясняет отсутствие реализованной и offline-протестированной технической возможности.
### 3.7. MEDIUM/HIGH: T045 — summary, а не требуемый evidence package
T045 требует ссылки на fixture IDs/digests, command outputs, schema/scan summaries и test results. Общий test-plan требует такого evidence для каждой проверки: [tasks.md (line 122)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/tasks.md:122), [test-plan.md (line 5)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/test-plan.md:5).
В [offline-validation.md (line 7)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/verification/offline-validation.md:7) есть только общие PASS-утверждения. Нет привязанных outputs, результатов schema/scan, reconciliation, измеренных счётчиков или manifest этого запуска.
Кроме того:
- target-state-change.json упомянута как используемая, но текущие C#-тесты её не читают;
- она отсутствует в fixture manifest;
- проверка manifest валидирует только перечисленные записи, поэтому пропущенный файл не замечает: [FixtureManifestTests.cs (line 18)](C:/CodingAgents/codex/projects/OM_Automatization/tests/BpmSoftSync.Domain.Tests/FixtureManifestTests.cs:18).
Хорошая часть документа: он явно не заявляет live qualification, acceptance или Apply permission. Ошибка заключается в достаточности evidence и объёме приписанного тестам покрытия.
### 3.8. MEDIUM: статус-контроль и handoff не доведены до согласованного состояния
В истории S03 сначала прямо сказано: задачи не закрыты, потому что тесты и итоговые проверки неполны. После доработки появились названия suites и PASS, но текущий tasks.md не обновлён.
По Implement protocol завершённые задачи должны отмечаться, а completion проверяться против spec, Plan и coverage: [speckit-implement/SKILL.md (line 179)](C:/CodingAgents/codex/projects/OM_Automatization/.agents/skills/speckit-implement/SKILL.md:179).
Однако сейчас просто поставить [X] было бы ошибкой: значительная часть критериев действительно не выполнена.
Handoff тоже содержит два разных состояния:
- planning handoff всё ещё говорит, что production/tests не существуют — это устаревший снимок: [архивный handoff-slices-to-tasks.md](C:/CodingAgents/codex/projects/OM_Automatization/docs/archive/handoffs/feature-001/2026-09-09-handoff-slices-to-tasks.md:14);
- operator runbook описывает только offline path и запрос решения — [operator-runbook.md (line 3)](C:/CodingAgents/codex/projects/OM_Automatization/docs/read-only-handoff/operator-runbook.md:3).
Fixture-only характер T041 сам по себе соответствует её тексту. Противоречие возникает потому, что T046 ссылается на этот документ как на процедуру положительного ручного запуска, которой там нет.
Сопутствующие расхождения, которые должны попасть в regression
Они возникли раньше S03, но влияют на T043/T044 и будущий live use:
- Research фиксирует четыре endpoints, код и S01-тесты — пять с GET_PACKAGES: [research.md (line 13)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/research.md:13), [ReadEndpointAllowlist.cs (line 10)](C:/CodingAgents/codex/projects/OM_Automatization/src/BpmSoftSync.Adapters.BpmSoft/ReadEndpointAllowlist.cs:10). Stage source называет GetPackages кандидатом; кандидат и утверждённый exact runtime contract необходимо согласовать.
- Fake WriteCallCount считает любой GET/POST безопасным по методу, хотя read/write различаются endpoint semantics: [FakeReadOnlyTransport.cs (line 16)](C:/CodingAgents/codex/projects/OM_Automatization/tests/BpmSoftSync.Testing/FakeReadOnlyTransport.cs:16).
- Reader проверяет порядковые cursor tokens и duplicates, но не стабильную сортировку identities по объявленному order key: [CatalogReader.cs (line 27)](C:/CodingAgents/codex/projects/OM_Automatization/src/BpmSoftSync.Domain/CatalogReader.cs:27).
- Fingerprint использует строковую сериализацию с разделителями вместо запланированного canonical JSON: [TargetFingerprint.cs (line 20)](C:/CodingAgents/codex/projects/OM_Automatization/src/BpmSoftSync.Domain/TargetFingerprint.cs:20), [research.md (line 28)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/research.md:28).
## 4. Оценка трёх гипотез
Гипотеза	Вердикт	Доказательство
Slices разбиты некорректно	Частично подтверждается	Последовательность offline S01→S02→S03 разумна. Но S03 включает conditional live verification без отдельного результата «live workflow готов и проверен offline». Полнота Feature 001 не обеспечена: [slices:158 (line 158)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/implementation-slices.md:158), [plan:73 (line 73)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/plan.md:73).
Tasks недостаточны	Подтверждается	Нет явных implementation tasks для positive admission, interactive session, live transport/source и их production CLI wiring перед T046. T030 ограничена refusal path: [tasks:76 (line 76)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/tasks.md:76), [tasks:123 (line 123)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/tasks.md:123). При этом уже существующие T043–T045 тоже выполнены неполно.
Тесты отнесены не к той feature	Не подтверждается	Reader, qualification, forecast и общий run/evidence принадлежат 001. Функции 002–004 должны их повторно использовать: [spec:76 (line 76)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/spec.md:76), [spec:92 (line 92)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/spec.md:92). Ошибочна преимущественно классификация уровня и достаточности тестов: unit/helper test назван E2E/security regression.


## 5. Рекомендуемое исправление без внесения изменений
Рекомендую доработать Feature 001, сохранив уже полезные компоненты. Отдельная product feature/specification не нужна: live read-only функционал уже входит в действующее требование.
Чтение и qualification должны оставаться в 001. Материализация результата в .xlsx принадлежит 002: [002/spec.md:9 (line 9)](C:/CodingAgents/codex/projects/OM_Automatization/specs/002-workbook-pair-control/spec.md:9). Перенос reader в 004 создал бы обратную зависимость и нарушил накопительную архитектуру.
Предлагаемая корректировка:
1. Зафиксировать реальные остатки S03 и prerequisites S01/S02. Не переписывать исторический PASS как будто полного покрытия уже достаточно.
2. Определить с владельцем формат, источник и проверку AuthorizationReference. Это уже открытый E6: [critique:77 (line 77)](C:/CodingAgents/codex/projects/OM_Automatization/specs/001-read-only-catalog-qualification/critiques/critique-20260909-100855.md:77).
3. Явно выделить внутри Feature 001 дополнительный slice, например S04 — готовность live read-only пути, с разработкой и тестами только через fake HTTP handler.
4. Связать offline и будущий live режимы одним application workflow; fixture adapter и HTTP adapter должны поставлять данные в общий reader/reconciliation/evidence pipeline.
5. После этих изменений заново выполнить расширенные T043–T045. Старое evidence не переносить на изменённую реализацию.
6. Оставить T046 отдельным ручным этапом после технической готовности и нового owner authorization.
Предлагаемый dependency order:
Исправления S01/S02 → завершение offline S03 → S04 с offline adapter-tests → T043/T044 → T045 → отдельное решение владельца → T046 → human review → предусмотренное formal завершение Feature 001.
Критерии готовности перед T046:
- positive и negative admission проверены на synthetic input;
- до допуска наблюдаемо отсутствуют credential prompts и HTTP sends;
- каждый request проходит реальный classifier/request factory;
- два прохода используют один зафиксированный target/scope и общий reader;
- негативные сценарии дают конечные blocker outcomes;
- каждый durable path проходит строгую schema/scan boundary;
- evidence, journal, manifests и outcome связаны одним RunId;
- runbook воспроизводим независимым оператором;
- полный offline evidence package reviewed;
- открытые человеческие gates отображаются отдельно от технической готовности.
Безопасный план будущего ручного T046
1. Проверить новую сборку, её offline evidence и закрытие технических prerequisites.
2. Зафиксировать exact local target, declared read scope, ограничения и ожидаемые stop outcomes.
3. Запросить отдельное разрешение владельца. При отказе/отсутствии ответа записать факт невыполнения; credentials и HTTP не начинать.
4. Только после действительного положительного reference оператор вручную запускает будущий catalog qualify и сам вводит credentials в terminal.
5. Выполнить Pass A и Pass B без автоматического третьего прохода.
6. Сохранить безопасные endpoint metadata, manifests, counts/hashes, unsupported diagnostics, duration/response sizes, forecast, schema/scan results и terminal outcome.
7. При target change или другом terminal blocker остановиться; любой новый run требует нового разрешения.
8. Передать evidence человеку. Review result не даёт разрешений на Excel, compare, Apply, browser или Git.
Этот план не является текущим разрешением и не исполнялся.
## 6. Что должен создать или изменить отдельный implementation agent
Все перечисленное — предложение для будущей работы.
Артефакты / компоненты	Конкретная работа
F/spec.md	Уточнить technical readiness против live authorization; описать ожидаемый передаваемый результат qualification для 002.
F/plan.md, research.md, data-model.md, contracts/cli-contract.md	Согласовать authorization trust rule, exact endpoint set, target/scope configuration, typed response/source ports, canonical fingerprint, run/seal contract.
F/implementation-slices.md, tasks.md, implementation prompts	Добавить недостающий slice/tasks, зависимости T046; задать task-level completion evidence. Отмечать только фактически завершённое.
F/test-plan.md, quickstart.md	Заменить общие обещания проверяемой матрицей; согласовать CLI-команды с реальным production workflow.
F/verification/offline-validation.md и будущие evidence artifacts	Создать датированный воспроизводимый пакет с fixture IDs/hashes, commands/exit codes, scan/schema/reconciliation outputs и непрошедшими checks.
docs/read-only-handoff/*.md	Полноценные settings schema, executable offline runbook, отдельная conditional manual procedure и failure/recovery steps.
Application, Cli, BPMSoft adapters	Typed catalog source, validated target/scope, admission, credential/session boundary, HTTP transport через узкую policy, общая composition root.
Domain и FileSystem	Полное reconciliation, reader contracts, строгая evidence schema, отсутствие обходных writes, append-only journal и terminal seal.
CatalogQualificationTests, AuthorizationGateTests	Spy counters; ровно два чтения; отсутствие Pass C; refusal/no response/mismatch; synthetic positive admission; ошибки Pass A/B и обязательное sealing.
ReadOnlyQualificationE2ETests	Запуск настоящего production CLI/application workflow с fixture adapter; все expected inventory/manifests/hashes вычисляются из входа и независимо сверяются.
ReadOnlyQualificationSecurityRegressionTests	Полная fixture matrix, malformed payloads, paging/order/shape mutations, endpoint bypass, output/run-tree leakage и ограниченное завершение.
RunStoreTests, EvidenceEnvelopeTests, SecretValueScannerTests, AuditMetadataTests	Все durable paths, отдельные canaries, типы/обязательные/вложенные поля, hashes, overwrite/concurrency, scanner/schema failure → terminal run без success seal.
CLI/architecture/handoff tests	Реальная команда diagnose --run, blocker matrix, корректные exit codes, project/assembly boundaries, skill dry-run и воспроизводимость инструкции.
Fixture manifest	Включить target-change fixture и проверять полноту набора, а не только hashes уже перечисленных файлов.


Дополнительно нужны реальные setup/read/diagnosis skills, если FR-015 остаётся в прежней формулировке: в локальном .agents/skills обнаружены SpecKit skills, но продуктовая orchestration этого workflow не представлена. Соответствующая строка traceability должна вести к реализации и dry-run test, а не только к архитектурному запрету.
## 7. Подтверждённые текущие запреты и blockers
- Exact active target подтверждён: specs/001-read-only-catalog-qualification; профиль — L2 / l2-pilot.
- T025–T046 остаются unchecked; T043–T045 не имеют достаточного completion evidence.
- Live read-only workflow технически отсутствует.
- Формат/источник/правило проверки AuthorizationReference не определены. Reference не создавался, credentials не запрашивались.
- FULL_CATALOG_NOT_QUALIFIED и INDEX_SYNC_UNRESOLVED остаются открытыми.
- .specify/memory/spec.md, .specify/memory/plan.md, .specify/memory/changelog.md отсутствуют. Это не доказательство отсутствия всего кода, но завершённой консолидированной памяти Feature 001 нет.
- Jira/Confluence sync отключены; их существующие skip traces не являются verification.
- В аудите не выполнялись Git commands, HTTP, BPMSoft access, Excel, compare, Apply, browser, Write/Manage, T046, sync/archive hooks, speckit.converge или speckit.verify.run.
- Исходники, тесты, документы, SpecKit artifacts, .gitignore и immutable source drafts не изменялись.
HUMAN_REVIEW_REQUIRED остаётся точкой передачи evidence человеку. Оно не является authorization, acceptance или Apply permission.
