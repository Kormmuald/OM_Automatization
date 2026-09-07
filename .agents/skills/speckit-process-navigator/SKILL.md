---
name: speckit-process-navigator
description: Show class-aware next-step guidance after SpecKit workflow commands. Use as an advisory extension hook after /SpecKit Init, /SpecKit Clarify, /SpecKit Plan, /SpecKit Analyze, /SpecKit Tasks, and /SpecKit Implement. It reads .specify/project.yml and the selected workflow overlay, then reports the recommended next step without enforcing gates or modifying files.
---

## Purpose

Act as the shared class-aware navigator for SpecKit workflow commands. This skill is advisory only: it explains the next recommended action after a command completes. It must not replace `speckit-class-gate`, weaken hooks, or reimplement any SpecKit command.

## Inputs

Read these files from the project root:

1. `.specify/project.yml` - project settings and selected initiative class written by `/SpecKit Init`.
2. `.specify/workflows/speckit/class-workflow.yml` - class policy and navigation fallback.
3. `.specify/workflows/overlays/speckit/<selected-overlay>.yml` - class-specific navigation metadata.
4. `.specify/memory/constitution.md` - project authority and human gates, if present.
5. `.specify/feature.json` and the current `specs/<feature>/` artifacts, if present, only to summarize current artifact availability.

## Navigation Logic

1. Read `project.document_language`, `initiative.class`, and selected overlay/profile from `.specify/project.yml`. If class or
   overlay is missing, report that process navigation is blocked and recommend `/SpecKit Init`. Do
   not modify files.
2. Load `.specify/workflows/speckit/class-workflow.yml`.
3. Load the selected overlay from `.specify/workflows/overlays/speckit/<overlay>.yml`.
4. Determine the completed command from the hook context:
   - Prefer the command named by the current after-hook context if visible.
   - Otherwise infer it from the user's latest requested SpecKit command.
   - Recognize both public commands such as `/SpecKit Plan` and extension command ids such as `speckit.critique.run`.
   - If unclear, say that the completed command is unclear and show the selected class plus the next command inferred from the first missing required command.
5. Look up `navigation.after_command[completed_command]` in the selected overlay.
6. If an exact entry exists, report its `automatic_hooks`, `next`, `reason`, and `human_decision` fields when present.
7. If no exact entry exists, use `navigation.fallback_rule` from `class-workflow.yml`: infer the next required command after the completed command from the selected class `required_commands` order. Explain that this is an inference.
8. Check visible artifacts lightly:
   - If `.specify/feature.json` points to a feature directory, list missing required artifacts for the selected class that are relevant before the recommended next command.
   - Do not treat missing artifacts as a hard stop unless the overlay text says a human decision is needed. Hard stops belong to gates and commands.

## Output

Return a concise block:

```text
Process navigation
Current class: L2 - Pilot
Completed step: /SpecKit Plan
Automatic hooks: ...
Recommended next step: /SpecKit Analyze
Reason: ...
Human decision needed: ...
Watch items: ...
```

If navigation cannot run:

```text
Process navigation: unavailable
Reason: ...
Recommended action: ...
```

## Rules

- Do not create or edit files.
- Do not execute the recommended next command.
- Do not approve, reject, promote, or downgrade an initiative class.
- Do not duplicate gate enforcement. If a transition seems unsafe, phrase it as a watch item or human decision, not as a gate result.
- Write the user-facing navigation block in Russian regardless of `project.document_language`.
- Treat `project.document_language` as document-generation policy only. Mention it only when the recommended next step creates or updates project documents whose skill explicitly uses that setting.
- Keep the output short enough to appear at the end of another command's report.
