# Субагент: controlled workbook delivery

**Рекомендуемая модель:** `gpt-5.6-sol`, reasoning `high`.

## Предусловие

Работай только после G4: есть recorded G2 pass, владелец принял или явно скорректировал contract, отдельно разрешил создание `.xlsx` и при необходимости минимального инструмента его наполнения. G4 зафиксирован 2026-09-04: разрешён bounded verified baseline; полный каталог не является acceptance condition этой delivery и должен позднее выгружаться уже разработанным инструментом.

## Цель

Создать и заполнить две локальные Excel-книги Model Catalog и Lookup Catalog только verified local BPMSoft baseline, в соответствии с approved contract; подготовить минимальный tool лишь если это необходимо для воспроизводимого наполнения.

## Входы

- approved decision packet G3;
- актуальные `WORKBOOK_CONTRACT_VISION.md` и SDD drafts;
- verified read-only evidence и approved read path;
- `docs/PROJECT_HANDOFF.md`.

## Scope

Сначала дай plan-before-code: разрешённые files, source data, workbook validation, tests и risks. Отдельным обязательным пунктом плана покажи, как будет проверено, что `Columns.ActualIndexed`, не выводимый автоматически из `schema.indexes[]`, не скрывает и не искажает строки read-only `Indexes`. После одобрения выполни только approved workbook/tool work, сохрани обе книги в Git-ready location и не используй Google values/rows/formulas как input. Локально generated formulas/validation/service data допускаются только по approved contract.

## Out of scope и stop conditions

Нет BPMSoft write/Manage/delete, Google data import, automatic Git commit/push, browser write, hidden merge или scope expansion. Остановись при несовпадении evidence/contract, Excel limit/scale risk, невозможности сохранить stable IDs/pair metadata, либо если нужен новый product decision. Если раздельные `ActualIndexed` и `Indexes` дают потерю, искажение или неоднозначный будущий add/drop intent, зафиксируй `INDEX_SYNC_UNRESOLVED`; не придумывай автоматическое правило и вынеси владельцу вариант исключить index loading из текущей версии.

## Verification и output

Покажи contract validation обеих книг, source counts/hashes, formula/validation checks, absence внешних links/secrets, повторяемость generation и все `not run` checks. Вынеси отдельный pass/fail/partial результат проверки `ActualIndexed` versus `Indexes` на выгрузке и явно напомни, что будущая загрузка индексов требует нового gate. Верни changed files, paths двух книг, tool status, evidence и gaps. Не принимай результат.
