# Prompt для повторного этапа Critique после изменения scope Feature 001

```text
Выполни повторный Critique только для active feature
specs/001-read-only-catalog-qualification. Это повторная проверка после изменения
scope 2026-09-13: к автономной verification добавлен условный ручной read-only run
на реальном BPMSoft после успешных offline checks, ручного запуска оператора либо прямой текущей просьбы пользователя.
Все сообщения и финальный отчёт пиши по-русски.

## До начала

1. Прочитай .specify/feature.json. Если feature_directory не равен ровно
   specs/001-read-only-catalog-qualification, остановись, назови фактический target
   и не изменяй ничего.
2. Прочитай полностью AGENTS.md, .specify/memory/constitution.md,
   .specify/project.yml, .specify/extensions.yml, immutable common vision
   preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md, active spec.md
   с Clarifications, checklists/requirements.md, plan.md, research.md, data-model.md,
   все contracts/, quickstart.md, test-plan.md, implementation-slices.md,
   HANDOFF.md, tasks-generation-prompt.md, critique-prompt.md,
   предыдущий Critique critiques/critique-20260908-224121.md, а также фактические
   код/tests, если они существуют.
3. Не читай этапные source drafts из preparation/docs/product-specs/, кроме
   названного immutable common vision. Ничего в этой source-директории не меняй.
4. Прочитай полный SKILL.md $speckit-critique до действий и выполни обязательный
   speckit.critique.run как post-Plan hook согласно skill и .specify/extensions.yml.

## Критерии повторного Critique

Проверь, что Feature 001 остаётся безопасной read-only feature и что новый ручной
этап согласован во всех артефактах:

- только после успешных offline tests агент запрашивает у владельца явное разрешение
  на конкретные target и read scope;
- offline и automatic paths не имеют live run, credential prompt или попытки
  подключения;
- оператор вручную запускает `catalog qualify`, либо агент действует по прямой текущей
  просьбе пользователя; `AuthorizationReference` не требуется;
- credentials вводятся только в terminal;
- два прохода остаются read-only; TARGET_STATE_CHANGED_DURING_QUALIFICATION terminal,
  Pass C/automatic repeat/retry loop запрещены;
- реальный run не выдаёт разрешение на Excel, compare, Apply, browser write, Git,
  index mutation или Write/Manage;
- FULL_CATALOG_NOT_QUALIFIED остаётся до safe evidence и human review, а
  INDEX_SYNC_UNRESOLVED сохраняется;
- tasks-generation-prompt.md не создаёт автоматического live start и не выполняет
  live run во время Tasks;
- implementation-slices.md и тестовый план содержат будущую условную manual task,
  safe evidence, stop conditions и coverage FR-010/SC-002.

Также проверь полноту и согласованность US1–US3, FR-001–FR-019 и SC-001–SC-007,
отсутствие scope expansion, distinction между planned checks и фактическими evidence,
а также то, что legacy prototypes не считаются production implementation.

## Ограничения

Не запускай /SpecKit Tasks, /SpecKit Analyze, /SpecKit Implement, live BPMSoft,
credential prompt, write/browser/Git action или slice-pack generation. Не исправляй
артефакты молча.

## Отчёт

Верни:

1. итог: PASS, PASS_WITH_ACTIONS или BLOCKED;
2. таблицу: requirement → артефакт/раздел → verdict → evidence/причина;
3. список findings с severity, точным путём и разделом, риском и минимально
   достаточным вариантом исправления;
4. отдельное подтверждение сохранённых gates и честных отсутствий;
5. явное указание, что Critique не создаёт tasks.md и не разрешает implementation
   либо live run.

Если findings нет, прямо укажи, что этот Critique заменяет устаревший Critique
2026-09-08 как актуальную проверку scope перед будущим Tasks.
```
