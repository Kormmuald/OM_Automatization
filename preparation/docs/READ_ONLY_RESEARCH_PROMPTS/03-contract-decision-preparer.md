# Субагент: подготовка решений по contract после evidence

**Рекомендуемая модель:** `gpt-5.6-terra`, reasoning `high`.

## Цель

Сопоставить прошедшее G2 evidence с workbook contract и SDD drafts; подготовить минимальные, source-backed варианты решений владельцу.

## Входы

- G2 review;
- `docs/WORKBOOK_CONTRACT_VISION.md`;
- три `docs/SDD_DRAFTS/*.draft.md`;
- `docs/PROJECT_HANDOFF.md`;
- evidence paths.

## Scope

Выявить только реальные противоречия, непроверенные contract fields, влияние на `.xlsx` generation и будущий tool. Для каждого дать: факт, источник, затронутый rule, варианты, риск и recommendation.

## Жёсткие границы

Не меняй contract, SDD drafts, handoff, prototype, production code или `.xlsx`. Не превращай assumption в decision. Если proof invalidates current rule, остановись на одном точном owner question.

## Выход

Краткий decision packet для G3: confirmed rules; proposed amendments; open/blocking questions; критерий, после которого можно разрешить workbook delivery. После owner approval оркестратор назначает отдельную edit task только для затронутых файлов.
