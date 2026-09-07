DRAFT — NOT YET SPEC KIT CANONICAL

# Конституция проекта: локальный синхронизатор ОМ BPMSoft

## Статус и назначение

- **[owner decision]** Этот файл — временный repo-native draft устойчивых правил проекта. Он не является canonical GitHub Spec Kit `constitution.md` и не разрешает реализацию, BPMSoft probe или изменение production-кода.
- **[owner decision]** Будущий продукт — локальный Windows-комплект: CLI-приложение и обязательные Codex skills. Он будет передан разработчикам BPMSoft для самостоятельного запуска в их локальном окружении.
- **[confirmed]** Рабочий процесс следует цепочке «контекст и ограничения → constitution → spec → clarify/review → plan → slices/tasks → implementation/verification → human acceptance/handoff». Источник: [PROJECT_HANDOFF.md](../PROJECT_HANDOFF.md), раздел 0.

## Неизменяемые правила безопасности

- **[owner decision]** Пароль вводит только человек-оператор непосредственно в CLI либо, при browser verification, в отдельной интерактивной browser-сессии. Skills не получают, не передают и не сохраняют credentials.
- **[confirmed]** Password, cookies, CSRF и login response существуют лишь в памяти процесса и не пишутся в workbook, исходники, аргументы командной строки, логи, evidence или drafts. Источник: [PROJECT_HANDOFF.md](../PROJECT_HANDOFF.md), разделы 0/3.4; [Program.cs](../../PrototypeReadOnlyPull/Program.cs).
- **[owner decision]** Browser verification strictly read-only. Она не открывает write-путь и не даёт skills права менять BPMSoft.
- **[owner decision]** Любое отклонение от Windows + установленного desktop Excel + локально развёрнутого BPMSoft — ответственность пользователя. Эти предпосылки позднее описываются в README; создание README сейчас не входит в работу.

## Разделение read, plan и apply

- **[owner decision]** Read/compare всегда strictly read-only; он не создаёт и не изменяет BPMSoft и не скрывает двусторонний merge.
- **[owner decision]** Compare формирует человекочитаемый отчёт и машиночитаемый неизменяемый change plan. Apply не пересчитывает diff молча.
- **[owner decision]** Только человек-оператор подтверждает сделанный вручную backup и целиком принимает либо отклоняет plan. Skills показывают evidence, но не могут одобрить plan или запустить Apply.
- **[owner decision]** Изменение workbook, template version, target fingerprint либо затронутого состояния BPMSoft после compare инвалидирует plan. Apply блокируется до нового read-only compare и регенерации артефакта.
- **[owner decision]** При первой неудачной операции Apply CLI останавливается без автоматического rollback, сохраняет безопасный audit/evidence и требует read-back с ручным решением оператора.
- **[owner decision]** Автоматизация backup/restore и SQL-удалений — Out of Scope MVP.

## Идентичность и допустимые изменения

- **[confirmed]** Server `Id`, schema `UId` и column `UId` относятся к разным уровням модели и не взаимозаменяемы. Источник: [PROJECT_HANDOFF.md](../PROJECT_HANDOFF.md), разделы 3.9 и 11.
- **[owner decision]** Для существующих сущностей соответствие строится только по подтверждённому server `Id`/контекстному устойчивому идентификатору contract; `Code` и `Name` не являются технической identity.
- **[owner decision]** Для новой lookup row используется только локальный `DraftRowToken` как корреляционный маркер. Apply допустим лишь при доказуемом `DraftRowToken → RecordId` через create-response или однозначный read-back; иначе `DO NOT START`.
- **[confirmed]** Клиент не генерирует BPMSoft GUID для новых схем, колонок, registry records или data rows. Источник: [PROJECT_HANDOFF.md](../PROJECT_HANDOFF.md), раздел 11.
- **[owner decision]** MVP допускает только создание и доказанно разрешённое обновление схем ОМ, полей, индексов, lookup registry и lookup rows. Удаление объектов, полей, индексов и строк BPMSoft — Out of Scope MVP.
- **[owner decision, G3]** `Indexes` is protected read-only evidence: its membership is `GetSchema.schema.indexes[].columns[].columnUId -> Columns.ColumnUId`, including an `Own` or `Inherited` target in the selected package layer. `IndexUId` is an observed identity, while index-member `uId` is not a column identity. `Columns.ActualIndexed` remains separate legacy compatibility context and is never inferred from membership. This decision authorizes no index mutation, full-catalog pull, workbook/tool work, or Manage/Write scope.
- **[owner decision, G4 safety rule]** Every workbook export must test that separate `ActualIndexed` and `Indexes` representations do not lose or distort index membership. Any future index load/apply requires an independent proof that the separation cannot produce false add/drop actions. Failure or ambiguity is `INDEX_SYNC_UNRESOLVED`; index loading may be deferred entirely while read-only index evidence remains. G4 permits bounded workbook/tool delivery, not BPMSoft Write/Manage, Google input or `SyncOM` changes; full-catalog extraction is deferred to the developed tool and is not a current acceptance condition.

## Evidence, проверка и human gates

- **[owner decision]** CLI владеет всеми детерминированными и security-critical checks: IDs, hashes, fingerprints, plan validity и read-back. Skills не обходят эти gates.
- **[owner decision]** Skills обязательно запускают и сопровождают CLI, проверяют разрешённые исходные данные, интерпретируют безопасные результаты и проводят read-only browser verification.
- **[owner decision]** CLI жёстко блокирует compare при повреждённой pair metadata, недопустимом изменении identity/fingerprint/snapshot/derived fields, ошибочной formula/validation либо недопустимом `Desired*` value. Excel protection применяется только к полностью read-only листам; mixed-листы редактируемы целиком, поэтому CLI/parser, а не sheet protection, является обязательной границей безопасности. Skills объясняют причину, но не обходят этот block.
- **[owner decision, 2026-09-05]** Для lookup-reference разрешено одновременно заполнить `ReferenceRecordId` и `ReferenceDraftRowToken`. Будущая загрузка использует валидный `ReferenceRecordId`; при его отсутствии — подтверждённое разрешение draft token в server GUID; при отсутствии обоих — пустую ссылку. Некорректный непустой GUID блокирует загрузку.
- **[owner decision]** Обязательные validation skills сравнивают проблемный input с предыдущими Git versions обеих Excel-книг, определяют изменение, после которого возникла ошибка, и предлагают корректировку. Они не применяют корректировку автоматически.
- **[owner decision]** Skill создаёт Git commit обеих Excel-книг только после successful Apply, CLI read-back, browser verification и явного подтверждения оператора, что update завершено успешно.
- **[owner decision]** Этот commit атомарно фиксирует обе Excel-книги и содержит в metadata идентификатор запуска и хэш immutable plan.
- **[owner decision]** Обе Excel-книги ведутся в отдельном удалённом Git repository проекта ОМ; только local Git history недостаточна для MVP. Provider и repository выбирает конкретный пользователь при initial setup и фиксирует в отдельном settings artifact инструмента.
- **[owner decision]** Settings artifact содержит только несекретные Git parameters: provider, repository URL и branch. Git access использует уже настроенный credential manager пользователя и не записывается в settings artifact, Excel, audit или evidence.
- **[owner decision]** После полного success-gate skill отправляет Git commit в remote repository только при отсутствии conflict. Git conflict автоматически не разрешается и передаётся пользователю.
- **[owner decision]** После manual Git-conflict resolution обе Excel-книги повторно проходят CLI validation и read-only compare; прежний immutable plan использовать запрещено.
- **[owner decision]** Если remote push не состоялся без Git conflict из-за сети или authentication, skill сохраняет local commit, пишет safe audit/evidence и предлагает пользователю повторить push позже.
- **[owner decision]** При failed или partially applied update skill не создаёт Git commit. Он сохраняет только безопасные audit/evidence и ждёт ручного решения оператора.
- **[owner decision]** Audit, evidence и run journal остаются только в локальных run folders и никогда не добавляются в Git.
- **[owner decision]** Успешный цикл требует полного post-apply CLI read-back и обязательной browser-проверки. В browser проверяются все structural changes (schemas, columns, indexes и lookup registry); для lookup rows проверяются 10% операций каждого типа, минимум 3 и максимум 10, с одной new и одной updated row при их наличии.
- **[owner decision]** Набор lookup-row browser sample вычисляется до Apply из immutable plan hash и устойчивого ключа операции, сохраняется как evidence и не выбирается skills произвольно. Порог browser acceptance — 100% выбранных checks; любое расхождение, пропуск или отсутствие evidence даёт `verification failed` без automatic correction.
- **[confirmed]** Evidence не содержит credential values, login response, cookie/CSRF values или raw lookup-row values по умолчанию. Источник: [PROJECT_HANDOFF.md](../PROJECT_HANDOFF.md), раздел 0 «Безопасный evidence».
- **[owner decision]** Каждый запуск создаёт отдельную versioned run-folder в date-based local catalogue. Внутри неё раздельно хранятся папка audit и папка evidence, а также отдельный журнал запуска; названия файлов audit/evidence содержат дату и время проведения.
- **[owner decision]** Run metadata обязательно фиксирует версии приложения, workbook template, Codex skills, BPMSoft и Excel tables, а для Excel tables — также дату и время изменения.
- **[owner decision]** Retention и очистка audit/evidence — ответственность пользователя. MVP не выполняет automatic cleanup.

## Текущий технический stop condition

- **[confirmed]** До доказательства explicit deterministic ordering, complete repeatable pagination и exact schema/lookup-registry mapping запрещены full-catalog pull, создание `.xlsx` и открытие write scope. Источник: [PROJECT_HANDOFF.md](../PROJECT_HANDOFF.md), раздел 0 «Что не проверено и технический stop condition».
- **[owner decision]** Отдельный strictly read-only research по ordering, pagination и schema/lookup mapping обязателен до первой implementation-ready child spec; workbook foundation development до него не начинается.
- **[owner decision]** BPMSoft write, проверка Manage/Write и реализация не начинаются без отдельного будущего решения владельца, даже после завершения этих drafts.
- **[confirmed, bounded]** P3-C evidence covers only two simple, one-member, non-auto-named indexes and observed `orderDirection=0`; composite/auto-name/general order semantics, catalog coverage and inherited `column.indexed` semantics remain open read gates.
