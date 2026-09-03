Совместно с владельцем подготовь и после обсуждения создай редактируемые draft-версии входных SDD-артефактов для будущего GitHub Spec Kit workflow. Не начинай реализацию, новый BPMSoft probe или официальный Spec Kit workflow.

Рабочая папка: `C:\CodingAgents\codex\projects\OM_Automatization`.

Сначала полностью прочитай:

1. `docs\PROJECT_HANDOFF.md`, особенно разделы 0 и 1;
2. `docs\WORKBOOK_CONTRACT_VISION.md`;
3. `PrototypeReadOnlyPull\Program.cs` и `PrototypeReadOnlyPull\BpmSoftReadOnlyPull.csproj` только как evidence уже выполненного read-only исследования;
4. основной текст локального курса:
   - `C:\CodingAgents\codex\projects\VibeCeH\docs\pilot-course\lessons\lesson-01-codex-operational-start\23-lesson-1-confluence-WC_act.md`;
   - `C:\CodingAgents\codex\projects\VibeCeH\docs\pilot-course\lessons\lesson-02-lightweight-spec-and-spec-review\02-lesson-2-confluence-text.md` и `02b-lesson-2-spec-kit-and-spec-review-confluence-text.md`;
   - `C:\CodingAgents\codex\projects\VibeCeH\docs\pilot-course\lessons\lesson-03-managed-implementation-by-spec\03-lesson-3A.md` и `05-lesson-3B.md`;
   - `C:\CodingAgents\codex\projects\VibeCeH\docs\pilot-course\lessons\lesson-04-orchestration-control-gates-and-acceptance\05-lesson-4A.md` и `06-lesson-4B_V2.md`.

Курс задаёт последовательность:

```text
контекст и ограничения
-> constitution
-> spec
-> clarify + review / DoD spec
-> implementation plan
-> slices -> tasks -> bounded agent tasks
-> implementation/review/verification
-> human acceptance + handoff
```

Работай в трёх режимах.

1. **Обсуждение до записи.** Сначала проведи совместное discovery: цель, пользователи/сценарии, scope/out of scope, устойчивые project rules, requirements, acceptance criteria, подтверждённые facts/evidence, assumptions, риски и блокирующие вопросы. Задавай владельцу ровно один вопрос за раз. После каждого ответа коротко фиксируй: факты, решение, assumptions и один следующий открытый вопрос. Не выдавай предположение за решение и не проектируй API/классы преждевременно.
2. **Создание drafts.** Когда владелец подтвердит, что существенные нюансы для первого черновика обсуждены, создай с помощью `apply_patch` папку `docs\SDD_DRAFTS\` и ровно три файла с верхней строкой `DRAFT — NOT YET SPEC KIT CANONICAL`:
   - `constitution.draft.md` — устойчивые правила проекта: безопасность, secret handling, read/plan/apply separation, identity, evidence, human gates и неизменяемые ограничения;
   - `first-feature-spec.draft.md` — минимальная первая feature: цель, пользователи и observable outcomes, scope/out of scope, functional/non-functional requirements, acceptance criteria и зависимости;
   - `clarify-review.draft.md` — подтверждённые facts/evidence с путями, decisions, assumptions, open questions, blocking questions, spec DoD и критерий перехода к plan.

   В каждом draft приводи ссылки на источники, помечай статус каждого утверждения (`confirmed`, `owner decision`, `assumption`, `open`) и не скрывай технические пробелы. Не создавай plan, tasks или slices.
3. **Совместная правка.** Покажи владельцу краткую карту созданных drafts и ровно один вопрос для первого review. По каждому следующему ответу владельца обновляй только затронутые draft-файлы, показывай суть изменения и переходи к одному следующему вопросу. Не объявляй draft готовым к Spec Kit без явного подтверждения владельца и прохождения DoD spec.

Граница статуса drafts:

- `docs\SDD_DRAFTS\` — временная repo-native рабочая область для совместной правки. Она не равна `.specify/`, `specs/` и не является canonical GitHub Spec Kit layout.
- Лишь после отдельного решения владельца drafts могут быть перенесены или преобразованы в официальный workflow `constitution -> spec -> clarify -> plan`.
- `slice-generator` сейчас не применяй: он нужен после устойчивых spec, plan, slices и tasks, чтобы упаковывать утверждённый slice, а не создавать SDD-контракт.

Требуемый фокус для ОМ:

- отделить project-wide rules от поведения первой feature;
- не смешивать подтверждённые результаты read-only BPMSoft probe и logical workbook contract с будущими желаниями/гипотезами;
- сохранить все stop conditions: запрет BPMSoft write без отдельного согласия, запрет Excel до read gates, credentials только в памяти процесса, отсутствие matching новых lookup rows по `Code`/`Name`;
- сделать acceptance criteria проверяемыми, а не общими обещаниями «надёжно», «удобно» или «готово»;
- если для drafts не хватает конкретного знания о BPMSoft, сформулировать один precise research question и почему он блокирует следующий артефакт. Не устраняй пробел реализацией, догадкой или write-вызовом.

Непересекаемые границы:

- Не инициализируй GitHub Spec Kit, не выполняй `/speckit.*` и не создавай/не изменяй `.specify/`, `specs/`, canonical `constitution.md`, `spec.md`, `plan.md`, `tasks.md`, `implementation-slices.md`, `docs/orchestration/`, prompt-packs, acceptance reports или новые handoff-файлы.
- Разрешено создать и править только три указанных draft-файла в `docs\SDD_DRAFTS\`, после обсуждения с владельцем. Не создавай дополнительные SDD-документы без отдельного решения.
- Не меняй `SyncOM/`, не пиши production-код и не меняй `PrototypeReadOnlyPull`, пока владелец отдельно не попросит новый strictly read-only research step.
- Не выполняй BPMSoft write API, не проверяй Manage/Write, не создавай данных, не создавай `.xlsx` и не используй Google-книги как input.
- Пароль, cookies, CSRF и login response не запрашивай, не принимай и не сохраняй.

После создания drafts обнови существующий `docs\PROJECT_HANDOFF.md`: пути к drafts, зафиксированные owner decisions, status review, незакрытые вопросы, exact stop condition и следующий один шаг. Затем полностью замени этот prompt так, чтобы он начинался с реально следующего незакрытого шага review. Если владелец ещё не подтвердил создание drafts, ничего в `docs\SDD_DRAFTS\` не создавай и продолжай только обсуждение.
