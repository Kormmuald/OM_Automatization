DRAFT — NOT YET SPEC KIT CANONICAL

# Верхнеуровневая спецификация: управляемый локальный синхронизатор ОМ BPMSoft

## Статус и граница

- **[owner decision]** Несмотря на историческое имя файла, это draft верхнего уровня (`spec of specs`) для всего MVP-синхронизатора, а не узкой первой функции.
- **[owner decision]** Файл не является canonical Spec Kit `spec.md`, не содержит plan, tasks или slices и не разрешает реализацию.

## Цель, пользователи и наблюдаемый результат

- **[owner decision]** Цель — дать техническому оператору управляемый сквозной цикл синхронизации ОМ и lookup data между локальной логической парой Excel-workbooks и локальным стендом BPMSoft.
- **[owner decision]** Во время тестирования оператором является владелец; после handoff независимым оператором является разработчик BPMSoft в своём локальном окружении.
- **[owner decision]** Оператор может пройти: подготовку workbook → read-only pull/compare → immutable plan → явное human approval → gated apply → read-back → обязательную browser verification и получить проверяемый результат без участия владельца.
- **[confirmed]** Стартовая технология — .NET 10/C#, guided CLI и локальный HTML-отчёт без кнопки Apply. Источник: [PROJECT_HANDOFF.md](../PROJECT_HANDOFF.md), раздел 3.1.

## Карта дочерних спецификаций

- **[owner decision]** 1. «Основа и шаблон workbook»: подготовка локальной пары Model Catalog и Lookup Catalog по logical workbook contract и verified BPMSoft baseline.
- **[owner decision]** 2. «Read-only pull и обновление Excel»: чтение, нормализация и controlled refresh пары workbook.
- **[owner decision]** 3. «Compare и immutable change plan»: детерминированный diff и безопасный загрузочный артефакт.
- **[owner decision]** 4. «Gated apply, read-back и workbook writeback»: явное целиковое применение ранее одобренного plan и получение server identities только доказуемым путём.
- **[owner decision]** 5. «Verification, validation, evidence и Codex boundary»: CLI checks, обязательная browser-проверка и роли skills/оператора.
- **[assumption]** Порядок реализации этих дочерних specs будет утверждаться позднее; эта карта не является планом работ.

## Scope

- **[owner decision]** Локальные `.xlsx` опираются на logical workbook contract и на read-only опыт прежнего приложения/Google-книг, но не являются механической копией их формата.
- **[owner decision]** Локально разрешено генерировать формулы, validation и служебные данные, необходимые для результата.
- **[owner decision]** Значения и строки Google-книг или прежнего приложения никогда не становятся данными для загрузки в BPMSoft; фактическое состояние стенда читается из локального BPMSoft.
- **[owner decision]** Оператор редактирует только явно обозначенные поля `Desired*`/editable, отражающие допустимые пользовательские изменения через BPMSoft UI. Server IDs, fingerprints, snapshots и derived/service fields вручную не редактируются.
- **[owner decision]** MVP планирует лишь create и доказанно допустимые update для ОМ/columns/indexes/lookup registry/lookup rows; delete исключён.
- **[owner decision, G3]** Until a dedicated write preflight, index definitions are read-only evidence only: one `Indexes` row per `schema.indexes[].columns[]` member, joined by `columnUId` to an Own or Inherited `Columns.ColumnUId` in the selected package layer. `IndexUId` is retained; member `uId` is not a column relation. `ActualIndexed` remains separate compatibility context and cannot be generated from this membership.
- **[owner decision]** Codex skills обязательны: они запускают CLI, проверяют разрешённые входные данные, передают CLI несекретные сведения, интерпретируют результаты и проводят read-only browser verification.

## Out of Scope MVP

- **[owner decision]** BPMSoft delete, SQL для удалений, автоматический backup/restore, cloud-стенды, Google-книги как input, Google Drive/Sheets и Apps Script.
- **[owner decision]** Подготовка Excel по постановке аналитика через отдельные будущие Codex skills.
- **[owner decision]** Обход hard gates CLI, скрытый двусторонний merge, частичное принятие plan, автоматический rollback и browser write-actions.
- **[confirmed]** До текущих API read gates не выполняются full pull, `.xlsx` generation, BPMSoft write или Manage/Write checks. Источник: [PROJECT_HANDOFF.md](../PROJECT_HANDOFF.md), разделы 0–1.

## Проверяемые требования и acceptance criteria

- **[owner decision] FR-01.** CLI обязан блокировать создание baseline workbook, full pull и любую write-capable операцию, пока не пройдены documented read gates; проверка: запуск до gates завершается явным `DO NOT START` без `.xlsx` и HTTP write calls.
- **[owner decision] FR-02.** После прохождения read gates система формирует логическую пару Model Catalog/Lookup Catalog по утверждённому contract; проверка: pair metadata, sheets, IDs, validations и links проходят contract validation.
- **[owner decision] FR-03.** Compare только читает workbook и target, формирует immutable plan, связанный с workbook hash/template version/target fingerprint/прочитанным target state; проверка: изменение любого связанного входа делает Apply недоступным и требует нового compare.
- **[owner decision] FR-04.** Apply требует ручного подтверждения backup и целикового human approval plan; проверка: без каждого подтверждения ни одна операция записи не отправляется.
- **[owner decision] FR-05.** Для existing records system использует подтверждённый server identity; для new lookup rows связывает `DraftRowToken` с `RecordId` только create-response либо однозначным read-back; проверка: отсутствующий mapping блокирует продолжение.
- **[owner decision] FR-06.** При первой ошибке Apply CLI прекращает дальнейшие операции без auto-rollback и предоставляет безопасный audit/evidence для read-back; проверка: fault-injection/integration check не показывает последующих write calls после первой ошибки.
- **[owner decision] FR-07.** Успешный cycle требует полного CLI post-apply read-back и обязательной read-only browser verification. Browser проверяет все changes схем ОМ, полей, индексов и lookup registry; для lookup rows проверяет 10% операций каждого типа, минимум 3 и максимум 10, включая new и updated rows при их наличии.
- **[owner decision] FR-08.** До Apply CLI вычисляет lookup-row browser sample из immutable plan hash и устойчивого ключа операции и сохраняет его в evidence; проверка: повторное вычисление для того же plan даёт тот же набор.
- **[owner decision] FR-09.** Browser acceptance равен 100% выбранных checks; расхождение, пропуск или отсутствие evidence переводит cycle в `verification failed` и не запускает automatic correction; проверка: integration/manual check с одним failed sample не получает success status.
- **[owner decision] FR-10.** Каждый cycle создаёт отдельную versioned run-folder в date-based local catalogue с раздельными папками audit и evidence, отдельным журналом и датой/временем в имени каждого audit/evidence file. Run metadata фиксирует application, workbook-template, Codex-skills, BPMSoft и Excel-tables versions, а для Excel tables — modification date/time; проверка: два запуска не смешивают artefacts и каждый имеет этот полный version set.
- **[owner decision] FR-11.** До compare CLI валидирует pair metadata, IDs/fingerprints/snapshots/derived fields, formulas, validation и `Desired*` values независимо от наличия Excel sheet protection; проверка: каждое недопустимое нарушение блокирует compare до исправления.
- **[owner decision] FR-12.** Mandatory skills получают safe diagnostics blocked validation, сравнивают обе Excel-книги с их previous Git versions, указывают change, связанный с нарушением, и предлагают корректировку без automatic edit; проверка: test fixture с изменённым protected field показывает blocking reason, relevant Git diff и proposed correction.
- **[owner decision] FR-13.** Skill создаёт Git commit обеих Excel-книг только после successful Apply, CLI read-back, browser verification и explicit operator confirmation of success; проверка: до всех gates и confirmation новый commit не создаётся, после них commit содержит обе книги текущего cycle.
- **[owner decision] FR-15.** Successful cycle фиксирует обе Excel-книги одним atomic Git commit с run ID и immutable plan hash в metadata; проверка: commit содержит обе книги, а его metadata однозначно связывает его с run folder и approved plan.
- **[owner decision] FR-16.** Audit, evidence и run journal не добавляются в Git; проверка: successful и failed cycles не меняют Git index этими artefacts.
- **[owner decision] FR-14.** Failed или partially applied cycle не создаёт Git commit; проверка: fault-injection/integration check оставляет Git history без нового commit и создаёт только audit/evidence для manual decision.
- **[owner decision] NFR-01.** Credentials, cookies, CSRF и login response не появляются в workbook, CLI args, logs, plans, evidence или skills context; проверка: redact/marker scan и review outputs не находят secret values.
- **[owner decision] NFR-02.** Операции чтения коллекций используют только доказанные explicit ordering и pagination; проверка: representative dataset дважды обходится без пропусков и дублей с одинаковым результатом.
- **[owner decision] NFR-03.** MVP не выполняет automatic cleanup audit/evidence; retention остаётся ответственностью пользователя; проверка: normal run не удаляет artefacts предыдущего запуска.
- **[owner decision] NFR-04.** Обе Excel-книги обязаны храниться в Git для previous-version diagnosis; проверка: skills могут получить previous version каждой книги для blocked-validation comparison.
- **[owner decision] NFR-05.** Git history обеих Excel-книг размещается в отдельном remote repository проекта ОМ; provider/repository выбирает пользователь при initial setup и фиксирует в отдельном settings artifact; проверка: handoff operator получает согласованную remote history, а не только local clone.
- **[owner decision] NFR-06.** Settings artifact содержит только несекретные provider, repository URL и branch; Git access использует existing user credential manager; проверка: settings, Excel, audit и evidence не содержат Git credential values.
- **[owner decision] FR-17.** После полного success-gate skill отправляет атомарный Git commit обеих Excel-книг, только если нет Git conflict; проверка: conflict-free repository получает один remote commit, а conflict не получает automatic resolution или remote overwrite.
- **[owner decision] FR-18.** После manual Git-conflict resolution CLI повторно валидирует обе Excel-книги и выполняет новый read-only compare; проверка: прежний plan блокируется, пока новый compare не создаст replacement plan.
- **[owner decision] FR-19.** Если remote push не состоялся без conflict из-за сети или authentication, local commit остаётся сохранённым, skill создаёт safe audit/evidence и предлагает later retry; проверка: failed push не теряет commit и не выполняет unrequested retry.
- **[owner decision] FR-20.** Pull/parser records index membership only from `GetSchema.schema.indexes[].columns[].columnUId`, preserving `SchemaUId` package-layer context, `IndexUId`, `IndexName`, `IsUnique`, target `ColumnUId`/name and zero-based member ordinal; verification: a bounded Test1 fixture resolves members to inherited `Code`/`Name` and rejects member `uId` or `Columns.ActualIndexed` as a substitute. Composite/auto-name/broader order semantics remain blocked until separately evidenced.
- **[owner decision] FR-21.** Workbook delivery must produce an explicit verification result that the separate `ActualIndexed` flag neither hides nor distorts exported `Indexes` membership. Before any later index load/apply, an independent gate must demonstrate that the separation cannot create false add/drop operations. Any mismatch or ambiguity yields `INDEX_SYNC_UNRESOLVED`; index loading is excluded unless the owner explicitly approves a corrected contract, while read-only index display may remain.
- **[owner decision] FR-22.** `LookupCatalog` содержит единый лист `LookupValues`, объединяющий metadata записи и нормализованные значения колонок; отдельного `LookupRows` нет. Excel sheet protection применяется только к полностью read-only листам; mixed-листы `Schemas`, `Columns`, `LookupRegistry` и `LookupValues` не защищены и доступны для редактирования целиком. Parser/compare остаётся hard gate для недопустимых изменений. Проверка: workbook read-back подтверждает точный состав листов/колонок и protection map, а desktop Excel позволяет редактировать mixed-листы, менять ширину колонок и применять существующие фильтры на read-only листах без изменения защищённых данных. Фактическая сортировка locked read-only ranges не является acceptance criterion; `AllowSorting=True`, если присутствует, считается только best-effort metadata.
- **[owner decision] FR-23.** `ReferenceRecordId` и `ReferenceDraftRowToken` могут быть заполнены одновременно. Loader выбирает валидный непустой `ReferenceRecordId`; иначе использует только однозначно разрешённый `ReferenceDraftRowToken`; иначе передаёт пустую ссылку. Некорректный непустой `ReferenceRecordId` блокирует загрузку. Проверка: parser fixtures подтверждают приоритет GUID, token fallback, null fallback и invalid-GUID blocker.

## Зависимости и технические блокеры

- **[confirmed]** Logical workbook contract v1 уже описывает physical pair и правила editable/derived/validation. Источник: [WORKBOOK_CONTRACT_VISION.md](../WORKBOOK_CONTRACT_VISION.md).
- **[confirmed]** Текущий probe подтвердил login, inventories, две schema shapes и paging shape, но не доказал stable ordering, full pagination, full schema/lookup mapping, target fingerprint или write semantics. Источник: [PROJECT_HANDOFF.md](../PROJECT_HANDOFF.md), раздел 0.
- **[owner decision]** Отдельный strictly read-only research по deterministic ordering, complete pagination и schema/lookup mapping предшествует первой implementation-ready child spec; до него workbook foundation не реализуется.
- **[open]** До отдельного strictly read-only research невозможно утвердить реализационный путь для child spec 1/2; до отдельного write preflight невозможно открыть child spec 4 для реализации.
- **[open]** Bounded P3-C evidence does not authorize full-catalog index generalisation or add/change/drop index API. Current inherited `column.indexed` semantics also remain unresolved, so `ActualIndexed` cannot be inferred from index membership.
