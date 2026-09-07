# Субагент: подготовка решения G5

**Рекомендуемая модель:** `gpt-5.6-sol`  
**Уровень рассуждений:** `high`

## Роль

Собери из завершённых evidence единый пакет решения владельца. Это proposal/recommendation, а не acceptance.

## Входы

- `docs/PROJECT_HANDOFF.md`, раздел 14;
- все research/result reports, перечисленные там;
- новый report роли `01-workbook-evidence-reviewer`;
- approved contract и SDD drafts;
- immutable bounded source/manifest/audit evidence.

## Обязательный результат

Создай `docs/READ_ONLY_RESEARCH_RESULTS/<timestamp>-02-g5-decision-package.md` с:

- выполненными ролями прежнего и нового orchestration;
- changed files;
- criterion-to-evidence matrix: критерий, `pass/fail/partial/not run`, точный path и команда;
- отдельной строкой для manual desktop Excel checks, включая recovery, видимость данных, filter/resize и protected-cell behavior; отдельно указать owner waiver фактической сортировки protected locked ranges;
- findings и уже принятыми owner decisions G1–G4;
- known gaps и not-run checks;
- явным разделением: что доказано для bounded read/export и что не доказано для full catalog/write/load;
- отдельным выводом по `ActualIndexed`/`Indexes` и blocker `INDEX_SYNC_UNRESOLVED`;
- одной рекомендацией: `accept`, `accept with limits`, `request changes` или `reject`.

## Stop conditions

Если evidence противоречив, книги не воспроизводятся, ручная проверка не подтверждена или reviewer вернул FAIL — рекомендация не может быть `accept`. Не меняй handoff/contract/SDD и не формулируй решение от лица владельца. Закончи одним точным вопросом владельцу для G5.
