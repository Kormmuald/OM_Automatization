# Orchestration Task Template

```markdown
# Orchestration Task: <slice>

## 1. Goal

Провести один slice через implementation, review flow, verification и acceptance preparation.

Кратко:
<что должно стать проверяемым результатом slice>

## 2. Input Artifacts

- Project-specific info: <path or none>
- Spec: <path>
- Implementation plan: <path>
- Slice description: <path or text>
- Existing verification notes: <path or none>
- Skill to use: managed-implementation-gate

## 3. Scope

In:
- <что входит>
- <какой результат должен быть виден>

Out:
- <что не входит>
- <что нельзя расширять без human gate>

## 4. Subagent Roles / Tasks

Implementation role:
- Task: <что реализовать>
- Expected output: <diff, files, summary>

Review role:
- Task: проверить результат относительно spec, slice, scope, risks и missing tests.
- Expected output: review findings, required fixes, accepted risks.

Verification role:
- Task: запустить или описать проверки.
- Expected output: commands, results, failed/not run checks, evidence.

Optional docs/handoff role:
- Task: обновить краткие notes, если это нужно для следующего шага.
- Expected output: concise handoff, без нового большого документа.

## 5. Control Gates

Stop and ask before:
- changing architecture or public contracts;
- expanding scope beyond this slice;
- violating SDD or project-specific constraints;
- changing deployment shape;
- replacing verification with an agent claim;
- accepting partial implementation as complete.

Required human decisions:
- approve or adjust subagent role split;
- accept or reject review findings;
- decide final acceptance status after evidence is collected.

## 6. Verification Expectations

Minimum evidence:
- build or equivalent project-level check;
- relevant unit/smoke/manual check;
- explicit note for every check that was not run;
- link, command, log excerpt, screenshot or path that lets a reviewer understand the evidence.

## 7. Output Format

Return:
- implemented or changed files;
- subagent role summary;
- review findings and decisions;
- verification evidence;
- known gaps and risks;
- recommendation for acceptance report.
```
