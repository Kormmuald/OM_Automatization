# Prompt: Run All Slice Prompt-Pack Generation

Дата фиксации: 2026-07-06.

## Purpose

Этот prompt используется, чтобы проверить готовность проекта к генерации slice prompt-packs и затем последовательно запускать существующий generator для каждого implementation slice. Runner может подготовить prompt-pack для всех slices заранее, даже если completion evidence предыдущих slices еще не существует, но такие future prompt-packs должны быть помечены как требующие execution-time refresh перед запуском реализации.

Этот prompt не заменяет и не дублирует правила из SDD / Spec Kit / slice-generator documents. Он должен использовать их как источники истины.

## Prompt To Use

```text
Ты проверяешь проект на готовность к генерации orchestration prompt-packs и, если явных блокеров нет, последовательно создаешь prompt-pack для каждого implementation slice.

Цель этого runner - создать planning-time prompt-packs для всех slices. Не останавливай генерацию будущих prompt-packs только потому, что previous acceptance report или previous handoff еще не существуют. Вместо этого помечай такие prompt-packs как generated-before-previous-evidence, требуй от их orchestrator prompt execution-time refresh gate перед реализацией slice и post-slice future-pack refresh gate после появления acceptance/handoff предыдущего slice.

## Language Policy

- Рабочие выводы, stop condition сообщения и финальный отчет для человека пиши на русском.
- Имена файлов, команды, task ids, gate names и quoted prompt fragments можно оставлять на английском.
- Не переписывай правила генерации внутри себя, если они уже есть в SDD / Spec Kit / slice-generator documents. Ссылайся на эти документы и следуй им.

## Mandatory Sources

Перед любыми действиями найди и прочитай `project-specific-info.md`.

Если человек не указал путь к `project-specific-info.md`, сначала проверь стандартное расположение текущего workflow: `<orchestration-root>/project-specific-info.md`. Если `<orchestration-root>` еще неизвестен, попробуй найти ближайший `project-specific-info.md` в явных orchestration / SDD / Spec Kit директориях проекта. Если найдено несколько кандидатов или ни одного кандидата, это readiness blocker: не начинай генерацию, а предложи человеку выбрать или создать authoritative `project-specific-info.md`.

Затем из `project-specific-info.md` прочитай:

- generator prompt path;
- orchestration task template path;
- acceptance report template path;
- orchestration root;
- slice prompt-pack layout convention;
- previous-slice input convention or a pointer to the generator rule that defines it;
- protected or special-purpose folders, if any;
- canonical Spec Kit / SDD sources.

После этого прочитай generator prompt и templates по путям из `project-specific-info.md`.

Затем из `project-specific-info.md` прочитай canonical Spec Kit / SDD sources:

- spec;
- clarify checklists, если указаны;
- implementation plan;
- data model, если указан;
- quickstart / verification scenarios, если указан;
- slices;
- tasks;
- constitution;
- SDD workflow, если указан.

Optional source-history documents можно читать только как справочный контекст и только после canonical sources. Они не заменяют canonical sources.

Protected or special-purpose folders из `project-specific-info.md` нельзя использовать как источники для генерации prompt-packs и нельзя изменять без явного human decision.

## Readiness Gate

До генерации первого prompt-pack проверь:

1. `project-specific-info.md` существует, является единственным authoritative project-specific info для этого run и содержит project identifier.
2. `project-specific-info.md` содержит необходимые данные для старта:
   - orchestration root;
   - slice prompt-pack layout convention, включая `slice-<n>`;
   - generator prompt path;
   - orchestration task template path;
   - acceptance report template path;
   - canonical Spec Kit / SDD source paths;
   - slices source path;
   - tasks source path;
   - first-slice mode inputs;
   - previous-slice input convention or pointer to the generator rule that defines it;
   - protected or special-purpose folders policy, even if the list is empty.
3. `project-specific-info.md` прошел Project-Specific Info Gate из generator prompt.
4. Все canonical paths, template paths, generator paths, orchestration root и protected/special-purpose folder paths из `project-specific-info.md` существуют, если они заявлены как existing paths.
5. Prompt-pack layout и previous-evidence layout, если они есть, выводятся из `project-specific-info.md` и generator prompt, а не из assumptions runner.
6. Slices source содержит полный список implementation slices и их порядок.
7. Tasks source содержит tasks, которые можно сопоставить с каждым slice.
8. First-slice mode описан: Slice 1 не требует previous handoff / acceptance.
9. Generator prompt поддерживает bulk/forecast prompt-pack generation для future slices без synthetic completion evidence, либо может быть применен в этом режиме без нарушения canonical sources.
10. Existing slice prompt-pack artifacts, если они есть, не конфликтуют с expected layout из `project-specific-info.md` и generator prompt.
11. Legacy evidence paths в tasks, если они есть, распознаны как references, а не как новая структура для записи prompt-packs.

Если `project-specific-info.md` отсутствует или в нем не хватает данных для старта:

- не начинай генерацию prompt-pack;
- кратко проанализируй структуру проекта и доступные SDD / Spec Kit / orchestration artifacts;
- предложи конкретные предположения, которыми можно дополнить `project-specific-info.md`;
- перечисли missing fields and candidate paths;
- остановись и жди human decision.

Если все необходимые данные есть и не конфликтуют с canonical sources, не останавливайся только для подтверждения project-specific info.

Если найден явный blocker, остановись до генерации prompt-pack и спроси человека, что делать. Сообщи:

- какой gate провален;
- какие файлы или утверждения конфликтуют;
- почему это блокирует генерацию;
- какие варианты решения видишь.

Не продолжай без явного human decision.

## Model Allocation Preflight Gate

Before creating or updating any slice prompt-pack, determine the subagent model and reasoning-effort allocation for every planned slice and every role that will appear in that slice:

- Implementation;
- Unit-test;
- Review;
- Verification;
- Docs/handoff.

Use the canonical slice boundary, task complexity, UI/state/routing/deploy risk, expected verification burden, and cost-control considerations to propose a concrete allocation table. For each role in each slice, specify:

- model, for example `gpt-5.4`;
- reasoning effort, for example `medium` or `high`;
- any allowed escalation condition, for example "upgrade to `high` only if route/view-state test planning becomes unusually complex";
- a one-sentence rationale tied to the slice risk.

Show this allocation table to the human before prompt-pack generation starts and explicitly ask for confirmation or corrections. Do not create or update any slice prompt-pack until the human confirms the allocation or provides corrected values.

If the human changes the allocation, use the human-approved allocation as authoritative for this run. If the human does not confirm, stop with a missing model-allocation decision instead of generating prompt-packs.

The approved allocation must be passed into each call/use of `next-slice-prompt-pack-generator.md` and must be written into the generated prompt-pack:

- `prompts.md` must include a concise `Model Guidance` section for that slice;
- `prompts/orchestrator.md` must include a `Model Guidance` section that tells the orchestrator which model and reasoning effort to use for each role;
- each subagent prompt must include the model guidance for that role.

No separate root-level ledger is required by default. The decision is recorded in the generated prompt-packs and in the runner final report. Create a separate model-allocation ledger only if the human explicitly asks for one; otherwise avoid adding new root files.

## Slice Iteration

Определи список slices и их порядок из canonical slices source. Затем обрабатывай slices строго по порядку.

Для каждого target slice:

1. Resolve target prompt-pack directory from `project-specific-info.md` and generator prompt. Generic convention remains `<orchestration-root>/slice-<n>/`.
2. Проверь, существует ли уже готовый prompt-pack в resolved target prompt-pack directory.
3. Если prompt-pack уже существует, проверь его по Post-Generation Validation ниже. Не перезаписывай, не пересоздавай и не "улучшай" его без human approval.
4. Если prompt-pack отсутствует, запусти правила generator prompt из `project-specific-info.md` для target slice in bulk/forecast generation mode.
5. Для Slice 1 используй first-slice mode из `project-specific-info.md`.
6. Для Slice 2+ используй previous-slice inputs только согласно generator prompt and `project-specific-info.md`.
7. Если previous completion evidence уже существует, используй его как input для generation. Если оно отсутствует, не создавай synthetic, placeholder или speculative acceptance/handoff documents; вместо этого явно зафиксируй в generated prompt-pack: `Previous evidence status: pending - generated before previous slice completion`.
8. Создавай только prompt-pack artifacts, которые разрешены generator, внутри resolved target prompt-pack directory.
8a. Pass the human-approved model/reasoning allocation for this target slice into the generator and require it to be embedded in `prompts.md`, `prompts/orchestrator.md`, and the relevant subagent prompts.
9. Do not create acceptance report or handoff as completed-slice evidence during prompt-pack generation. Those documents belong to implementation/acceptance completion, unless a human explicitly authorizes a different workflow.

## Post-Generation Validation

After each generated or existing prompt-pack, validate it before moving to the next slice:

1. Required files exist under resolved target prompt-pack directory from `project-specific-info.md` and generator prompt.
2. `prompts.md` references every subagent prompt file.
3. `orchestrator.md` references every subagent prompt file and preserves the required sequencing.
4. The pack references `project-specific-info.md` as an input, but does not treat it as a replacement for canonical Spec Kit / SDD sources.
5. Slice title, target tasks, boundaries, In / Out scope and verification expectations match canonical slices/tasks/spec/plan.
6. Slice 1 marks previous acceptance report and handoff as `N/A - first slice`.
7. Slice 2+ follows previous-slice input rules from generator prompt and `project-specific-info.md`: either actual previous evidence is referenced, or missing previous evidence is explicitly marked as pending with an execution-time refresh gate and post-slice future-pack refresh gate.
8. Stop conditions include:
   - scope expansion beyond the current slice;
   - violating SDD or project-specific constraints;
   - changing spec/plan/slice/task boundaries without approval;
   - conflict between project-specific notes, previous handoff/acceptance and canonical sources;
   - unsupported verification claims;
   - missing unit-test evidence for new slice functionality without documented reason.
   - executing a generated-before-previous-evidence prompt-pack without first checking actual previous acceptance/handoff once they exist.
   - leaving the next prompt-pack in pending status after actual previous acceptance/handoff exists and has been checked with no required corrections.
9. The pack does not ask any subagent to accept its own work.
10. The pack does not ask implementation, unit-test, review, verification or docs/handoff roles to exceed their responsibility split from the generator.
11. Legacy acceptance evidence paths from tasks are mapped or noted if relevant, but new prompt-pack structure remains the layout from `project-specific-info.md` and generator prompt.
12. No files were created or changed under protected or special-purpose folders from `project-specific-info.md`.
13. `prompts.md`, `prompts/orchestrator.md`, and each subagent prompt contain model/reasoning guidance that matches the human-approved allocation for that slice.
14. If an existing prompt-pack lacks model/reasoning guidance or conflicts with the approved allocation, report it. Do not rewrite an existing prompt-pack without human approval unless this run explicitly includes updating that pack.

If validation finds a critical issue in a prompt-pack generated during this run and you can fix it safely within that newly generated prompt-pack, fix it and rerun validation for that slice.

If validation finds an issue in a prompt-pack that existed before this run, do not rewrite, recreate or improve it without human approval. Report the issue, classify whether it blocks later slices, and stop only if it affects correctness, scope control or the next slice gate.

If validation finds a critical issue that requires changing canonical sources, project-specific rules, slice boundaries, acceptance conventions or human decisions, stop and ask the human what to do.

Non-critical issues may be recorded and carried into the final report only if they do not affect correctness, scope control or the next slice gate.

## Next-Slice Gate

Before moving from Slice `<n>` to Slice `<n+1>`, check what the next slice requires.

Use only the next-slice and previous-slice rules from generator prompt and `project-specific-info.md`.

If the next slice requires completion evidence from Slice `<n>` and that evidence does not exist after generating only a prompt-pack for Slice `<n>`, continue generating the next prompt-pack only in bulk/forecast mode. The next prompt-pack must state that it was generated before previous completion evidence existed, must require execution-time refresh before implementation, and must support a post-slice future-pack refresh gate that can mark it `execution-ready` after actual previous evidence appears and is checked.

Do not invent completion evidence to continue automation.

## Stop Conditions

Stop immediately and ask the human before continuing if:

- Readiness Gate fails.
- `project-specific-info.md` conflicts with canonical Spec Kit / SDD sources.
- A target slice cannot be mapped to canonical tasks.
- A target slice has no clear boundary in canonical sources.
- Existing prompt-pack artifacts conflict with canonical sources or the current generator.
- Generator-required previous-slice evidence is missing and generator prompt cannot support bulk/forecast generation without fabricating evidence.
- Any generated pack expands scope beyond the target slice.
- Any generated pack conflicts with SDD, Spec Kit, previous handoff/acceptance or project-specific constraints.
- Continuing would require changing canonical sources, slice boundaries, acceptance rules or implementation evidence.
- Verification or acceptance would be claimed without evidence.
- Any change would touch protected or special-purpose folders from `project-specific-info.md`.
- The model allocation preflight has not been confirmed by the human.
- A generated pack cannot record the approved model/reasoning allocation without changing generator rules or protected files.

## Final Report

When the run ends, report:

- readiness result;
- slice list discovered;
- prompt-packs created;
- prompt-packs already existing and validated;
- slice where execution stopped, if any;
- exact stop condition, if any;
- files created or changed;
- validation checks performed after each slice;
- prompt-packs generated with pending previous evidence;
- execution-time refresh gates that future orchestrators must run;
- post-slice future-pack refresh/status gates included in generated orchestrator prompts;
- approved model/reasoning allocation by slice and role;
- any human corrections to the proposed model allocation;
- assumptions used;
- questions for the human;
- non-critical issues or follow-ups.

If all slices were generated and validated, say that explicitly. If some prompt-packs were generated before previous completion evidence existed, say that they are planning-time prompt-packs and must be refreshed against actual acceptance/handoff before implementation.
```
