---
name: "speckit-jira-background-sync"
description: "Automatic Jira background status synchronization hook for Spec Kit implementation; syncs or skips and always writes trace artifacts."
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
   - Translate generated wrapper text and fallback labels into Russian. Examples: `Фича: specs/<name>`, `Задачи`, `Статус в Spec Kit`, `Фаза`, `Прогресс`, `Задача завершена локально через Spec Kit`.
   - Do not send English boilerplate such as `Task completed locally via spec-kit` or `Progress` to Jira.
   - Trace JSON may keep stable English machine tokens such as `synced`, `skipped`, `failed`, and reason codes.
5. Resolve the target spec directory in this order:
   - `--spec <name>` from `$ARGUMENTS`
   - current directory if it is inside `specs/<name>/`
   - current git branch after removing common prefixes `feature/`, `spec/`, `bugfix/`, `hotfix/`, `release/`
   - the only `specs/<name>/` directory containing `jira-mapping.json`
   - otherwise the most recently modified `specs/<name>/jira-mapping.json`
6. Always write a trace event to `specs/<name>/jira-sync-trace.json` when a spec is resolved. If no spec can be resolved, write to `.specify/traces/jira/background-sync.json`.
7. A trace event MUST include:
   - ISO timestamp
   - hook name: `after_implement`
   - spec name when known
   - project settings path when present
   - config path
   - action: `synced`, `skipped`, or `failed`
   - reason
   - Jira sync enabled flag when available
   - Jira project key when available
   - mapping path when available
8. If `integrations.jira.sync_enabled` is not `true`, do not call Jira. Record `action: "skipped"` and reason `jira sync disabled by project settings`.
9. If Jira is not configured, do not fail the Spec Kit workflow. Record `action: "skipped"` and a precise reason.
10. Jira is considered not configured when both `.project.key` in `jira-config.yml` and `SPECKIT_JIRA_PROJECT_KEY` are empty.
11. If `specs/<name>/jira-mapping.json` does not exist, do not call Jira. Record `action: "skipped"` with reason `mapping not found`.
12. If the mapping has `mode: "stage-epic"` and `sync.sync_task_status` is false, do not sync phases or tasks. Optionally refresh Stage/Epic descriptions from constitution/spec if supported by the MCP server, then record `action: "synced"` or `action: "skipped"` with reason `task status sync disabled`.
13. If a mapping created by the upstream 2-level/3-level command is encountered and task sync is enabled, execute the upstream extension procedure from `.specify/extensions/jira/commands/sync-status.md` using the configured MCP server and the Russian Jira-facing language policy above.
14. If MCP tools are unavailable or the Jira operation fails, record `action: "failed"` with the error summary, but do not undo implementation changes.

## Output

Report only the trace path and whether Jira sync was synced, skipped, or failed.
