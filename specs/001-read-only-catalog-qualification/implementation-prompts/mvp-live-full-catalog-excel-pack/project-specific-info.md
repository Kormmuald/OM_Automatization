# Project-specific information

## Рабочая область и feature

- Workspace: `C:/CodingAgents/codex/projects/OM_Automatization`.
- Active feature: `specs/001-read-only-catalog-qualification/`.
- Production entry point: `src/BpmSoftSync.Cli`.
- Текущий handoff: `specs/001-read-only-catalog-qualification/HANDOFF.md`.
- Stage evidence: `specs/001-read-only-catalog-qualification/verification/mvp-slices/`.

Перед каждым stage сверяй `.specify/feature.json`: номер ветки или имя каталога сами по
себе не переключают active feature.

## Canonical и reference sources

Canonical mutable sources для сверки: Feature 001 `spec.md`, `plan.md`, `tasks.md`,
`research.md`, `data-model.md`, `test-plan.md`, `contracts/cli-contract.md` и текущий
`HANDOFF.md`. При конфликте остановись и передай его оркестратору.

Immutable sources — только для чтения:

- `preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md` — общее видение;
- `preparation/PrototypeReadOnlyPull/` — прототип чтения BPMSoft;
- `preparation/WorkbookDeliveryTool/` — прототип Excel delivery;
- `preparation/workbooks/` — исходные workbook templates.

Монолитный `../MVP-live-full-catalog-excel.md` фиксирует исходный MVP intent. Этот pack
декомпозирует его, но не уменьшает.

Чтобы не загружать монолит целиком на каждом этапе, используй такую маршрутизацию его
разделов (вместе с относящимися canonical artifacts):

| Stage | Разделы монолитной постановки |
| --- | --- |
| S00 | весь документ: reconciliation без реализации |
| S01 | «Production composition root», «Реальная read-only BPMSoft сессия», security части «Tests-first» |
| S02 | «Полный WorkspaceInventory и объектная модель» |
| S03 | «Полная выгрузка справочников», paging части «Общий ordered reader» |
| S04 | «Общий ordered reader и два полных прохода», `QualifiedCatalogSnapshot/v1`, «Разделение output и evidence» |
| S05 | «Реальная Excel-пара» и относящиеся критерии готовности |
| S06 | composition root, offline/E2E, diagnostics и deliverable live-проверки |
| S07 | opt-in live test, live prompt, stop conditions и критерии готовности |
| S08 | документация, финальный отчёт и фактические статусы |

## Ограничения исполнения

В рабочем дереве могут быть чужие незакоммиченные изменения. Каждый worker меняет
только файлы своего stage, перечисляет их в результате и не выполняет Git mutation.
Reviewer проверяет фактический diff, а не доверяет текстовому отчёту worker.

S01–S06 используют только fixtures/fakes. Реальный BPMSoft разрешён только в S07 после
accepted S06 и явного подтверждения пользователя в текущем чате. Credentials вводятся
пользователем в terminal и нигде не сохраняются.
