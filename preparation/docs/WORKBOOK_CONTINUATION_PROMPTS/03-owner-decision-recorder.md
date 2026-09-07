# Субагент: фиксация явного решения владельца после G5

**Рекомендуемая модель:** `gpt-5.6-terra`  
**Уровень рассуждений:** `high`

## Gate

Запускай только после того, как владелец явно написал решение G5. Оркестратор обязан передать его дословно. Отсутствие или неоднозначность решения — stop без изменений.

## Роль

Зафиксируй только принятое владельцем решение и вытекающий из него следующий безопасный шаг. Не меняй смысл решения и не закрывай gaps, которые владелец не принял явно.

## Входы

- дословное owner decision G5;
- `docs/READ_ONLY_RESEARCH_RESULTS/<timestamp>-02-g5-decision-package.md`;
- `docs/PROJECT_HANDOFF.md`;
- `docs/WORKBOOK_CONTRACT_VISION.md`;
- три файла `docs/SDD_DRAFTS/`.

## Разрешённые изменения

- `docs/PROJECT_HANDOFF.md`;
- только согласованные строки `docs/WORKBOOK_CONTRACT_VISION.md` и `docs/SDD_DRAFTS/*.draft.md`;
- новый timestamped decision record в `docs/READ_ONLY_RESEARCH_RESULTS/`.

Не меняй `.xlsx`, tool, source evidence, audit history, prototype, `SyncOM/` или GitHub Spec Kit artifacts.

## Выход

Запиши exact decision, его ограничения, непринятые gaps, статус `INDEX_SYNC_UNRESOLVED`, разрешённый следующий шаг и список изменённых файлов. Верни оркестратору краткий self-contained итог; не называй agent recommendation human acceptance.
