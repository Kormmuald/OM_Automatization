# Обновление действующего проекта из корпоративного шаблона

Для новых тестовых проектов можно распаковать шаблон целиком.

Для действующих проектов используйте скрипт обновления, чтобы не затереть рабочие артефакты проекта.

## Предпросмотр

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\update-methodology.ps1 -TargetPath "C:\path\to\existing-project"
```

## Применение

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\update-methodology.ps1 -TargetPath "C:\path\to\existing-project" -Apply
```

Скрипт обновляет методические файлы и папки:

- `.agents/skills`
- `.specify/extensions`
- `.specify/integrations`
- `.specify/scripts`
- `.specify/templates`
- `.specify/workflows`
- `.specify/extensions.yml`
- `.specify/integration.json`
- `.specify/.gitignore`
- `AGENTS.md`
- `scripts`
- `UPDATE_FROM_TEMPLATE.md`
- `METHODOLOGY.md`
- `SPEC_KIT_CUSTOMIZATIONS.md`

Скрипт работает мягко внутри перечисленных путей: файлы из шаблона перезаписывают одноименные managed-файлы проекта, а проектные файлы и папки, которых нет в manifest методики, сохраняются. Файлы, которые были записаны предыдущей версией методики и tracked в `.specify/integrations/*.manifest.json`, удаляются только если их больше нет в новом шаблоне. Это позволяет убирать устаревшие managed hooks/workflow/scripts, не стирая локальные проектные расширения, skills, шаблоны или утилиты.

Скрипт сохраняет проектные данные:

- `specs`
- `.specify/memory`
- `.specify/project.yml`
- `.specify/jira-constitution-mapping.json`
- `.specify/traces`
- `README.md`

После замены методических файлов скрипт проверяет сохраненные `.specify/project.yml` и `.specify/memory/constitution.md`. Если в `project.yml` не хватает новых ключей, он переносит legacy-значения языка документов и Confluence root из конституции в `.specify/project.yml`. Конституция не дополняется новыми runtime-разделами.

Перед обновлением `.agents/skills` скрипт проверяет managed skills методики. После обновления он проверяет, что каждый `SKILL.md`, включая сохраненные проектные skills, записан как UTF-8 без BOM и начинается с YAML frontmatter. Для `speckit-*` skills он также проверяет наличие `agents/openai.yaml` с `interface.short_description` и explicit-only policy у пользовательских SpecKit-команд. Затем скрипт пересобирает `.specify/integrations/codex.manifest.json` по фактическому набору `SKILL.md` и `openai.yaml`, а `.specify/integrations/corporate-methodology.manifest.json` - по managed-файлам методики для будущих мягких обновлений.

Перед заменой скрипт создает backup в `.specify/backups/methodology-update-YYYYMMDD-HHMMSS`.

`README.md` принадлежит проекту: скрипт его не заменяет, не удаляет и не использует для определения версии методики. Актуальная версия и описание методики находятся в `METHODOLOGY.md`.
