# Acceptance Report Template

```markdown
# Acceptance Report: <slice>

## 1. Scope

- Slice: <какой slice принимается>
- Project-specific info: <path or none>
- Spec / plan reference: <ссылка или путь>
- Skill used: <например, managed-implementation-gate>
- Subagent roles / tasks: <краткий список>
- Review flow: <кто проверял результат, findings, решения>
- Control gates / decisions: <какие gate были пройдены>
- Out of scope: <что не входило>

## 2. Acceptance Criteria Match

| Criterion | Status | Evidence | Notes |
| --- | --- | --- | --- |
| AC1 | <pass/fail/partial/not checked> | <команда, тест, лог, screenshot> | <комментарий> |
| Project-specific info alignment | <pass/fail/partial/not checked> | <сверка с project-specific info и SDD/Spec Kit sources> | <комментарий> |

## 3. Verification Evidence

- Build: <pass/fail/not run>
- Unit tests: <pass/fail/not run>
- Integration tests: <pass/fail/not run>
- UI smoke: <pass/fail/not run>
- Manual happy path: <pass/fail/not run>
- Negative cases: <pass/fail/not run>
- Evidence links or commands: <пути, команды, логи, screenshots>

## 4. Known Gaps

- Not checked: <что не проверялось>
- Known limitations: <известные ограничения>
- Risks: <остаточные риски>

## 5. Acceptance Decision

- Decision: <Accept / Accept with limitations / Request changes / Reject>
- Reason: <почему принято такое решение>
- Follow-up: <что делать дальше>
```
