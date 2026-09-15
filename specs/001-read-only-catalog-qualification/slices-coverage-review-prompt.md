# Prompt для отдельного review-агента: полнота slices Feature 001
```text
Выполни только независимый read-only review полноты срезов реализации для active feature
`specs/001-read-only-catalog-qualification`. Не изменяй файлы и не создавай новых
артефактов. Все сообщения и итоговый отчёт пиши по-русски.

## Проверка target

Сначала прочитай `.specify/feature.json`. Если `feature_directory` не равен ровно
`specs/001-read-only-catalog-qualification`, остановись и сообщи фактический target.
Не делай никаких выводов по иной feature.

## Обязательные источники

Прочитай полностью:

- `AGENTS.md`;
- `.specify/memory/constitution.md`;
- `.specify/project.yml` и `.specify/extensions.yml`;
- неизменяемый общий spec
  `preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md`;
- active `spec.md`, включая `## Clarifications`;
- `checklists/requirements.md`, `plan.md`, `research.md`, `data-model.md`,
  каждый файл в `contracts/`, `quickstart.md`, `test-plan.md`;
- `implementation-slices.md`, `HANDOFF.md` и
  `tasks-generation-prompt.md`;
- фактические код и tests, если существуют.

Общий spec и любые source drafts в `preparation/docs/product-specs/` неизменяемы.
Не читай этапные source drafts, кроме явно указанного общего `spec.md`; ничего в
`preparation/docs/product-specs/` не меняй.

## Предмет review

Проверь, что `implementation-slices.md` полностью и без scope expansion покрывает
Feature 001, а не только отдельные технические слои.

Для каждого slice проверь наличие и согласованность:

1. порядка, наблюдаемого и отдельно проверяемого результата;
2. зависимостей и входов от предыдущих slices;
3. связей с User Stories, FR, NFR и SC;
4. In Scope и Out of Scope;
5. будущих задач, не создающих `tasks.md`;
6. проверки и ожидаемого безопасного evidence;
7. stop conditions;
8. выхода для следующего slice;
9. preserved human gates.

Проверь трассировку `requirement → slice → future task → check`:

- каждая US1–US3, FR-001–FR-019 и SC-001–SC-007 должна иметь хотя бы один slice и
  проверку;
- связь должна быть содержательной: проверка действительно подтверждает требование, а
  не только упоминает его;
- нет будущей задачи, проверки или результата вне среза;
- порядок S01 → S02 → S03 покрывает зависимости plan и test-plan;
- будущие задачи оставляют порядок: setup/test/fixture/security → implementation →
  slice verification → final solution validation.

Отдельно проверь сохранение обязательных ограничений во всех slices и связанных
handoff/prompt:

- `FULL_CATALOG_NOT_QUALIFIED`;
- отсутствие отдельной authorization для live read-only run и отсутствие credential prompts;
- `INDEX_SYNC_UNRESOLVED`;
- отсутствие Write/Manage permissions;
- запрет Excel, compare, Apply, browser actions, Git и index mutation;
- `TARGET_STATE_CHANGED_DURING_QUALIFICATION` terminal; запрещены Pass C,
  автоматический новый двойной проход и retry loop;
- offline sanitized fixtures/fake transport являются единственным допустимым test context;
- handoff не выдаёт себя за acceptance/evidence, а tasks-generation prompt не обходит gates.

Не запускай `/SpecKit Tasks`, `/SpecKit Analyze`, `/SpecKit Implement`, hooks,
live BPMSoft, credential prompts, browser/write/Git actions или slice-pack generation.
Не исправляй найденные проблемы.

## Формат отчёта

Верни только review-отчёт:

1. Итог: `PASS` либо `GAPS_FOUND`.
2. Таблица покрытия: US, FR, SC → slice → future task → check.
3. Таблица замечаний. Для каждого замечания укажи severity (`BLOCKER`, `HIGH`,
   `MEDIUM`, `LOW`), точный путь и раздел, затронутые requirement IDs, почему
   покрытие неполно/противоречиво, а также минимально достаточную рекомендацию.
4. Отдельные разделы: «Сохранённые gates», «Проверка scope», «Честные отсутствия».
5. Явно подтверди, что review не является human approval, не создаёт `tasks.md` и не
   разрешает implementation или live run.

Если явных пробелов нет, всё равно зафиксируй ограничения и честные отсутствия. Не
превращай отсутствие реального кода/tests в доказательство выполнения feature.
```
