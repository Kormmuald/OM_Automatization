# Copy-paste prompt для этапа `critique`

```text
Выполни этап Critique только для active feature
`specs/001-read-only-catalog-qualification`. Все сообщения и итоговый отчёт — на
русском языке. Не переходи к `/SpecKit Tasks`, `/SpecKit Analyze` или
`/SpecKit Implement`.

## Проверка target и границ

До любых действий прочитай `.specify/feature.json`. Если `feature_directory` не
равен ровно `specs/001-read-only-catalog-qualification`, остановись, сообщи
фактический target и ничего не меняй.

Прочитай полностью `AGENTS.md`, `.specify/memory/constitution.md`,
`.specify/project.yml`, `.specify/extensions.yml`, общий неизменяемый spec
`preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md`, active
`spec.md` вместе с `## Clarifications`, `checklists/requirements.md`,
`plan.md`, `research.md`, `data-model.md`, все файлы из `contracts/`,
`quickstart.md`, `test-plan.md`, `implementation-slices.md`,
`handoff-slices-to-tasks.md`, `tasks-generation-prompt.md`, а также
фактический код и tests, если они существуют.

Считай весь `preparation/docs/product-specs/` неизменяемым. Не читай этапные source
drafts, кроме названного общего `spec.md`, и ничего в этой директории не меняй.

## Обязательная процедура Critique

Перед выполнением прочитай полный `SKILL.md` команды `$speckit-critique` и следуй
ему. Проверь `.specify/extensions.yml`: `speckit.critique.run` является mandatory
post-Plan hook. Выполни Critique только в пределах её предусмотренного workflow и
зафиксируй результат согласно skill/hook. Не обходи и не имитируй hook.

## Предмет проверки

Проверь Plan как технический мост от Spec к будущим tasks:

1. Каждая US1–US3, FR-001–FR-019 и SC-001–SC-007 имеет непротиворечивый технический
   путь и проверку в Plan/Test Plan.
2. Plan не расширяет scope до Excel, compare, Apply, browser actions, Git, index
   mutation или Write/Manage. Единственное допустимое live BPMSoft действие —
   условный ручной read-only run после successful offline checks, ручного запуска
   оператором либо прямой текущей просьбы пользователя; credential prompt остаётся
   только terminal-приглашением оператора, `AuthorizationReference` не требуется.
3. Решения Research, data model и CLI contract согласованы с Plan; не допущены
   Name/Code joins, loss unknown shapes, raw secret/value persistence или обход
   endpoint allowlist.
4. Риски и stop conditions fail-closed: `FULL_CATALOG_NOT_QUALIFIED` до завершения
   условного ручного run и human review, запрет автоматического live start,
   `INDEX_SYNC_UNRESOLVED`, отсутствие Write/Manage, а также terminal
   `TARGET_STATE_CHANGED_DURING_QUALIFICATION` без Pass C/retry.
5. Draft `implementation-slices.md` не добавляет scope и покрывает Plan/Test Plan,
   включая qualification summary, audit metadata и early handoff package. Его
   `DRAFT — HUMAN REVIEW REQUIRED` не является approval.
6. Нет подмены planned checks фактическими evidence; legacy prototypes остаются
   reference/characterization material, а не production implementation.

## Отчёт и условия остановки

Не исправляй противоречия молча. При blocker или существенном расхождении остановись
до любых следующих workflow-команд и укажи: severity, точный файл/раздел,
затронутые требования, причину, риск и минимально достаточную рекомендацию.

В финальном отчёте укажи:

- итог `PASS`, `PASS_WITH_ACTIONS` или `BLOCKED`;
- таблицу требований/планового решения/проверки/риска;
- найденные gaps или подтверждённые ограничения;
- отдельно сохранённые human gates и честные отсутствия;
- какие артефакты Critique создала или обновила, если это разрешено skill;
- что `tasks.md` не была создана и что следующий этап требует явного human approval
  `implementation-slices.md`, неизменного target и отсутствия scope expansion.

Не выполняй live BPMSoft, credential prompts, write/browser/Git actions, slice-pack
generation или отдельные Codex tasks.
```
