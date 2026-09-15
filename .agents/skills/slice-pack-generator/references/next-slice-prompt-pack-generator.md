# Prompt: Generate Orchestration Prompt Pack For Next Slice

Дата фиксации: 2026-07-06.

## Purpose

Этот prompt используется, чтобы создать orchestration prompts для первого или следующего implementation slice:

- prompt index;
- orchestrator prompt;
- implementation subagent prompt;
- unit-test subagent prompt;
- review subagent prompt;
- verification subagent prompt;
- optional docs/handoff subagent prompt.

Prompt должен адаптировать пакет под конкретный slice, используя canonical Spec Kit / SDD sources текущего проекта. Для Slice 1 он использует начальный baseline из spec/plan/slices/tasks без previous handoff. Для Slice 2+ он использует acceptance report и handoff предыдущего slice, если они уже существуют; если пакет создается заранее, он фиксирует pending evidence и требует refresh перед исполнением.

Generator поддерживает два режима:

- execution-ready mode: для next executable slice, когда required previous acceptance report и handoff уже существуют;
- bulk/forecast generation mode: для предварительной генерации prompt-packs всех future slices, когда previous completion evidence еще может отсутствовать. В этом режиме нельзя фабриковать evidence; prompt-pack должен быть явно помечен как generated before previous evidence и должен требовать execution-time refresh gate перед реализацией slice.

## Prompt To Use

```text
Ты готовишь orchestration prompt pack для implementation slice в проекте, описанном в `project-specific-info.md`.

Language policy:

- `prompts.md` and `orchestrator.md` should be in Russian because they are read by the human orchestrator.
- All subagent prompt files should be in English:
  - `implementation-subagent.md`
  - `unit-test-subagent.md`
  - `review-subagent.md`
  - `verification-subagent.md`
  - `docs-handoff-subagent.md`
- Subagent prompts must instruct subagents to return their outputs in English.
- The orchestrator must provide chat updates, user-facing replies, final decision recommendations, acceptance report summaries and handoff summaries in Russian.
- Technical file names, commands, gate names, task ids and quoted prompt fragments may remain in English everywhere when that preserves precision.

## Project-Specific Info Gate

До генерации prompt-pack обязательно прочитай и проверь `project-specific-info.md`.

Нормальный путь должен быть дешевым:

1. прочитай `project-specific-info.md`;
2. проверь, что в нем достаточно данных для старта:
   - project name или другой project identifier;
   - пути к canonical Spec Kit / SDD sources;
   - путь к orchestration task template;
   - путь к acceptance report template;
   - first-slice rules;
   - previous-slice rules;
3. проверь, что указанные файлы и директории существуют;
4. если есть project-specific notes, используй их только как дополнительный вход и сверяй с canonical Spec Kit / SDD sources, previous acceptance report и previous handoff.

Если `project-specific-info.md` отсутствует, неполон, содержит несуществующие пути или конфликтует с canonical sources, это stop condition: не создавай prompt-pack как готовый к исполнению.

Только при провале этого gate включай глубокое исследование проекта. В этом случае:

- изучи доступную структуру проекта и существующие Spec Kit / SDD / orchestration директории;
- предложи человеку, как заполнить `project-specific-info.md`;
- укажи, какие недостающие директории или документы стоит создать, если их нет;
- не продолжай генерацию prompt-pack без явного human approval.

## Inputs

Перед созданием prompt-пакета прочитай минимальный набор источников из `project-specific-info.md`:

- Project-specific info: `project-specific-info.md`
- Spec: `<path from project-specific-info>`
- Clarify checklists: `<path from project-specific-info, if used>`
- Implementation plan: `<path from project-specific-info>`
- Data model: `<path from project-specific-info, if used>`
- Quickstart / verification scenarios: `<path from project-specific-info, if used>`
- Slices: `<path from project-specific-info>`
- Tasks: `<path from project-specific-info>`
- Constitution: `<path from project-specific-info>`
- SDD workflow: `<path from project-specific-info, if used>`
- Orchestration task template: `<path from project-specific-info>`
- Acceptance report template: `<path from project-specific-info>`

Также прочитай optional source-history артефакты из `project-specific-info.md`, если нужно уточнить происхождение требований, но не используй их вместо canonical Spec Kit / SDD sources.

Для Slice 2+ обязательно прочитай handoff и acceptance report предыдущего slice:

- Previous acceptance report: `<path from project-specific-info or resolved previous-slice convention>`
- Previous handoff: `<path from project-specific-info or resolved previous-slice convention>`

Если создается Slice 1, отсутствие previous `acceptance-report` или `handoff` не является stop condition. Для Slice 1 зафиксируй:

- Previous acceptance report: `N/A - first slice`
- Previous handoff: `N/A - first slice`
- Initial baseline: canonical spec/plan/slices/tasks, quickstart, data model, SDD workflow and templates.

Если создается Slice 2 или любой последующий slice и предыдущие `acceptance-report` или `handoff` отсутствуют:

- в execution-ready mode это stop condition: не создавай prompt-пакет как готовый к исполнению;
- в bulk/forecast generation mode продолжай генерацию, но зафиксируй:
  - Previous acceptance report: `Pending - generated before previous slice completion`;
  - Previous handoff: `Pending - generated before previous slice completion`;
  - Execution readiness: `requires refresh against actual previous acceptance report and handoff before implementation`.

В bulk/forecast generation mode boundary текущего slice выводи из canonical spec/plan/slices/tasks и project-specific notes, но не делай claims о фактически завершенном предыдущем slice.

## Parameters To Resolve

Определи из slices, tasks, canonical plan/spec и, для Slice 2+, previous handoff и previous acceptance report:

- номер целевого slice;
- название целевого slice;
- цель slice;
- tasks, относящиеся только к этому slice;
- что уже сделано и верифицировано в предыдущем slice; для Slice 1 укажи `none - initial implementation slice`;
- какие known gaps из предыдущего slice влияют на текущий; для Slice 1 укажи gaps from spec/plan/slices/tasks only;
- какие файлы/модули уже существуют и должны быть переиспользованы;
- что явно запрещено делать в целевом slice;
- какие verification checks ожидаются.

Если handoff говорит "Do next", используй это как главный практический вход для выбора следующего task boundary, но сверяй его с slices и tasks. Если `project-specific-info.md` содержит project-specific notes, используй их как дополнительный вход, но не как замену SDD. Если handoff, project-specific notes и canonical sources конфликтуют, это stop condition: не скрывай конфликт. Для Slice 1 handoff отсутствует штатно; task boundary выводится напрямую из first-slice inputs в `project-specific-info.md` и соответствующих canonical sources.

Если prompt-pack создается в bulk/forecast generation mode без actual previous handoff/acceptance, явно укажи, что:

- previous completed state is unknown at generation time;
- current slice boundary is forecast from canonical sources only;
- orchestrator must run execution-time refresh gate once actual previous acceptance/handoff exist;
- if actual previous evidence changes current or later slice assumptions, orchestrator must stop and ask the human whether to update affected prompt-packs.

## Model Guidance Input Gate

Before creating or updating any prompt-pack files, resolve the model and reasoning-effort allocation for the target slice.

If this generator is being called by `all-slice-prompt-pack-runner.md`, use the human-approved allocation supplied by the runner for this slice.

If no approved allocation is supplied, propose a concrete allocation for every role that will appear in the target slice and ask the human to confirm or correct it before writing files. Include:

- Implementation model and reasoning effort;
- Unit-test model and reasoning effort;
- Review model and reasoning effort;
- Verification model and reasoning effort;
- Docs/handoff model and reasoning effort;
- any allowed escalation condition;
- a one-sentence rationale per role tied to slice complexity and cost control.

Do not create or update the prompt-pack until the human confirms the allocation or provides corrected values. Treat missing confirmation as a stop condition.

The approved allocation must be embedded in the generated prompt-pack:

- `prompts.md` includes a concise `Model Guidance` section;
- `prompts/orchestrator.md` includes a `Model Guidance` section telling the orchestrator what model/reasoning effort to use when spawning each role;
- each subagent prompt includes a role-specific `Model guidance:` line.

These model settings must explicitly state that they do not weaken managed-gate requirements, stop conditions, evidence requirements, publication gates, or human acceptance requirements.

## Orchestration Folder Layout

Все slice-specific orchestration artifacts должны жить внутри папки соответствующего slice.

Правильная структура:

- `docs/orchestration/slice-<n>/prompts.md`
- `docs/orchestration/slice-<n>/prompts/orchestrator.md`
- `docs/orchestration/slice-<n>/prompts/implementation-subagent.md`
- `docs/orchestration/slice-<n>/prompts/unit-test-subagent.md`
- `docs/orchestration/slice-<n>/prompts/review-subagent.md`
- `docs/orchestration/slice-<n>/prompts/verification-subagent.md`
- `docs/orchestration/slice-<n>/prompts/docs-handoff-subagent.md`
- `docs/orchestration/slice-<n>/acceptance-report.md`, когда slice завершен;
- `docs/orchestration/slice-<n>/handoff.md`, когда slice завершен.

Корень `docs/orchestration/` предназначен только для общих шаблонов, generator prompts и других не slice-specific документов. Не создавай новые файлы вида `docs/orchestration/slice-<n>-...md` и не создавай sibling-папки вида `docs/orchestration/slice-<n>-prompts/`.

Если `tasks.md` содержит старые ссылки на acceptance evidence вне этой структуры, считай их legacy evidence reference. В новом prompt pack используй структуру `docs/orchestration/slice-<n>/acceptance-report.md` и явно укажи mapping в `prompts.md`, чтобы reviewer понимал связь с task reference. Не создавай параллельную структуру без отдельного human decision.

## Output Files To Create

Создай файлы по этой схеме:

- `docs/orchestration/slice-<n>/prompts.md`
- `docs/orchestration/slice-<n>/prompts/orchestrator.md`
- `docs/orchestration/slice-<n>/prompts/implementation-subagent.md`
- `docs/orchestration/slice-<n>/prompts/unit-test-subagent.md`
- `docs/orchestration/slice-<n>/prompts/review-subagent.md`
- `docs/orchestration/slice-<n>/prompts/verification-subagent.md`
- `docs/orchestration/slice-<n>/prompts/docs-handoff-subagent.md`

Где `<n>` - номер целевого slice. Название slice укажи в заголовке и тексте `prompts.md`; имя папки должно оставаться стабильным `slice-<n>`.

Не перезаписывай prompt-файлы предыдущих slices.

## Required Content: Prompt Index

В index-файле опиши:

- purpose prompt-пакета;
- список prompt-файлов;
- sources of truth;
- project-specific info used;
- previous acceptance report и previous handoff, or pending previous evidence status; для Slice 1 явно указать `N/A - first slice`;
- execution readiness status:
  - `execution-ready` when required previous evidence is already checked and current prompt-pack assumptions match it;
  - `planning-pending-previous-evidence` when the prompt-pack was generated before previous slice completion;
  - evidence paths and timestamp/date of the last previous-evidence check, if checked;
- boundary текущего slice: In / Out;
- responsibility split;
- model guidance for every included role, copied from the approved allocation;
- какие роли не нужны для этого slice;
- какое human decision ожидается после завершения slice.

Index должен быть кратким и не дублировать полностью prompts subagents.

## Required Content: Orchestrator Prompt

В `orchestrator.md` обязательно включи:

- communication rule: оркестратор пишет chat updates и user-facing replies на русском;
- sources of truth;
- project-specific info as an input, never as a replacement for canonical sources;
- previous acceptance report и previous handoff как обязательные execution-time входы для Slice 2+; для Slice 1 использовать `N/A - first slice` и initial baseline;
- evidence status:
  - actual previous acceptance/handoff paths if available during generation;
  - or `Pending - generated before previous slice completion` if this prompt-pack was created in bulk/forecast generation mode;
- явный список prompt-файлов subagents, которые оркестратор должен использовать:
  - implementation subagent;
  - unit-test subagent;
  - review subagent;
  - verification subagent;
  - optional docs/handoff subagent;
- цель текущего slice;
- task boundary текущего slice;
- In scope / Out of scope;
- `Model Guidance` section with the approved model/reasoning allocation for implementation, unit-test, review, verification and docs/handoff roles, plus any allowed escalation conditions;
- required flow:
   1. прочитать `project-specific-info.md` и пройти Project-Specific Info Gate;
   2. прочитать canonical Spec Kit / SDD sources + previous handoff/acceptance для Slice 2+ или initial Slice 1 baseline;
   3. выполнить execution-time refresh gate before implementation:
      - if previous evidence was pending during prompt-pack generation, require actual previous acceptance report and handoff before implementation;
      - compare actual previous handoff/acceptance with this prompt-pack assumptions, canonical slices/tasks/spec/plan and project-specific notes;
      - if the check passes and no correction is needed, mark the current prompt-pack execution readiness status as `execution-ready` and record checked previous evidence paths;
      - if current prompt-pack or already-generated future prompt-packs need updates, stop and propose concrete corrections to the human;
      - do not implement from a stale or conflicting prompt-pack;
   4. отправить implementation prompt;
   5. получить implementation output;
   6. отправить unit-test prompt для составления и прогона unit-тестов по функционалу, написанному в рамках slice;
   7. получить unit-test output;
   8. отправить review prompt;
   9. обработать blockers/fixes; если review или unit-test требует fixes, вернуть focused fix request implementation или unit-test subagent в зависимости от ownership;
   10. отправить verification prompt;
   11. обработать verification gaps/failures;
   12. отправить docs/handoff prompt after verification evidence is sufficient for an acceptance recommendation, or stop with missing-evidence status if acceptance/handoff cannot be prepared;
   13. after acceptance report and handoff are created, run post-slice future-pack refresh gate for the next slice prompt-pack:
      - locate the next slice prompt-pack if it already exists;
      - compare the new acceptance report and handoff with the next prompt-pack assumptions, canonical slices/tasks/spec/plan and project-specific notes;
      - if no correction is needed, update only the next prompt-pack execution readiness status to `execution-ready`, record the actual previous evidence paths, and remove/clear the pending previous-evidence marker for that next slice;
      - after this status update, the next slice orchestrator does not need to repeat the "generated before previous evidence" correction check unless evidence changes again or new conflicts are found;
      - if correction is needed, do not rewrite the next prompt-pack automatically; propose concrete corrections to the human and leave the next prompt-pack status as `planning-pending-previous-evidence` or `needs-correction`;
   14. подготовить финальное решение;
- skill usage rules:
  - orchestrator использует `managed-implementation-gate` как controlling workflow;
  - implementation использует `execute`;
  - unit-test использует `execute` для тестового кода и test setup только в пределах текущего slice;
  - review использует `audit-only`;
  - verification использует `audit-only`;
  - docs/handoff использует skill только для formal log/handoff;
- stop conditions;
- expected final output.

Stop conditions должны включать как минимум:

- отсутствующий, неполный или конфликтующий `project-specific-info.md`;
- выход за границы текущего slice;
- violating SDD or project-specific constraints;
- public deployment or permanent public URL without a separate human gate;
- изменение spec/plan/slice/task boundaries без approval;
- конфликт handoff или project-specific notes с canonical Spec Kit / SDD sources;
- generated-before-previous-evidence prompt-pack is about to be executed before actual previous acceptance/handoff exists and is checked;
- actual previous handoff/acceptance changes current or future slice assumptions and requires prompt-pack correction;
- post-slice future-pack refresh cannot safely mark the next prompt-pack execution-ready;
- verification cannot support the claimed result;
- unexpected dependency churn, generated artifacts, secrets or broad unrelated changes;
- approved model/reasoning allocation is missing, unconfirmed, or cannot be recorded in the prompt-pack;
- отсутствие unit-test evidence для нового slice-функционала без явной причины и documented limitation.

## Required Content: Implementation Subagent Prompt

В `implementation-subagent.md` зафиксируй:

- language rule: write the subagent prompt in English and require the implementation subagent to return output in English;
- role-specific `Model guidance:` line copied from the approved allocation;
- `managed-implementation-gate` в `execute` mode;
- какие files читать перед plan-before-code, включая `project-specific-info.md` и canonical sources из него;
- previous handoff как источник current state для Slice 2+; для Slice 1 использовать initial baseline из spec/plan/slices/tasks;
- responsibility: implementation only, no final review, no acceptance claim;
- текущие tasks только этого slice;
- required plan-before-code fields;
- implementation requirements из slice/tasks;
- Do Not Implement section, включая будущие slices и anti-scope from canonical sources;
- verification expected from implementation subagent;
- expectation that implementation exposes testable logic through existing code structure when reasonable, without adding broad abstractions only for tests;
- return format.

Implementation prompt должен явно запрещать решать соседние slices "заодно".

## Required Content: Unit-Test Subagent Prompt

В `unit-test-subagent.md` зафиксируй:

- language rule: write the subagent prompt in English and require the unit-test subagent to return output in English;
- role-specific `Model guidance:` line copied from the approved allocation;
- `managed-implementation-gate` в `execute` mode;
- какие files читать:
  - `project-specific-info.md`;
  - sources of truth текущего slice;
  - previous handoff и acceptance report для Slice 2+; для Slice 1 initial baseline and `N/A - first slice`;
  - implementation output;
  - files changed by implementation;
  - package scripts и existing test setup;
- responsibility: составить и прогнать unit-тесты только для функционала, написанного в рамках текущего slice;
- ownership:
  - test files;
  - минимальные test setup/package script changes, если test framework уже есть или если новый setup явно нужен для выполнения обязательного правила;
  - no production behavior changes, кроме минимальных testability adjustments, которые нужно явно обосновать и согласовать через оркестратора;
- required plan-before-code fields:
  - что именно будет покрыто unit-тестами;
  - какие files/tests будут добавлены или изменены;
  - нужен ли test framework или scripts;
  - какие dependency/package/lockfile changes ожидаются;
  - почему изменения остаются внутри текущего slice;
  - команды для запуска unit-тестов;
- testing requirements:
  - unit-тесты должны проверять вычисления, маппинги, state helpers, форматирование, guards или другие чистые/изолируемые части нового slice-функционала;
  - если UI behavior сложно проверить unit-level без тяжелого setup, выделить и протестировать чистую логику, а UI оставить verification/smoke;
  - если в проекте нет test setup, добавить минимальный, объяснимый и scoped setup или остановиться с explicit blocker, если dependency churn нельзя безопасно привязать к slice;
  - unit-тесты должны запускаться командой package script, например `npm test` или `npm run test:unit`;
  - тесты не должны требовать ресурсов, интеграций, данных или окружений, которые canonical sources или relevant project-specific notes выводят за рамки текущего slice;
- Do Not Implement section:
  - не добавлять e2e/browser tests вместо unit tests, если это не отдельное дополнение;
  - не тестировать соседние slices как основную цель;
  - не менять acceptance criteria или task boundaries;
  - не добавлять broad refactors, snapshots или generated artifacts без необходимости;
- return format:
  - Mode used: `execute`;
  - Plan gate result;
  - Tests added/changed;
  - Any production/testability files changed and why;
  - Commands run and results;
  - Coverage mapped to current slice tasks;
  - Known gaps and not-run checks;
  - Explicit statement that no final review or acceptance decision is claimed.

## Required Content: Review Subagent Prompt

В `review-subagent.md` зафиксируй:

- language rule: write the subagent prompt in English and require the review subagent to return output in English;
- role-specific `Model guidance:` line copied from the approved allocation;
- `managed-implementation-gate` в `audit-only` mode;
- какие files читать, включая `project-specific-info.md` и canonical sources из него;
- previous handoff и previous acceptance report как контекст для Slice 2+; для Slice 1 initial baseline and `N/A - first slice`;
- responsibility: independent review only, no fixes unless orchestrator requests;
- inputs include implementation output, unit-test output and current diff;
- review scope:
  1. agent task alignment;
  2. slice alignment;
  3. implementation plan alignment;
  4. spec alignment;
  5. project-specific info alignment with canonical sources;
  6. unit-test alignment and adequacy for new slice functionality;
  7. previous handoff alignment for Slice 2+ or initial baseline alignment for Slice 1;
  8. repo health;
- blockers, связанные с текущим slice;
- findings classification: Blocker / Fix / Log / Pass;
- return format.

Review prompt должен проверять, что implementation не сломал результат предыдущего accepted slice для Slice 2+. Для Slice 1 review prompt должен проверить, что implementation не реализует соседние slices и не вводит scope, запрещенный canonical sources или relevant project-specific notes.

## Required Content: Verification Subagent Prompt

В `verification-subagent.md` зафиксируй:

- language rule: write the subagent prompt in English and require the verification subagent to return output in English;
- role-specific `Model guidance:` line copied from the approved allocation;
- `managed-implementation-gate` в `audit-only` mode;
- какие files читать, включая `project-specific-info.md` и canonical sources из него;
- previous handoff как baseline для regression/smoke для Slice 2+; для Slice 1 использовать initial baseline из quickstart/slices/tasks;
- responsibility: verification evidence only, no code changes unless explicitly asked;
- minimum checks из текущего slice;
- unit-test command from unit-test subagent, if available, must be rerun or explicitly marked not run with reason;
- regression check для ключевого результата предыдущего accepted slice для Slice 2+; для Slice 1 вместо regression check проверить самостоятельную закрываемость Slice 1 без зависимостей на Slice 2+;
- evidence requirements:
  - command/check;
  - result;
  - requirement covered;
  - what was not checked;
  - whether evidence supports task-level or slice-level verification;
- stop conditions;
- return format.

Verification prompt не должен claim final acceptance, если текущий slice не финальный.

## Required Content: Docs / Handoff Subagent Prompt

В `docs-handoff-subagent.md` зафиксируй:

- language rule: write the subagent prompt in English and require the docs/handoff subagent to return output in English; the orchestrator owns Russian user-facing reporting and may translate/summarize docs/handoff output into Russian;
- role-specific `Model guidance:` line copied from the approved allocation;
- skill optional: использовать `managed-implementation-gate` только для formal implementation log/handoff;
- inputs:
  - `project-specific-info.md`;
  - orchestrator decision;
  - implementation summary;
  - review findings;
  - verification evidence;
  - unit-test evidence and not-run checks;
  - known gaps;
- previous handoff для Slice 2+ или `N/A - first slice`;
- previous acceptance report для Slice 2+ или `N/A - first slice`;
- responsibility: handoff clarity only, no code, no review, no verification, no acceptance decision;
- task:
   - подготовить concise handoff notes for the next slice;
   - подготовить или предложить структуру `docs/orchestration/slice-<n>/handoff.md`;
   - подготовить или предложить структуру `docs/orchestration/slice-<n>/acceptance-report.md`;
   - указать, меняют ли фактические результаты текущего slice предположения уже созданных future prompt-packs;
   - подготовить evidence summary for post-slice future-pack refresh gate;
- output sections:
  - Current state;
  - Changed files;
  - Verification summary;
  - Open items;
  - Do next;
  - Do not do;
  - Impact on future prompt-packs;
  - Next prompt-pack readiness recommendation;
  - Sources of truth.

Docs/Handoff prompt должен учитывать, что по завершению slice должны появиться два документа:

- `docs/orchestration/slice-<n>/acceptance-report.md`
- `docs/orchestration/slice-<n>/handoff.md`

Acceptance report должен следовать `docs/orchestration/acceptance-report-template.md`.
Handoff должен быть похож по структуре на previous handoff, но отражать фактический результат текущего slice. Для Slice 1 создай первый handoff с той же структурой, без ссылки на предыдущий handoff.

После появления `acceptance-report.md` и `handoff.md` orchestrator должен сразу выполнить post-slice future-pack refresh gate. Если следующий prompt-pack уже существует и новые документы не требуют корректировок, orchestrator должен обновить только readiness/status metadata следующего prompt-pack на `execution-ready`, записать checked previous evidence paths и убрать pending previous-evidence marker. Если следующий prompt-pack требует корректировок, orchestrator должен оставить его неготовым к реализации, перечислить конкретные правки и запросить human decision.

## Responsibility Split To Preserve

Сохраняй разделение ролей:

- Orchestrator owns sequencing, gate decisions and final recommendation.
- Implementation owns scoped diff only.
- Review owns independent diff review only.
- Unit-Test owns scoped unit-test creation and unit-test execution only.
- Verification owns independent evidence only.
- Docs/Handoff owns state transfer only.

Не позволяй implementation subagent принимать свой slice.
Не позволяй unit-test subagent принимать slice или расширять production scope.
Не позволяй review subagent подменять verification.
Не позволяй verification subagent исправлять код без отдельного решения оркестратора.
Не позволяй docs/handoff subagent принимать slice.

## Skill Usage To Preserve

- Orchestrator: use `managed-implementation-gate` as controlling workflow.
- Implementation: use `managed-implementation-gate` in `execute` mode.
- Unit-Test: use `managed-implementation-gate` in `execute` mode for test files/test setup only.
- Review: use `managed-implementation-gate` in `audit-only` mode.
- Verification: use `managed-implementation-gate` in `audit-only` mode.
- Docs/Handoff: required when the orchestrator prepares acceptance-report/handoff artifacts or formal state transfer; optional only if the human explicitly handles those documents outside the generated workflow.
- Any role that is out of scope for the current slice must not be created.

## Final Self-Check

Before finishing, verify:

- `project-specific-info.md` was read and passed the Project-Specific Info Gate before prompt generation started;
- every prompt file exists;
- orchestrator prompt references every subagent prompt file;
- index references every prompt file;
- index and orchestrator prompt include model guidance matching the approved allocation;
- every included subagent prompt includes its role-specific model guidance;
- unit-test subagent prompt exists and requires creating/running unit tests for current slice functionality;
- review prompt checks unit-test adequacy;
- verification prompt reruns or accounts for unit-test command;
- all slice-specific orchestration artifacts are under `docs/orchestration/slice-<n>/`;
- previous acceptance report and handoff are referenced for Slice 2+ or explicitly marked `N/A - first slice`;
- generated-before-previous-evidence prompt-packs include an execution-time refresh gate;
- generated-before-previous-evidence prompt-packs include a post-slice future-pack refresh gate that can mark the next pack `execution-ready` after actual acceptance/handoff exists;
- current slice tasks match tasks source from `project-specific-info.md`;
- out-of-scope items match canonical sources and any relevant project-specific notes;
- project-specific notes were used only as auxiliary input and did not replace canonical Spec Kit / SDD sources;
- `managed-implementation-gate` usage is explicit and mode-specific;
- docs/handoff prompt requires creation or proposal of acceptance report and handoff for the completed slice;
- no prompt asks a subagent to accept its own work;
- no prompt expands scope into a future slice.

Return a concise summary:

- files created;
- slice covered;
- previous handoff/acceptance used, or `N/A - first slice`;
- subagent roles;
- project-specific info used;
- any assumptions or stop conditions;
- approved model/reasoning allocation used for this slice.
```

## Project-Specific Context

Проектные notes, конкретные пути, previous-slice inputs и optional next-slice hints должны находиться в `project-specific-info.md`, а не в этом generator prompt.
