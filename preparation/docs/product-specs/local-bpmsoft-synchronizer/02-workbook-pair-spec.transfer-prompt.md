# Промпт переноса спецификации 02

Перенеси неизменяемый источник
`preparation/docs/product-specs/local-bpmsoft-synchronizer/02-workbook-pair-spec.md`
в корпоративный Spec Kit как отдельную L2 feature для создания, валидации и
контролируемого обновления логической пары Excel-книг. Не редактируй источник,
не перемещай его и не используй `preparation/docs/product-specs/` как каталог
feature.

Сначала прочитай `.specify/project.yml`, `.specify/memory/constitution.md`,
`class-workflow.yml` и overlay `l2-pilot`, затем выполни class gate для
`/SpecKit Specify`. Создавай только `specs/<feature>/spec.md` на русском.
Jira и Confluence отключены: не создавай Stage, mapping или trace.
`specs/<feature>/` должна быть новой отдельной папкой данной feature. После
Specify её checklist, research, data-model, quickstart, plan и tasks создаются
в этой же папке соответствующими L2-командами; сейчас создавай только `spec.md`.

Перенеси без изменения смысла `WB-001`…`WB-013`. Допустимо сгруппировать их по
контракту, целостности пары, безопасному парсингу, snapshot/update и
критериям проверки, но в явной таблице трассируемости сохрани все исходные IDs.
Обязательно зафиксируй:

- `WORKBOOK_CONTRACT_VISION.md` как обязательную границу формата;
- атомарность и общие `PairId`, `PullRunId`, hashes/fingerprints/template metadata;
- parser как security boundary, fail-closed реакцию на formula/external link/VBA,
  повреждённые метаданные и недопустимые изменения;
- identity `RecordId|DraftRowToken`, правила ссылок и запрет ручной подстановки
  `DraftRowToken`;
- server-priority snapshot/update без автоматического удаления;
- read-only представление индексов и обязательный blocker
  `INDEX_SYNC_UNRESOLVED`.

Определи ограниченный L2-пилот с Excel desktop на Windows, успехом/остановкой,
fallback и решением человека. Не включай compare, immutable plan, BPMSoft Apply,
запись через браузер, Delete или изменение индексов. Не выдумывай данные,
доказательства, поддержку LibreOffice или допуск полного каталога.

Не создавай plan, tasks или код. Следующий обязательный шаг после Specify —
`/SpecKit Clarify`.
