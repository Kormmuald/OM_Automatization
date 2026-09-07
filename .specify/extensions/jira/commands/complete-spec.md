---
description: "Transition an archived feature spec's Jira issue to Done"
tools:
  - '{mcp_server}/issue_get'
  - '{mcp_server}/transition_list'
  - '{mcp_server}/issue_transition'
  - '{mcp_server}/issue_update'
---

# Complete Archived Spec in Jira

This command transitions the Jira issue mapped to the archived feature spec to the configured Done state. It is normally executed automatically by the corporate `after_archive` hook.

## User Input

```text
$ARGUMENTS
```

Accepts either a feature directory path, for example `specs/007-invoice-settings`, or `--spec <name>`.

## Behavior

1. Load `.specify/project.yml` and skip when `integrations.jira.sync_enabled` is not `true`.
2. Load `.specify/extensions/jira/jira-config.yml`.
3. Resolve the target feature directory and read `specs/<feature>/jira-mapping.json`.
4. Jira-facing text policy:
   - Write Jira summaries, descriptions, and comments in Russian.
   - Preserve technical identifiers as-is: issue keys, file paths, field IDs, status tokens, command names, requirement IDs, URLs, and code identifiers.
   - Trace JSON may keep stable English machine tokens.
5. Identify the feature-level Jira issue:
   - `stage-epic` mappings: the mapped Epic key.
   - Legacy mappings where `hierarchy.level_1.source` is `spec.md`: the mapped Stage key.
6. Use `workflow.done_transition`, `status_mapping.completed`, or `Done` as the completion transition.
7. Transition only that feature-level issue; do not transition constitution Stage, phase issues, or task/story issues.
8. When adding a completion comment, use Russian text:
   `Spec Kit архивировал specs/<feature>; работа по фиче завершена.`
9. Write `specs/<feature>/jira-completion-trace.json` with `transitioned`, `skipped`, or `failed`.

This command is idempotent: when the target issue is already in the configured Done status, it records `skipped` and performs no transition.
