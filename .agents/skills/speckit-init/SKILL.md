---
name: "speckit-init"
description: "Initialize project runtime settings and initiative class. Use after /SpecKit Constitution and before /SpecKit Specify."
compatibility: "Requires spec-kit project structure with .specify/ directory"
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding.

## Purpose

This command owns project initialization for the corporate contour. It configures project-level
runtime settings in `.specify/project.yml` and seeds Jira/Confluence mapping inputs when needed.
It does not create feature specifications; `/SpecKit Specify` is the required next command for all
initiative classes.

Do not put project settings into `.specify/memory/constitution.md`. The constitution is governance
content. Project settings and external synchronization identities are machine-readable runtime
state.

## Required Analysis Before Questions

Before asking about initiative class, `/SpecKit Init` MUST analyze
`.specify/memory/constitution.md` together with any explicit user input from `$ARGUMENTS`.

The class decision is not a user preference question. Init MUST:

1. Read `.specify/workflows/speckit/class-workflow.yml`.
2. Extract classification facts from the constitution:
   - purpose and expected result;
   - baseline delivery form;
   - scope and anti-scope;
   - safe and unsafe assumptions;
   - users, stakeholders, and visibility;
   - data type and sensitivity;
   - integrations and touched environments;
   - lifecycle duration and support expectations;
   - failure impact, fallback, acceptance, and human gates;
   - L2 Business Case content when filled.
3. Compare extracted facts to the `L1`, `L2`, and `L3` class signals and promotion triggers.
4. Recommend one concrete class before any class prompt is shown.
5. Classify conservatively: if two classes are plausible, recommend the stricter class until a
   constitution-aligned owner narrows scope or removes the higher-risk facts.

If the constitution contains enough facts for a `high` or `medium` confidence decision, write the
recommended class into `.specify/project.yml` without asking the user to choose among class options.
Report the recommendation, confidence, and constitution facts that drove it.

Ask a class question only when one of these exceptions applies:

- the constitution lacks facts needed to distinguish `L1`, `L2`, and `L3`;
- the constitution has conflicting facts that lead to different classes;
- the user explicitly asks to override the recommendation.

When asking because facts are missing or conflicting, do not ask "which class do you want?".
Ask for the missing classification facts, show the current recommended class first, and explain what
specific answer would keep the class or force promotion.

## Required Questions

Ask concise questions for values not already provided by user input or existing files:

1. Document language for Constitution/Specify-generated documents:
   - `ru` for Russian
   - `en` for English
   - Default: `ru`
2. Jira synchronization:
   - enabled or disabled
   - if enabled, whether the constitution already exists in Jira
   - if it already exists, the target Jira issue key to update
3. Confluence synchronization:
   - enabled or disabled
   - if enabled, the root parent page URL for future `specs/<feature>/spec.md` mirror pages
4. Missing or conflicting classification facts, but only when the required class analysis above
   cannot produce a concrete `high` or `medium` confidence recommendation.
`/SpecKit Init` is the only owner of initiative class selection. If the class must change later,
rerun `/SpecKit Init` with the updated class decision and preserve unrelated project settings.

## Interactive Questioning

Ask Init questions in Russian. Multiple-choice option descriptions, recommendations, reasoning,
validation prompts, and reply instructions must also be in Russian. Keep only stable technical
identifiers such as `ru`, `en`, `L1`, `L2`, `L3`, file paths, YAML/JSON keys, Jira issue keys, and
command names in their original form.

Before asking the first Init question, output this Russian note as a normal visible assistant
message: `Запущен /SpecKit Init. Для удобного ответа через кнопки включите режим "План" в текущей сессии. Если режим "План" недоступен или вы не хотите переключаться, можно ответить текстом по таблице вариантов.`

Prefer clickable structured choice prompts over typed replies whenever the current Codex
environment exposes an interactive choice tool:

- First render the full Init question, recommendation, and option table as a normal visible
  assistant message. Do not hide the question inside tool details or progress text.
- Then call the real `request_user_input` interactive choice tool as an additional clickable answer
  channel for the same question, only if the task is running in Plan mode and that tool is
  available in the current mode.
- In Default mode or any non-Plan mode, do not call `request_user_input` under any circumstance.
  Ask the pending Init question as the final normal visible assistant response for the turn,
  include the full option table and typed-answer instruction, then stop and wait for the user's
  text reply.
- If several missing settings can be collected in one structured prompt call, group up to the
  number of questions supported by the tool; otherwise ask them sequentially. Do not sacrifice
  clarity to force all Init settings into one prompt.
- Put the recommended answer first and mark its label with `(Recommended)` when the structured
  prompt requires that ordering.
- Use stable per-question identifiers such as `init_document_language`, `init_jira_sync`,
  `init_confluence_sync`, and `init_class_fact_gap`.
- If the structured prompt supports only 2-3 clickable options, prioritize the recommended option
  plus the strongest alternatives; use the prompt's free-form/Other affordance for less common
  values such as an existing Jira issue key or Confluence root URL.
- Do not simulate clickable buttons with Markdown inline code, badges, pills, or text like
  ``A`` ``B`` ``C``. Those are not real choices and should be used only as ordinary typed-answer
  examples in fallback instructions.
- After a clicked selection, map the selected option back to the original option letter/answer text
  and continue the same validation and settings-write flow.

For every multiple-choice Init question, format the visible fallback as:

`**Question:** <full Russian question ending with ?>`

`**Recommended:** Option [X] - <brief Russian reasoning>`

| Вариант | Описание |
|--------|-------------|
| A | <описание варианта A на русском> |
| B | <описание варианта B на русском> |
| C | <описание варианта C на русском, если нужно> |
| Short | Другой короткий ответ или значение, если нужно |

After the table, add this instruction in Russian: `Можно ответить буквой варианта (например, "A"), принять рекомендацию словами "да" или "recommended", либо дать свой короткий ответ.`

When asking for a free-form value such as an existing Jira issue key or Confluence root page URL,
prefer a structured question whose choices are "указать значение сейчас", "оставить пустым/отключить
синхронизацию", and the recommended safe default. If the user chooses to provide the value, ask for
that single value as the final visible question for the turn unless it was already included in the
user input.

## Settings Writes

Update `.specify/project.yml` without disturbing unrelated keys. Required shape:

```yaml
schema_version: "1.1"
project:
  document_language: "ru" # or "en"
initiative:
  class: L1
  class_name: "Proof of Concept"
  classified_at: "YYYY-MM-DD"
  decision_owner: "..."
  confidence: "high|medium|low"
  rationale: "..."
  workflow:
    base: "speckit"
    profile: "l1-proof-of-concept"
    overlay: "l1-proof-of-concept"
integrations:
  jira:
    sync_enabled: true
  confluence:
    sync_enabled: false
    spec_root_page_url: null
```

If Jira sync is enabled and the user says the constitution already exists in Jira, write
`.specify/jira-constitution-mapping.json` before any Jira sync can run:

```json
{
  "project": "<configured Jira project key when known>",
  "issue_type": "Stage",
  "stage_key": "<existing issue key>",
  "constitution_hash": null,
  "synced_at": null,
  "source": ".specify/memory/constitution.md",
  "seeded_by": "speckit-init"
}
```

This mapping is the only place to store the external identity of an existing Jira constitution
issue. `.specify/project.yml` must not store `stage_key` or other Jira issue keys.

## Completion

After settings are written:

- do not create `specs/<feature>/spec.md`;
- do not execute `before_specify` or `after_specify` hooks;
- report created or updated settings and mapping files;
- report the concrete class recommendation, confidence, and constitution facts used for the
  decision;
- tell the user that the required next command is `/SpecKit Specify` with the feature description.
