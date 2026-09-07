---
title: Background Spec Sync to Confluence
description: Automatically create or update a Confluence mirror page for the active Spec Kit feature spec.md before planning.
---

## Usage and Functionality

This command is intended to be invoked only by `.specify/extensions.yml` as an automatic `before_plan` hook. It treats the user's transition to `/SpecKit Plan` as confirmation that the current `specs/<feature>/spec.md` should be mirrored to Confluence.

The command synchronizes only the active feature specification file. It MUST NOT include `plan.md`, constitution, classification, research, tasks, quickstart, contracts, or supporting docs in the Confluence page body.

The Confluence page is a Russian-language readable mirror. Translate human-facing prose from `spec.md` into Russian before publishing. Preserve technical identifiers, requirement IDs, commands, file paths, URLs, code blocks, inline code, schema names, field names, status tokens, and source references exactly unless the source itself already contains a Russian equivalent. Do not modify the local `spec.md`.

## Required Behavior

1. Load `.specify/project.yml` if it exists. Treat `integrations.confluence.sync_enabled` as disabled when the file or key is missing.
2. Resolve the target spec directory in this order:
   - `--spec <name>` from user input
   - current directory if it is inside `specs/<name>/`
   - current git branch after removing common prefixes `feature/`, `spec/`, `bugfix/`, `hotfix/`, `release/`
   - the only `specs/<name>/` directory containing `spec.md`
   - otherwise the most recently modified `specs/<name>/spec.md`
3. Always write a trace event to `specs/<name>/confluence-sync-trace.json` when a spec is resolved. If no spec can be resolved, write to `.specify/traces/confluence/background-spec-sync.json`.
4. A trace event MUST include ISO timestamp, hook name `before_plan`, spec name when known, project settings path when present, action (`created`, `updated`, `skipped`, or `failed`), reason, Confluence sync enabled flag when available, root page URL when available, mapping path when available, source path when available, source hash when available, and remote page version/title when available.
5. If `integrations.confluence.sync_enabled` is not `true`, do not call Confluence. Record `action: "skipped"` and reason `confluence sync disabled by project settings`.
6. Extract the Confluence root page URL from `.specify/project.yml` key `integrations.confluence.spec_root_page_url`. For legacy projects only, if that key is absent or empty, read `.specify/memory/constitution.md` and fall back to the `## Корневая страница Confluence` / `## Confluence root page` section. Treat empty placeholders, `Не используется`, `Not used`, and non-Confluence URLs as not configured.
7. If no valid root page URL is configured, do not call Confluence. Record `action: "skipped"` and reason `confluence root page not configured in project settings`.
8. Read only `specs/<name>/spec.md` as the source body. Compute a stable content hash from its original UTF-8 bytes before translation.
9. Use `specs/<name>/confluence-mapping.json` as the local mapping file. The mapping shape is:
   - `mode: "spec-page"`
   - `page_id`
   - `page_url`
   - `parent_page_url`
   - `space_key` when available
   - `source_path: "specs/<feature>/spec.md"`
   - `source_hash`
   - `last_synced_at`
   - `last_action: "created" | "updated" | "skipped" | "failed"`
10. If the mapping exists and its `source_hash` equals the current hash, do not call Confluence. Update the mapping timestamp/action to `skipped`, record `reason: "content unchanged"`, and write the trace.
11. If the mapping exists, update the mapped page by `page_id` or `page_url`. Never create a duplicate page unless the user explicitly provides an override outside the automatic hook.
12. If the mapping does not exist, create a child page under the configured root page.
13. Build the page title deterministically as `<feature directory>: <spec title>`, where `spec title` comes from the first Markdown H1 in `spec.md`; fall back to the feature directory name.
14. Render the Confluence spec body in Russian. Keep Markdown structure, headings, numbered IDs, tables, checklists, code blocks, and traceability markers stable; translate only reader-facing prose.
15. The Confluence page content MUST contain a Russian service block with source path, sync timestamp, source hash, and a notice meaning `Confluence is a mirror; source of truth is repository spec.md`.
16. Replace the mirrored spec body on every update. Preserve only the service block and any stable page wrapper needed by Confluence; do not merge sections selectively.
17. Before update, fetch the existing page metadata/content when possible and record the observed remote version/title in the trace for drift detection. Drift never blocks the update; the spec body is still replaced.
18. If MCP tools are unavailable or the Confluence operation fails, record `action: "failed"` with the error summary, but do not fail the Plan workflow or undo generated Spec Kit files.

## Output

Report only the trace path, mapping path when known, and whether Confluence spec sync was created, updated, skipped, or failed.
