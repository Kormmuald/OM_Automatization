# Промпт переноса спецификации 04

Перенеси неизменяемый источник
`preparation/docs/product-specs/local-bpmsoft-synchronizer/04-gated-apply-spec.md`
в корпоративный Spec Kit как отдельную **заблокированную safety-gates L2 feature**
для preflight, разрешённого Apply и read-back. Не редактируй источник и не
создавай feature внутри `preparation/docs/product-specs/`.

Прочитай `.specify/project.yml`, `.specify/memory/constitution.md`,
`class-workflow.yml` и overlay `l2-pilot`, затем выполни `speckit-class-gate`
перед `/SpecKit Specify`. Создавай только `specs/<feature>/spec.md` на русском.
Jira/Confluence синхронизация отключена: Stage, mapping и trace не нужны.
Создай новую отдельную папку `specs/<feature>/`: только в ней размещаются
последующие checklist, research, data-model (если применимо), quickstart, plan и
tasks данной feature. Этот запуск ограничен `spec.md`; будущие артефакты создаются
соответствующими L2-командами в этой же папке.

Сохрани тезисную суть всех `APPLY-001`…`APPLY-012`, перечня кандидатных видов
операций и их статусов `DENIED_*`. В новой структуре явно раздели:

- prerequisites, требующие решения человека: owner-approved operation allowlist,
  отдельная авторизация write preflight, manual backup и approval plan целиком;
- доказательный operation-level preflight с positive/negative cases;
- требования точного immutable plan, fail-closed execution, first-error stop,
  read-back и доказуемого `DraftRowToken -> RecordId`;
- абсолютные запреты Delete, rollback, index operations, browser write,
  автопродолжение частичного запуска и поиск идентичности по Name/Code.

Статус feature должен честно отражать blocker: можно специфицировать и
реализовывать лишь безопасные локальные проверки/закрытое поведение, но нельзя
создавать или исполнять live BPMSoft write/preflight без новых явных решений
владельца. Не преобразуй pending кандидатов в approved operations, не
придумывай endpoints/payloads/evidence и не обходи `INDEX_SYNC_UNRESOLVED`.

Задай L2 success/stop/fallback и требуемое итоговое человеческое решение. Не
создавай plan, tasks или реализацию в этом запуске; после Specify обязателен
`/SpecKit Clarify`.
