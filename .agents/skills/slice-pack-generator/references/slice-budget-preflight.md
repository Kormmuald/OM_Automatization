# Prompt: Slice Budget Preflight And Calibration

Дата фиксации: 2026-07-06.

## Purpose

Этот prompt используется как отдельный этап перед запуском implementation slice и, при необходимости, после завершения slice.

Цели:

- до запуска slice оценить, вероятно ли уложиться в текущий 5-часовой лимит;
- дать решение `GO`, `GO WITH WATCH`, `SPLIT FIRST` или `DO NOT START`;
- сохранить калибровочные данные по фактической стоимости slice;
- фиксировать фактический расход лимита в процентах от 5-часового окна, если такая метрика доступна через UI, API или явный human-provided reading;
- не фабриковать точные token/limit значения, если они недоступны.

Этот prompt не реализует slice, не принимает slice и не заменяет managed implementation gate. Он является preflight/ledger stage вокруг существующего orchestration workflow.

## Prompt To Use

```text
Ты выполняешь Slice Budget Preflight And Calibration для PEnergy Block 2 MVP Demo.

Пиши user-facing output на русском. Имена файлов, команд, task ids, gate names и quoted prompt fragments можно оставлять на английском.

## Mode

Работай в одном из режимов:

1. `preflight`: оценка до запуска slice.
2. `post-run-record`: фиксация фактического расхода после завершения slice.
3. `preflight-and-record`: сначала оценка, затем, если человек уже дал фактические замеры, обновление истории.

Если режим не указан, используй `preflight`.

## Inputs

Обязательные входы:

- Target slice number: `<n>`.
- Budget window: `5 hours`, unless the human explicitly provides another budget window.
- Project-specific info: `docs/orchestration/project-specific-info.md`.

Опциональные входы для `post-run-record`:

- Pre-run limit used percent: `<number>%`, if known.
- Post-run limit used percent: `<number>%`, if known.
- Actual tokens used: `<number>`, if available.
- Actual elapsed time: `<duration>`, if available.
- Human note about model/UI limit meter, if available.

Если фактический процент расхода 5-часового лимита не предоставлен и недоступен через текущую среду, не выдумывай его. Запиши `actual_limit_delta_percent: unknown` и, отдельно, `estimated_limit_delta_percent`.

## Files And Sources

Read first:

- `docs/orchestration/project-specific-info.md`

Then resolve from project-specific info and read only what is needed for the target slice:

- canonical Spec Kit / SDD source paths;
- slices source;
- tasks source;
- target prompt-pack index: `docs/orchestration/slice-<n>/prompts.md`, if it exists;
- target orchestrator prompt and subagent prompts, if they exist;
- previous acceptance report and handoff for Slice 2+, if they exist and are required for execution-ready mode.

Use the following calibration files:

- Historical ledger: `docs/orchestration/slice-budget-history.jsonl`
- Optional latest report: `docs/orchestration/slice-<n>/budget-preflight.md`

If the historical ledger does not exist during `preflight`, continue with static-only estimation. Do not create an empty ledger just to satisfy the prompt.

## Hard Boundaries

Do not implement the slice.
Do not modify source code.
Do not update acceptance reports or handoff documents except when the human explicitly asks for a separate docs task.
Do not change canonical spec, plan, tasks, slices, constitution, SDD workflow or prompt-pack boundaries.
Do not mark a slice as accepted.
Do not report exact token or limit percentages unless the measurement source is available.

Allowed writes:

- create or update `docs/orchestration/slice-<n>/budget-preflight.md`;
- append one JSONL record to `docs/orchestration/slice-budget-history.jsonl` in `post-run-record` or `preflight-and-record` mode when actual or human-provided measurement data exists.

## Static Estimate

Produce a static estimate from visible artifacts before applying calibration.

Estimate these inputs:

1. `context_size`:
   - prompt-pack files for the target slice;
   - required canonical sources;
   - previous acceptance/handoff for Slice 2+;
   - likely code files named by current slice tasks, if the repository already contains them.
2. `task_complexity`:
   - number of tasks mapped to the target slice;
   - number of likely changed files;
   - whether the slice includes UI layout, routing, state, tests, screenshots, browser smoke, deploy, or docs/handoff.
3. `workflow_roles`:
   - orchestrator;
   - implementation subagent;
   - unit-test subagent;
   - review subagent;
   - verification subagent;
   - docs/handoff subagent.
4. `risk_multipliers`:
   - missing previous evidence;
   - generated-before-previous-evidence prompt-pack requiring refresh;
   - unclear task-to-slice mapping;
   - expected screenshots or browser checks;
   - likely fix cycle after review or verification;
   - dirty worktree touching relevant files;
   - missing test setup or dependency uncertainty.

If a tokenizer is available, use it. Otherwise estimate tokens approximately from character counts and clearly mark the estimate as approximate. Keep measured and estimated values separate.

Suggested static bands:

- `low`: small docs-only or narrow code slice, no browser/screenshot, few tasks.
- `medium`: normal feature slice with implementation, tests, review and manual verification.
- `high`: routing, broad UI state, many files, screenshots/browser checks, deploy, missing previous evidence, or likely fix cycle.
- `very_high`: unclear boundaries, missing evidence, broad refactor risk, deploy/publication gate, or multiple likely fix cycles.

## Calibration

If `docs/orchestration/slice-budget-history.jsonl` exists, read it and prefer recent records from this project.

Use comparable records first:

- same project;
- same workflow shape;
- similar slice complexity;
- similar roles used;
- similar UI/test/verification requirements.

Calculate a calibrated estimate:

- compare previous `static_estimated_limit_delta_percent` with `actual_limit_delta_percent`, when both exist;
- derive a conservative correction factor from the last 3 comparable records;
- if fewer than 2 comparable records exist, show calibration as weak and keep static estimate dominant;
- if actual readings are `unknown`, do not use them as calibration ground truth.

Report calibration confidence:

- `none`: no usable history;
- `weak`: 1 usable record or only partial actual readings;
- `moderate`: 2-3 comparable records;
- `strong`: 4+ comparable records with actual percent or token measurements.

## Five-Hour Limit Percent

When actual readings are available:

- `slice_limit_delta_percent = post_run_limit_used_percent - pre_run_limit_used_percent`
- If the meter resets or rolls over during the run, mark the result as `ambiguous` and explain why.
- If only actual tokens are available but not the 5-hour limit denominator, record tokens but do not convert them to a percentage.
- If both actual tokens and denominator are available, record both.

When readings are not available:

- report `actual_limit_delta_percent: unknown`;
- report `estimated_limit_delta_percent` as a forecast;
- label the forecast as estimated, not measured.

## Verdict

Return one verdict:

- `GO`: likely fits comfortably; expected usage is below 60% of the 5-hour budget and pessimistic case is below 80%.
- `GO WITH WATCH`: likely fits, but the pessimistic case may approach the budget; start only if the human accepts a possible continuation/fix turn.
- `SPLIT FIRST`: likely too risky for one 5-hour window; recommend a smaller slice boundary before implementation.
- `DO NOT START`: missing evidence, conflicting sources, unclear boundary, or expected cost is too high to start responsibly.

If exact percent readings are unavailable, base the verdict on estimated percent plus qualitative risk, and say so.

## Preflight Report Format

Create or update `docs/orchestration/slice-<n>/budget-preflight.md` with:

```markdown
# Budget Preflight: Slice <n> - <title>

Дата оценки: <YYYY-MM-DD>

## Verdict

- Verdict: <GO / GO WITH WATCH / SPLIT FIRST / DO NOT START>
- Budget window: 5 hours
- Confidence: <low / medium / high>
- Calibration confidence: <none / weak / moderate / strong>

## Estimate

| Metric | Optimistic | Likely | Pessimistic | Notes |
| --- | ---: | ---: | ---: | --- |
| Estimated elapsed time | <value> | <value> | <value> | <why> |
| Estimated limit delta | <value>% | <value>% | <value>% | estimated, not measured unless stated |
| Estimated token use | <value> | <value> | <value> | approximate or tokenizer-based |

## Static Drivers

- Context size: <summary>
- Task complexity: <summary>
- Workflow roles: <summary>
- Verification cost: <summary>
- Risk multipliers: <summary>

## Calibration Used

- History file: `docs/orchestration/slice-budget-history.jsonl`
- Comparable records: <count>
- Correction factor: <value or N/A>
- Notes: <summary>

## Recommendation

<short recommendation>

## Measurement Fields For Post-Run

- pre_run_limit_used_percent: <unknown or value>
- post_run_limit_used_percent: <unknown or value>
- actual_limit_delta_percent: <unknown or value>
- actual_tokens_used: <unknown or value>
- actual_elapsed_time: <unknown or value>
- measurement_source: <UI / API / human-provided / unavailable>
```

## History Record Format

When actual or human-provided post-run data exists, append exactly one JSON object as one line to `docs/orchestration/slice-budget-history.jsonl`.

Use this schema:

```json
{
  "date": "YYYY-MM-DD",
  "project": "PEnergy Block 2 MVP Demo",
  "slice": 1,
  "slice_title": "App Shell And Demo Data",
  "mode": "post-run-record",
  "budget_window_hours": 5,
  "pre_run_limit_used_percent": null,
  "post_run_limit_used_percent": null,
  "actual_limit_delta_percent": null,
  "actual_tokens_used": null,
  "actual_elapsed_time_minutes": null,
  "measurement_source": "unavailable",
  "static_estimated_limit_delta_percent_likely": null,
  "calibrated_estimated_limit_delta_percent_likely": null,
  "verdict_before_run": null,
  "workflow_roles_used": ["orchestrator", "implementation", "unit-test", "review", "verification", "docs-handoff"],
  "risk_notes": [],
  "evidence_paths": []
}
```

Rules:

- Use `null` for unavailable numeric fields.
- Use `measurement_source: "UI"`, `"API"`, `"human-provided"` or `"unavailable"`.
- Do not append duplicate records for the same slice/run unless explicitly correcting a previous record. If correcting, add `"correction_of": "<date-or-record-id>"`.
- Keep JSONL machine-readable: one compact JSON object per line.

## Final Response Format

For `preflight`, return:

- verdict;
- likely/pessimistic budget estimate;
- top risk drivers;
- whether calibration history was used;
- path to the preflight report;
- what post-run readings should be captured.

For `post-run-record`, return:

- actual measured percent if available;
- actual tokens/time if available;
- whether the history ledger was appended;
- any calibration note for future slices;
- path to updated report/history.
```

## Integration Notes

Recommended placement in the slice workflow:

1. Generate or refresh the target slice prompt-pack.
2. Run this budget preflight prompt in `preflight` mode.
3. Human decides whether to start, split or defer the slice.
4. Run the normal managed implementation gate workflow.
5. After the slice completes, run this prompt in `post-run-record` mode with the available UI/API/human-provided usage readings.

This stage is intentionally advisory. A `GO` verdict is not acceptance, and a `SPLIT FIRST` verdict is not a change to canonical slice boundaries unless the human explicitly approves such a change.
