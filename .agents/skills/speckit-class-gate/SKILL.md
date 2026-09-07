---
name: speckit-class-gate
description: Enforce the selected SpecKit initiative class before workflow commands run. Use as a mandatory extension hook before /SpecKit Init, /SpecKit Specify, /SpecKit Clarify, /SpecKit Plan, /SpecKit Analyze, /SpecKit Tasks, and /SpecKit Implement to ensure /SpecKit Init has written .specify/project.yml, the selected class is valid, the command is allowed by .specify/workflows/speckit/class-workflow.yml, and required class artifacts/gates are visible before proceeding.
---

## Purpose

Act as the shared class-aware guard for baseline SpecKit commands. Do not reimplement the commands themselves.

## Inputs

Read these files from the project root:

1. `.specify/project.yml` - project settings and selected initiative class written by `/SpecKit Init`.
2. `.specify/workflows/speckit/class-workflow.yml` - class policy: commands, artifacts, overlays, and promotion triggers.
3. `.specify/memory/constitution.md` - project authority and human gates.
4. `.specify/workflows/overlays/speckit/<selected-overlay>.yml` - class-specific command requirements.

## Gate Logic

1. If `.specify/memory/constitution.md` is missing or still looks unfilled, stop and tell the user to run `/SpecKit Constitution`.
2. Determine the command being guarded from the invoking context:
   - Prefer the command named by the current hook context if visible.
   - Otherwise infer it from the user's latest requested SpecKit command.
   - If unclear, ask for the target command and do not proceed.
3. When the guarded command is `/SpecKit Init`, enforce constitution
   readiness before allowing feature initialization:
   - The approval section `## Статус согласования` MUST contain an approved value. Accept
     `Согласовано` as canonical, and accept legacy `Approved` for existing projects.
   - Required `L1` sections MUST be filled. Accept canonical Russian headings and legacy English
     headings for compatibility:
     `## Рамка проекта` / `## Project Frame`,
     `## Основные принципы`,
     `## Базовая форма поставки` / `## Delivery baseline`,
     `## Границы работ` / `## Scope boundaries`,
     `## Анти-границы` / `## Anti-scope`,
     `## Безопасные допущения` / `## Safe assumptions`,
     `## Небезопасные допущения` / `## Unsafe assumptions`,
     `## Контрольные точки человека` / `## Human gates`,
     `## Требования к проверке` / `## Verification requirements`,
     `## Модель решения о приемке` / `## Acceptance decision model`,
     `## Источник истины` / `## Source of truth`, and
     `## Управление` / `## Governance`.
   - For class `L2` or higher, the `## Бизнес-кейс L2` / `## L2 Business Case` section and its
     subsections MUST also be filled.
   - A required section is unfilled if its body still contains bracket placeholders such as `[PROJECT_NAME]`, `[SCOPE_BOUNDARIES]`, or similar, or if it is empty apart from instructional text.
   - If readiness fails, stop and ask the user to complete the missing sections or explicitly
     approve the constitution by changing the status to `Согласовано` (`Approved` is accepted only
     for legacy constitutions).
   - Do not require `initiative.class` before `/SpecKit Init`; Init owns class selection and writes
     it to `.specify/project.yml`.
4. For every guarded command after `/SpecKit Init`, including `/SpecKit Specify`, read `initiative.class` from `.specify/project.yml`.
   If it is missing, null, or unknown, stop and tell the user to run `/SpecKit Init`.
5. Load the selected class from `class-workflow.yml` and the selected overlay file named by
   `.specify/project.yml`.
6. Check whether the command is listed under `required_commands` or `conditional_commands` for the selected class.
7. If the command is not allowed for the selected class, stop and explain the class mismatch and the next valid command.
8. For allowed commands, report the selected class, profile, overlay, approval status when checked, and any class-specific artifacts/gates the user must keep in view.

## Output

For a pass:

```text
Class gate: PASS
Class: L2 - Pilot
Workflow profile: l2-pilot
Overlay: l2-pilot
Guarded command: /SpecKit Plan
Class-specific artifacts to maintain: ...
```

For a stop:

```text
Class gate: STOP
Reason: ...
Required action: ...
```

Keep the gate concise. Do not create or edit project artifacts; `/SpecKit Init` owns project
settings and initiative class writes.
