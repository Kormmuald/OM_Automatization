---
title: Update Confluence Spec Mirror
description: Update an existing Confluence mirror page from the active Spec Kit feature spec.md only.
---

## Usage and Functionality
Given an Atlassian Confluence URL, update the existing spec mirror page at that location. If the user did not provide a URL, read `specs/<feature>/confluence-mapping.json` and use its `page_url` or `page_id`. If no mapping exists and no valid URL was provided, ask for a valid Confluence URL or tell the user to run `speckit.confluence.write`.

This command updates only the feature specification. It MUST NOT include `plan.md`, constitution, classification, research, tasks, quickstart, contracts, or supporting docs in the Confluence page body.

The Confluence page is a Russian-language readable mirror. Translate human-facing prose from `spec.md` into Russian before publishing. Preserve technical identifiers, requirement IDs, commands, file paths, URLs, code blocks, inline code, schema names, field names, status tokens, and source references exactly unless the source itself already contains a Russian equivalent. Do not modify the local `spec.md`.

Updates use Replace spec body mode: preserve only the service block and required Confluence wrapper, then replace the mirrored body with the Russian rendering of the current `specs/<feature>/spec.md`.

If this command is being executed in the CLI, you may need to use an Atlassian API token and email address to access the site. If you cannot access Atlassian after about a minute or two, try using the token and email address. Ask for them if not already provided.

Please also be sure to log your updates and progress in the console as you go along.

## Parameters
- **confluence_url** (string, optional when `confluence-mapping.json` exists): the URL of the existing Confluence spec mirror page to update
- **atlassian_api_token** (string, required when using CLI): API token to connect to Atlassian when using CLI
- **atlassian_email_address** (string, required when using CLI): the email address for your Atlassian account

### Input template
```
Confluence URL: [confluence_url]
Atlassian API token: [atlassian_api_token] // if using CLI
Atlassian email address: [atlassian_email_address] // if using CLI
```

## Notes
- The agent will use the MCP server to access Confluence. Ensure the MCP server is configured and running (see setup_mcp.sh).
- Fetch existing page metadata/content first and record remote version/title in `specs/<feature>/confluence-sync-trace.json` for drift detection.
- Update `specs/<feature>/confluence-mapping.json` with the current page link, source hash, timestamp, and action.
- If the source hash is unchanged, skip the Confluence update and record `reason: "content unchanged"`.
- For more details and workflow, see the main README.
