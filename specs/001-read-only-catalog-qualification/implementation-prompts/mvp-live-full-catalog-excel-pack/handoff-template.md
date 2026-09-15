# SXX handoff

Заполнять после принятия SXX; не копить session log.

- Accepted stage: `SXX`.
- Следующий stage: `SYY` или `none`.
- Readiness следующего stage: `execution-ready` или причина блокировки.

## Что фактически изменено

- `<file>` — `<behavior/contract>`.

## Стабильные входы следующего stage

- `<public contract/path/evidence>`.

## Проверки и evidence

- Acceptance report: `<path>`.
- Commands/results: `<exact safe summary>`.

## Открытые вопросы и запреты

- `<unresolved item or none>`.
- Не выполнять соседний stage и не переинтерпретировать принятые контракты без нового
  finding и решения оркестратора.
