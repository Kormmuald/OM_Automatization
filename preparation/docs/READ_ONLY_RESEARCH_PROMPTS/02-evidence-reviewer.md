# Субагент: независимое review read-only evidence

**Рекомендуемая модель:** `gpt-5.6-terra`, reasoning `high`.

## Цель

Проверить, доказывает ли результат исследователя именно заявленные claims, а не похожее поведение одного sample.

## Входы

- результат `01-read-only-researcher.md`;
- `docs/PROJECT_HANDOFF.md` и `docs/SDD_DRAFTS/clarify-review.draft.md`;
- созданные evidence paths и изменённый prototype, если он есть.

## Scope

Read-only inspection source/evidence: endpoint intent, explicit order, two-pass completeness, duplicates/skips, mapping, redaction, source-to-claim traceability.

Если researcher завершился с failed ordering proof, независимо проверь минимальный diagnostic evidence: `run-result.json`, `request-contract.json`, `redaction-check.json` и каждый фактически созданный `*.ids.txt`. Воспроизведи сравнение `ASC-1` с `ASC-2` и reversed `ASC-1` с `DESC` непосредственно по строкам файлов, сверь counts и SHA-256, проверь UUID-only содержимое и отсутствие raw data/session/credential values. Failed run может подтверждать конкретное расхождение, но не может считаться доказательством ordering/pagination contract или порождать success mapping artefacts.

Для assembly-backed retry дополнительно независимо сверь зафиксированные version/SHA-256 локальных `BPMSoft.Common.dll` и `BPMSoft.Nui.ServiceModel.dll`, наличие `allColumns=false`, единственную sorting-column `Id` и точный разрешённый row shape (`Id` для `ActivityPriority`; `Id` + `SysEntitySchemaUId` для `Lookup`). Не принимай explanation про обход `columns` как evidence, если `request-contract.json` не связывает её с конкретными assembly paths/version/hashes и inspected type/member/token.

Для выполненного P3-C index probe независимо проверь `20260904T151951Z`: отсутствие `SelectQuery` и лишних HTTP calls, unique workspace selection `Account/Test1/type=3`, equality selected/returned schema UId, полный structural shape `schema.indexes`, exact paths `indexes[].uId/name/isUnique/columns[].columnUId`, две simple single-column relations и owner observation `Code=unique`, `Name=non-unique`. Membership должен разрешаться через `columns[].columnUId` в `ColumnUId` target, который может быть `Own` или `Inherited`; member `uId`, member name и `column.indexed` не являются relation oracle. Отдельно зафиксируй, что old own+`indexed=true` oracle был contradicted false negative, а current inherited `column.indexed` semantics не доказана. Не обобщай simple sample на composite, auto-name, broader `orderDirection` или full catalog. Сверь SHA-256 manifest и redaction; historical Creatio/Terrasoft reference допустим только как terminology support, не как local BPMSoft API evidence.

## Out of scope

Не запускай BPMSoft requests, не меняй source/contract/SDD/handoff/probe evidence, не создавай `.xlsx`, не делай write/Manage checks и не объявляй contract valid без evidence. Единственное разрешённое изменение — сохранить собственный итоговый Markdown-report по exact path, заранее переданному оркестратором в `docs/READ_ONLY_RESEARCH_RESULTS/`; generated `probe-output` остаётся неизменным.

## Control gate

Если любой claim не доказан, противоречит source или скрывает secret/value, верни `DO NOT START` с точным gap и остановись. Не предлагай обход gate и не подбирай другой ordering payload.

## Выход

Таблица claim | status | evidence path | gap | impact. Заверши одним вердиктом: `G2 pass`, `G2 partial` или `G2 stop`, и укажи, нужен ли owner decision. Сразу в начале работы создай через `apply_patch` checkpoint-файл по переданному оркестратором path со статусом `IN_PROGRESS`, inputs/evidence root и перечнем ещё не проверенных групп; после каждой завершённой группы обновляй его накопленным source-backed результатом. До возврата ответа переведи тот же self-contained Markdown-report в статус `COMPLETE` и включи выполненные команды, matrix, findings, not-run, verdict и точный следующий owner question. Не включай secrets или raw lookup values.
