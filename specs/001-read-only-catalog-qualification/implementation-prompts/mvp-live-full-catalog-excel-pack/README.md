# Prompt pack: MVP Feature 001 — live full-catalog pull в Excel

Это исполняемая декомпозиция большой постановки
[`MVP-live-full-catalog-excel.md`](../MVP-live-full-catalog-excel.md). Монолитный файл
остаётся requirements reference; не поручай его реализацию одному агенту.

Пакет подготовлен по best practices project-local skill
`.agents/skills/slice-pack-generator`, но generator workflow не запускался.

## Точка входа

Запусти [`orchestrator.md`](orchestrator.md). Оркестратор выполняет этапы только
последовательно. После каждого worker result он обязан запустить нового независимого
reviewer-субагента по [`prompts/alignment-reviewer.md`](prompts/alignment-reviewer.md).

## Этапы и модели

| Этап | Worker prompt | Worker | Независимый reviewer |
| --- | --- | --- | --- |
| S00 | [Canonical reconciliation](prompts/S00-canonical-reconciliation.md) | `gpt-5.6-terra`, `high` | `gpt-5.6-terra`, `medium` |
| S01 | [Session and HTTP boundary](prompts/S01-session-http-boundary.md) | `gpt-5.6-sol`, `high` | `gpt-5.6-terra`, `high` |
| S02 | [Full object model](prompts/S02-full-object-model.md) | `gpt-5.6-terra`, `high` | `gpt-5.6-terra`, `high` |
| S03 | [Full lookup data](prompts/S03-full-lookup-data.md) | `gpt-5.6-sol`, `high` | `gpt-5.6-terra`, `high` |
| S04 | [Two-pass snapshot and evidence](prompts/S04-two-pass-snapshot-evidence.md) | `gpt-5.6-sol`, `high` | `gpt-5.6-sol`, `high` |
| S05 | [Excel materialization](prompts/S05-excel-materialization.md) | `gpt-5.6-sol`, `high` | `gpt-5.6-sol`, `high` |
| S06 | [Production CLI and offline E2E](prompts/S06-production-cli-offline-e2e.md) | `gpt-5.6-terra`, `high` | `gpt-5.6-terra`, `high` |
| S07 | [Opt-in live integration](prompts/S07-live-integration.md) | `gpt-5.6-sol`, `high` | `gpt-5.6-terra`, `high` |
| S08 | [Final docs and handoff](prompts/S08-final-docs-handoff.md) | `gpt-5.6-terra`, `medium` | `gpt-5.6-terra`, `medium` |

Модельное распределение утверждено пользователем в запросе на создание этого pack.
Повторное подтверждение не требуется. Эскалация `Terra -> Sol` допустима только после
явного объяснения конкретной сложности; понижение reasoning запрещено.

## Readiness

- S00: `execution-ready`.
- S01–S08: `planning-pending-previous-evidence`.
- Каждый следующий этап становится `execution-ready` только после `Pass` независимого
  reviewer и создания фактических `acceptance-report.md` + `handoff.md` предыдущего.
- Canonical tracker после S00 — обновлённый `tasks.md`; pack его не заменяет.

Общие границы для всех ролей: [`shared-guardrails.md`](shared-guardrails.md).
Карта проекта и canonical sources: [`project-specific-info.md`](project-specific-info.md).
Форматы stage-gate артефактов:
[`acceptance-report-template.md`](acceptance-report-template.md) и
[`handoff-template.md`](handoff-template.md).
