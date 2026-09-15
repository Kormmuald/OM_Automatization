# Гипотезы remediation live qualification — Cycle 2

**Дата:** 2026-09-15
**Статус:** подготовлено Cycle 2 hypothesis worker; выбор, изменение кода и принятие не выполнялись.

## Факт, с которого начинается Cycle 2

Исторический S07 остаётся единственным full live qualification: он закончился
fail-closed `SCHEMA_INVENTORY_UNQUALIFIED` / `UNKNOWN_SHAPE_UNQUALIFIED`, exit
`2`, `RetryCount=0`; Pass B, reconciliation и Excel-пара не запускались.

Cycle 1 не изменил strict acceptance contract и не доказал H-001. Его измеримое
улучшение — отдельный bounded diagnostic с sealed safe evidence локализовал первый
отказ только до закрытой классификации:

| Поле | Ожидание адаптера | Безопасно наблюдённый JSON-kind | Что это доказывает |
| --- | --- | --- | --- |
| `SchemaPackageId` (`schema.package.id`) | `GuidString` | `String` | Первый отказ находится именно в этом predicate; значение не сохранялось и не раскрывалось. |

Это **не** доказывает, что строка является допустимым opaque package identity,
что она семантически эквивалентна GUID, что endpoint вернул ожидаемый вариант
контракта или что production adapter дефектен. Также это не доказывает H-001
как общий тезис о любом непокрытом subshape.

## Источники и границы этого обновления

Для этого обновления использованы только безопасные S07/S08 и Cycle 1 reports,
current Feature 001 artifacts, закрытые domain/adapter/test contracts и immutable
reference source code из `preparation/PrototypeReadOnlyPull/` и
`preparation/SyncOM/`. Historical references служат только semantic/API clue;
raw response files, bodies, values, identifiers и secrets не переносятся и не
фиксируются в hypothesis register.

Любой будущий probe сначала должен быть offline characterization/inspection.
Если потребуется live verification, допускается только отдельная reviewer-approved
bounded read-only route с уже существующими interactive credentials. Durable
evidence может содержать только closed path/category/kind, count/cardinality or
length buckets, route/outcome and validator/seal facts. Запрещены raw response
data, values, GUID, names, URL, credentials, cookies, CSRF and login response.
Во всех гипотезах запрещены Write, Manage, Compare, Apply, browser write, SQL
mutation, delete, Git push, `SELECT_QUERY`, lookup traversal, Pass B/Pass C,
automatic retry/rerun и Excel publication.

## Предыдущие гипотезы: статус без дублирования

| ID | Статус после Cycle 1 | Evidence и связь с Cycle 2 |
| --- | --- | --- |
| H-001 — strict adapter не покрывает допустимый full-scope schema subshape | **Не доказана; частично локализована.** | Safe first-failure discriminator показал только `SchemaPackageId: GuidString → String`. H-001 не подтверждена и не опровергнута: допустимость observed string не установлена. Новые H-004…H-006 разбирают именно этот факт. |
| H-002 — `schema.id` чрезмерно обязателен | **Не проверялась; не поддержана текущим evidence.** | Первым failure является package `id`, не schema `id`; H-002 не выбирается, пока новый safe fact не укажет на `SchemaId`. |
| H-003 — index/member form не покрыт parser | **Не проверялась; не поддержана текущим evidence.** | Traversal останавливается до index parsing; H-003 не выбирается, пока first-failure path не изменится на index branch. |

## Новые проверяемые гипотезы

### H-004 — `schema.package.id` является допустимым opaque identity, а не GUID

- **Вероятность:** средняя (0,50).
- **Формулировка:** BPMSoft variant может представлять package `id` как
  non-GUID string identity. Current `TryGuid(package, "id", ...)` отклоняет её
  до создания `PackageLayerIdentity`, хотя package-layer identity в qualification
  может требовать typed opaque representation rather than forced `Guid`.
- **Затронутые компоненты:** `WorkspaceInventoryAdapter`, `PackageLayerIdentity`
  and `SchemaIdentity` contracts, canonicalization/fingerprint/reconciliation,
  sanitized S02 fixtures and adapter/domain tests. Transport, allowlist, lookup
  and Excel layout не меняются до доказанного нового domain contract.
- **Сначала offline probe:** создать characterization matrix только для
  `schema.package.id`: GUID string, opaque non-empty string, empty/whitespace
  string, missing, `null`, number and object. Отдельно проследить usage current
  package identity through canonicalization/fingerprint and prove that proposed
  explicit opaque variant cannot merge package layers or weaken A/B equality.
- **Live probe при необходимости:** только existing bounded diagnostic; record
  не расширяется raw value. Допустим лишь closed `SchemaPackageId`, expected
  `GuidString`, observed `String`, plus one derived non-secret validity class
  (`guid-parseable`/`non-guid-opaque`/`invalid-empty`) if independently reviewed.
- **PASS:** offline matrix and identity proof show a lossless, collision-safe
  opaque package-id variant; bounded safe evidence is consistent with
  `non-guid-opaque`. **FAIL:** string is empty/whitespace or otherwise invalid,
  opaque variant creates ambiguity/collision, or an authoritative typed contract
  requires a GUID.
- **Запрещённое изменение:** no name-only fallback, no replacing package-layer
  identity with schema name, no acceptance relaxation without collision and
  two-pass regression proof.

### H-005 — contract separates `package.id` (opaque server key) from GUID `package.uId`

- **Вероятность:** средняя (0,35).
- **Формулировка:** `schema.package.id` may be an opaque application/database
  key while `schema.package.uId` remains the GUID identity suitable for stable
  package-layer qualification. The adapter currently requires both as GUID and
  therefore conflates their semantic roles.
- **Затронутые компоненты:** `WorkspaceInventoryAdapter`, package-layer/domain
  identity and fingerprint contract, duplicate/collision handling, fake-handler
  characterization and reconciliation tests. It does not authorize endpoint or
  export changes.
- **Сначала offline probe:** inspect typed construction and all fingerprint
  inputs; create sanitized schemas differing only in `package.id` while holding
  a valid `package.uId`, then schemas differing only in `package.uId`. Prove the
  proposed semantic separation preserves package-layer distinction, target
  fingerprint determinism and A/B equality; include negative collision vectors.
- **Live probe при необходимости:** use only the bounded safe evidence already
  capable of reporting the failing `SchemaPackageId` category. Do not capture
  either identifier. A separate code-reviewed derived fact may report only that
  the companion `package.uId` passed/failed its `GuidString` predicate.
- **PASS:** a reviewed contract maps opaque `id` to a non-primary provenance
  field and GUID `uId` to stable identity without losing or merging layers; all
  collision/reconciliation vectors pass. **FAIL:** `uId` is absent/invalid,
  package identity is still ambiguous, or repository contract demonstrates that
  `id` itself must be the primary GUID.
- **Запрещённое изменение:** do not silently discard `package.id`, derive
  identity from display names, or reuse an A-pass identity cache in B.

### H-006 — `GetSchema` response variant is endpoint/call-context dependent

- **Вероятность:** низкая-средняя (0,25).
- **Формулировка:** the same read-only `SCHEMA_GET` route may return a package
  object with a string `id` only for a specific workspace item, package layer or
  request contract variant. The fact could be target data variation rather than
  a universal schema-model rule; a parser rewrite based on one item would be
  overbroad.
- **Затронутые компоненты:** bounded diagnostic source/request construction,
  workspace-to-schema identity correlation, sanitized fake-handler tests and
  safe diagnostic evidence design. No change to endpoint allowlist, auth,
  normal qualification or Excel is in scope for this hypothesis.
- **Сначала offline probe:** characterize the exact current `SCHEMA_GET`
  request construction against fake responses for a conventional GUID package
  and opaque-string package; verify first-blocker behaviour and that the
  diagnostic stops at one schema call. Inspect reference source code only for
  request-shape semantics, never for raw historical payloads.
- **Live probe при необходимости:** one reviewer-authorized bounded run may
  report only fixed route, first-failure path/category/kind and ordinal/count
  buckets. It must not collect a schema/package name, identifier, URL, request
  body or a second schema sample merely to compare variants.
- **PASS:** offline request characterization is exact and safe evidence proves
  the observed variant belongs to the existing route/first item; it remains
  explicitly unknown whether variation is universal. **FAIL:** request fixture
  mismatch, transport rejection, or the first failure moves to another closed
  path; then this is not evidence for an endpoint-context variant.
- **Запрещённое действие:** no probing multiple schemas for comparison, no new
  endpoint, no request-body broadening and no automatic retry.

## Очерёдность и блокер

Ни одна гипотеза не выбирается этим документом. H-004 is the most direct
semantic explanation, but H-005 must be ruled in/out before any production
contract change because it determines whether an opaque `id` is identity or
provenance. H-006 is a boundary check, not a reason to generalize parser
acceptance. Новый reviewer должен выбрать ровно одну hypothesis and approve its
offline probe before a worker changes code. Successful offline tests or bounded
diagnostic do not qualify the target and do not authorize Excel output.
