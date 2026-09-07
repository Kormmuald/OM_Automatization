# Оркестратор продолжения: финальная проверка workbook delivery и G5

**Рекомендуемая модель:** `gpt-5.6-sol`  
**Уровень рассуждений:** `high`

## Цель

Продолжить с зафиксированного состояния прежнего task, не повторяя завершённые research/contract/workbook этапы. Организовать независимую проверку исправленной пары Excel, подготовить G5-пакет решения владельца и только после явного решения владельца зафиксировать его в handoff/SDD/contract.

Рабочая директория: `C:\CodingAgents\codex\projects\OM_Automatization\preparation`. Все относительные пути ниже заданы от неё.

## Источники истины

Прочитай полностью и в этом порядке:

1. `docs/PROJECT_HANDOFF.md`, сначала раздел 14, затем разделы 0, 10.1, 10.2 и 13;
2. `docs/READ_ONLY_RESEARCH_RESULTS/20260904-04-workbook-delivery-result.md`;
3. `docs/READ_ONLY_RESEARCH_RESULTS/20260904-G4-owner-decision.md`;
4. `docs/WORKBOOK_CONTRACT_VISION.md`;
5. три файла `docs/SDD_DRAFTS/`;
6. `WorkbookDeliveryTool/Program.cs` и `.csproj`;
7. numbered prompts в `docs/WORKBOOK_CONTINUATION_PROMPTS/`.

Раздел 14 handoff является актуальным состоянием и явно отменяет старые «следующие шаги», если они ему противоречат.

## Обязательный usage gate перед каждым субагентом

Перед запуском **каждого** следующего субагента полностью прочитай и примени global skill `codex-five-hour-usage-gate`, затем из рабочей директории выполни:

```powershell
pwsh -NoProfile -File "C:\Users\Evgenii_2\.codex\skills\codex-five-hour-usage-gate\scripts\Get-CodexFiveHourUsage.ps1" -ThresholdPercent 20
```

Используй только unique 300-minute window. Запуск разрешён лишь при `gate_result = PASS` и `remaining_percent >= 20`. При `PAUSE`, `UNKNOWN`, malformed output или отсутствии unique 300-minute window не запускай субагента и не подменяй результат UI/оценкой. Покажи владельцу `remaining_percent`, `reset_time`, `threshold`, `gate_result`, `source` и жди решения. Старое разовое исключение уже использовано и не действует.

## Последовательность

1. Проверь актуальный статус canonical replacement в handoff. Если template-v2 файлы всё ещё находятся в `audit/pending-v2/`, сначала убедись, что обе canonical книги закрыты; сохрани текущую пару как superseded evidence, замени обе книги вместе, повтори tool verify/hashes и добавь append-only audit. Не запускай для этой механической локальной операции субагента.
2. Убедись, что исправленные книги существуют по canonical paths и что latest delivery report описывает объединённый `LookupValues`, разрешённые filter/resize operations и owner waiver фактической сортировки protected locked ranges.
3. После usage `PASS` запусти `01-workbook-evidence-reviewer.md`.
4. Если reviewer находит повреждение, потерю данных, расхождение contract или невоспроизводимую проверку — остановись и покажи владельцу source-backed finding. Не исправляй автоматически.
5. После нового usage `PASS` запусти `02-g5-decision-preparer.md`.
6. Покажи владельцу G5 matrix, gaps и recommendation. Не принимай результат вместо владельца.
7. Только после явного owner decision и нового usage `PASS` запусти `03-owner-decision-recorder.md` с дословным решением владельца.

## Scope

Разрешены offline build/self-test/verify, чтение OOXML, read-only открытие книг в desktop Excel, проверка evidence и запись новых audit/report файлов. Повторный BPMSoft capture не нужен и не разрешён без нового явного решения владельца.

Запрещены BPMSoft Write/Manage/delete/compile/save/create/update, Google input, изменение `SyncOM/`, secrets, auto backup, GitHub Spec Kit и canonical spec/plan/tasks/slices. Full catalog не является условием G5 этой bounded delivery.

## Критические проверки

- `LookupCatalog` содержит `LookupValues` и не содержит `LookupRows`;
- `LookupValues` содержит все row metadata и value fields, а metadata согласована внутри каждой группы записи;
- desktop Excel не предлагает recovery;
- защита сохраняет protected cells, но позволяет менять ширину колонок и применять существующие фильтры; фактическая сортировка locked read-only ranges не является acceptance criterion по решению владельца;
- две книги связаны одинаковыми pair metadata;
- индексы экспортированы только из `schema.indexes[]`, без вывода из `ActualIndexed`;
- будущая загрузка индексов остаётся `INDEX_SYNC_UNRESOLVED`;
- отсутствуют formulas, external links, macros, connections и secrets.

## Первый ответ нового task

Кратко верни: восстановленное состояние, оставшиеся роли, gates, известные ограничения и что именно будет проверено. Затем выполняй цепочку; не перезапускай старые роли `01`–`04` прежнего пакета.

## Итог

До решения владельца: только draft G5 report. После решения: перечисли выполненные роли, changed files, criterion-to-evidence matrix с путями/командами, not-run checks, findings, owner decisions, known gaps и рекомендацию `accept / accept with limits / request changes / reject` как рекомендацию агента, не как human acceptance.
