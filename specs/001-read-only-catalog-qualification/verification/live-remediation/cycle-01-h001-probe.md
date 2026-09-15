# Cycle 1 — controlled offline probe H-001

**Дата:** 2026-09-15
**Worker:** `gpt-5.6-terra` / `high`
**Статус:** завершён офлайн; не является acceptance и не разрешает live run.

## Граница и входные данные

Проверка ограничена существующими sanitized fixtures, текущим code/tests и
accepted artifacts Feature 001. Не выполнялись live BPMSoft calls, network auth,
browser action, credential use, production/test/fixture changes, Git commit/push,
Write/Manage/Compare/Apply, SQL mutation или delete.

Прочитаны canonical Feature 001 artifacts, current `HANDOFF.md`, S00–S08
acceptance/handoff/review evidence, S07 blocker evidence, current adapter/transport
and tests, а также immutable legacy/prototype material только как semantic
reference. Legacy material не использовался как runtime proof или source for a
copy.

Независимый reviewer выбрал H-001: full traversal может встретить легитимный
schema subshape, отсутствующий в строгом typed adapter contract. Основание выбора:
исторический S07 сохранил только generic terminal result, тогда как code содержит
несколько обязательных schema branches, дающих этот же result.

## Команды и результаты

| Команда | Результат |
| --- | --- |
| `Get-Content`/`rg` по canonical artifacts, S07 и текущему adapter/test surface | завершено; использованы только локальные read-only inputs. |
| `dotnet run --project tests/BpmSoftSync.Adapters.BpmSoft.Tests/BpmSoftSync.Adapters.BpmSoft.Tests.csproj -c Release --no-restore` | exit `0`; прошли `ReadEndpointAllowlistTests`, `SessionTests`, `UnknownShapeTests`, `WorkspaceInventoryAdapterTests`, `LookupCatalogSourceTests`. Это fake-handler suite, без live target. |
| `rg` по `WorkspaceInventoryAdapter.cs` для blocker branches и `BuildEnvelope` | подтверждены broad scopes и отсутствие failed-path envelope. |

## Матрица current contract и существующего доказательства

| Узел / вариант | Current adapter outcome | Existing fixture/test proof | Безопасный discriminator в S07 evidence |
| --- | --- | --- | --- |
| root `schema`: отсутствует, `null`, не object | `SCHEMA_INVENTORY_UNQUALIFIED`, scope `schema` | Нет отдельной matrix; есть aggregate malformed fixture. | Нет field/path/kind. |
| root `schema.name`, `schema.uId`, `schema.id` | Тот же blocker/scope при missing, `null`, wrong kind или invalid required scalar. | Success fixture подтверждает только valid вариант; aggregate malformed fixture не локализует поле. | Нет field/path/kind. |
| `schema.package` и её required identity/name fields | Тот же blocker/scope `schema`. | Только valid вариант в success fixture. | Нет field/path/kind. |
| `schema.columns`, `schema.inheritedColumns` | Тот же blocker/scope `schema`, если array отсутствует/`null`/не array. | Aggregate malformed fixture omits required arrays и подтверждает fail-closed result, но не различает каждый array и JSON kind. | Нет field/path/kind/cardinality. |
| `schema.parentSchema` | Absent/`null` допускаются; non-null object требует typed name and identity, иначе scope `schema-parent`. | Valid parent и absent parent покрыты success fixture; missing/`null`/wrong-kind matrix отсутствует. | Только broad scope `schema-parent`, если он durable. |
| column and inherited-column members | Любое нарушение обязательного member field даёт scope `schema-members`. | Success fixture покрывает typed examples; negative matrix отсутствует. | Только broad scope `schema-members`, без owning array/member/rule. |
| index and member paths | Любое нарушение required index/member field даёт scope `schema-members`; отдельный public relation reader имеет иной index blocker, но full observed-schema path его не использует. | Success composite example и read-only relation test существуют; missing/`null`/wrong-kind matrix отсутствует. | Только broad scope `schema-members`; H-003 не отделён. |

## Structural evidence assessment

`LosslessShapeEnvelope` корректно сохраняет structural tree, scalar classes/lengths
and hashes для unknown properties после успешной adaptation. Это подтверждают
existing unknown-shape tests. Однако early returns required-schema parser строят
только `Blocker`; они не вызывают `BuildEnvelope` и не передают в blocker
property-path, observed JSON kind, array cardinality или failed rule.

Исторический S07 durable evidence имеет only `SCHEMA_INVENTORY_UNQUALIFIED` /
`UNKNOWN_SHAPE_UNQUALIFIED` with scope `schema`. Этого недостаточно, чтобы
безопасно отличить root/package/schema-id cause H-001 от specialised H-002 или
index/member cause H-003. Следовательно, H-001 не подтверждена и не опровергнута.

## Безопасное решение

- **H-001:** `unresolved`.
- **Live diagnostic сейчас:** **не разрешён**. Existing safe structural
  discriminator не устанавливает конкретный failed path и не удовлетворяет PASS
  criterion H-001.
- **Blocker:** отсутствие required-shape characterization matrix и safe,
  durable failed-path structural envelope.

## Минимальная proposal boundary (не patch)

Отдельный implementation worker может предложить и покрыть fixtures/tests для
одного минимального diagnostic contract at the first failed required schema path:

1. Сохранить только path class, expected rule, property names, JSON kinds, array
   cardinalities, scalar class/length/hash and a hashed stable identity; raw JSON,
   raw scalar, schema/package display name, identifier value, URL, cookie, CSRF,
   login response и credentials запрещены.
2. Сначала fixtures/characterization доказывают missing/`null`/wrong-kind behavior
   for root schema, candidate identity/package fields, both column arrays, parent
   and index/member paths.
3. После implementation, validation и independent reviewer `Pass` можно лишь
   рекомендовать ровно один bounded diagnostic: already-allowlisted
   `AUTH_LOGIN` → `WORKSPACE_ITEMS` → `SCHEMA_GET` до первого blocker, без
   `SELECT_QUERY`, Pass B, Excel output, retry или automatic rerun.

Эта proposal не меняет разрешённые endpoints и не authorizes a live attempt.
