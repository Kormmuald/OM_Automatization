DRAFT — NOT YET SPEC KIT CANONICAL

# Clarify и review: верхнеуровневый synchronizer (`spec of specs`)

## Источники и подтверждённое evidence

- **[confirmed]** Метод SDD и порядок constitution → spec → clarify/review → plan определены в [PROJECT_HANDOFF.md](../PROJECT_HANDOFF.md), раздел 0, и локальном курсе `C:\CodingAgents\codex\projects\VibeCeH\docs\pilot-course\lessons`.
- **[confirmed]** Logical workbook contract v1 находится в [WORKBOOK_CONTRACT_VISION.md](../WORKBOOK_CONTRACT_VISION.md); он является источником already agreed workbook rules, но не доказательством BPMSoft write semantics.
- **[confirmed]** Read-only evidence находится в `C:\CodingAgents\codex\projects\OM_Automatization\preparation\PrototypeReadOnlyPull\bin\Release\net10.0\probe-output\20260903T124828Z`; безопасные shapes и summary не содержат login response, credential values, cookie/CSRF values или raw lookup values.
- **[confirmed]** [Program.cs](../../PrototypeReadOnlyPull/Program.cs) и [BpmSoftReadOnlyPull.csproj](../../PrototypeReadOnlyPull/BpmSoftReadOnlyPull.csproj) подтверждают назначение prototype: интерактивный login, read-only endpoints, `isPageable=true`, redacted shapes; они не являются реализацией синхронизатора.
- **[confirmed]** Probe подтвердил: login; `GetPackages` (97 packages); `GetWorkspaceItems` (9 157 items); shapes `AcademyURL` и `ActivityPriority`; `SelectQuery` offsets 0/2 для `ActivityPriority` без overlap. Он не доказал explicit stable order и complete pagination. Источник: [PROJECT_HANDOFF.md](../PROJECT_HANDOFF.md), раздел 0.
- **[confirmed, bounded]** `Account/Test1/type=3` read-only probe от 2026-09-04 сохранил два simple one-member `GetSchema.schema.indexes[]` objects. `indexes[].uId`, `.name`, `.isUnique` и member `.columns[].columnUId` доказаны; последний точно связывает members с `Code`/`Name`, хотя эти target columns inherited в Test1 layer. Источники: `docs/READ_ONLY_RESEARCH_RESULTS/20260904T151951Z-Account-Test1-index-research.md`, `...-index-evidence-review.md`, evidence roots `20260904T151951Z` и `20260904T132710Z`.

## Зафиксированные решения владельца

- **[owner decision]** Draft package описывает весь будущий synchronizer как `spec of specs` с пятью дочерними specs, а не одну ближайшую read-only feature.
- **[owner decision]** Strictly read-only research по deterministic ordering, complete pagination и schema/lookup mapping обязателен до первой implementation-ready child spec; он не является реализацией synchronizer.
- **[owner decision]** Это research и последующий conditional workbook workflow будут продолжены в отдельном Codex task по передаваемому handoff и prompt package; текущий task не запускает их.
- **[owner decision]** Целевые пользователи: владелец как test operator, затем независимый BPMSoft developer; target environment — Windows, desktop Excel, local BPMSoft. Отклонения — ответственность пользователя и позже фиксируются в README.
- **[owner decision]** CLI владеет hard deterministic/security gates; mandatory skills запускают CLI, проверяют разрешённые данные, интерпретируют safe results и выполняют read-only browser verification.
- **[owner decision]** Human operator единолично вводит credentials, подтверждает backup и целиком approve/reject immutable plan. Browser может требовать отдельного интерактивного login, но не передаёт его секреты skills.
- **[owner decision]** Workbook использует logical contract и legacy опыт как reference; locally generated formulas/validation/service data допустимы, а legacy/Google rows and values никогда не загружаются в BPMSoft.
- **[owner decision]** Existing entities сопоставляются только устойчивой server identity; new lookup rows требуют `DraftRowToken → RecordId`, не matching по `Code`/`Name`.
- **[owner decision]** MVP создаёт и разрешённо обновляет, но не удаляет BPMSoft entities. Backup automation, SQL delete skills и analyst-to-Excel preparation исключены.
- **[owner decision]** Workbook/target изменения после compare инвалидируют plan; Apply останавливается на первой ошибке без auto-rollback, затем нужны read-back и human decision.
- **[owner decision]** Success требует полного CLI post-apply check и обязательной read-only browser verification: все structural changes проверяются в browser; для lookup rows — 10% per type, минимум 3, максимум 10, с new/updated coverage при наличии.
- **[owner decision]** Lookup-row sample вычисляется до Apply из immutable plan hash и устойчивого ключа операции, сохраняется как evidence и имеет 100% acceptance threshold; failed/missing browser check означает `verification failed` без automatic correction.
- **[owner decision]** Каждый запуск имеет отдельную versioned folder в local date-based catalogue: раздельные audit/evidence folders, отдельный run journal и timestamp в имени каждого audit/evidence file. Run metadata включает application, workbook-template, Codex-skills, BPMSoft и Excel-tables versions; Excel tables дополнительно несут modification date/time. Retention/cleanup остаются ответственностью пользователя; automatic cleanup в MVP отсутствует.
- **[owner decision]** Invalid workbook input blocks compare: broken pair metadata, manual protected-field change, formula/validation failure или invalid `Desired*` value. Mandatory skills explain the block, use previous Git versions обеих Excel-книг to identify the causal change and propose, but never automatically apply, a correction.
- **[owner decision, G3]** P3-C contract correction is approved: `Indexes` is protected read-only evidence built from `schema.indexes[]`, with one row per member and `IndexUId`; membership is solely `indexes[].columns[].columnUId -> Columns.ColumnUId`, and the target may be `Own` or `Inherited` in the selected package layer. `Columns.ActualIndexed` stays a separate legacy-compatibility flag and is not inferred from index membership. This is not G4 and does not authorize workbook/tool work, full-catalog pull, Manage/Write, or mutation APIs.
- **[owner decision, 2026-09-05]** В `LookupCatalog` metadata записи и значения колонок находятся только в едином `LookupValues`; отдельный `LookupRows` удалён. Защита листа обязана разрешать изменение ширины колонок и применение существующих фильтров, не разрешая редактировать protected/derived cells. Последующее решение владельца снимает обязательность фактической сортировки locked read-only ranges; `AllowSorting=True` не является acceptance evidence.
- **[owner decision, 2026-09-05, supersedes protection/XOR detail above]** Excel protection остаётся только на полностью read-only листах; mixed-листы `Schemas`, `Columns`, `LookupRegistry` и `LookupValues` доступны для редактирования целиком, а недопустимые изменения блокирует parser/compare. `ReferenceRecordId` и `ReferenceDraftRowToken` разрешено заполнять одновременно; будущая загрузка использует валидный GUID, иначе resolved draft token, иначе пустую ссылку. Некорректный непустой GUID блокирует загрузку.
- **[owner decision, 2026-09-05, protected-sheet sorting]** Владелец сохраняет Excel protection полностью read-only листов и явно исключает фактическую сортировку их locked ranges из обязательных требований. Resize и existing-filter behavior остаются обязательными; сортировка mixed unprotected sheets доступна обычными средствами Excel.
- **[owner decision, G4]** Read-only evidence is accepted with its recorded limits and G2 is closed for workbook v1. Controlled bounded read-only catalogue extraction, creation/verification of both `.xlsx` files and a minimal local filler tool are authorized. Full-catalog extraction is not an acceptance condition now; the developed tool will attempt it later. BPMSoft Write/Manage, Google input and `SyncOM` changes remain prohibited.
- **[owner decision, mandatory index safety gate]** Workbook delivery must verify that keeping `ActualIndexed` separate from `schema.indexes[]` does not hide or distort exported index membership. Before any future index load/apply, a separate gate must prove that the separation cannot create false add/drop operations. Ambiguity blocks with `INDEX_SYNC_UNRESOLVED`; the owner may defer all index loading to a later iteration while retaining read-only `Indexes`.

## Assumptions, которые нельзя считать решениями

- **[assumption]** Реализация будет последовательно вести child specs в указанном порядке; порядок ещё не утверждён как delivery plan.
- **[assumption]** Exact representation устойчивого ключа операции и технический формат сохранения sample ещё не определены, но не должны менять утверждённые размер, coverage и acceptance threshold.
- **[assumption]** После отдельного read research target fingerprint можно будет построить без доступа к write API; механизм не выбран.
- **[assumption]** Exact local root path, folder-name pattern и schema файлов audit/evidence ещё не определены; они не могут отменить раздельное хранение, timestamping, approved version set или user-managed retention.

## Open и blocking вопросы

- **[blocking, open] Precise research question:** какой exact параметр и JSON payload для `DataService/json/SyncReply/SelectQuery` в локальном BPMSoft 1.8 задаёт явный детерминированный порядок, и может ли повторный полный обход representative lookup с этим порядком дважды доказать отсутствие пропусков и дублей?
- **[blocking, open] Why it blocks:** без этого нельзя считать full-catalog read complete и воспроизводимым; поэтому запрещены child-spec implementation для baseline workbook/pull, создание `.xlsx` и любой переход к write scope. Источник текущего stop condition: [PROJECT_HANDOFF.md](../PROJECT_HANDOFF.md), раздел 0.
- **[open]** Exact schema/lookup registry mapping, target fingerprint, scale и normalisation ещё не подтверждены для local BPMSoft 1.8.
- **[open]** P3-C does not prove composite or auto-named indexes, broader `orderDirection` semantics, full-catalog index representation, or current inherited `column.indexed` semantics. Add/change/drop index APIs remain a separate write-preflight gate.
- **[open, mandatory next-stage check]** Export and later load behaviour of the separate `ActualIndexed` flag versus `Indexes` membership must be tested explicitly. A failed or ambiguous result does not authorize a workaround; it triggers `INDEX_SYNC_UNRESOLVED` and an owner decision whether index loading is removed from the current version.
- **[open]** Manage/Write rights, allowed mutation fields, create/save/read-back responses, compile/atomicity и production `DraftRowToken → RecordId` требуют отдельного будущего write preflight; они не исследуются сейчас.
- **[open]** Exact representation stable operation key и evidence format browser sample не утверждены; это не разрешает менять accepted selection policy.
- **[owner decision]** Обе Excel-книги хранятся в Git и являются source для previous-version diagnosis. Skill создаёт commit обеих книг только после successful Apply, CLI read-back, browser verification и explicit operator confirmation of success.
- **[owner decision]** Successful cycle фиксирует обе Excel-книги одним atomic Git commit; его metadata содержит run ID и immutable plan hash.
- **[owner decision]** Обе Excel-книги ведутся в отдельном remote Git repository проекта ОМ; local-only repository не удовлетворяет MVP. Provider/repository выбираются пользователем при initial setup и фиксируются в отдельном settings artifact.
- **[owner decision]** Settings artifact содержит только несекретные provider, repository URL и branch; Git access остаётся в existing user credential manager и не сохраняется в settings, Excel, audit/evidence.
- **[owner decision]** После полного success-gate skill отправляет commit в remote repository только без Git conflict. Conflict не разрешается автоматически и требует действия пользователя.
- **[owner decision]** После manual Git-conflict resolution обе Excel-книги повторно проходят CLI validation и read-only compare; previous immutable plan запрещён.
- **[owner decision]** Failed remote push без conflict из-за сети/authentication сохраняет local commit, добавляет safe audit/evidence и требует явного user retry.
- **[owner decision]** Audit, evidence и run journal остаются только в local run folders и не добавляются в Git.
- **[owner decision]** Failed/partially applied cycle не создаёт Git commit; skill сохраняет audit/evidence и ждёт manual operator decision.
- **[owner decision]** Remote Git provider не задаётся проектом: его выбирает пользователь при initial setup. Exact shape settings artifact и authority remote publication после local commit ещё не определены.

## DoD спецификации и переход к plan

- **[owner decision]** Draft не может быть объявлен готовым к Spec Kit без явного owner confirmation и прохождения DoD.
- **[open] DoD check:** цели, users, верхнеуровневый scope, child-spec map, safety rules и testable acceptance criteria зафиксированы в drafts; нужны первый owner review и устранение противоречий.
- **[open] DoD check:** blocking read question не закрыт, поэтому implementation plan, tasks и slices не создаются.
- **[owner decision] Transition criterion:** к планированию конкретной child spec можно перейти только после human review, отсутствия blocking contradictions в соответствующей spec и прохождения её необходимых evidence gates. Общий canonical Spec Kit workflow требует отдельного решения владельца.
