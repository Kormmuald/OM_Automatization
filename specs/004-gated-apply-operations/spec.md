# Спецификация функции: управляемый Apply, проверка и завершение цикла

**Ветка функции**: `004-gated-apply-operations`

**Создано**: 2026-09-07

**Статус**: Черновик

**Вводные**: Четвёртая функция завершает операторский L2-сценарий: operation-level preflight, whole-plan gated Apply, staged read-back, browser verification, workbook-only writeback и безопасная Git-финализация. Она НЕ утверждает, что компоненты реализованы, и НЕ разрешает live/write: candidate kinds остаются `DENIED` до точных owner decisions и version-specific evidence. Общее видение: [source](../../preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md); этапные sources: [04](../../preparation/docs/product-specs/local-bpmsoft-synchronizer/04-gated-apply-spec.md) и [05](../../preparation/docs/product-specs/local-bpmsoft-synchronizer/05-verification-operations-spec.md); workbook contract: [WORKBOOK_CONTRACT_VISION.md](../../preparation/docs/WORKBOOK_CONTRACT_VISION.md). Все sources неизменяемы.

## Пользовательские сценарии и проверка *(обязательно)*

### Пользовательская история 1 — Допустить только доказанный operation kind (Приоритет: P1)

Владелец отдельно рассматривает exact request/response/read-back contract и positive/negative evidence каждого candidate kind; всё неутверждённое остаётся запрещённым.

**Почему этот приоритет**: наличие спецификации или plan не является полномочием на Write/Manage.

**Независимая проверка**: machine allowlist и human decision record совпадают; отсутствующие kinds/fields, delete и indexes невозможно dispatch; каждый допущенный kind имеет полный evidence package конкретной BPMSoft version.

**Сценарии приемки**:

1. **Дано** candidate kind без owner allowlist либо принятого preflight evidence, **Когда** он встречается в intent/plan, **Тогда** admission возвращает `WRITE_ALLOWLIST_UNAPPROVED` или `WRITE_SEMANTICS_UNPROVEN`, writes равны 0.
2. **Дано** отдельно авторизованный bounded preflight, **Когда** проверяются positive и required negative cases, **Тогда** evidence остаётся scoped к одному kind/version и не расширяет остальные permissions.

### Пользовательская история 2 — Выполнить точный план с остановкой на первой ошибке (Приоритет: P1)

Оператор подтверждает ручной backup и принимает либо отклоняет весь plan; исполнитель валидирует exact immutable input и выполняет только approved operations в plan order.

**Почему этот приоритет**: это центральная fail-closed граница любых изменений BPMSoft.

**Независимая проверка**: fault injection в каждой позиции доказывает отсутствие subsequent writes и Git commit; altered/stale input даёт 0 writes до dispatch.

**Сценарии приемки**:

1. **Дано** exact canonical plan 003 с fixed `RunContext`, current allowlist, unchanged books/target и два human confirmations, **Когда** Apply начинается, **Тогда** admission пересчитывает `SamplingBasisHash` и `PlanHash`, отклоняет non-canonical/unknown/duplicate content и исполняет только allowlisted operations в exact order с per-operation audit.
2. **Дано** первая ошибка, timeout, malformed response или ambiguity, **Когда** она возникает, **Тогда** дальнейшие writes прекращаются; automatic rollback/retry/delete не выполняются.
3. **Дано** partial prior run, **Когда** оператор хочет повторить, **Тогда** обязательны новый read-back/compare/plan и новое human decision, а продолжение старого plan запрещено.

### Пользовательская история 3 — Доказать итог через полный CLI read-back (Приоритет: P1)

После structural stage система получает доказанные server IDs, выполняет требуемую compile/read sequence и только после PASS допускает lookup stage; затем сверяет всё affected state.

**Почему этот приоритет**: browser evidence не заменяет авторитетный CLI read-back.

**Независимая проверка**: каждая expected field и ID mapping связана plan operation → request → response/read-back; ambiguity/mismatch блокирует success и следующий stage.

**Сценарии приемки**:

1. **Дано** structural operations, **Когда** save/compile/read-back sequence не даёт PASS, **Тогда** lookup stage не начинается.
2. **Дано** новый schema/column/registry/lookup row, **Когда** server identity отсутствует или неоднозначна, **Тогда** выполнение останавливается без Name/Code fallback.
3. **Дано** завершённые разрешённые stages, **Когда** финальный CLI read-back сравнивает affected state, **Тогда** 100% expected fields совпадают до browser/Git success.

### Пользовательская история 4 — Выполнить browser verification только для чтения (Приоритет: P1)

Оператор использует отдельный интерактивный browser login при необходимости и проверяет все structural operations плюс plan-fixed deterministic lookup sample без UI writes. Невозможность выполнить автоматизированную проверку отделяется от подтверждённого расхождения в UI.

**Почему этот приоритет**: это обязательное дополнение к CLI evidence и условие успеха до Git.

**Независимая проверка**: checklist покрывает exact stable keys; 100% checks PASS, read-only action review не обнаруживает write-capable UI actions, credentials/session отсутствуют в skill context и evidence.

**Сценарии приемки**:

1. **Дано** успешный полный CLI read-back, **Когда** browser verification выполняется, **Тогда** все structural keys и exact lookup sample stable keys из plan 003 проверяются read-only с отдельными evidence results; повторный выбор или смещение sample после Apply запрещены.
2. **Дано** browser automation не смогла выполнить или завершить обязательные проверки из-за недоступности браузера, login/session либо неисправности инструмента, **Когда** вычисляется итог, **Тогда** фиксируется `VERIFICATION_PENDING`, оператору сообщается о необходимости самостоятельной read-only проверки либо предлагается помощь в устранении причины; Apply не объявляется неуспешным, workbook-only writeback после successful CLI read-back разрешён, а Git success остаётся запрещён.
3. **Дано** оператор выполнил самостоятельную read-only проверку exact keys и сохранил требуемое evidence, **Когда** 100% проверок подтверждают ожидаемое состояние, **Тогда** результат переводится из `VERIFICATION_PENDING` в `VERIFIED` и может использоваться в последующем human success confirmation.
4. **Дано** завершённая автоматизированная или самостоятельная проверка показывает, что хотя бы одно ожидаемое изменение фактически не применилось либо не видно в UI стенда, **Когда** результат фиксируется, **Тогда** устанавливается `VERIFICATION_FAILED`, Git success запрещается, а оператору предлагаются диагностика и отдельное решение об исправлении или откате; автоматический откат не выполняется.

### Пользовательская история 5 — Обновить книги только доказанными значениями (Приоритет: P2)

После успешного read-back отдельная local-only команда записывает в книги только доказанные IDs/actual values, если content hashes не изменились. Команда разрешена как при `VERIFIED`, так и при `VERIFICATION_PENDING`, поскольку опирается на авторитетный CLI read-back, а не на browser evidence.

**Почему этот приоритет**: это завершает корреляцию draft identities, не смешивая её с BPMSoft writes.

**Независимая проверка**: endpoint capture показывает 0 BPMSoft calls, before/after cell diff входит в allowlist, а изменение книги блокирует writeback без merge.

**Сценарии приемки**:

1. **Дано** доказанные mappings и unchanged hashes, **Когда** выполняется writeback, **Тогда** меняются только разрешённые identity/actual/reference cells с cell-level audit.
2. **Дано** workbook changed после plan, **Когда** writeback проверяет hash, **Тогда** операция прекращается и требуется новый cycle.
3. **Дано** browser status равен `VERIFICATION_PENDING`, а полный CLI read-back успешен, **Когда** оператор запускает workbook-only writeback, **Тогда** writeback разрешён, фиксирует pending status в audit/evidence и не разрешает Git success.
4. **Дано** после writeback оператор подтвердил `VERIFICATION_FAILED` и отдельно одобрил исправление либо откат, **Когда** состояние BPMSoft затем изменилось, **Тогда** workbook не считается финальным и должен быть повторно согласован только из нового successful CLI read-back; автоматическая компенсация книги запрещена.

### Пользовательская история 6 — Завершить цикл одним безопасным Git commit (Приоритет: P2)

Только после полного CLI read-back, 100% browser acceptance и явного подтверждения человека обе книги фиксируются одним commit с `RunId`/plan hash и безопасно push.

**Почему этот приоритет**: Git — последняя стадия принятого успеха, а не средство маскировать partial result.

**Независимая проверка**: temporary repository fixtures покрывают success, dirty/unrelated state, conflict, network/auth failure и invalidation после conflict.

**Сценарии приемки**:

1. **Дано** все gates PASS и human success confirmation, **Когда** выполняется finalize, **Тогда** один commit содержит обе книги и message связывает `RunId`/plan hash; run artifacts отсутствуют.
2. **Дано** failed/partial cycle, `VERIFICATION_PENDING` или `VERIFICATION_FAILED`, **Когда** finalize запрошен, **Тогда** workbook commit не создаётся, даже если workbook-only writeback уже выполнен.
3. **Дано** conflict, **Когда** push не может быть безопасно завершён, **Тогда** auto-resolution запрещён; после manual resolution обязательны pair validation и новый compare/plan.
4. **Дано** network/auth push failure без conflict, **Когда** локальный commit уже создан, **Тогда** он сохраняется и ждёт explicit retry.

### Пользовательская история 7 — Передать и принять MVP на чистой машине (Приоритет: P2)

Handoff operator устанавливает пакет и skills, проходит документированный полный цикл без устных знаний владельца, а человек принимает, меняет, останавливает или масштабирует пилот.

**Почему этот приоритет**: 004 завершает общую acceptance, но агент не подменяет human decision.

**Независимая проверка**: clean-machine protocol сопоставляет каждый MVP criterion с evidence и exact owner decision; package не содержит secrets или локальных absolute paths.

**Сценарии приемки**:

1. **Дано** self-contained Windows package и отдельная подготовленная среда, **Когда** независимый оператор проходит supported flow, **Тогда** он завершает его без устных указаний либо получает точный documented blocker/fallback.
2. **Дано** полная evidence matrix, **Когда** владелец принимает решение L2, **Тогда** решение явно фиксирует continue/change/stop/scale/promote и не выводится из agent recommendation.

### Граничные случаи

- Все candidate writes по умолчанию: create schema/column/lookup row и update fields — `DENIED_PENDING_*`; registry registration — `DENIED_PENDING_OWNER_SCOPE_AND_PREFLIGHT`; indexes — `DENIED_INDEX_SYNC_UNRESOLVED`; delete — `DENIED_OUT_OF_SCOPE`.
- Permission denied, stale state, timeout/HTTP failure, malformed response, ambiguous identity и read-back mismatch входят в обязательный negative preflight каждого kind.
- Plan byte/`RunContext`/schema/allowlist/input/target mismatch, unknown field, duplicate key или non-canonical serialization останавливает до first write; Apply не пересчитывает diff.
- Failure на границе structural/lookup stages не допускает следующий stage; автоматический rollback отсутствует. После подтверждённого `VERIFICATION_FAILED` исправление или откат возможны только как отдельное явное решение оператора с новыми обязательными checks.
- `DraftRowToken -> RecordId` без доказанного response/read-back никогда не использует Name/Code.
- Browser login/session остаются только у человека; технически невыполненная проверка даёт `VERIFICATION_PENDING`, а подтверждённое UI mismatch — `VERIFICATION_FAILED`; ни один из этих статусов не может быть отмечен PASS или разрешить Git success.
- Git dirty/unrelated state или conflict не очищаются и не разрешаются автоматически.
- Run artifacts никогда не добавляются в workbook repository; secrets остаются в credential manager или interactive memory.

### Ожидания к формальным проверкам *(обязательно)*

- **Проверки инкрементов**: будущий `test-plan.md` ДОЛЖЕН покрыть per-kind preflight, deny-by-default dispatch, exact plan validation, fault injection, staged read-back/ID mapping, writeback cell diff, browser sampling/read-only actions, Git scenarios и skills boundaries.
- **Проверки итогового решения**: успешный Git запрещён до полного CLI read-back и 100% browser acceptance; clean-machine acceptance охватывает cumulative regression 001→002→003→004 и все MVP criteria.
- **Ожидания к доказательствам**: owner decisions, allowlist/evidence packages, plan/audit/read-back reconciliation, browser checklist/screenshots без secrets, endpoint captures, Git refs/output и clean-machine sign-off.

## Требования *(обязательно)*

### Функциональные требования

- **FR-001 — per-kind preflight package** (`04-gated-apply-spec.md:APPLY-001`): для каждого candidate kind до live probe ОБЯЗАНЫ быть заданы exact endpoint/payload, expected response/read-back, permissions, failures и cleanup/manual recovery boundary.
- **FR-002 — required preflight outcomes** (`APPLY-002`): evidence ОБЯЗАНО включать positive, permission denied, stale state, ambiguous identity, timeout/HTTP failure, malformed response и read-back mismatch.
- **FR-003 — owner allowlist** (`APPLY-003`; общий `spec.md:FR-013`): kind/field включается только после owner review/acceptance; omissions остаются denied, а evidence одного kind не переносится на другой.
- **FR-004 — complete admission** (`APPLY-004`; общие `FR-010`, `FR-013`, `FR-014`): Apply требует exact immutable canonical plan, fixed `RunContext`, совпадающие пересчитанные `SamplingBasisHash` и `PlanHash`, current allowlist version, unchanged bindings/state, manual backup confirmation и whole-plan approval. Unknown fields, duplicate keys и non-canonical bytes блокируют admission. Отдельный `PlanArtifactHash` не требуется; file SHA-256 допустим только как audit/manifest checksum.
- **FR-005 — exact execution and stop-first-error** (`APPLY-005`; общий `FR-014`): executor следует только plan order, пишет per-operation audit и прекращает все later writes после первой ошибки.
- **FR-006 — forbidden dispatch** (`APPLY-006`; общий `FR-022`): delete/rollback/index operations и endpoints отсутствуют; недопущенные kinds остаются blockers.
- **FR-007 — staged structural gate** (`APPLY-007`; общий `FR-016`): structural save/compile/read-back sequence выполняется ровно в доказанной форме; lookup stage не начинается до PASS.
- **FR-008 — доказанная server identity** (`APPLY-008`; общий `FR-015`): новые server IDs принимаются только из доказанного response или однозначного read-back; missing/ambiguous mapping останавливает выполнение.
- **FR-009 — draft token mapping** (`APPLY-009`; общие `FR-011`, `FR-015`): `DraftRowToken -> RecordId` уникален, проверяем и используется для dependencies; Name/Code fallback запрещён.
- **FR-010 — полный affected-state read-back** (`APPLY-010`; общий `FR-016`): финальный CLI read-back сверяет каждое expected field; любое mismatch означает failed/partial result без Git.
- **FR-011 — workbook-only writeback** (`APPLY-011`; общий `FR-017`): отдельная команда после successful read-back и unchanged workbook hashes меняет только доказанные ID/actual/reference cells, ведёт cell-level audit и делает 0 BPMSoft calls. Browser status `VERIFICATION_PENDING` не блокирует writeback, но сохраняется в audit/evidence и продолжает блокировать Git success. Если последующее `VERIFICATION_FAILED` приводит к отдельно одобренному исправлению или откату BPMSoft, workbook требует нового successful CLI read-back и повторного writeback; автоматическая компенсация запрещена.
- **FR-012 — safe retry** (`APPLY-012`): partial plan не продолжается автоматически; обязательны новый read, validation, compare, plan и human decision.
- **FR-013 — browser session boundary** (`05-verification-operations-spec.md:OPS-004`; общий `FR-018`): browser verification строго read-only, при необходимости использует отдельный interactive login; skill не получает credentials/session.
- **FR-014 — browser coverage** (`05-verification-operations-spec.md:OPS-005`): 100% structural operations и exact plan-fixed lookup sample stable keys проверяются без повторного выбора. Admission ОБЯЗАН проверить `SamplingBasisHash`, пересчитать ranks из канонической сериализации tuple (`SamplingBasisHash`, operation kind, stable operation key), воспроизвести правило обязательного lowest-rank new/update coverage, сверить сохранённый sample и `PlanHash`; lookup sample сохраняет 10% per kind, min 3, max 10.
- **FR-015 — browser outcomes и 100% acceptance** (`OPS-006`): невозможность завершить проверку из-за browser/login/session/tooling даёт `VERIFICATION_PENDING`, уведомление оператору и выбор между устранением причины и самостоятельной read-only проверкой exact keys. Самостоятельная проверка с полным evidence может дать `VERIFIED`. Фактически обнаруженное отсутствие/несоответствие изменения в UI даёт `VERIFICATION_FAILED` и отдельный выбор диагностики, исправления либо операторского отката. Только `VERIFIED` разрешает последующий human success confirmation и Git success; browser flow и executor не исправляют состояние и не выполняют rollback автоматически.
- **FR-016 — CLI authority** (`OPS-007`): полный CLI read-back остаётся авторитетным и ОБЯЗАН завершиться до browser success; browser evidence только дополняет его.
- **FR-017 — Git settings boundary** (`OPS-008`): настройки содержат только provider, repository URL и branch; credentials остаются в user credential manager.
- **FR-018 — atomic success commit/push** (`OPS-009`; общий `FR-020`): после successful CLI read-back, завершённого либо явно пропущенного optional writeback, browser status `VERIFIED`, остальных gates и следующего за ними explicit human success confirmation создаётся один commit обеих books с `RunId`/plan hash и push только без conflict.
- **FR-019 — Git failure paths** (`OPS-010`): conflict не разрешается автоматически и инвалидирует plan до новой validation/compare; network/auth failure сохраняет local commit и требует explicit retry.
- **FR-020 — no commit without accepted success** (`OPS-011`): failed/partial cycle, `VERIFICATION_PENDING` и `VERIFICATION_FAILED` не создают workbook commit, включая случай уже выполненного workbook-only writeback; run artifacts исключены из workbook repository.
- **FR-021 — полный набор skills** (`OPS-012`; общий `FR-021`): setup, pull, compare/review, Apply accompaniment, browser verify, blocked-input diagnosis и Git finalize skills только оркестрируют CLI/read-only browser/Git gates, не получают secrets и не принимают critical decisions.
- **FR-022 — полный handoff и clean-machine acceptance** (`OPS-013`): пакет ОБЯЗАН включать settings schema, install/update, operator runbook, failure playbook и acceptance procedure; независимый operator связывает все MVP criteria с evidence и human decision.
- **FR-023 — общий run/audit/evidence consumer** (`OPS-001`, `OPS-002`, `OPS-003`; общий `FR-019`): Apply/read-back/browser/writeback/Git runs используют механизм 001, фиксируют versions/gates/plan hash, не перезаписывают runs и проходят deny-by-default evidence/secret scan.
- **FR-024 — cumulative trace** (общий `NFR-006`): каждый stable operation key ОБЯЗАН трассироваться plan → request → response/read-back → browser/evidence → workbook writeback/Git result при применимости.
- **FR-025 — MVP acceptance**: успех возможен только при выполнении всех восьми критериев общего `spec.md §9`, включая full catalog, per-kind evidence, failure tests, full CLI read-back, 100% browser checks, Git behavior, secret scan и отсутствие index operations.

### Ключевые сущности *(включайте, если функция связана с данными)*

- **WriteOperationKind / AllowlistDecision / PreflightEvidence**: exact scope, BPMSoft version и human acceptance одного kind.
- **ApplyAdmission / ExecutionEvent**: plan bindings, confirmations, stable operation key и stop-first-error outcome.
- **IdentityProof / ReadBackResult**: доказанный server ID mapping и full affected-state reconciliation.
- **BrowserCheck / VerificationResult**: read-only check stable key, required coverage и evidence outcome.
- **WorkbookWriteback**: unchanged-hash gate и allowlisted cell mappings.
- **GitFinalization / CleanMachineAcceptance**: two-book commit/push state, failure path и explicit human pilot decision.

## Критерии успеха *(обязательно)*

### Измеримые результаты

- **SC-001**: 100% dispatched kinds/fields присутствуют одновременно в exact plan, current owner allowlist и accepted version-specific evidence; undocumented dispatches равны 0.
- **SC-002**: mutation каждого admission binding, `RunContext`, canonical byte, `SamplingBasisHash` или `PlanHash` и fault injection в каждой operation position дают 0 writes после failure и 0 workbook commits.
- **SC-003**: 100% expected affected fields и новых IDs подтверждены полным CLI read-back; ambiguous/missing mappings равны 0 для success.
- **SC-004**: browser checks покрывают 100% structural operations и exact deterministic lookup sample из plan; пересчитанные basis/ranks/sample совпадают с plan в 100% случаев, post-Apply resampling равен 0, acceptance rate для successful cycle — 100%; browser/login/session/tooling failures дают только `VERIFICATION_PENDING`, подтверждённые UI mismatches — только `VERIFICATION_FAILED`, и оба статуса блокируют Git success.
- **SC-005**: workbook-only writeback при `VERIFIED` или `VERIFICATION_PENDING` делает 0 BPMSoft calls и меняет 100% только allowlisted proven cells; changed input block rate — 100%; последующее изменение BPMSoft после отдельно одобренного исправления/отката требует нового CLI read-back до повторного согласования workbook.
- **SC-006**: successful Git fixture создаёт ровно один commit с обеими books и `RunId`/plan hash; failed/partial fixtures создают 0 commits.
- **SC-007**: conflict/network/auth/dirty-state fixtures соблюдают documented stop/retry/invalidation behavior в 100% случаев без auto-resolution.
- **SC-008**: все persisted artifacts проходят schema/secret/raw-value scans; leaked secret canaries равны 0.
- **SC-009**: независимый оператор на чистой Windows-машине завершает supported cycle без устных знаний либо получает точный documented blocker; каждый MVP criterion имеет evidence/human decision.

## Допущения

- Candidate operation kinds не утверждены: до owner allowlist и accepted preflight все остаются `DENIED`; текущее G0 разрешает только пересборку specifications.
- Separate live authorizations нужны для full-catalog run, каждого write preflight и Apply; разработка безопасных локальных checks не равна admission.
- `INDEX_SYNC_UNRESOLVED`, delete/rollback/browser-write запреты сохраняются независимо от остальных PASS.
- Точные BPMSoft endpoints/payloads/permissions/compile sequence не угадываются; они возникают только из bounded evidence и human acceptance.
- Реализация, live BPMSoft, backup, Apply, browser execution, Git commit/push и clean-machine acceptance в рамках этой спецификации не выполняются.
