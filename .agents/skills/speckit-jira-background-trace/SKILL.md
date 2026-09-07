---
name: "speckit-jira-background-trace"
description: "Automatic Jira background projection hook for Spec Kit tasks; creates or skips Jira issues and always writes trace artifacts."
compatibility: "Requires spec-kit project structure with .specify/ directory and optional Jira MCP server"
---

## Inputs

```text
$ARGUMENTS
```

## Required Behavior

1. Treat this as a background hook. Keep user-facing output short and factual.
2. Load `.specify/project.yml` if it exists. Treat `integrations.jira.sync_enabled` as disabled when the file or key is missing.
3. Load `.specify/extensions/jira/jira-config.yml` if it exists.
4. Jira-facing language policy:
   - Write every Jira `summary`, `description`, and comment in Russian.
   - Preserve technical identifiers as-is: issue keys, file paths, field IDs, status tokens, command names, requirement IDs, URLs, and code identifiers.
   - Use source artifact text directly when it is already Russian.
   - Translate generated wrapper text and fallback labels into Russian. Examples: `Фича: specs/<name>`, `Задачи`, `Статус в Spec Kit`, `Фаза`, `Прогресс`.
   - Do not send English boilerplate such as `Phase from spec`, `Task from spec`, `Progress`, or `Spec Kit Constitution` to Jira.
   - Trace JSON may keep stable English machine tokens such as `created`, `skipped`, `failed`, and reason codes.
5. Resolve the target spec directory in this order:
   - `--spec <name>` from `$ARGUMENTS`
   - current directory if it is inside `specs/<name>/`
   - current git branch after removing common prefixes `feature/`, `spec/`, `bugfix/`, `hotfix/`, `release/`
   - the only `specs/<name>/` directory containing both `spec.md` and `tasks.md`
   - otherwise the most recently modified `specs/<name>/tasks.md`
6. Always write a trace event to `specs/<name>/jira-trace.json` when a spec is resolved. If no spec can be resolved, write to `.specify/traces/jira/background-trace.json`.
7. A trace event MUST include:
   - ISO timestamp
   - hook name: `after_tasks`
   - spec name when known
   - project settings path when present
   - config path
   - action: `created`, `skipped`, or `failed`
   - reason
   - Jira sync enabled flag when available
   - Jira project key when available
   - mapping path when available
8. If `integrations.jira.sync_enabled` is not `true`, do not call Jira. Record `action: "skipped"` and reason `jira sync disabled by project settings`.
9. If Jira is not configured, do not fail the Spec Kit workflow. Record `action: "skipped"` and a precise reason.
10. Jira is considered not configured when both `.project.key` in `jira-config.yml` and `SPECKIT_JIRA_PROJECT_KEY` are empty.
11. If `specs/<name>/jira-mapping.json` already exists, do not create duplicates. Record `action: "skipped"` with reason `mapping already exists`.
12. If Jira is configured and no mapping exists, create only the configured top-level Jira projection:
   - read `.specify/jira-constitution-mapping.json` and reuse its `stage_key` when present
   - create one `mapping.constitution_artifact` issue from `.specify/memory/constitution.md` only if no synced Stage key exists and `sync.create_stage_from_constitution` is true
   - create one `mapping.spec_artifact` issue from `specs/<name>/spec.md` when `sync.create_epic_from_spec` is true
   - set the Epic's Stage field from `field_mappings.epic_stage` / `epic.stage_field` / `mapping.relationships.constitution_spec`
   - do not create Jira issues for phases or tasks when `sync.create_phases` and `sync.create_tasks` are false
13. For the current corporate default, this means Jira project `TPL`, constitution -> `Stage`, spec -> `Epic`, and Epic.Stage -> `customfield_18723`.
14. Save `specs/<name>/jira-mapping.json` with `mode: "stage-epic"`, the Stage issue key, the Epic issue key, `stage_field`, and zero phase/task counts.
15. If MCP tools are unavailable or the Jira operation fails, record `action: "failed"` with the error summary, but do not undo generated Spec Kit files.

## Output

Report only the trace path and whether Jira creation was created, skipped, or failed.
