# Спецификация функции: строгое сравнение и неизменяемый план

**Ветка функции**: `003-compare-read-only-plan`

**Создано**: 2026-09-07

**Статус**: Черновик

**Вводные**: Третья функция четырёхэтапного L2-пилота преобразует validated pair 002 и актуальное read-only state 001 в одну каноническую модель, безопасный HTML-отчёт и immutable machine-readable plan. Общее видение: [source](../../preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md); этапный source: [03](../../preparation/docs/product-specs/local-bpmsoft-synchronizer/03-compare-plan-spec.md); сквозной source: [05](../../preparation/docs/product-specs/local-bpmsoft-synchronizer/05-verification-operations-spec.md); workbook contract: [WORKBOOK_CONTRACT_VISION.md](../../preparation/docs/WORKBOOK_CONTRACT_VISION.md). Все sources неизменяемы.

## Пользовательские сценарии и проверка *(обязательно)*

### Пользовательская история 1 — Получить безопасный результат сравнения (Приоритет: P1)

Оператор сравнивает обе книги и актуальную цель без записи в Excel или BPMSoft и получает согласованные report/plan либо именованные blocker.

**Почему этот приоритет**: это единственная разрешённая основа решения человека перед будущим Apply.

**Независимая проверка**: endpoint capture показывает 0 writes, а независимая сверка подтверждает exact equality операций, counts и blockers в report и plan.

**Сценарии приемки**:

1. **Дано** validated pair и актуальный target fingerprint, **Когда** выполняется compare, **Тогда** report и plan создаются из одной canonical model и имеют один набор операций/blockers.
2. **Дано** unsupported intent, stale state или invalid identity, **Когда** выполняется compare, **Тогда** результат содержит безопасный named blocker и не маскирует его как no-op.
3. **Дано** пустая разница, **Когда** выполняется compare, **Тогда** создаётся проверяемый no-action result, а не Apply session с пустым смыслом.

### Пользовательская история 2 — Получить детерминированный целиком связанный план (Приоритет: P1)

Reviewer проверяет stable operation keys, dependency order, workbook/target/allowlist bindings и предвычисленный browser sample material до решения по плану целиком.

**Почему этот приоритет**: Apply не имеет права пересчитать diff или принять изменённый input.

**Независимая проверка**: перестановка Excel rows и input enumeration не меняет plan semantics/hash/order; изменение любого bound field делает future Apply invalid без writes.

**Сценарии приемки**:

1. **Дано** одинаковые domain inputs и один заранее зафиксированный `RunContext` (`RunId`, `CreatedAtUtc`) в разном presentation order, **Когда** compare выполняется повторно, **Тогда** canonical plan bytes, `PlanHash` и total operation order совпадают. Новый `RunContext` намеренно создаёт другой `PlanHash`, даже если canonical operation set совпадает.
2. **Дано** изменённый workbook hash, template/baseline/target/affected-state fingerprint, allowlist version или operation, **Когда** future Apply validator читает plan, **Тогда** план отклоняется и compare не вызывается из Apply.
3. **Дано** цикл, duplicate producer или unresolved dependency, **Когда** строится operation graph, **Тогда** plan блокируется `DEPENDENCY_CYCLE` либо точным dependency blocker.

### Пользовательская история 3 — Проверить безопасный отчёт и выборку будущей browser verification (Приоритет: P2)

Оператор читает локальный report без Apply controls и видит безопасные причины, source cell/path и remediation; будущая browser выборка lookup changes уже зафиксирована детерминированно.

**Почему этот приоритет**: report должен помогать человеку принять plan, а выборка не должна смещаться после Apply.

**Независимая проверка**: report inspection подтверждает отсутствие Apply UI и секретных/raw values, а property tests для 0/1/2/3/10/11/large counts подтверждают exact sample rule.

**Сценарии приемки**:

1. **Дано** blocker, **Когда** открывается report, **Тогда** он показывает stable keys, safe source location/reason/remediation без credentials и raw lookup values.
2. **Дано** N lookup operations одного kind, **Когда** plan фиксируется, **Тогда** sample содержит 10%, минимум 3, максимум 10 с new/update coverage при наличии; при N<3 проверяются все. Точные stable keys выбираются ранжированием по SHA-256 канонической сериализации tuple (`SamplingBasisHash`, operation kind, stable operation key).

### Пользовательская история 4 — Передать контракт результата в 004 (Приоритет: P2)

Следующий этап получает неизменяемый plan/report/audit contract, достаточный для validation-only admission, но не разрешение записи и не реализованный Apply.

**Почему этот приоритет**: граница plan-before-apply должна быть явной до появления write-capable components.

**Независимая проверка**: consumer contract проверяет schema/version/hash/bindings/order/sample и отклоняет неизвестную версию или изменённый plan без вызова compare/write.

**Сценарии приемки**:

1. **Дано** plan schema неизвестной версии, **Когда** 004 пытается его валидировать, **Тогда** admission прекращается до endpoint dispatch.
2. **Дано** candidate operation kind без owner allowlist/evidence, **Когда** compare встречает intent, **Тогда** вид остаётся DENIED/blocker и не становится разрешённым из-за наличия plan.

### Граничные случаи

- `Null`, `EmptyString` и `Value` различаются; lookup reference следует GUID → resolved draft token → empty, invalid non-empty GUID блокирует.
- Existing identity использует server IDs/UIds + package layer; Name/Code fallback запрещён.
- Rename, type/package/usage changes, delete, index mutation и неизвестные operation kinds не попадают в plan.
- `RunId` и `CreatedAtUtc` фиксируются один раз до compare и не читаются повторно из wall clock при построении/сериализации plan. Изменённый plan byte, `RunContext`, schema/version, workbook/pair/baseline/target/affected fingerprint или allowlist version даёт stale/invalid result до writes.
- Plan допускает только одну canonical serialization; unknown fields, duplicate JSON keys и non-canonical bytes блокируются до Apply.
- Report и plan с разными operation/blocker sets не могут иметь успешный статус.
- Browser sample для 0 содержит 0; для 1/2/3 — все; для 10 — 3; для 11 и больших N — округление правила должно быть однозначно зафиксировано на Clarify/Plan и покрыто property tests без изменения границ 10%/min/max. После определения количества операции ранжируются по SHA-256 канонической сериализации tuple (`SamplingBasisHash`, operation kind, stable operation key); одинаковый rank разрешается сравнением canonical stable keys.
- Audit artifacts используют механизм 001; raw lookup values и secrets по умолчанию запрещены.

### Ожидания к формальным проверкам *(обязательно)*

- **Проверки инкрементов**: будущий `test-plan.md` ДОЛЖЕН покрыть field compare fixtures, every named blocker, report/plan reconciliation, deterministic ordering/hashing, stale binding mutations, endpoint capture и sample boundaries.
- **Проверки итогового решения**: 003 регрессионно использует 001/002 и передаёт 004 неизменяемые bytes/hash/schema/bindings/stable keys/dependencies/sample; future Apply не пересчитывает diff.
- **Ожидания к доказательствам**: golden plans, property/metamorphic results, captured endpoints, HTML safety inspection, exact report-plan diff и mutation matrix.

## Требования *(обязательно)*

### Функциональные требования

- **FR-001 — read-only inputs** (`03-compare-plan-spec.md:CMP-001`; общий `spec.md:FR-009`): compare ОБЯЗАН валидировать пару через 002, читать target через 001 и не иметь доступного write endpoint.
- **FR-002 — field rules** (`CMP-002`): для каждого разрешённого `Desired*` ОБЯЗАНО быть canonical comparison rule; unspecified field не создаёт operation.
- **FR-003 — strict identity** (`CMP-003`; общий `FR-011`): existing entity связывается по server Id/UId + package layer; proposed dependency допускает Name только внутри local draft graph; global Name/Code matching запрещён.
- **FR-004 — forbidden intents** (`CMP-004`; общие `FR-008`, `FR-022`): delete, rename, existing type/package/usage change, index operation и неразрешённый kind исключаются и дают named blocker.
- **FR-005 — typed values** (`CMP-005`): canonical model ОБЯЗАНА различать `Null|EmptyString|Value`, typed representation и reference precedence из 002.
- **FR-006 — dependency order** (`CMP-006`; общий `FR-012`): каждая operation ОБЯЗАНА иметь stable key; graph/total order детерминированы; cycle, duplicate producer и unresolved dependency блокируют plan.
- **FR-007 — единая canonical model** (`CMP-007`): plan и HTML report ОБЯЗАНЫ строиться из одного in-memory result и иметь равные operations/counts/blockers.
- **FR-008 — полные bindings** (`CMP-008`; общий `FR-010`): до compare система ОБЯЗАНА один раз зафиксировать `RunContext` из `RunId` и `CreatedAtUtc`. Plan ОБЯЗАН содержать этот неизменяемый context, schema version, hashes обеих books, template/pair baseline, target и affected-state fingerprints, allowlist version, ordered operations, `SamplingBasisHash` и browser sample material.
- **FR-009 — immutable write-once plan** (`CMP-009`): plan записывается один раз в единственной canonical serialization. `PlanHash` ОБЯЗАН вычисляться от canonical plan content, включающего fixed `RunContext`, все bindings, ordered operations, blockers, `SamplingBasisHash` и `browserSample`, но исключающего только собственное поле `PlanHash`. 004 ОБЯЗАНА отклонять unknown fields, duplicate keys, non-canonical bytes и несовпадающий пересчитанный `PlanHash` и НЕ ДОЛЖНА вызывать compare logic. Отдельный `PlanArtifactHash` не является product contract; обычный SHA-256 файла может храниться только как audit/manifest checksum.
- **FR-010 — staleness** (`CMP-010`): изменение любого bound input/state/version/operation инвалидирует plan до write calls.
- **FR-011 — explicit no-op** (`CMP-011`): empty diff ОБЯЗАН выпускать auditable no-action result, не выдавая ложный Apply success.
- **FR-012 — safe blocker report** (`CMP-012`): report ОБЯЗАН показывать safe reason, stable keys, source cell/path и remediation без credentials/raw values.
- **FR-013 — blocker taxonomy**: как минимум `RENAME_NOT_SUPPORTED`, `TYPE_CHANGE_NOT_SUPPORTED`, `PACKAGE_TRANSFER_NOT_SUPPORTED`, `DELETE_NOT_SUPPORTED`, `INDEX_SYNC_UNRESOLVED`, `INVALID_REFERENCE_ID`, `REFERENCE_TOKEN_UNRESOLVED`, `DERIVED_FIELD_TAMPERED`, `PAIR_MISMATCH`, `STALE_TARGET`, `UNSUPPORTED_SHAPE`, `DEPENDENCY_CYCLE` имеют однозначные conditions.
- **FR-014 — deterministic browser sample material** (`05-verification-operations-spec.md:OPS-005`; общий `FR-018`): до добавления `browserSample` система ОБЯЗАНА вычислить `SamplingBasisHash` от canonical plan payload, содержащего fixed `RunContext`, все bindings и ordered operations, но исключающего `browserSample` и `PlanHash`. Для каждого lookup operation kind система ОБЯЗАНА ранжировать операции по SHA-256 канонической сериализации tuple (`SamplingBasisHash`, operation kind, stable operation key), детерминированно разрешать совпадение rank по canonical stable key и зафиксировать выбранные stable keys в plan: 10%, min 3, max 10. Если присутствуют и new, и update operations, сначала выбирается операция с минимальным rank каждой группы, затем оставшиеся места заполняются по общему rank без повторов. Structural operations передаются на 100% browser check. После включения sample `PlanHash` ОБЯЗАН связывать весь canonical plan content согласно FR-009.
- **FR-015 — audit/evidence consumer** (`OPS-001`, `OPS-002`, `OPS-003`; общий `FR-019`): compare run ОБЯЗАН использовать механизм 001, фиксировать versions, hashes, plan/result gates, не перезаписывать runs и проходить evidence schema/secret scan.
- **FR-016 — compare/review skill** (`OPS-012`; общий `FR-021`): skill ОБЯЗАН запускать compare/validation, показывать report и диагностику, но НЕ ДОЛЖЕН менять books/target, одобрять plan или реализовывать свой diff.
- **FR-017 — handoff contribution** (`OPS-013`): операторский пакет ОБЯЗАН документировать compare, report review, plan invalidation/no-op и передачу exact plan в 004; complete clean-machine flow проверяется в 004.
- **FR-018 — Apply consumer contract** (общие `FR-013`, `FR-014`): 003 определяет validation-only input contract; candidate writes остаются `DENIED` до отдельного owner allowlist и version-specific positive/negative evidence.
- **FR-019 — архитектурная независимость** (общий `NFR-009`): comparison/canonical plan model НЕ ДОЛЖНА зависеть от HTML renderer, BPMSoft write executor, browser automation или Git; adapters не изменяют domain semantics.

### Ключевые сущности *(включайте, если функция связана с данными)*

- **CanonicalComparison**: operations, blockers и no-op result из validated pair/target.
- **PlannedOperation**: stable key, kind, identity, expected before/after, dependencies и source location.
- **RunContext**: один раз зафиксированные до compare `RunId` и `CreatedAtUtc`; новый context означает новый plan instance.
- **ImmutablePlan**: единственные canonical bytes, `SamplingBasisHash`, `PlanHash`, fixed `RunContext`, complete bindings, ordered operations и browser sample.
- **SafeHtmlReport**: human-readable projection той же canonical model без Apply action.
- **PlanValidationContract**: правила future admission без diff recomputation.

## Критерии успеха *(обязательно)*

### Измеримые результаты

- **SC-001**: 100% compare network calls принадлежат read allowlist; Excel/BPMSoft write calls равны 0.
- **SC-002**: report и plan имеют 100% совпадающие operation keys, counts и blockers; расхождения равны 0.
- **SC-003**: случайные permutations row/input order при одном fixed `RunContext` дают одинаковые canonical plan bytes/`SamplingBasisHash`/`PlanHash`/total order в 100% property runs; смена `RunContext` меняет `PlanHash`, но не canonical operation set для тех же domain inputs.
- **SC-004**: изменение каждого bound field из mutation matrix инвалидирует plan до writes в 100% случаев.
- **SC-005**: every forbidden/unsupported intent fixture даёт точный named blocker; silently dropped intents равны 0.
- **SC-006**: sample property tests для 0/1/2/3/10/11/large N соблюдают 10%/min/max и new/update coverage в 100% случаев; перестановка входов не меняет `SamplingBasisHash`, ranks или выбранные stable keys, а изменение любого bound plan content меняет basis либо инвалидирует plan.
- **SC-007**: HTML report содержит 0 Apply controls и проходит secret/raw-value scan; safe remediation присутствует для 100% blockers.
- **SC-008**: consumer fixture 004 принимает только exact known-version plan и отклоняет changed/unknown plan без compare/write calls.

## Допущения

- 001 и 002 предоставляют qualified/validated inputs; их незакрытые gates не считаются закрытыми фактом запуска compare.
- Точный byte-level формат canonical serialization и правило округления 10% формализуются на Clarify/Plan, сохраняя единственность serialization, fixed `RunContext`, двухэтапный `SamplingBasisHash` → `browserSample` → `PlanHash` contract и min/max boundaries.
- Candidate operation kinds не утверждены текущим заданием; значение по умолчанию — `DENIED`.
- Не входят: owner approval, backup confirmation, preflight/write, Apply, read-back execution, workbook writeback, browser execution и Git.
