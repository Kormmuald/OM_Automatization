# Оркестратор: read-only research и conditional workbook workflow

**Рекомендуемая модель:** `gpt-5.6-sol`, reasoning `high`.

## 1. Цель

Провести управляемую цепочку: доказать read-only контракт локального BPMSoft, зафиксировать evidence, получить решение владельца по каждому выявленному расхождению и только после положительных gates создать/проверить пару Excel-книг и минимальный инструмент её наполнения при необходимости.

Рабочая директория этого task: `C:\CodingAgents\codex\projects\OM_Automatization\preparation`. Все последующие относительные пути заданы от неё.

## 2. Источники истины

Прочитай полностью и в этом порядке:

1. `docs/PROJECT_HANDOFF.md`, особенно разделы 0, 1 и 13;
2. три файла `docs/SDD_DRAFTS/`;
3. `docs/WORKBOOK_CONTRACT_VISION.md`;
4. `PrototypeReadOnlyPull/Program.cs` и `.csproj` только как existing evidence;
5. numbered prompts в этой папке.

## 3. Scope и anti-scope

В scope — только цепочка, заданная numbered prompts, и её evidence.

Out of scope до соответствующих human gates: GitHub Spec Kit; `.specify/`; canonical spec/plan/tasks/slices; BPMSoft write/Manage; delete; Google как input; secrets; автоматический backup; изменение `SyncOM/`.

## 4. Последовательность и control gates

Перед запуском **каждого** следующего субагента выполни global skill `codex-five-hour-usage-gate` из рабочей директории:

```powershell
pwsh -NoProfile -File "C:\Users\Evgenii_2\.codex\skills\codex-five-hour-usage-gate\scripts\Get-CodexFiveHourUsage.ps1" -ThresholdPercent 20
```

Используй только возвращённое skill значение unique 300-minute window. Запуск разрешён лишь при `gate_result = PASS`, то есть если `remaining_percent` не ниже 20%. При `PAUSE`, `UNKNOWN`, malformed output или отсутствии unique 300-minute window не запускай субагента, не подменяй результат UI/оценкой и покажи владельцу remaining percent, reset time, threshold, gate result и source; жди его решения.

1. `01-read-only-researcher.md` — запускай только после **G1**: владелец разрешил strictly read-only research и usage gate дал `PASS`.
2. `02-evidence-reviewer.md` — после researcher evidence.
3. **G2:** если evidence неполно, противоречиво или разрушает contract, остановись, покажи source-backed finding и жди решения владельца.
4. `03-contract-decision-preparer.md` — только для proposal; изменения source contracts/drafts — лишь после **G3**: explicit owner approval.
5. `04-workbook-delivery.md` — только после **G4**: владелец принял read evidence, одобрил contract и отдельно разрешил `.xlsx`/tool work.
6. `05-verification-and-report.md` — после workbook delivery.
7. **G5:** собери criterion-to-evidence matrix, gaps и recommendation; human acceptance/изменение handoff только после обсуждения с владельцем.

## 5. Жёсткие остановки

Остановись и спроси владельца, не выбирая вариант сам, если: нет explicit deterministic order/pagination proof; mapping расходится с contract; требуется write API; нужна новая архитектура/public contract; предлагается Google data import; нужен секрет; proof не воспроизводится; Excel/tool work не был явно разрешён.

## 6. Первый ответ

Пока не меняй файлы и не запускай subagents. Верни только: role split, порядок, gates, конкретные риски и один вопрос для G1.

## 7. Итоговый формат после G5

Верни: выполненные роли; changed files; criterion-to-evidence matrix с путями/командами; not-run checks; findings и owner decisions; known gaps; рекомендацию `accept / accept with limits / request changes / reject`. Не принимай результат вместо человека.
