---
name: "speckit-jira-complete-spec"
description: "Post-archive Jira hook that transitions the archived feature spec's mapped Jira issue to Done."
compatibility: "Requires spec-kit project structure with .specify/ directory and optional Jira MCP server"
---

## Inputs

```text
$ARGUMENTS
```

## Required Behavior

1. Treat this as a background post-archival hook. Keep user-facing output short and factual.
2. Load `.specify/project.yml` if it exists. Treat `integrations.jira.sync_enabled` as disabled when the file or key is missing.
3. Load `.specify/extensions/jira/jira-config.yml` if it exists.
4. Jira-facing language policy:
   - Write every Jira `summary`, `description`, and comment in Russian.
   - Preserve technical identifiers as-is: issue keys, file paths, field IDs, status tokens, command names, requirement IDs, URLs, and code identifiers.
   - Translate generated completion text into Russian.
   - Trace JSON may keep stable English machine tokens such as `transitioned`, `skipped`, `failed`, and reason codes.
5. Resolve the target spec directory in this order:
   - first non-flag token from `$ARGUMENTS` when it resolves to `specs/<name>/` or to an absolute feature directory
   - `--spec <name>` from `$ARGUMENTS`
   - current directory if it is inside `specs/<name>/`
   - current git branch after removing common prefixes `feature/`, `spec/`, `bugfix/`, `hotfix/`, `release/`
   - the only `specs/<name>/` directory containing `jira-mapping.json`
   - otherwise the most recently modified `specs/<name>/jira-mapping.json`
6. Always write a trace event to `specs/<name>/jira-completion-trace.json` when a spec is resolved. If no spec can be resolved, write to `.specify/traces/jira/complete-spec.json`.
7. A trace event MUST include:
   - ISO timestamp
   - hook name: `after_archive`
   - spec name when known
   - project settings path when present
   - config path
   - action: `transitioned`, `skipped`, or `failed`
   - reason
   - Jira sync enabled flag when available
   - Jira project key when available
   - mapping path when available
   - target issue key when available
   - target transition name or ID when used
8. If `integrations.jira.sync_enabled` is not `true`, do not call Jira. Record `action: "skipped"` and reason `jira sync disabled by project settings`.
9. Jira is considered not configured when both `.project.key` in `jira-config.yml` and `SPECKIT_JIRA_PROJECT_KEY` are empty. If not configured, do not fail the Spec Kit workflow. Record `action: "skipped"` and reason `jira project not configured`.
10. If `specs/<name>/jira-mapping.json` does not exist, do not call Jira. Record `action: "skipped"` with reason `mapping not found`.
11. Resolve the Jira issue that represents the archived spec:
    - For the corporate `stage-epic` mapping, use the feature-level Epic key from `epic.key`, `epic_key`, `spec.key`, `issue.key`, or `issue_key`, whichever exists first.
    - For older mappings where `hierarchy.level_1.source` is `spec.md`, use `stage.key` because the Stage issue is the spec-level Jira issue in that mapping.
    - Do not transition the constitution Stage when it represents `.specify/memory/constitution.md` rather than the feature spec.
    - Do not transition phase/task issues from `epics[].stories[]`; this hook closes the feature-level Jira issue only.
12. Read the target issue when the MCP server provides `issue_get`. If the current status already equals the configured done status, record `action: "skipped"` and reason `already done`.
13. Determine the completion transition:
    - Use `workflow.done_transition` from `jira-config.yml` when present.
    - Otherwise use `status_mapping.completed`.
    - Otherwise default to `Done`.
    - If transition lookup is available, prefer an available transition whose name exactly matches the configured transition, then a case-insensitive match, then common completion names `Done`, `Resolve Issue`, `Resolved`, `Close Issue`, `Closed`.
14. Transition the issue using the configured MCP server's `issue_transition` tool. If the tool accepts transition names, pass the chosen transition name; if it requires an ID, pass the transition ID returned by `transition_list`.
15. Add a short completion comment when `issue_update` supports comments:
    `Spec Kit архивировал specs/<name>; работа по фиче завершена.`
16. If MCP tools are unavailable, the target issue cannot be identified, or the Jira operation fails, record `action: "failed"` with the error summary, but do not undo archival changes and do not mark the archive as failed.

## Output

Report only the trace path and whether Jira completion was transitioned, skipped, or failed.
