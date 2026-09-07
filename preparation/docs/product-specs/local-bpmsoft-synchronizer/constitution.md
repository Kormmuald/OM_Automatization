# Constitution локального синхронизатора BPMSoft

Статус: **draft — owner review required; not ratified**.

## Core Principles

### I. Controlled rewrite, not legacy port

Исторический `SyncOM` служит источником domain knowledge, characterization и API-кандидатов, но не доказательством корректности нового продукта. Любое унаследованное поведение явно классифицируется как `reuse`, `rewrite`, `drop` или `defer`; перенос «как есть» запрещён.

### II. Human-controlled security boundaries

Секреты и решения с необратимым или внешним эффектом остаются у человека. Инструмент и навыки не получают и не сохраняют credentials, не одобряют change plan и не обходят явные контрольные этапы оператора.

### III. Read, plan and change are separated

Чтение, сравнение и проверка не изменяют целевую систему. Любое допустимое изменение исполняется только по заранее сформированному, проверяемому и явно одобренному плану; при ошибке система безопасно останавливается, а не угадывает исправление или продолжение.

### IV. Identity and information fidelity are non-negotiable

Соответствие существующих сущностей основано на доказанной серверной identity и контексте. Неоднозначность не компенсируется эвристикой по отображаемым именам. Неизвестные структуры и неподдержанные намерения фиксируются без молчаливой потери либо фабрикации данных и блокируют только зависимую возможность.

### V. Deterministic, evidence-based operation

Одинаковые валидные входы должны давать семантически одинаковый результат. Значимые утверждения о состоянии, безопасности и успешном изменении требуют воспроизводимых проверок и безопасного evidence; автоматизированный PASS не заменяет решение владельца там, где оно требуется.

## Development Workflow and Quality Gates

Работа следует цепочке: Constitution → specification → clarification/review → implementation plan → tasks/slices → implementation and verification → human acceptance/handoff. Переход к следующей стадии возможен, только если закрыты применимые requirements и явно зафиксированы остающиеся assumptions, risks и blockers.

Каждый implementation artifact обязан иметь трассировку к требованиям, выполненным проверкам, evidence и необходимым human decisions. Product requirements, технические контракты, текущие blockers, operational procedures и статусы решений владельца фиксируются в соответствующих spec, plan, handoff и decision records, а не дублируются здесь.

## Governance

Constitution задаёт правила толкования для всех артефактов этого пакета. При конфликте действует следующее: новое явное решение владельца имеет приоритет; изменение устойчивого принципа оформляется поправкой к Constitution; требования и детали реализации уточняются в своих целевых артефактах.

Каждая поправка должна содержать причину, влияние на требования и артефакты, а также миграционные последствия. Исключение, принятое для одного контрольного этапа, не распространяется автоматически на другие этапы или область продукта.

**Version**: `0.2-draft` | **Ratified**: not ratified | **Last Amended**: 2026-09-07

## Related artifacts

- [Верхнеуровневая спецификация](spec.md)
- [Workbook-pair specification](02-workbook-pair-spec.md)
- [Compare and immutable-plan specification](03-compare-plan-spec.md)
- [Gated Apply specification](04-gated-apply-spec.md)
- [Verification and operations specification](05-verification-operations-spec.md)
- [Clarification and blocker review](clarify-review.md)
- [Project handoff](../../PROJECT_HANDOFF.md)
