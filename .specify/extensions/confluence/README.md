# spec-kit-confluence

## Overview
**spec-kit-confluence** is an extension for [spec-kit](https://github.com/your-org/spec-kit) that mirrors confirmed feature specifications into Confluence. In the corporate template, Confluence is a readable mirror for `specs/<feature>/spec.md` only; repository files remain the source of truth.

## Key Features
- **Automated Spec Mirror Creation:** Creates a Confluence child page for the confirmed feature `spec.md` before `/SpecKit Plan`.
- **Document Retrieval:** Fetches and displays the contents of any existing Confluence page for in-context review.
- **Replace-Body Updates:** Updates the mapped Confluence page by replacing the mirrored spec body from the latest `spec.md`.
- **Local Mapping and Trace:** Stores the page link in `specs/<feature>/confluence-mapping.json` and writes `specs/<feature>/confluence-sync-trace.json`.
- **MCP Integration:** Uses the MCP server to securely access Confluence across all commands.

## Prerequisites
- [spec-kit](https://github.com/your-org/spec-kit) v0.1.0 or higher
- A Confluence account with API access
- MCP server credentials/configuration (`*/Code/User/mcp.json` or `*/github-copilot/intellij/mcp.json`)

## Setup
1. Run the setup script:
    ```sh
    chmod +x setup_mcp.sh
    ./setup_mcp.sh
    ```
    This will configure the MCP server for Confluence access.
2. Refer to [this doc](https://support.atlassian.com/atlassian-rovo-mcp-server/docs/getting-started-with-the-atlassian-remote-mcp-server) for CLI congifuration.

## Usage and Functionality
### Overall
The agent will:
- Use the MCP server to access Confluence (ensure the MCP server is configured and running)
- Read only the active `specs/<feature>/spec.md` when creating or updating a mirror
- Render the Confluence page in Russian while preserving technical identifiers, paths, commands, URLs, code blocks, IDs, and source references
- Read the root parent page URL from `integrations.confluence.spec_root_page_url` in `.specify/project.yml`, with legacy fallback to `## Корневая страница Confluence` / `## Confluence root page`
- Store the created or updated page link in `specs/<feature>/confluence-mapping.json`
- Retrieve an existing doc for review when requested

### `write` command
Run the `write` command to create a Confluence child page for the active feature spec. Include a parent page URL, or configure it in the constitution:
- Confluence parent page URL where spec pages should be created
- Atlassian API token **(if run on the CLI)**
- Atlassian email address **(if run on the CLI)**

For more details, see [commands/write.md](commands/write.md).

### `read` command
To retrieve and display the contents of an existing Confluence document, run the `read` command. Be sure to include the following:
- Confluence URL of the existing doc to read
- Atlassian API token **(if run on the CLI)**
- Atlassian email address **(if run on the CLI)**

For more details, see [commands/read.md](commands/read.md).

### `update` command
To update an existing Confluence spec mirror with the latest `spec.md`, run the `update` command. The command uses `specs/<feature>/confluence-mapping.json` when no URL is provided. Be sure to include the following when no mapping exists:
- Confluence URL of the existing spec mirror page to update
- Atlassian API token **(if run on the CLI)**
- Atlassian email address **(if run on the CLI)**

For more details, see [commands/update.md](commands/update.md).

### Input format
```
Confluence URL: [confluence_url]
Atlassian API token: [atlassian_api_token] // if using CLI
Atlassian email address: [atlassian_email_address] // if using CLI

[For read/update, tell the agent what you want to do with the read content or how you want to modify it]
```

### Automatic `before_plan` hook
When `.specify/project.yml` contains `integrations.confluence.sync_enabled: true`, the corporate template runs `speckit.confluence.background-spec-sync` before `/SpecKit Plan`. The hook creates or updates the spec mirror under `integrations.confluence.spec_root_page_url`, skips safely when Confluence is disabled or not configured, and never creates duplicates when `confluence-mapping.json` already exists.

## License
MIT
