# Контрольный пакет G0

- **Run ID:** `20260907-165314-7f12`
- **Project root / shared checkout:** `C:/CodingAgents/codex/projects/OM_Automatization`
- **Project / host:** `25d4b380-7443-40eb-b1e3-fe9949a2592d` / `local`
- **RUN_DIR:** `C:/CodingAgents/codex/projects/OM_Automatization/specReadinessPrompts/runs/20260907-165314-7f12`
- **Immutable sources:** весь `preparation/docs/product-specs/` (включая untracked historical transfer-prompts), `preparation/docs/WORKBOOK_CONTRACT_VISION.md`; не редактировать, не перемещать и не удалять.
- **Settings to preserve:** `AGENTS.md`, `.specify/project.yml`, `.specify/memory/constitution.md`, skills/workflows/extensions; исходно имеются пользовательские незакоммиченные изменения `.specify/memory/constitution.md` и `.specify/project.yml`.

## Инвентарь на старте

- Существуют только `specs/001-read-only-catalog-qualification/spec.md`, `specs/002-workbook-pair-control/spec.md`, `specs/003-compare-read-only-plan/spec.md`.
- `specs/004-gated-apply-operations/` отсутствует и будет создана ролью 01 только в согласованном run.
- До запуска не обнаружены `plan.md`, `tasks.md` или implementation evidence в трёх целевых каталогах.
- В `preparation/docs/product-specs/local-bpmsoft-synchronizer/` присутствуют общий `spec.md`, пять этапных источников `01`–`05` и вспомогательные исторические документы; они входят в неизменяемую область.

## Последовательность и разрешённые области

| Роль | Модель / thinking | Цель | Разрешённые изменения |
| --- | --- | --- | --- |
| 01 Пересборщик | `gpt-5.6-sol` / `high` | Backup и пересборка четырёх Specify-артефактов | Три проверенных старых `specs/001`–`003` после верифицированного backup, новая `004`, `.specify/feature.json`, обязательные local skipped traces, RUN_DIR |
| 02 Связанность | `gpt-5.6-terra` / `high` | Закрепить зависимости и накопительную регрессию | Точечные изменения четырёх specs/checklists, `AGENTS.md`, RUN_DIR |
| 03 Проверщик | `gpt-5.6-sol` / `high` | Независимый аудит | Только отчёты/findings/handoff/manifest в RUN_DIR |
| 04 Корректор | `gpt-5.6-sol` / `high` | Сначала план, затем только утверждённые исправления | Фаза A: только файлы RUN_DIR; фаза B: только diff утверждённого плана |

Каждая роль получает полный актуальный `HANDOFF.md`, полный `README.md`, свой полный role prompt и прямой запрет делегировать работу или запускать другие задачи. Одновременно разрешён только один исполнитель. При паузе, вопросе, hash/manifest mismatch, появлении чужих файлов в зоне перезаписи либо работе вне scope следующий переход запрещён.

## G0 — запрашиваемое разрешение

Разрешение запускает только роль 01 с указанными моделью и thinking. Перед удалением она обязана подтвердить backup и состав трёх точных каталогов; исходные черновики, конституция, настройки и production-код не входят в delete scope.
