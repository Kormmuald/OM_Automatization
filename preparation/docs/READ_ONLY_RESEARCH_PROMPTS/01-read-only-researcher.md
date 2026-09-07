# Субагент: доказательство read-only BPMSoft контракта

**Рекомендуемая модель:** `gpt-5.6-sol`, reasoning `high`.

## Цель

Закрыть только blocking research question: найти доказанный explicit deterministic order для `SelectQuery`, дважды обойти representative lookup без skips/duplicates и подтвердить exact schema/lookup-registry mapping, нужный workbook contract.

## Входы

- `docs/PROJECT_HANDOFF.md`, разделы 0, 1, 10, 11 и 13;
- `docs/SDD_DRAFTS/clarify-review.draft.md`;
- `docs/WORKBOOK_CONTRACT_VISION.md`;
- `PrototypeReadOnlyPull/Program.cs` и текущий safe evidence.

## Разрешённый scope

Только `PrototypeReadOnlyPull`, только login/read endpoints, redacted response shapes/summaries и safe local evidence. При необходимости разрешено предложить и после plan-before-change изменить prototype для нового strictly read-only payload.

### Минимальный diagnostic evidence при failed ordering proof

После owner-approved G2 retry prototype обязан сохранить безопасный диагностический набор даже если ASC/DESC assertion не прошёл. Формат намеренно простой:

- отдельный UTF-8 `.txt` для каждого фактически завершённого pass: один UUID `Id` на строку в server-returned order;
- `request-contract.json` только с read-only endpoint и payload без auth/session values;
- `run-result.json` со статусом `CONFIRMED` или `NOT_CONFIRMED`, sanitized failure reason, counts, SHA-256 sequence digests, фактически завершёнными checks и `not run`;
- `redaction-check.json` с результатом pre-write scan.

Для `ActivityPriority` ожидаемые имена: `ActivityPriority.asc-pass-1.ids.txt`, `ActivityPriority.asc-pass-2.ids.txt`, `ActivityPriority.desc-control.ids.txt`. Для `Lookup` используется тот же pattern только если traversal был фактически достигнут. Failed run не должен создавать mapping/proof artefact со статусом success.

Перед записью весь диагностический набор проверяется на отсутствие login request/response, password, headers, cookies, CSRF, raw lookup values, captions/descriptions и иных data values. Разрешены только technical UUID identities, порядок строк, counts/digests, read payload и безопасный verdict. При failed check дальнейшие BPMSoft requests прекращаются; alternative payload автоматически не подбирается.

### Assembly-backed ordering retry для local BPMSoft 1.8.0.14107

Owner-approved retry использует подтверждённую локальными assemblies форму, а не новый guessed enum:

- `SelectQueryColumn.OrderDirection` имеет тип `BPMSoft.Common.OrderDirection`: `None=0`, `Ascending=1`, `Descending=2`;
- `allColumns=true` выбирает `SelectAllColumns` и обходит explicit `columns`, поэтому предыдущий `Id.orderDirection` не применялся;
- retry обязан установить `allColumns=false`;
- `ActivityPriority` явно запрашивает только `Id`;
- `Lookup` явно запрашивает только `Id` и `SysEntitySchemaUId`; sorting задаётся только на `Id`, а `SysEntitySchemaUId.orderDirection=0`;
- перед live retry сверяются version/SHA-256 `BPMSoft.Common.dll` и `BPMSoft.Nui.ServiceModel.dll` с зафиксированными в approved pre-change packet; значения и assembly-backed rationale записываются в безопасный `request-contract.json`.

Если версии/hashes изменились, response содержит неожиданные row fields либо `allColumns=false` не даёт exact reverse, retry немедленно останавливается по обычному failed-evidence правилу. Автоматически возвращать `allColumns=true`, менять enum или добавлять поля запрещено.

### P3-C: bounded non-empty index probe — выполнен 2026-09-04

`Account/Test1/type=3` уже исследован в `PrototypeReadOnlyPull/bin/Release/net10.0/probe-output/20260904T151951Z`; новый запуск не разрешается этим prompt. Bounded sample подтвердил два simple single-member `schema.indexes[]` objects: unique `Index1` и non-unique `Index2`; их members ссылаются соответственно на `Code`/`Name` через `schema.indexes[].columns[].columnUId`. Обзор: `docs/READ_ONLY_RESEARCH_RESULTS/20260904T151951Z-Account-Test1-index-research.md` и `...-index-evidence-review.md`.

Исправленное evidence-backed правило для возможного будущего owner-approved probe: index definition принадлежит выбранному schema/package layer, но target column разрешён как `Own` **или** `Inherited` в этом layer. Membership доказывает только `columns[].columnUId`, который должен совпасть с `ColumnUId`; member `columns[].uId`, member name и `column.indexed` не являются substitute. Не требовать own `Code`/`Name` и `indexed=true`: именно это был false-negative oracle выполненного sample. Разрешённые safe artefacts при новом отдельном разрешении: request contract, bounded workspace selection, redacted response/index shape, mapping both own/inherited columns, exact paths, run result, redaction check и manifest.

Для такого нового bounded claim `CONFIRMED` допустим лишь при однозначной member relation, наблюдаемом index `uId`/`name`/`isUnique`, и безопасной фиксации cardinality members. Composite, auto-name, broader `orderDirection` semantics и full-catalog behaviour не повышаются из simple sample; `column.indexed` у inherited target требует отдельного question/evidence. Неоднозначность, unsafe scalar или расхождение с owner oracle дают `NOT_CONFIRMED` без второго payload/run.

Исторический reference `docs/REFERENCES/creatio-terrasoft-manual-section-registration/README.md` используется только для различения `Id` и `UId`; SQL из него не выполняется и не является доказательством index/GetSchema contract BPMSoft 1.8.

## Запрещено

Не выполнять Write/Manage, create/update/delete/save, compile, backup, `.xlsx`, full-catalog pull до доказанного order/pagination, Google input. Не проси и не принимай password/cookies/CSRF/login response; оператор вводит credentials самостоятельно в интерактивном окне.

## Gates

- До change: верни exact предполагаемый read-only endpoint/payload, proof method и ожидаемые безопасные artefacts; жди одобрения оркестратора.
- Немедленно останови дальнейшие BPMSoft requests при неизвестной семантике, рискованном endpoint либо evidence, не подтверждающем order/mapping. Если уже получены разрешённые diagnostic UUID sequences, сохрани только описанный выше безопасный failed-run набор со статусом `NOT_CONFIRMED`; не подбирай alternative payload без нового решения владельца.

## Verification

Покажи build result, exact commands (без secrets), paths evidence, two-pass IDs/counts/diff, line-by-line comparison `ASC-1` с `ASC-2` и reversed `ASC-1` с `DESC`, SHA-256 файлов, redaction check и явно `not run` для всех write/Excel действий. Сравнение должно воспроизводиться непосредственно из сохранённых `.txt`, а не только из runtime assertion.

## Выход

Краткий evidence report: claim → source/path → result; confirmed/not confirmed; impact на stop condition; exact команды повторного сравнения сохранённых `.txt`; precise question владельцу, если gate не пройден. Не меняй SDD/contract/handoff.
