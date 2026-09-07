---
name: "speckit-confluence-write"
description: "Create a Confluence mirror page from the current Spec Kit feature spec.md."
compatibility: "Requires spec-kit project structure with .specify/ directory and Confluence MCP access"
metadata:
  source: ".specify/extensions/confluence/commands/write.md"
---

## User Input

```text
$ARGUMENTS
```

You MUST consider the user input before proceeding.

## Required Behavior

1. Load `.specify/project.yml` if it exists. Treat `integrations.confluence.sync_enabled` as disabled when the file or key is missing.
2. If `integrations.confluence.sync_enabled` is not `true`, do not write to Confluence. Report that Confluence sync is disabled by project settings and stop.
3. Read `.specify/extensions/confluence/commands/write.md` from the project root and follow its command instructions.
4. Resolve the active feature directory using the same order as `speckit.confluence.background-spec-sync`.
5. Require a valid Confluence parent page URL in the input. If it is missing, read
   `integrations.confluence.spec_root_page_url` from `.specify/project.yml`. For legacy projects
   only, if that key is absent or empty, fall back to the `## Корневая страница Confluence` /
   `## Confluence root page` section from `.specify/memory/constitution.md`. If neither source
   contains a valid Confluence URL, ask the user for one.
6. Gather only the active `specs/<feature>/spec.md`. Do not include `.specify/memory/constitution.md`, `.specify/project.yml`, `plan.md`, research, tasks, quickstart, contracts, or supporting docs in the Confluence page body.
7. Render the Confluence page in Russian: translate human-facing prose from `spec.md`, preserve technical identifiers, requirement IDs, commands, file paths, URLs, code blocks, inline code, schema names, field names, status tokens, and source references, and do not modify the local `spec.md`.
8. Use available Confluence/Atlassian MCP tools or connector capabilities to create a child page under the target parent page.
9. Save the created page link in `specs/<feature>/confluence-mapping.json` using the `spec-page` mapping shape from `background-spec-sync.md`, and write `specs/<feature>/confluence-sync-trace.json`.
10. If `confluence-mapping.json` already exists, do not create a duplicate page unless the user explicitly supplied an override. Prefer updating the mapped page or tell the user to run `speckit.confluence.update`.
11. Keep credentials out of files and logs. Ask for an Atlassian API token/email only if the configured MCP access is unavailable and the environment requires CLI credentials.

## Output

Report the Confluence page URL, mapping path, trace path, and confirm that only `spec.md` was used.
