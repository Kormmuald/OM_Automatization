---
name: "speckit-jira-constitution-sync"
description: "Synchronize an approved Spec Kit constitution to a Jira Stage before specification."
compatibility: "Requires spec-kit project structure with .specify/ directory and optional Jira MCP server"
---

## Inputs

```text
$ARGUMENTS
```

## Required Behavior

1. Treat this as a background guard before specification. Keep user-facing output short and factual.
2. Load `.specify/project.yml` if it exists. Treat `integrations.jira.sync_enabled` as disabled when the file or key is missing.
3. Load `.specify/memory/constitution.md`.
4. Load `.specify/extensions/jira/jira-config.yml`.
5. Jira-facing language policy:
   - Write every Jira `summary`, `description`, and comment in Russian.
   - Preserve technical identifiers as-is: issue keys, file paths, field IDs, status tokens, command names, requirement IDs, URLs, and code identifiers.
   - If source content is already Russian, use it directly after rendering.
   - If generated fallback text is needed, write it in Russian. For example, use `Конституция Spec Kit` instead of `Spec Kit Constitution`.
   - Trace JSON may keep stable English machine tokens such as `synced`, `skipped`, `failed`, and reason codes.
6. Always write a trace event to `.specify/traces/jira/constitution-sync.json`.
7. A trace event MUST include:
   - ISO timestamp
   - hook name: `before_specify`
   - project settings path when present
   - config path
   - constitution path
   - action: `synced`, `skipped`, or `failed`
   - reason
   - Jira sync enabled flag when available
   - Jira project key when available
   - mapping path
   - constitution hash when available
   - Stage key when available
8. If `integrations.jira.sync_enabled` is not `true`, do not call Jira. Record `action: "skipped"` and reason `jira sync disabled by project settings`.
9. Read the approval status from the `## Статус согласования` section. The first non-empty line after that heading is the status.
10. If the status is not `Согласовано` or legacy `Approved`, do not call Jira. Record `action: "skipped"` and reason `constitution not approved`.
11. Jira is considered not configured when both `.project.key` in `jira-config.yml` and `SPECKIT_JIRA_PROJECT_KEY` are empty. If not configured, do not fail the Spec Kit workflow. Record `action: "skipped"` and reason `jira project not configured`.
12. Compute a stable SHA-256 hash of `.specify/memory/constitution.md`.
13. Use `.specify/jira-constitution-mapping.json` as the idempotency record and as the place to
    seed an already existing Jira constitution/Stage issue. Do not store an existing constitution
    issue key in `.specify/project.yml`; that file owns policy, while this mapping owns the
    external artifact identity. The mapping MUST include:
    - `project`
    - `issue_type: "Stage"` or the configured `mapping.constitution_artifact`
    - `stage_key`
    - `constitution_hash`
    - `synced_at`
    - `source: ".specify/memory/constitution.md"`
14. If mapping exists, `stage_key` exists, and `constitution_hash` matches the current hash, do not call Jira. Record `action: "skipped"` and reason `already synced`.
15. Before creating or updating Stage, render the Jira description:
    - If `stage.description_renderer` is `jira-wiki-business-case-panels`, parse the constitution into renderer placeholders and render `.specify/extensions/jira/renderers/constitution-stage.jira`.
    - Do not pass raw constitution Markdown to Jira when a renderer is configured.
    - If rendering fails, record `action: "failed"` with the missing placeholder or section summary.
16. If mapping exists with `stage_key` but hash changed or is missing, update the existing Stage
    issue description from the rendered constitution and update the mapping hash. This is the
    intended behavior when `/SpecKit Init` or a human has prefilled
    `.specify/jira-constitution-mapping.json` with an existing Jira issue key.
17. If mapping does not exist or has no `stage_key`, create a new Stage issue in project `TPL` (or configured project key) using:
    - issue type: `mapping.constitution_artifact` or `stage.issue_type` or `Stage`
    - summary: project name from `### Название проекта` when present; otherwise first H1; otherwise `Конституция Spec Kit`
    - description: rendered constitution content
    - labels/custom fields from `defaults.constitution`
18. Renderer placeholder mapping for `jira-wiki-business-case-panels`:
    - `project_name`: `### Название проекта`
    - `business_customer`: `### Бизнес-заказчик`
    - `problem_or_opportunity`: `### Проблема / Возможность`
    - `idea_description`: `### Описание идеи`
    - `effect_savings`: effect subsection or bullet containing `Экономия`; fallback to `### Предполагаемый эффект`
    - `effect_acceleration`: effect subsection or bullet containing `Ускорение`; fallback to `### Предполагаемый эффект`
    - `effect_new_capabilities`: effect subsection or bullet containing `Новые возможности`; fallback to `### Предполагаемый эффект`
    - `timeline`: `### Сроки`
    - `budget`: `### Бюджет`
    - `risks`: `### Риски`
    - `constraints_and_assumptions`: combine `### Ограничения` / `### Допущения` or fallback to `### Ограничения и допущения`
    - `communications`: `### Коммуникации`
    - Heading aliases MUST be supported for compatibility where the constitution was generated
      before Russian localization, especially `## Governance` / `## Управление`,
      `## Confluence root page` / `## Корневая страница Confluence`,
      and `## L2 Business Case` / `## Бизнес-кейс L2`.
19. If a mapped section is absent but the project class is `L1`, use `Не применимо для L1` for L2 Business Case fields. If the class is `L2` or higher, missing mapped sections are render failures.
20. If MCP tools are unavailable or the Jira operation fails, record `action: "failed"` with the error summary, but do not modify the constitution.

## Output

Report only whether constitution sync was synced, skipped, or failed, plus the trace path and Stage key when available.
