# Spec Kit Customizations

This file records intentional differences from the stock Spec Kit project layout and from installed upstream extensions. Keep it updated whenever the template changes behavior, files from upstream are patched, or local wrapper skills are added.

## Update Rule

For every customization, record:

- date
- source baseline, when known
- what changed
- why it changed
- affected files
- update risk when pulling upstream changes

## 2026-09-03 - Constitution-Driven Init Class Recommendation

Baseline: `/SpecKit Init` owned initiative class selection, but the skill still described the
initial class as a required question. It only said to recommend a class when facts were sufficient,
which allowed the agent to ask the user to choose `L1`, `L2`, or `L3` even when the constitution
already contained enough classification evidence.

Changed:

- Added a mandatory pre-question class analysis step to `/SpecKit Init`.
- Required Init to read `.specify/memory/constitution.md` and
  `.specify/workflows/speckit/class-workflow.yml`, extract classification facts, compare them to
  class signals and promotion triggers, and recommend one concrete class.
- Required Init to write the recommended class into `.specify/project.yml` without asking for class
  choice when confidence is `high` or `medium`.
- Limited class questions to missing/conflicting classification facts or explicit user override.
- Updated `README.md` and bumped methodology version to `1.3.10`.
- Updated `scripts/update-methodology.ps1` to rebuild `.specify/integrations/codex.manifest.json` from `.agents/skills/*/SKILL.md` after methodology upgrades.
- Refreshed the template `codex.manifest.json` so all current Spec Kit skills are listed.
- Updated `README.md` and bumped methodology version to `1.3.11`.

Why:

- Initiative class is a governance decision derived from the constitution, not a preference the
  user should select manually by default.
- The active contour removed `/SpecKit Classify`, so Init must fully own classification analysis
  rather than showing the old class-selection prompt.
- Conservative automatic recommendation keeps workflow routing deterministic while still allowing
  human correction when the constitution is incomplete or contradictory.

Affected files:

- `.agents/skills/speckit-init/SKILL.md`
- `README.md`
- `SPEC_KIT_CUSTOMIZATIONS.md`

Update risk:

- High. Future changes to Init prompts must preserve constitution-driven class recommendation and
  must not reintroduce a default "choose class" question before analyzing the constitution.

## 2026-09-03 - Natural-Language Corporate Lifecycle Routing

Baseline: the template had class-aware command gates and hooks, but root agent instructions did not
explicitly say how to interpret broad natural-language requests such as "пройди весь жизненный
цикл для текущей спецификации". The upstream `.specify/workflows/speckit/workflow.yml` still
describes the stock `specify -> plan -> tasks -> implement` cycle.

Changed:

- Added a root `AGENTS.md` rule for full-lifecycle natural-language requests.
- Required the agent to read `.specify/project.yml`, use `initiative.class` and the selected
  workflow overlay/profile, then follow the class-specific `required_commands` from
  `.specify/workflows/speckit/class-workflow.yml`.
- Explicitly blocked fallback to the stock `Full SDD Cycle` when the class, overlay, feature, or
  prerequisite artifacts cannot be determined.
- Required mandatory hooks from `.specify/extensions.yml` to remain in force during the lifecycle.

Why:

- Users reasonably ask for the whole lifecycle in natural language rather than naming every command.
- The corporate lifecycle is longer than the stock Spec Kit workflow and depends on the selected
  initiative class.
- The agent must choose the class-aware workflow deterministically instead of inferring the shorter
  upstream cycle.

Affected files:

- `AGENTS.md`
- `SPEC_KIT_CUSTOMIZATIONS.md`

Update risk:

- Medium. Future updates to upstream workflow files or root agent instructions must preserve the
  rule that natural-language lifecycle requests route through `.specify/project.yml` and the
  selected class overlay.

## 2026-09-03 - Clickable Init Options

Baseline: `/SpecKit Init` asked for missing runtime settings as a concise text batch, while
`/SpecKit Clarify` already supported Codex structured choice prompts with clickable options in
Plan mode.

Changed:

- Updated `/SpecKit Init` to show a visible startup note explaining that button-based answers are
  available through Codex `"План"` mode.
- Required Init questions to use the same visible question/recommendation/options pattern as
  Clarify.
- Required Plan-mode use of the real `request_user_input` structured choice tool when available,
  with Markdown option tables as the fallback for Default or non-interactive modes.
- Documented the button-capable Init behavior in `README.md`.
- Updated methodology version to `1.3.9`.

Why:

- Init collects routine project settings that are easier and less error-prone to answer with
  selectable options.
- Users need the same interaction model for project initialization and feature clarification.
- Non-Plan and plain text runs still need a complete visible question and answer format.

Affected files:

- `.agents/skills/speckit-init/SKILL.md`
- `README.md`
- `SPEC_KIT_CUSTOMIZATIONS.md`

Update risk:

- Medium. Future changes to `/SpecKit Init` must preserve the structured-choice interaction rule
  and the visible Plan-mode startup hint.

## 2026-09-03 - Explicit Specify and L1 Archive Workflow Gates

Baseline: corporate workflow overlays had `/SpecKit Init` as the class-selection step and treated
the first feature spec as part of Init behavior in the local wrapper. L1 archival was not mandatory.

Changed:

- Made `/SpecKit Specify` an explicit required command immediately after `/SpecKit Init` for L1,
  L2, and L3.
- Updated `/SpecKit Init` so it owns project runtime settings and class selection only; it no
  longer creates `specs/<feature>/spec.md` or runs specify hooks.
- Updated `/SpecKit Specify` wording so it is the required feature-specification step after Init,
  not a legacy Init implementation detail.
- Made `speckit.archive.run` mandatory for L1 after `/SpecKit Implement`.
- Kept `/SpecKit Converge` and `speckit.verify.run` mandatory for L2/L3 before archive.

Why:

- A workflow that requires `spec.md` must require the command that creates it.
- Init and Specify have different ownership: project runtime/class settings versus feature
  specification.
- Even L1 outcomes should be archived so the PoC learning/result is preserved before the human
  continue/stop/promote decision.

Affected files:

- `.agents/skills/speckit-init/SKILL.md`
- `.agents/skills/speckit-specify/SKILL.md`
- `.agents/skills/speckit-class-gate/SKILL.md`
- `.agents/skills/speckit-implement/SKILL.md`
- `.specify/extensions.yml`
- `.specify/workflows/speckit/class-workflow.yml`
- `.specify/workflows/overlays/speckit/l1-proof-of-concept.yml`
- `.specify/workflows/overlays/speckit/l2-pilot.yml`
- `.specify/workflows/overlays/speckit/l3-production-solution.yml`
- `README.md`
- `SPEC_KIT_CUSTOMIZATIONS.md`

Update risk:

- Medium/High. Future Spec Kit updates must preserve the separation between Init and Specify and
  the class-aware post-implement hook order.

## 2026-09-03 - Chat Output Language Uses Project Settings Boundary

Baseline: methodology version `1.3.8` moved runtime project settings into
`.specify/project.yml`, but root agent instructions and the process navigator still described the
chat language rule in terms of the legacy constitution language location.

Changed:

- Updated root `AGENTS.md` to name `.specify/project.yml` and `project.document_language` as the
  active document language setting.
- Kept the corporate rule that all Codex chat output is Russian regardless of document language.
- Added a legacy fallback note for projects that do not yet have `.specify/project.yml` or
  `project.document_language`.
- Made `speckit-process-navigator` read `project.document_language` explicitly and clarified that
  it affects only document-generating next steps, not the language of the navigation block itself.

Why:

- Advisory after-hooks such as `speckit.process.navigator` should follow the same project settings
  boundary as the rest of the migrated template.
- Users need consistent Russian recommendations in chat even when generated project documents use
  another language.

Affected files:

- `AGENTS.md`
- `.agents/skills/speckit-process-navigator/SKILL.md`
- `SPEC_KIT_CUSTOMIZATIONS.md`

Update risk:

- Low. Future language-policy changes must preserve the separation between chat output language
  and document artifact language.

## 2026-09-03 - Init Mandatory for Every Initiative Class

Baseline: methodology version `1.3.7` removed `Classify` and made `Init` the owner of class
selection, but the gate wording still allowed an ambiguous interpretation where class state could
be required before Init itself.

Changed:

- Made `/SpecKit Init` explicitly mandatory in all `L1`, `L2`, and `L3` required command lists.
- Clarified `speckit-class-gate` behavior: for `/SpecKit Init`, check constitution readiness and
  approval, but do not require `initiative.class` yet because Init writes it; for all later
  class-aware commands, require `initiative.class` and workflow overlay from `.specify/project.yml`.
- Fixed the `class-workflow.yml` navigation indentation while touching the workflow contract.
- Updated methodology version to `1.3.8`.

Why:

- Init is the project initialization boundary for language, integration policy, Confluence root,
  Jira constitution mapping seed, and initiative class.
- Requiring class state before Init would make the intended order impossible:
  `/SpecKit Constitution` -> `/SpecKit Init`.

Affected files:

- `.agents/skills/speckit-class-gate/SKILL.md`
- `.specify/workflows/speckit/class-workflow.yml`
- `.specify/workflows/overlays/speckit/l1-proof-of-concept.yml`
- `.specify/workflows/overlays/speckit/l2-pilot.yml`
- `.specify/workflows/overlays/speckit/l3-production-solution.yml`
- `README.md`
- `SPEC_KIT_CUSTOMIZATIONS.md`

Update risk:

- Medium. Future gate or workflow changes must preserve the distinction between allowing Init to
  create class state and requiring that class state after Init.

## 2026-09-03 - Init Fully Replaces Classify

Baseline: methodology version `1.3.6` introduced `speckit-init` but still kept
`speckit-classify`, `.specify/classification.yml`, and `.specify/memory/initiative-class.md` as
separate class decision surfaces.

Changed:

- Removed the `speckit-classify` skill from the active template.
- Removed `.specify/classification.yml` and `.specify/memory/initiative-class.md` from the active
  template.
- Made `/SpecKit Init` the only owner of initiative class selection and later class changes.
- Updated class gate, process navigator, plan, implement, Confluence wrappers, workflows, README,
  update instructions, and update script to remove active dependencies on Classify artifacts.
- Updated methodology version to `1.3.7`.

Why:

- A separate Classify step conflicts with the intended order `/SpecKit Constitution` ->
  `/SpecKit Init`.
- Keeping two class decision surfaces would create drift between Init, Classify,
  `.specify/project.yml`, and legacy trace files.
- Project class is runtime project state and belongs in `.specify/project.yml`.

Affected files:

- `.agents/skills/speckit-classify/`
- `.specify/classification.yml`
- `.specify/memory/initiative-class.md`
- `.agents/skills/speckit-init/SKILL.md`
- `.agents/skills/speckit-class-gate/SKILL.md`
- `.agents/skills/speckit-process-navigator/SKILL.md`
- `.agents/skills/speckit-plan/SKILL.md`
- `.agents/skills/speckit-implement/SKILL.md`
- `.agents/skills/speckit-confluence-write/SKILL.md`
- `.agents/skills/speckit-confluence-update/SKILL.md`
- `.agents/skills/speckit-constitution/SKILL.md`
- `.specify/workflows/speckit/class-workflow.yml`
- `.specify/workflows/overlays/speckit/l1-proof-of-concept.yml`
- `.specify/workflows/overlays/speckit/l2-pilot.yml`
- `.specify/workflows/overlays/speckit/l3-production-solution.yml`
- `scripts/update-methodology.ps1`
- `README.md`
- `UPDATE_FROM_TEMPLATE.md`
- `SPEC_KIT_CUSTOMIZATIONS.md`

Update risk:

- High. Future upstream or local changes must not reintroduce `speckit-classify`,
  `.specify/classification.yml`, or `.specify/memory/initiative-class.md` as active workflow
  dependencies.

## 2026-09-03 - Project Init Owns Runtime Settings

Baseline: methodology version `1.3.5` stored document language, initiative class, and Confluence
root page in `.specify/memory/constitution.md`, while `.specify/project.yml` only stored
integration enablement flags.

Changed:

- Added `speckit-init` as the project initialization step after `/SpecKit Constitution`, including
  initial class selection.
- Expanded `.specify/project.yml` to schema `1.1` with `project.document_language`,
  `initiative.*`, and `integrations.confluence.spec_root_page_url`.
- Removed document language, initiative class, and Confluence root page sections from the active
  constitution template and template memory constitution.
- Updated `/SpecKit Init` to write selected class into `.specify/project.yml`.
- Updated class gate, process navigator, plan, implement, specify/init, and Confluence sync
  instructions to read project settings first and use old constitution sections only as legacy
  fallback.
- Clarified that an already existing Jira constitution issue is seeded in
  `.specify/jira-constitution-mapping.json`, not in `.specify/project.yml`, so Jira sync updates
  the target Stage instead of creating a duplicate.
- Updated `scripts/update-methodology.ps1` to migrate legacy language and Confluence root values
  into `.specify/project.yml` instead of adding runtime sections to preserved constitutions.
- Updated methodology version to `1.3.6`.

Why:

- Runtime settings are not constitution content. Language, integration routing, and selected
  workflow class need a machine-readable project policy surface that can grow without overloading
  governance markdown.
- Jira issue identity is mapping/idempotency state, not general project policy.
- Keeping old constitution sections as fallback preserves existing projects while making the new
  source of truth explicit.

Affected files:

- `.specify/project.yml`
- `.specify/templates/constitution-template.md`
- `.specify/memory/constitution.md`
- `.agents/skills/speckit-init/SKILL.md`
- `.agents/skills/speckit-constitution/SKILL.md`
- `.agents/skills/speckit-class-gate/SKILL.md`
- `.agents/skills/speckit-process-navigator/SKILL.md`
- `.agents/skills/speckit-specify/SKILL.md`
- `.agents/skills/speckit-plan/SKILL.md`
- `.agents/skills/speckit-implement/SKILL.md`
- `.agents/skills/speckit-jira-constitution-sync/SKILL.md`
- `.agents/skills/speckit-confluence-background-spec-sync/SKILL.md`
- `.agents/skills/speckit-confluence-write/SKILL.md`
- `.specify/extensions/confluence/commands/background-spec-sync.md`
- `.specify/extensions/confluence/commands/write.md`
- `.specify/extensions/confluence/README.md`
- `.specify/workflows/speckit/class-workflow.yml`
- `.specify/workflows/overlays/speckit/l1-proof-of-concept.yml`
- `.specify/workflows/overlays/speckit/l2-pilot.yml`
- `.specify/workflows/overlays/speckit/l3-production-solution.yml`
- `scripts/update-methodology.ps1`
- `README.md`
- `UPDATE_FROM_TEMPLATE.md`
- `SPEC_KIT_CUSTOMIZATIONS.md`

Update risk:

- High. Future upstream refreshes can reintroduce document-language or class parsing from
  constitution markdown. Preserve `.specify/project.yml` as the primary settings source and keep
  legacy constitution parsing only for migration/fallback.

## 2026-09-03 - Classify Criteria Split from Workflow Overlays

Baseline: `speckit-classify` treated `references/class-workflow.md` as the source of truth for
class definitions, promotion triggers, workflow commands, and required artifacts.

Changed:

- Removed the duplicated `Commands By Class` and `Artifacts By Class` sections from the
  `speckit-classify` class workflow reference.
- Kept `class-workflow.md` focused on L1/L2/L3 classification criteria and promotion triggers.
- Clarified that workflow commands, automatic hooks, human decisions, and required artifacts are
  defined by the project overlays in `.specify/workflows/overlays/speckit/`.
- Updated the `speckit-classify` skill output contract so it reports the selected workflow overlay
  instead of inventing command and artifact lists from the classification reference.

Why:

- Class selection and workflow routing are separate concerns.
- The project already stores class-specific SpecKit routing in overlay files such as
  `l1-proof-of-concept.yml`, `l2-pilot.yml`, and `l3-production-solution.yml`.
- Keeping command and artifact lists in both places creates drift and can make non-existent
  commands look mandatory.

Affected files:

- `.agents/skills/speckit-classify/SKILL.md`
- `.agents/skills/speckit-classify/references/class-workflow.md`
- `.specify/workflows/overlays/speckit/l1-proof-of-concept.yml`
- `.specify/workflows/overlays/speckit/l2-pilot.yml`
- `.specify/workflows/overlays/speckit/l3-production-solution.yml`
- `SPEC_KIT_CUSTOMIZATIONS.md`

Update risk:

- Medium. Future changes to classification criteria must stay in `class-workflow.md`, while
  changes to command order, hooks, human decisions, or required artifacts must stay in the overlay
  YAML files.

## 2026-09-03 - Semantic User Story Count for Specify

Baseline: methodology version `1.3.3` used a Russian `spec-template.md` that still contained
three ready-made user story sections inherited from the upstream Spec Kit example structure.

Changed:

- Updated `/SpecKit Specify` instructions so generated `spec.md` files do not target exactly
  three `User Story` sections by default.
- Required the number of user stories to follow the feature semantics: one standalone user
  capability produces one user story, more than three standalone capabilities must all be listed.
- Required setup, error handling, and administration to become separate user stories only when
  they provide standalone user value and an independently testable increment.
- Reduced `.specify/templates/spec-template.md` to one concrete story example plus an explicit
  instruction to add further stories only when needed.
- Recorded in methodology version `1.3.5`.

Why:

- The stock Spec Kit template uses three user story placeholders as examples, but corporate
  users interpreted that as a target count.
- Artificially forcing three stories creates invented scope for small features, while stopping at
  three can drop real user capabilities for larger features.
- Corporate Story artifacts and Spec Kit `User Story` slices are related but not guaranteed to be
  one-to-one.

Affected files:

- `.agents/skills/speckit-specify/SKILL.md`
- `.specify/templates/spec-template.md`
- `README.md`
- `SPEC_KIT_CUSTOMIZATIONS.md`

Update risk:

- Medium. Future upstream Spec Kit template or skill refreshes may reintroduce three placeholder
  user stories; preserve the semantic-count rule when rebasing.

## 2026-09-03 - Russian Chat Output via AGENTS.md

Baseline: methodology version `1.3.4` had Russian report rules for selected commands, while some
new or upstream-refreshed skills could still emit user-facing Codex chat output in English unless
each `SKILL.md` was patched individually.

Changed:

- Added root `AGENTS.md` with a `## User-Facing Chat Language` policy.
- Required all skill output emitted into the Codex chat window to be in Russian regardless of
  `.specify/memory/constitution.md` document language.
- Preserved stable technical identifiers from translation: command names, skill names, file paths,
  URLs, code identifiers, JSON/YAML keys, branch names, Jira keys, requirement IDs, integration
  status tokens, and literal user-provided text.
- Added `AGENTS.md` to `scripts/update-methodology.ps1` so active projects receive the global
  instruction during methodology updates.
- Removed the duplicated `## User-Facing Chat Language` section from individual
  `.agents/skills/*/SKILL.md` files.
- Updated methodology version to `1.3.5`.

Why:

- The constitution language setting controls selected generated documents, not the language of
  the assistant's chat interaction with corporate users.
- Users should receive consistent Russian explanations, questions, warnings, and completion
  reports even when a project chooses English artifacts.
- The policy must survive new skills and upstream skill refreshes without requiring every
  `SKILL.md` to be manually patched.

Affected files:

- `AGENTS.md`
- `scripts/update-methodology.ps1`
- `README.md`
- `UPDATE_FROM_TEMPLATE.md`
- `SPEC_KIT_CUSTOMIZATIONS.md`

Update risk:

- Low/Medium. Future project templates must continue shipping root `AGENTS.md` and the update
  script must continue copying it. Individual upstream skill files no longer need a local patch for
  this rule.

## 2026-09-03 - Constitution and Specify Document Language

Baseline: methodology version `1.3.1` generated the constitution mostly in Russian but kept
English section labels, status values, normative words, and the feature specification template.

Changed:

- Added `## Язык документов` to the constitution template and project memory constitution.
- Made Russian the default document language for `/SpecKit Constitution` and `/SpecKit Specify`,
  with `English` as the explicit alternative.
- Translated the default `spec-template.md` to Russian, including metadata labels, `Дано` /
  `Когда` / `Тогда`, `ОБЯЗАНА`, and `ДОЛЖНЫ`.
- Updated `/SpecKit Specify` so its generated `spec.md` and `checklists/requirements.md` follow
  the constitution language setting.
- Kept other Spec Kit skills and their generated artifacts in English unless their own skill says
  otherwise.
- Added compatibility rules for Russian headings and statuses in class gate, Jira constitution
  sync, Confluence root page lookup, and archive status updates.
- Updated `scripts/update-methodology.ps1` so preserved project constitutions receive the missing
  document language section.

Why:

- Mixed Russian/English artifacts are hard for corporate users to read.
- The language choice must be explicit at constitution time and must not break automation that
  previously keyed on English fragments such as `Status`.

Affected files:

- `.specify/templates/constitution-template.md`
- `.specify/memory/constitution.md`
- `.specify/templates/spec-template.md`
- `.agents/skills/speckit-constitution/SKILL.md`
- `.agents/skills/speckit-specify/SKILL.md`
- `.agents/skills/speckit-class-gate/SKILL.md`
- `.agents/skills/speckit-jira-constitution-sync/SKILL.md`
- `.agents/skills/speckit-confluence-background-spec-sync/SKILL.md`
- `.agents/skills/speckit-archive-run/SKILL.md`
- `.specify/extensions/confluence/commands/background-spec-sync.md`
- `.specify/extensions/confluence/commands/write.md`
- `.specify/extensions/archive/commands/archive.md`
- `scripts/update-methodology.ps1`
- `README.md`
- `UPDATE_FROM_TEMPLATE.md`
- `SPEC_KIT_CUSTOMIZATIONS.md`

Update risk:

- Medium/High. Future upstream Spec Kit updates can reintroduce English-only templates or
  parsers. Preserve Russian/English aliases for user-facing document headings and status fields,
  while keeping machine identifiers such as `FR-001`, `SC-001`, paths, commands, and JSON keys
  stable.

## 2026-09-03 - Russian Detailed Plan Completion Report

Baseline: stock Spec Kit `speckit-plan` completion report only required branch, implementation plan path, and generated artifact list.

Changed:

- Required `/SpecKit Plan` to report completion in Russian regardless of `.specify/memory/constitution.md` language settings.
- Expanded the completion report contract so the user receives a readable summary of completed planning work, key architecture decisions, rationale, rejected alternatives where available, artifact paths, Constitution Check/gate results, mandatory hook outcomes, risks, assumptions, plan gaps, and notes before `/SpecKit Tasks`.
- Kept the full `plan.md` as the source artifact while preventing the user-facing answer from becoming a pasted copy of the whole plan.
- Updated methodology version to `1.3.2`.

Why:

- Vibe coders often skip large planning documents, but they still need to understand architectural decisions before task generation and implementation.
- The planning command is the right moment to surface decisions, tradeoffs, risks, and next-step constraints without forcing the user to read the entire generated plan.
- Russian reporting must be stable for the corporate methodology even when a project constitution uses another language.

Affected files:

- `.agents/skills/speckit-plan/SKILL.md`
- `README.md`
- `SPEC_KIT_CUSTOMIZATIONS.md`

Update risk:

- Medium. Future upstream changes to `templates/commands/plan.md` or generated `speckit-plan` wrappers must preserve the corporate Russian completion-report contract.

## 2026-09-03 - Roll Back TDD Extension

Baseline: methodology version `1.3.0` included local `spec-kit-tdd` v1.1.2 as a vendored extension with Codex command wrappers and automatic hooks around task generation, implementation, and post-implementation verification.

Changed:

- Removed `tdd` from `.specify/extensions.yml` installed extensions.
- Removed automatic `speckit.tdd.plan`, `speckit.tdd.run`, and `speckit.tdd.verify` hooks.
- Removed the `tdd` registry entry from `.specify/extensions/.registry`.
- Removed the vendored `.specify/extensions/tdd/` extension directory.
- Removed generated Codex skill wrappers `speckit-tdd-setup`, `speckit-tdd-plan`, `speckit-tdd-run`, and `speckit-tdd-verify`.
- Removed the explicit TDD execution instruction from `speckit-implement`.
- Updated methodology version to `1.3.1`.

Why:

- The TDD extension currently blocks parts of the corporate workflow and needs a separate evaluation before it becomes a default methodology component.
- Default projects should keep mandatory corporate test/verification tasks, but should not auto-enforce the red-green-refactor extension flow.

Affected files:

- `.specify/extensions.yml`
- `.specify/extensions/.registry`
- `.specify/extensions/tdd/`
- `.agents/skills/speckit-tdd-setup/`
- `.agents/skills/speckit-tdd-plan/`
- `.agents/skills/speckit-tdd-run/`
- `.agents/skills/speckit-tdd-verify/`
- `.agents/skills/speckit-implement/SKILL.md`
- `README.md`
- `SPEC_KIT_CUSTOMIZATIONS.md`

Update risk:

- Medium. If `spec-kit-tdd` is reintroduced later, install it deliberately and keep it opt-in until the hook behavior is proven compatible with the corporate L1/L2/L3 workflow.

## 2026-08-26 - Confluence Spec Mirror Before Planning

Baseline: embedded `spec-kit-confluence` v1.1.1 exposed manual `write`, `read`, and `update` commands that synthesized specification and planning artifacts into Confluence design docs.

Changed:

- Added `## Confluence root page` to the constitution template and project memory constitution.
- Added mandatory `hooks.before_plan` entry for `speckit.confluence.background-spec-sync`.
- Added `speckit-confluence-background-spec-sync` Codex skill wrapper.
- Added `.specify/extensions/confluence/commands/background-spec-sync.md`.
- Updated Confluence `write` and `update` wrappers to mirror only `specs/<feature>/spec.md`.
- Required Confluence mirror pages to be rendered in Russian while preserving technical identifiers, paths, commands, URLs, code blocks, IDs, and source references.
- Added the `specs/<feature>/confluence-mapping.json` and `specs/<feature>/confluence-sync-trace.json` runtime contract.
- Updated `scripts/update-methodology.ps1` so preserved project constitutions receive the missing `## Confluence root page` section during methodology updates.
- Updated `scripts/update-methodology.ps1` so future updates also copy the root `scripts/` folder and `UPDATE_FROM_TEMPLATE.md` into active projects.
- Updated README and vendored Confluence extension docs with the spec-only mirror policy.
- Updated methodology version to `1.3.0`.

Why:

- Confluence should be a readable mirror of confirmed feature specifications, not a second source of truth or a synthesized design document.
- Moving from `/SpecKit Specify` to `/SpecKit Plan` is the practical confirmation signal for publishing `spec.md`.
- The created Confluence page URL must be discoverable locally in the same way Jira issue links are discoverable from `jira-mapping.json`.

Affected files:

- `.specify/extensions.yml`
- `.specify/templates/constitution-template.md`
- `.specify/memory/constitution.md`
- `.specify/extensions/confluence/extension.yml`
- `.specify/extensions/confluence/commands/background-spec-sync.md`
- `.specify/extensions/confluence/commands/write.md`
- `.specify/extensions/confluence/commands/update.md`
- `.specify/extensions/confluence/README.md`
- `.specify/extensions/confluence/CHANGELOG.md`
- `.agents/skills/speckit-confluence-background-spec-sync/SKILL.md`
- `.agents/skills/speckit-confluence-write/SKILL.md`
- `.agents/skills/speckit-confluence-update/SKILL.md`
- `scripts/update-methodology.ps1`
- `UPDATE_FROM_TEMPLATE.md`
- `README.md`
- `SPEC_KIT_CUSTOMIZATIONS.md`

Update risk:

- Medium/High. Preserve the corporate spec-only mirror behavior, `before_plan` hook, and local mapping/trace contract when updating vendored `spec-kit-confluence`.

## 2026-08-26 - Jira Done Transition After Spec Archival

Baseline: archive completed project memory consolidation, while Jira background hooks only created the Stage/Epic projection and synchronized status after implementation.

Changed:

- Added mandatory `hooks.after_archive` entry for `speckit.jira.complete-spec`.
- Added `speckit-jira-complete-spec` Codex skill wrapper.
- Added `.specify/extensions/jira/commands/complete-spec.md`.
- Added `workflow.done_status` and `workflow.done_transition` to Jira config and template.
- Updated `speckit-archive-run` so mandatory post-archive hooks are actually invoked with the resolved absolute feature directory.
- Updated README with the new post-archive Jira completion trace.

Why:

- Archiving a specific feature spec is the process signal that feature work is complete.
- The mapped Jira issue for that spec should move to `Done` automatically after archival, while still skipping safely when Jira sync is disabled or the mapping is missing.

Affected files:

- `.specify/extensions.yml`
- `.agents/skills/speckit-jira-complete-spec/SKILL.md`
- `.specify/extensions/jira/commands/complete-spec.md`
- `.specify/extensions/jira/extension.yml`
- `.specify/extensions/jira/jira-config.yml`
- `.specify/extensions/jira/jira-config.template.yml`
- `.agents/skills/speckit-archive-run/SKILL.md`
- `.specify/extensions/archive/commands/archive.md`
- `README.md`
- `SPEC_KIT_CUSTOMIZATIONS.md`

Update risk:

- Medium. Preserve the `after_archive` hook and `workflow.done_*` settings when updating vendored `spec-kit-jira` or `spec-kit-archive`.

## 2026-08-26 - Safe Methodology Update Script

Baseline: colleagues updated trial projects manually by copying files from the corporate template.

Changed:

- Added `scripts/update-methodology.ps1` for preview/apply updates from a template copy into an existing Spec Kit project.
- Added `UPDATE_FROM_TEMPLATE.md` with the safe update flow.
- Updated `README.md` with methodology version `1.2.0` and a short update warning.

Why:

- Existing projects must receive methodology changes without overwriting project data such as `specs/`, `.specify/memory/`, `.specify/project.yml`, classification state, and traces.
- Removed skills/extensions should disappear during an update, so method directories are replaced deliberately after backup instead of copied loosely on top.

Affected files:

- `scripts/update-methodology.ps1`
- `UPDATE_FROM_TEMPLATE.md`
- `README.md`
- `SPEC_KIT_CUSTOMIZATIONS.md`

Update risk:

- Medium. Keep the replace/preserve lists aligned with future Spec Kit layout changes. Never add project-owned artifacts such as `specs/`, `.specify/memory/`, `.specify/project.yml`, or traces to the replace list.

## 2026-08-24 - Rebased Corporate Template onto Spec Kit 1.0.1

Baseline: clean `specify init` output from `github/spec-kit` v1.0.1 with `--integration codex --integration-options "--skills" --script ps --ignore-agent-tools`.

Changed:

- Created a new corporate template from the clean v1.0.1 init output instead of overwriting the existing v0.16.2 corporate template.
- Preserved v1.0.1 core scripts, including PowerShell prerequisite handling.
- Preserved v1.0.1 `$speckit-checklist` and `$speckit-taskstoissues` behavior instead of copying older corporate versions.
- Re-applied corporate constitution, class workflow, Jira/Confluence/archive/verify/critique extensions, project integration policy, Russian Clarify behavior, L3 critique hook handling, and L2/L3 converge/verify/archive hook handling.

Why:

- The existing corporate template was based on Spec Kit v0.16.2, while upstream released v1.0.1 on 2026-08-21.
- Corporate process changes must move onto the newer upstream base without losing upstream fixes.

Affected files:

- `.specify/init-options.json`
- `.specify/templates/constitution-template.md`
- `.specify/templates/checklist-template.md`
- `.specify/memory/constitution.md`
- `.specify/memory/initiative-class.md`
- `.specify/project.yml`
- `.specify/extensions.yml`
- `.specify/workflows/speckit/class-workflow.yml`
- `.specify/workflows/overlays/speckit/`
- `.specify/extensions/`
- `.agents/skills/speckit-clarify/SKILL.md`
- `.agents/skills/speckit-plan/SKILL.md`
- `.agents/skills/speckit-implement/SKILL.md`
- `.agents/skills/speckit-class-*`
- `.agents/skills/speckit-jira-*`
- `.agents/skills/speckit-confluence-*`
- `.agents/skills/speckit-archive-run`
- `.agents/skills/speckit-verify-run`
- `.agents/skills/speckit-critique-run`
- `.agents/skills/speckit-process-navigator`

Update risk:

- Medium/High. Future Spec Kit core skill updates must be merged carefully: keep upstream scripts and generic core behavior unless a corporate hook or language policy explicitly overrides it.

## 2026-08-19 - Corporate Constitution Template

Baseline: stock Spec Kit constitution flow.

Changed:

- Replaced the narrow default constitution with a broad corporate initiation document.
- Added explicit approval status in `## Статус согласования`.
- Added L1 baseline sections and an L2 Business Case section in the same document.
- Made `Approved` the required status before `/SpecKit Specify`.

Why:

- The template needs a lightweight L1 path and a richer L2+ path without splitting the initiation artifact.
- Approval must be visible in the source-of-truth document, not only in conversation.

Affected files:

- `.specify/templates/constitution-template.md`
- `.specify/memory/constitution.md`
- `README.md`

Update risk:

- Medium. Upstream constitution-template changes must be manually reconciled with the corporate template.

## 2026-08-19 - Initiative Class Workflow

Baseline: stock Spec Kit command sequence. The `speckit-classify` skill was created on 2026-08-18 and recorded here on 2026-08-19.

Changed:

- Added classification profiles `L1`, `L2`, and `L3`.
- Added machine-readable classification state and human-readable decision trace.
- Added `speckit-class-gate` as a mandatory hook before core Spec Kit commands.
- `speckit-class-gate` enforces the selected class before `specify`, `clarify`, `plan`, `analyze`, `tasks`, and `implement`.

Why:

- Different initiative classes require different artifact depth and gates.
- Downstream commands must not run before the initiative class is known and allowed.

Affected files:

- `.specify/classification.yml`
- `.specify/memory/initiative-class.md`
- `.specify/workflows/speckit/class-workflow.yml`
- `.specify/workflows/overlays/speckit/*.yml`
- `.agents/skills/speckit-classify/`
- `.agents/skills/speckit-class-gate/`
- `.specify/extensions.yml`
- `README.md`

Update risk:

- Medium. Upstream changes to command hook semantics or workflow schema must be checked against `.specify/extensions.yml`.

## 2026-08-19 - Class-Aware Process Navigation

Baseline: corporate three-class Spec Kit workflow with class gates and overlays.

Changed:

- Added advisory `navigation` metadata to `.specify/workflows/speckit/class-workflow.yml`.
- Added per-class `navigation.after_command` guidance to the L1, L2, and L3 overlays.
- Added `speckit-process-navigator` as a shared advisory after-hook skill.
- Registered `speckit.process.navigator` in `.specify/extensions.yml` after core workflow steps.

Why:

- Users need a clear next step at the end of every Spec Kit step.
- Next-step guidance must depend on the selected initiative class without creating a second process source of truth.
- Gates remain owned by `.specify/extensions.yml`, `class-workflow.yml`, overlays, and `speckit-class-gate`; navigation follows the same hook pattern but only explains the next action.

Affected files:

- `.specify/workflows/speckit/class-workflow.yml`
- `.specify/workflows/overlays/speckit/l1-proof-of-concept.yml`
- `.specify/workflows/overlays/speckit/l2-pilot.yml`
- `.specify/workflows/overlays/speckit/l3-production-solution.yml`
- `.specify/extensions.yml`
- `.agents/skills/speckit-process-navigator/SKILL.md`

Update risk:

- Low/Medium. Upstream hook semantics changes must preserve `after_*` hook dispatch. Overlay navigation keys should stay advisory and must not replace hook/gate enforcement.

## 2026-08-19 - Approved Constitution Gate Before Specify

Baseline: stock Spec Kit allows `/SpecKit Specify` after normal command prerequisites; it does not enforce this corporate approval model.

Changed:

- `/SpecKit Specify` is blocked unless `.specify/memory/constitution.md` has `## Статус согласования` set to `Approved`.
- The readiness check lives in `speckit-class-gate`, which runs as the first `before_specify` hook.
- The gate also checks that required L1 sections are filled and, for L2+, that the L2 Business Case section is filled.
- Jira constitution sync runs only after this gate in the same `before_specify` chain.

Why:

- Specification work must not start from a draft or partially filled initiation document.
- Approval remains a simple manual change/prompt instead of being owned by `/SpecKit Constitution`.

Affected files:

- `.agents/skills/speckit-class-gate/SKILL.md`
- `.specify/extensions.yml`
- `.specify/memory/constitution.md`
- `.specify/templates/constitution-template.md`
- `README.md`

Update risk:

- Medium. If upstream changes `/SpecKit Specify` or hook ordering, this gate must remain the first `before_specify` hook.

## 2026-08-19 - Embedded spec-kit-jira Extension

Baseline: `mbachorik/spec-kit-jira` v3.0.0 from `https://github.com/mbachorik/spec-kit-jira`.

Changed:

- Installed the extension into `.specify/extensions/jira/` as a vendored template component.
- Removed the downloaded nested `.git` directory so the extension is part of this template tree, not a nested repository.
- Added `jira-config.yml` next to `jira-config.template.yml`.

Why:

- Jira integration is part of the corporate process and should travel with the template.
- The extension should not require a manual `specify extension add jira` step for each new project.

Affected files:

- `.specify/extensions/jira/`
- `.specify/extensions/jira/jira-config.yml`
- `.specify/extensions/jira/jira-config.template.yml`
- `.specify/extensions/jira/extension.yml`
- `.specify/.gitignore`
- `README.md`

Update risk:

- High. Upstream extension updates must be applied deliberately. Compare upstream files against `.specify/extensions/jira/` and preserve corporate config/wrapper behavior.

## 2026-08-19 - Corporate Jira Projection Profile

Baseline: upstream `spec-kit-jira` maps `spec.md -> Epic`, `tasks.md phase headers -> Story`, and task items -> Task.

Changed:

- Jira project is `TPL`.
- Constitution maps to Jira issue type `Stage`.
- Feature spec maps to Jira issue type `Epic`.
- Phases and tasks are not synchronized to Jira.
- `Epic` links to `Stage` through field `Epic.Stage`.
- `Epic.Stage` field ID was discovered via Atlassian MCP as `customfield_18723`.

Why:

- Corporate Jira should track the governance/feature level only.
- Implementation phases and local tasks remain Spec Kit artifacts, avoiding Jira noise.

Affected files:

- `.specify/extensions/jira/jira-config.yml`
- `.specify/extensions/jira/jira-config.template.yml`
- `.specify/extensions/jira/extension.yml`
- `.agents/skills/speckit-jira-background-trace/SKILL.md`
- `.agents/skills/speckit-jira-background-sync/SKILL.md`
- `README.md`

Update risk:

- High. Keys such as `sync.create_phases`, `sync.create_tasks`, and `mapping.constitution_artifact` are corporate wrapper policy, not upstream `spec-kit-jira` schema.

## 2026-08-19 - Jira Background Hooks

Baseline: upstream `spec-kit-jira` exposes manual slash commands and an optional `after_tasks` hook.

Changed:

- Added mandatory background hook before `/SpecKit Specify`:
  `speckit.jira.constitution-sync`.
- Added mandatory background hook after `/SpecKit Tasks`:
  `speckit.jira.background-trace`.
- Added mandatory background hook after `/SpecKit Implement`:
  `speckit.jira.background-sync`.
- Hooks write trace files and skip safely when prerequisites are missing.

Why:

- Jira synchronization should be part of the process when enabled for the project, but not a manual command users have to remember.
- Approval remains a simple manual edit/prompt; synchronization is guaranteed by the next Spec Kit step.

Affected files:

- `.specify/extensions.yml`
- `.agents/skills/speckit-jira-constitution-sync/SKILL.md`
- `.agents/skills/speckit-jira-background-trace/SKILL.md`
- `.agents/skills/speckit-jira-background-sync/SKILL.md`
- `README.md`

Update risk:

- Medium. The hook model depends on Codex skill instructions honoring `.specify/extensions.yml`.

## 2026-08-19 - Jira Wiki Renderer for Constitution Stage

Baseline: upstream `spec-kit-jira` sends source artifact content directly into Jira descriptions.

Changed:

- Added a corporate renderer for the Jira `Stage` description.
- The renderer converts `.specify/memory/constitution.md` into Jira wiki markup panels.
- `speckit-jira-constitution-sync` must use the renderer when `stage.description_renderer` is `jira-wiki-business-case-panels`.
- Raw constitution Markdown must not be sent to Jira when the renderer is configured.

Why:

- Jira Server/Data Center renders `{panel:...}` wiki markup as readable colored panels.
- The business case should be readable in Jira in the same format expected by the existing business-case template.

Affected files:

- `.specify/extensions/jira/renderers/constitution-stage.jira`
- `.specify/extensions/jira/jira-config.yml`
- `.specify/extensions/jira/jira-config.template.yml`
- `.specify/extensions/jira/extension.yml`
- `.agents/skills/speckit-jira-constitution-sync/SKILL.md`
- `README.md`

Update risk:

- Medium. This is corporate wrapper behavior, not upstream `spec-kit-jira` behavior. Preserve it when updating the vendored extension.

## 2026-08-19 - Embedded spec-kit-archive Extension

Baseline: `stn1slv/spec-kit-archive` v1.2.2 from `https://github.com/stn1slv/spec-kit-archive`.

Changed:

- Installed the extension into `.specify/extensions/archive/` as a vendored template component.
- Added `speckit-archive-run` as a Codex skill wrapper for `speckit.archive.run`.
- Added mandatory full-scope archival for L1 after `/SpecKit Implement` and for L2/L3 after successful `/SpecKit Converge` and `speckit.verify.run`.
- Updated L1/L2/L3 workflow profiles to require `speckit.archive.run` and the project-level memory artifacts it maintains.

Why:

- All initiative classes must consolidate completed feature knowledge into permanent project memory.
- Archival should be part of the workflow, not a manual command users need to remember.
- Corporate archival is full-scope; scope-only flags such as `--plan-only` are not used by the automatic hook.

Affected files:

- `.specify/extensions/archive/`
- `.agents/skills/speckit-archive-run/SKILL.md`
- `.agents/skills/speckit-implement/SKILL.md`
- `.specify/extensions.yml`
- `.specify/workflows/speckit/class-workflow.yml`
- `.specify/workflows/overlays/speckit/l1-proof-of-concept.yml`
- `.specify/workflows/overlays/speckit/l2-pilot.yml`
- `.specify/workflows/overlays/speckit/l3-production-solution.yml`
- `README.md`

Update risk:

- High. Upstream extension updates must be applied deliberately. Compare upstream files against `.specify/extensions/archive/` and preserve the corporate full-scope L1/L2/L3 hook behavior.

## 2026-08-19 - Embedded spec-kit-confluence Extension

Baseline: `aaronrsun/spec-kit-confluence` v1.1.1 from `https://github.com/aaronrsun/spec-kit-confluence`.

Changed:

- Installed the extension into `.specify/extensions/confluence/` as a vendored template component.
- Added Codex skill wrappers for `speckit.confluence.write`, `speckit.confluence.read`, and `speckit.confluence.update`.
- Left Confluence actions as manual commands; no automatic hooks were added to `.specify/extensions.yml`.

Why:

- Confluence design docs should be available from every project created from the corporate template.
- Publishing or updating Confluence pages requires an explicit target URL and should remain user-directed.

Affected files:

- `.specify/extensions/confluence/`
- `.agents/skills/speckit-confluence-write/SKILL.md`
- `.agents/skills/speckit-confluence-read/SKILL.md`
- `.agents/skills/speckit-confluence-update/SKILL.md`
- `README.md`

Update risk:

- Medium. Upstream extension updates must be applied deliberately. Compare upstream files against `.specify/extensions/confluence/` and preserve local Codex wrapper behavior.

## 2026-08-19 - Embedded spec-kit-verify Extension

Baseline: `ismaelJimenez/spec-kit-verify` v1.0.3 from `https://github.com/ismaelJimenez/spec-kit-verify`.

Changed:

- Installed the extension into `.specify/extensions/verify/` as a vendored template component.
- Added `verify-config.yml` with the upstream default `report.max_findings: 50`.
- Replaced emoji status markers in `scripts/powershell/load-config.ps1` with ASCII markers so Windows PowerShell can parse the vendored script reliably.
- Added `speckit-verify-run` as a Codex skill wrapper for `speckit.verify.run`.
- Required `speckit.verify.run` reports to be written in Russian while preserving technical identifiers, commands, paths, and severity tokens.
- Added mandatory post-implementation verification after successful `/SpecKit Converge` for L2 and L3 initiatives.
- Updated L2/L3 workflow profiles to require both `/SpecKit Converge` and `speckit.verify.run`.

Why:

- Pilot and production initiatives need an implementation quality gate against spec, plan, tasks, and constitution before downstream status sync or archival.
- Verification should be part of the workflow, not a manual command users need to remember.

Affected files:

- `.specify/extensions/verify/`
- `.specify/extensions/verify/verify-config.yml`
- `.specify/extensions/verify/scripts/powershell/load-config.ps1`
- `.agents/skills/speckit-verify-run/SKILL.md`
- `.agents/skills/speckit-implement/SKILL.md`
- `.specify/extensions.yml`
- `.specify/workflows/speckit/class-workflow.yml`
- `.specify/workflows/overlays/speckit/l2-pilot.yml`
- `.specify/workflows/overlays/speckit/l3-production-solution.yml`
- `README.md`

Update risk:

- High. Upstream extension updates must be applied deliberately. Compare upstream files against `.specify/extensions/verify/` and preserve the corporate L2/L3 mandatory hook behavior.

## 2026-08-19 - Embedded spec-kit-critique Extension

Baseline: `arunt14/spec-kit-critique` v1.0.0 from `https://github.com/arunt14/spec-kit-critique`.

Changed:

- Installed the extension into `.specify/extensions/critique/` as a vendored template component.
- Added `speckit-critique-run` as a Codex skill wrapper for `speckit.critique.run`.
- Added mandatory post-plan critique after `/SpecKit Plan` for L3 initiatives.
- Updated the L3 workflow profile to require `speckit.critique.run` and `specs/<feature>/critiques/critique-*.md`.

Why:

- Production initiatives need a formal product and engineering challenge before tasks are generated and implementation starts.
- L1/L2 should not pay this heavier review cost unless invoked manually.

Affected files:

- `.specify/extensions/critique/`
- `.agents/skills/speckit-critique-run/SKILL.md`
- `.agents/skills/speckit-plan/SKILL.md`
- `.specify/extensions.yml`
- `.specify/workflows/speckit/class-workflow.yml`
- `.specify/workflows/overlays/speckit/l3-production-solution.yml`
- `README.md`

Update risk:

- High. Upstream extension updates must be applied deliberately. Compare upstream files against `.specify/extensions/critique/` and preserve the corporate L3-only mandatory hook behavior.

## 2026-08-24 - Russian Clarify Questions

Baseline: stock Spec Kit clarify command from `templates/commands/clarify.md`.

Changed:

- Updated the Codex wrapper for `/SpecKit Clarify` so user-facing clarification questions are asked in Russian.
- Required multiple-choice answer options, recommendations, suggested answers, reasoning, "Почему это важно" explanations, disambiguation prompts, and reply instructions to be in Russian.
- Kept protocol markers, option letters, requirement IDs, file paths, and command names in their original technical form.

Why:

- Corporate users should be able to answer `/clarify` questions without switching language context.
- Clarification prompts should still preserve the upstream structured interaction format.

Affected files:

- `.agents/skills/speckit-clarify/SKILL.md`

Update risk:

- Medium. Upstream changes to `templates/commands/clarify.md` must be reconciled with the Russian-language interactive questioning rule.

## 2026-08-24 - Project Integration Sync Policy

Baseline: Jira background hooks were registered unconditionally in `.specify/extensions.yml` and skipped only when Jira project configuration was missing.

Changed:

- Added `.specify/project.yml` as the project-level integration policy file.
- Added `integrations.jira.sync_enabled` and `integrations.confluence.sync_enabled` flags.
- Updated Jira background hook skills to read `.specify/project.yml` before calling Jira and to write `skipped` traces when Jira sync is disabled.
- Updated Confluence write/update wrapper skills to stop before writing when Confluence sync is disabled.
- Left `.specify/extensions.yml` responsible for hook registration only; execution policy now lives in `.specify/project.yml`.

Why:

- Individual projects need to opt in or out of corporate mirrors without editing vendored extension configs or hook wiring.
- Jira and Confluence need one shared project policy shape before Confluence gets automatic hooks.

Affected files:

- `.specify/project.yml`
- `.specify/extensions.yml`
- `.specify/workflows/speckit/class-workflow.yml`
- `.agents/skills/speckit-jira-constitution-sync/SKILL.md`
- `.agents/skills/speckit-jira-background-trace/SKILL.md`
- `.agents/skills/speckit-jira-background-sync/SKILL.md`
- `.agents/skills/speckit-confluence-write/SKILL.md`
- `.agents/skills/speckit-confluence-update/SKILL.md`
- `README.md`

Update risk:

- Medium. Future hook wrappers must read `.specify/project.yml` first and keep extension-specific files limited to technical mapping/configuration.

## 2026-08-26 - Clickable Clarify Options

Baseline: Russian Clarify wrapper still asked users to type option letters for multiple-choice answers.

Changed:

- Updated `/SpecKit Clarify` to prefer Codex structured choice prompts with clickable options when the runtime exposes that capability.
- Required the full question, recommendation, and option table to remain visible in the assistant message before showing any clickable prompt, so the question is not hidden in tool details.
- Clarified that clickable options require the real `request_user_input` interactive tool and must not be simulated with Markdown inline-code pills.
- Added a visible recommendation before the first clarification question telling users to enable `"План"` mode in the current session for convenient button-based answers.
- Defined non-Plan behavior explicitly: never call `request_user_input`, never leave the only question copy in collapsed details, and end the turn on a normal visible question with a Markdown option table.
- Kept Markdown option tables and typed letter replies as a fallback for modes where structured prompts are unavailable or cannot represent the answer set.

Why:

- Users should be able to answer common Clarify questions by clicking an option instead of typing a letter.
- Users need to know that button-based answers depend on the current session being in `"План"` mode.
- Non-Plan users must still see and answer the clarification without expanding execution details.
- The fallback preserves compatibility with plain text Spec Kit and non-interactive Codex runs.

Affected files:

- `.agents/skills/speckit-clarify/SKILL.md`
- `README.md`

Update risk:

- Medium. Upstream changes to `templates/commands/clarify.md` must be reconciled with both the Russian-language and structured-choice interaction rules.

## 2026-08-26 - Same-Day Clarify Session Headings

Baseline: Clarify reused `### Session YYYY-MM-DD` for repeated runs on the same day.

Changed:

- Updated `/SpecKit Clarify` so each command run creates a distinct same-day clarification heading.
- The first run keeps `### Session YYYY-MM-DD`; later runs use `### Session YYYY-MM-DD #2`, `#3`, and so on.
- Clarified that the 5-question limit applies to the current command run/session, not to all Clarify runs on the same calendar day.

Why:

- Multiple Clarify runs on one day should not be merged into one apparent session in `spec.md`.
- Users may deliberately run Clarify again when they want another batch of high-impact questions, while keeping each run bounded and reviewable.
- Deferred coverage items should reflect the current run's quota or planning suitability, not accidental reuse of a daily heading.

Affected files:

- `.agents/skills/speckit-clarify/SKILL.md`
- `README.md`

Update risk:

- Medium. Upstream changes to `templates/commands/clarify.md` must preserve unique same-day session headings and per-run quota semantics.

## Trace Artifacts

The following files are expected runtime outputs and are not upstream source files:

- `.specify/jira-constitution-mapping.json`
- `.specify/traces/jira/constitution-sync.json`
- `.specify/traces/jira/background-trace.json`
- `.specify/traces/jira/background-sync.json`
- `specs/<feature>/jira-mapping.json`
- `specs/<feature>/jira-trace.json`
- `specs/<feature>/jira-sync-trace.json`
- `specs/<feature>/confluence-mapping.json`
- `specs/<feature>/confluence-sync-trace.json`
- `.specify/traces/confluence/background-spec-sync.json`
- `specs/<feature>/critiques/critique-*.md`
- `.specify/memory/spec.md`
- `.specify/memory/plan.md`
- `.specify/memory/changelog.md`

## 2026-08-24 - Formal Increment and Final Solution Test Plan

Baseline: Spec Kit v1.0.1 tasks template treats tests as optional unless explicitly requested.

Changed:

- Added `.specify/templates/test-plan-template.md`.
- Updated `spec-template.md` so Story-level independent tests explicitly feed the formal test plan instead of replacing it.
- Updated `plan-template.md` and `speckit-plan` so `/SpecKit Plan` creates `specs/<feature>/test-plan.md`.
- Updated `tasks-template.md` and `speckit-tasks` so tests are mandatory for every user-story increment and for final solution validation.
- Updated `speckit-implement` with a corporate test gate before implementation: missing increment or final validation tasks stop implementation and require regenerating `tasks.md`.
- Updated `README.md` to document the new formal test-plan artifact.

Why:

- Corporate Spec Kit must not rely on informal "Independent Test" prose or optional test tasks.
- Every increment needs explicit verification before it can be treated as independently deliverable.
- The final solution needs a separate acceptance-level test package that proves cross-increment behavior, not only isolated Story completion.

Affected files:

- `.specify/templates/test-plan-template.md`
- `.specify/templates/spec-template.md`
- `.specify/templates/plan-template.md`
- `.specify/templates/tasks-template.md`
- `.agents/skills/speckit-plan/SKILL.md`
- `.agents/skills/speckit-tasks/SKILL.md`
- `.agents/skills/speckit-implement/SKILL.md`
- `README.md`

Update risk:

- Medium. Future upstream changes to plan/tasks/implement templates must preserve the corporate rule that tests are mandatory and split into increment-level and final-solution validation.

## 2026-08-24 - Russian Archive Report

Baseline: embedded `spec-kit-archive` v1.2.2 wrote the Step 6 Archival Report in English.

Changed:

- Required `speckit.archive.run` reports to be written in Russian while preserving technical identifiers, commands, paths, IDs, status tokens, `RETIRED:` markers, and `[Source: ...]` refs.
- Updated both the vendored Archive command and the Codex wrapper skill copy.

Why:

- Corporate users need Archive run results in the same language as the rest of the project governance workflow.
- Traceability tokens must stay stable across Russian report text.

Affected files:

- `.specify/extensions/archive/commands/archive.md`
- `.agents/skills/speckit-archive-run/SKILL.md`

Update risk:

- Medium. Upstream changes to `commands/archive.md` must be reconciled with the Russian-language report rule.


## 2026-09-03 - Codex Skill Selector Metadata

Changed:

- Added `agents/openai.yaml` UI metadata to every bundled SpecKit skill.
- Added update-time validation requiring a 25-64 character `interface.short_description` for every skill.
- Expanded `codex.manifest.json` generation to hash both `SKILL.md` and `agents/openai.yaml` files.
- Bumped the methodology version to 1.3.12.

Why:

- Codex Desktop's skill selector can omit project skills that do not provide the short UI description, even when their `SKILL.md` files are valid and discoverable.
- A successful methodology update must verify the user-visible skill inventory, not only copy the skill instructions.

Affected files:

- `.agents/skills/*/agents/openai.yaml`
- `scripts/update-methodology.ps1`
- `.specify/integrations/codex.manifest.json`
- `README.md`
- `UPDATE_FROM_TEMPLATE.md`

Update risk:

- Low. Future bundled skills must include valid Codex UI metadata before the methodology can be applied.
## 2026-09-03 - Explicit SpecKit Command Discovery

Changed:

- Marked 21 user-facing SpecKit workflow skills with `policy.allow_implicit_invocation: false`.
- Kept seven internal gate, navigator, Jira, and Confluence hook skills available for implicit invocation.
- Extended methodology-update validation to enforce the explicit command policy.
- Bumped the methodology version to 1.3.13.

Why:

- Codex limits the initial skill catalog to 2% of the model context or 8,000 characters and may omit skills when many personal, plugin, and project skills are installed.
- Explicit-only workflow commands remain selectable through `$skill-name` without consuming that initial catalog budget.

Affected files:

- `.agents/skills/*/agents/openai.yaml`
- `scripts/update-methodology.ps1`
- `README.md`
- `UPDATE_FROM_TEMPLATE.md`

Update risk:

- Medium. User-facing workflow commands require explicit selection; internal hooks must retain implicit invocation.
## 1.3.14 - Codex skill encoding validation

- Removed UTF-8 BOM from 27 `SKILL.md` files. Codex requires the YAML frontmatter delimiter `---` to be the first bytes of the file and ignored every affected skill.
- Added source and post-copy validation to `scripts/update-methodology.ps1` so methodology updates fail before replacement when a `SKILL.md` contains a BOM or does not start with YAML frontmatter.
- Bumped the methodology version to 1.3.14.
## 1.3.15 - Preserve project-specific Codex skills

- Changed `scripts/update-methodology.ps1` to update only methodology-owned `speckit-*` directories under `.agents/skills`.
- Project-specific skills with other names are now preserved across methodology updates and remain included in the regenerated Codex manifest.
- Scoped mandatory `agents/openai.yaml` validation to methodology-owned skills while retaining BOM and frontmatter validation for every project skill.
- Bumped the methodology version to 1.3.15.
## 1.3.16 - Separate project README from methodology documentation

- Renamed the root methodology document from `README.md` to `METHODOLOGY.md`.
- Removed `README.md` from the updater replacement set and declared it as preserved project data.
- Added `METHODOLOGY.md` to methodology updates and changed version reporting to point to that file.
- Bumped the methodology version to 1.3.16.
