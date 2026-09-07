# Corporate Spec Kit Agent Instructions

## Project Rules

### Artifact Policy

Creating and updating project artifacts is authorized.

### Draft Preservation

The authoritative source drafts for GitHub Spec Kit are in
`preparation/docs/product-specs/`. Do not rename, move, edit, delete, or
overwrite any file in this directory. Use these files only as source material
when creating or updating Spec Kit artifacts.

Keep Spec Kit outputs separate from the source drafts:

- Constitution: `.specify/memory/constitution.md`
- Feature specification and its follow-on artifacts: `specs/<feature>/`

When invoking a Spec Kit skill, explicitly name the relevant source-draft
path and state that it is immutable. Never set `SPECIFY_FEATURE_DIRECTORY` to
`preparation/docs/product-specs/`.

## Corporate Lifecycle Routing

When the user asks in natural language to run the full lifecycle for the current specification, continue the current specification to completion, finish the current feature, or otherwise perform an end-to-end Spec Kit cycle, interpret the request through the corporate class-aware workflow, not through the stock Spec Kit `Full SDD Cycle`.

This applies to Russian and English phrasings, including but not limited to: "пройди весь жизненный цикл для текущей спецификации", "выполни полный цикл", "доведи текущую спецификацию до конца", "заверши текущую спецификацию", "run the full lifecycle", "complete the current spec", and "finish the current feature".

For such requests:

1. Read `.specify/project.yml` first.
2. Use `initiative.class` and its selected workflow overlay/profile from `.specify/project.yml` as the authoritative routing source.
3. Load `.specify/workflows/speckit/class-workflow.yml`.
4. Load the selected overlay from `.specify/workflows/overlays/speckit/<selected-overlay>.yml`.
5. Determine the current feature from `.specify/feature.json` and existing `specs/<feature>/` artifacts.
6. Follow the selected class `required_commands` order from `class-workflow.yml`, starting at the first required command whose required inputs/artifacts are missing or stale for the current feature.
7. Respect all mandatory hooks from `.specify/extensions.yml`; do not bypass `speckit-class-gate`, `speckit.critique.run`, `speckit.converge`, `speckit.verify.run`, `speckit.archive.run`, Jira hooks, or Confluence hooks when they apply.
8. If the class, overlay, current feature, or required prior artifact cannot be determined, stop and explain the missing prerequisite in Russian instead of falling back to the stock workflow.

The stock `.specify/workflows/speckit/workflow.yml` is an upstream baseline reference only for this template. It must not override the class-specific workflow selected in `.specify/project.yml`.

## User-Facing Chat Language

All user-facing text emitted into the Codex chat window MUST be in Russian regardless of the document language configured in `.specify/project.yml`.

This includes progress notes, questions, completion reports, summaries, warnings, next actions, and error explanations.

Preserve technical identifiers exactly when translating would change their meaning: command names, skill names, file paths, URLs, code identifiers, JSON/YAML keys, branch names, Jira keys, requirement IDs, status tokens used by integrations, and literal user-provided text.

## Document Language Scope

The document language configured in `project.document_language` in `.specify/project.yml` applies only where a specific skill says it applies. In the corporate template, it applies to documents created by `/SpecKit Constitution` and `/SpecKit Specify`.

For legacy projects only, a skill may fall back to the old document language section in `.specify/memory/constitution.md` when `.specify/project.yml` or `project.document_language` is missing.

Documents created by other skills keep their own artifact language policy. This does not change the Russian chat-output rule above.
