# Prompt: Apply MVP discovery with the owner

Работай с пользователем почти полностью по-русски. Твоя задача — **вместе с владельцем определить безопасный путь разработки ограниченного Apply MVP**, а не реализовывать Apply и не выполнять никаких write-операций.

## Обязательный контекст

Сначала прочитай:

1. `AGENTS.md`;
2. `.specify/feature.json`;
3. `specs/001-read-only-catalog-qualification/HANDOFF.md`;
4. `docs/TODO.md`;
5. `specs/003-compare-read-only-plan/spec.md` и `specs/004-gated-apply-operations/spec.md`;
6. существующие CLI/domain/application/adapters и tests — только для понимания уже доказанного Compare MVP.

`preparation/docs/product-specs/` — неизменяемые source drafts: не редактируй, не перемещай и не используй как output directory.

## Установленные факты

- Strict Feature 001 не квалифицирована; `CATALOG_ORDER_OR_PAGING_UNQUALIFIED` остаётся открытым.
- Практический best-effort Compare MVP доказан отдельными read-only live runs: combined OM + typed lookup intent дал `completed`, `operations=2`, `blockers=0`.
- Это не разрешает Apply, не доказывает полноту каталога, права записи, endpoint/payload или write safety.
- `compare-plan.json` конфиденциален, user-local и содержит desired values. Не проси, не читай и не выводи его содержимое, credentials, origin, cookies или реальные lookup values.
- Existing Compare поддерживает только `Columns.DesiredRequired` для existing `Own` columns, typed scalar `LookupValues.Value` и opaque `LookupReference.ReferenceRecordId`. Это candidate intents, не Apply allowlist.

## Жёсткие запреты

Не выполняй и не предлагай как следующий автоматический шаг:

- BPMSoft writes, write-capable transport/endpoints, live probes, Apply, SQL, browser write, Excel writeback, backup/restore, delete/rollback, Git commit/push;
- credentials/origin/password requests или чтение локального plan;
- создание параллельного приложения;
- утверждения, что любой candidate kind уже разрешён.

Не изменяй код, specs, handoff или TODO без отдельного прямого указания владельца. Никакой ответ пользователя не является разрешением на live write, пока он явно не описан отдельным решением.

## Формат работы

1. Кратко изложи доказанные границы и спроси **один** вопрос: какую бизнес-ценность должен дать первый Apply MVP — change `DesiredRequired`, scalar lookup value или только доказательство admission/preflight?
2. После ответа предложи не более трёх вариантов, каждый с:
   - точным candidate kind и strict identity;
   - ожидаемой бизнес-пользой;
   - неизвестными BPMSoft write-contract/permission/read-back фактами;
   - preflight/evidence, которые надо доказать до первого write;
   - рисками данных, recovery boundary и тем, что остаётся denied.
3. Рекомендуй самый узкий вариант только при явной причине. Не подменяй owner decision техническим предположением.
4. Согласуй с владельцем decision package: выбранный kind, явные exclusions, required evidence, human gates, stop conditions и критерии перехода от design к implementation planning.
5. Заверши кратким owner-ready decision record и списком открытых вопросов. Если владелец попросит сохранить его, сначала уточни target document и только затем редактируй разрешённый artifact.

## Минимально ожидаемый результат discovery

Результат должен позволить следующему implementation агенту подготовить spec/plan без расширения scope и должен содержать:

- ровно один или явно ноль candidate write kinds;
- deny-by-default boundary;
- preflight matrix;
- immutable-plan/staleness requirements;
- exact required human approvals;
- per-operation audit/read-back/stop-first-error requirements;
- список будущих local tests и отдельно gated live evidence;
- явную фразу: «никакой Apply или live write не разрешён этим discovery».
