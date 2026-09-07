---
name: "speckit-confluence-background-spec-sync"
description: "Automatic Confluence background mirror hook for confirmed Spec Kit feature spec.md files."
compatibility: "Requires spec-kit project structure with .specify/ directory and optional Confluence MCP access"
metadata:
  source: ".specify/extensions/confluence/commands/background-spec-sync.md"
---

## Inputs

```text
$ARGUMENTS
```

## Required Behavior

1. Treat this as a background hook. Keep user-facing output short and factual.
2. Read `.specify/extensions/confluence/commands/background-spec-sync.md` from the project root and follow its command instructions exactly.
3. Synchronize only the active `specs/<feature>/spec.md` file. Do not include `plan.md`, constitution, classification, research, tasks, quickstart, contracts, or supporting docs in the Confluence page body.
4. Render the Confluence page as a Russian-language readable mirror. Translate human-facing prose from `spec.md` into Russian before publishing, but do not modify the local `spec.md`.
5. Preserve technical identifiers, requirement IDs, commands, file paths, URLs, code blocks, inline code, schema names, field names, status tokens, and source references exactly unless the source itself already contains a Russian equivalent.
6. Load `.specify/project.yml` if it exists. Treat `integrations.confluence.sync_enabled` as disabled when the file or key is missing.
7. Resolve the target spec directory in this order:
   - `--spec <name>` from `$ARGUMENTS`
   - current directory if it is inside `specs/<name>/`
   - current git branch after removing common prefixes `feature/`, `spec/`, `bugfix/`, `hotfix/`, `release/`
   - the only `specs/<name>/` directory containing `spec.md`
   - otherwise the most recently modified `specs/<name>/spec.md`
8. Always write a trace event to `specs/<name>/confluence-sync-trace.json` when a spec is resolved. If no spec can be resolved, write to `.specify/traces/confluence/background-spec-sync.json`.
9. Extract the Confluence root page URL from `.specify/project.yml` key
   `integrations.confluence.spec_root_page_url`. For legacy projects only, if that key is absent
   or empty, fall back to the `## Корневая страница Confluence` / `## Confluence root page`
   section of `.specify/memory/constitution.md`. Treat empty placeholders, `Не используется`,
   `Not used`, and non-Confluence URLs as not configured.
10. If Confluence sync is disabled or the root page is not configured, do not call Confluence. Record `action: "skipped"` with the precise reason.
11. Compute a stable source hash from the original UTF-8 bytes of `specs/<name>/spec.md`, before translation.
12. Use `specs/<name>/confluence-mapping.json` as the local mapping file with this shape:
    - `mode: "spec-page"`
    - `page_id`
    - `page_url`
    - `parent_page_url`
    - `space_key` when available
    - `source_path: "specs/<feature>/spec.md"`
    - `source_hash`
    - `last_synced_at`
    - `last_action: "created" | "updated" | "skipped" | "failed"`
13. If the mapping exists and the hash is unchanged, do not call Confluence. Update the mapping timestamp/action to `skipped`, write the trace, and report `skipped`.
14. If the mapping exists, update the mapped page; never create a duplicate page from the automatic hook.
15. If the mapping does not exist, create a child page under the configured root page and save the resulting page link in the mapping.
16. Build the page title as `<feature directory>: <spec title>`, using the first Markdown H1 in `spec.md` translated to Russian as needed, and falling back to the feature directory name.
17. Page content must include a small Russian service block with source path, sync timestamp, source hash, and a notice meaning `Confluence is a mirror; source of truth is repository spec.md`.
18. Updates use Replace spec body mode: preserve only the service block and required Confluence page wrapper, then replace the mirrored body with the Russian rendering of the current `spec.md`.
19. Fetch remote page metadata before updating when possible and record remote version/title in the trace for drift detection. Do not block on drift.
20. If MCP tools are unavailable or the Confluence operation fails, record `action: "failed"` with the error summary, but do not fail the Plan workflow or undo generated Spec Kit files.

## Output

Report only the trace path, mapping path when known, and whether Confluence spec sync was created, updated, skipped, or failed.
