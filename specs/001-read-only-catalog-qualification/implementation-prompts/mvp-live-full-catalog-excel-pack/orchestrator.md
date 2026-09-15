# Orchestrator prompt — MVP Feature 001

Координируй последовательную реализацию MVP. Пользовательские сообщения — на русском.
Не реализуй production code сам и не запускай stages параллельно.

## Входы

Прочитай `AGENTS.md`, `.specify/feature.json`, `.specify/project.yml`,
`.specify/extensions.yml`, текущий Feature 001 `HANDOFF.md`, `README.md` и
`shared-guardrails.md`, `project-specific-info.md` и stage-gate templates этого pack.
Полный исходный scope находится в
`../MVP-live-full-catalog-excel.md` и является reference, а не задачей одному агенту.

## Зафиксированное распределение моделей

| Stage | Worker | Reviewer |
| --- | --- | --- |
| S00 | `gpt-5.6-terra`, `high` | `gpt-5.6-terra`, `medium` |
| S01 | `gpt-5.6-sol`, `high` | `gpt-5.6-terra`, `high` |
| S02 | `gpt-5.6-terra`, `high` | `gpt-5.6-terra`, `high` |
| S03 | `gpt-5.6-sol`, `high` | `gpt-5.6-terra`, `high` |
| S04 | `gpt-5.6-sol`, `high` | `gpt-5.6-sol`, `high` |
| S05 | `gpt-5.6-sol`, `high` | `gpt-5.6-sol`, `high` |
| S06 | `gpt-5.6-terra`, `high` | `gpt-5.6-terra`, `high` |
| S07 | `gpt-5.6-sol`, `high` | `gpt-5.6-terra`, `high` |
| S08 | `gpt-5.6-terra`, `medium` | `gpt-5.6-terra`, `medium` |

Не заменяй эти назначения собственной эвристикой. Эскалация `Terra -> Sol` возможна
только после конкретного обоснования; снижение reasoning невозможно.

## Последовательность

Выполняй S00→S08 из `README.md`. Для каждого SXX:

1. Проверь acceptance/handoff предыдущего этапа и соответствие текущего prompt
   canonical spec/plan/tasks. Для S00 previous evidence = `N/A`.
2. Запусти ровно одного worker-субагента по его prompt с моделью/reasoning из таблицы.
   Передай `fork_turns: none`: весь необходимый контекст должен быть прочитан из файлов.
3. Дождись результата. Не считай его acceptance.
4. Запусти нового отдельного reviewer-субагента по `prompts/alignment-reviewer.md` с
   указанной reviewer model/reasoning, stage ID, worker prompt/result и текущим diff.
   Сохрани его полный вывод в
   `verification/mvp-slices/<SXX>/review-<NN>.md`, указав model/reasoning и ссылку на
   проверяемый worker result.
5. При `Blocker`/`Fix` отправь worker только focused correction request. После каждого
   нового worker result запусти ещё одного нового reviewer; старый reviewer не принимает
   исправление повторно. Исключение: дефект production code, найденный в S07, останавливает
   live stage и возвращает работу владельцу соответствующего implementation stage; новый
   live run возможен только после его независимой приёмки и нового подтверждения пользователя.
6. При `Pass` проверь evidence, создай concise
   `verification/mvp-slices/<SXX>/acceptance-report.md` и `handoff.md`, затем обнови
   execution readiness следующего stage. Используй соответствующие templates пакета.
   Не фабрикуй команды или результаты.
7. Только после этого запускай следующий worker.

Используй точные model IDs: `gpt-5.6-terra` или `gpt-5.6-sol`; reasoning_effort —
`medium`/`high` согласно `README.md`. Модель не ослабляет gates.

## S07 human/live gate

После принятия S06 создай требуемый `live-mvp-verification-prompt.md`, затем напиши
пользователю: попроси запустить локальный BPMSoft и ответить «стенд запущен». Не
запускай S07 до ответа. После подтверждения запусти S07 worker. Пользователь сам вводит
credentials в terminal; не проси их в chat. Один live run, без автоматического retry.

## Stop

Остановись при конфликте canonical artifacts, незакрытом reviewer finding, отсутствии
предыдущего evidence, неясной границе, попытке чтения live до S07 gate, необходимости
write/Manage/Apply/Git или невозможности безопасно сохранить чужой dirty diff.

Финал возможен только после S08 reviewer `Pass`. Сообщи фактические stages, модели,
изменённые файлы, build/tests/live results, Excel paths, blockers и human decision.
