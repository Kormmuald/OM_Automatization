---
title: Write Spec to Confluence
description: Create a Confluence mirror page from the active Spec Kit feature spec.md only.
---

## Usage and Functionality
Given an Atlassian Confluence parent page URL, create a child page that mirrors the active `specs/<feature>/spec.md`. If the user did not provide a URL, read `integrations.confluence.spec_root_page_url` from `.specify/project.yml`. For legacy projects only, if that key is absent or empty, fall back to `## Корневая страница Confluence` / `## Confluence root page` from `.specify/memory/constitution.md`. If neither source contains a valid Confluence URL, ask for one.

This command writes only the feature specification. It MUST NOT include `plan.md`, constitution, classification, research, tasks, quickstart, contracts, or supporting docs in the Confluence page body.

The Confluence page is a Russian-language readable mirror. Translate human-facing prose from `spec.md` into Russian before publishing. Preserve technical identifiers, requirement IDs, commands, file paths, URLs, code blocks, inline code, schema names, field names, status tokens, and source references exactly unless the source itself already contains a Russian equivalent. Do not modify the local `spec.md`.

If this command is being executed in the CLI, you may need to use an Atlassian API token and email address to access the site. If you cannot access Atlassian after about a minute or two, trying using the token and email address. Ask for them if not already provided.

Please also be sure to log your updates and progress in the console as you go along.

## Parameters
- **confluence_url** (string, optional when constitution has a root page): the Confluence parent page URL where the spec mirror page will be created
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
- The created page must include a small Russian service block with source path, sync timestamp, source hash, and a notice meaning `Confluence is a mirror; source of truth is repository spec.md`.
- The command saves `specs/<feature>/confluence-mapping.json` and writes `specs/<feature>/confluence-sync-trace.json`.
- If `confluence-mapping.json` already exists, do not create a duplicate page unless the user explicitly asks to override it.
- For more details and workflow, see the main README.
