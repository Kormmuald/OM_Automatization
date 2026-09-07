---
name: "speckit-confluence-update"
description: "Update an existing Confluence mirror page from the latest Spec Kit feature spec.md."
compatibility: "Requires spec-kit project structure with .specify/ directory and Confluence MCP access"
metadata:
  source: ".specify/extensions/confluence/commands/update.md"
---

## User Input

```text
$ARGUMENTS
```

You MUST consider the user input before proceeding.

## Required Behavior

1. Load `.specify/project.yml` if it exists. Treat `integrations.confluence.sync_enabled` as disabled when the file or key is missing.
2. If `integrations.confluence.sync_enabled` is not `true`, do not update Confluence. Report that Confluence sync is disabled by project settings and stop.
3. Read `.specify/extensions/confluence/commands/update.md` from the project root and follow its command instructions.
4. Resolve the active feature directory using the same order as `speckit.confluence.background-spec-sync`.
5. Use an explicit valid Confluence URL from input when provided. If no URL is provided, read `specs/<feature>/confluence-mapping.json` and use `page_url` or `page_id`. If neither exists, ask for a valid Confluence URL or tell the user to run `speckit.confluence.write`.
6. Gather only the active `specs/<feature>/spec.md`. Do not include `.specify/memory/constitution.md`, `.specify/project.yml`, `plan.md`, research, tasks, quickstart, contracts, or supporting docs in the Confluence page body.
7. Render the Confluence page in Russian: translate human-facing prose from `spec.md`, preserve technical identifiers, requirement IDs, commands, file paths, URLs, code blocks, inline code, schema names, field names, status tokens, and source references, and do not modify the local `spec.md`.
8. Fetch the existing Confluence page metadata/content first and record remote version/title in `specs/<feature>/confluence-sync-trace.json` for drift detection.
9. Update in Replace spec body mode: preserve only the service block and required Confluence wrapper, then replace the mirrored spec body with the Russian rendering of the current `spec.md`.
10. Update `specs/<feature>/confluence-mapping.json` with the current source hash, page link, timestamp, and `last_action`.
11. Keep credentials out of files and logs. Ask for an Atlassian API token/email only if the configured MCP access is unavailable and the environment requires CLI credentials.

## Output

Report the Confluence page URL, mapping path, trace path, and whether the update was applied or skipped because content was unchanged.
