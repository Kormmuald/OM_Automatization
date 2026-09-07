---
name: "speckit-confluence-read"
description: "Read a Confluence document by URL and optionally derive or update local Spec Kit artifacts from it."
compatibility: "Requires spec-kit project structure with .specify/ directory and Confluence MCP access"
metadata:
  source: ".specify/extensions/confluence/commands/read.md"
---

## User Input

```text
$ARGUMENTS
```

You MUST consider the user input before proceeding.

## Required Behavior

1. Read `.specify/extensions/confluence/commands/read.md` from the project root and follow its command instructions.
2. Require a valid Confluence URL in the input. If it is missing or is not a Confluence URL, ask the user for one.
3. Use available Confluence/Atlassian MCP tools or connector capabilities to fetch the page content.
4. Unless the user asks for a different action, summarize the page and identify whether it can seed or update Spec Kit artifacts.
5. Do not modify local files unless the user explicitly asks to create or update artifacts from the page.
6. Keep credentials out of files and logs. Ask for an Atlassian API token/email only if the configured MCP access is unavailable and the environment requires CLI credentials.

## Output

Report the page title/URL, a concise summary, and any suggested Spec Kit artifact updates.
