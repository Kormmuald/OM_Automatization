# Корпоративная методика Spec Kit

Этот файл описывает установленную в проекте корпоративную методику Spec Kit, её состав и правила обновления.

Версия методики: **1.3.16**.

## Структура

- `.specify/` - инфраструктура Spec Kit, созданная через `specify init`.
- `.specify/project.yml` - проектные runtime-настройки: язык документов, выбранный класс инициативы и политика синхронизации Jira/Confluence.
- `AGENTS.md` - общие инструкции для Codex в проектах, созданных из шаблона.
- `.agents/` - Codex skills, установленные интеграцией Spec Kit.
- `specs/` - будущие feature specifications, которые будут создаваться workflow Spec Kit.
- `scripts/update-methodology.ps1` - безопасное обновление действующих проектов из новой версии шаблона.
- `UPDATE_FROM_TEMPLATE.md` - инструкция по обновлению действующего проекта.
- `.specify/templates/` - место хранения и переопределения шаблонов Spec Kit.
- `.specify/workflows/speckit/class-workflow.yml` - методическая матрица `L1/L2/L3 -> workflow profile -> commands -> artifacts`.
- `.specify/extensions.yml` - hooks, которые подключают `speckit-class-gate` перед базовыми командами Spec Kit.
- `.specify/extensions/jira/` - встроенное расширение `spec-kit-jira` для фоновой проекции Spec Kit артефактов в Jira.
- `.specify/extensions/confluence/` - встроенное расширение `spec-kit-confluence` для создания, чтения и обновления Confluence-зеркал по `specs/<feature>/spec.md`.
- `.specify/extensions/archive/` - встроенное расширение `spec-kit-archive` для консолидации завершенных L1/L2/L3 feature artifacts в `.specify/memory/`.
- `.agents/skills/speckit-converge/` - встроенная команда Spec Kit для обязательного L2/L3 сведения реализации с `spec.md`, `plan.md` и `tasks.md` после `/SpecKit Implement`.
- `.specify/extensions/verify/` - встроенное расширение `spec-kit-verify` для обязательной L2/L3 проверки реализации после successful `/SpecKit Converge`.
- `.specify/extensions/critique/` - встроенное расширение `spec-kit-critique` для обязательной L3 критики spec/plan после `/SpecKit Plan`.
- `.specify/templates/test-plan-template.md` - корпоративный шаблон формального тест-плана для проверок каждого инкремента и итогового решения.
- `SPEC_KIT_CUSTOMIZATIONS.md` - журнал отличий от коробочного Spec Kit и vendored upstream-расширений.

## Инициализация

Создано командой:

```powershell
specify init template_project --integration codex --integration-options "--skills" --script ps
```

## Обновление действующего проекта

Для действующих проектов не распаковывайте шаблон поверх всей папки проекта. Используйте `scripts/update-methodology.ps1`: он заменяет только методические файлы, делает backup, сохраняет `specs/`, `.specify/memory/`, `.specify/project.yml`, runtime traces и проектные Codex skills, а также переносит legacy-настройки языка и Confluence root из сохраненной конституции в `.specify/project.yml`, если таких ключей еще нет. В `.agents/skills` методика управляет только namespace `speckit-*`; остальные skills принадлежат проекту и не удаляются при обновлении.

До замены skills скрипт проверяет, что каждый `SKILL.md` записан как UTF-8 без BOM и начинается с YAML frontmatter, затем проверяет UI-метаданные и invocation policy в `agents/openai.yaml`. После замены он повторяет проверку и пересобирает `.specify/integrations/codex.manifest.json` по фактическим `SKILL.md` и `openai.yaml`. Пользовательские SpecKit-команды имеют `allow_implicit_invocation: false`: они остаются доступны через явный `$speckit-...`, но не расходуют ограниченный бюджет начального каталога skills; служебные hooks сохраняют автоматический вызов.

Подробная инструкция: `UPDATE_FROM_TEMPLATE.md`.

## Текущая настройка

- `.specify/templates/constitution-template.md` и `.specify/memory/constitution.md` устроены как единый широкий шаблон конституции: сначала идут обязательные `L1` блоки в логике Spec Kit, затем после разделителя `---` идет `Бизнес-кейс L2`.
- Статус согласования хранится прямо в конституции в разделе `## Статус согласования`; перед `/SpecKit Init` требуется значение `Согласовано`. Старое значение `Approved` продолжает приниматься для совместимости.
- Язык документов для `/SpecKit Constitution` и `/SpecKit Specify` хранится в `.specify/project.yml` в ключе `project.document_language`; допустимые значения - `ru` и `en`, по умолчанию `ru`. Прочие skills продолжают создавать свои документы на английском языке независимо от этой настройки.
- Все ответы, вопросы, отчеты, предупреждения и next actions, которые skills выводят пользователю в окно чата Codex, должны быть на русском языке независимо от `project.document_language`. Это общее правило задано в корневом `AGENTS.md`, поэтому оно применяется и к новым skills без правки их `SKILL.md`. Технические идентификаторы, пути, команды, ключи конфигурации, Jira keys и requirement IDs сохраняются без перевода.
- `L1` часть покрывает рамку проекта, принципы, базовую форму поставки, границы работ, анти-границы, допущения, контрольные точки человека, требования к проверке, модель решения о приемке, источник истины и управление.
- `Бизнес-кейс L2` обязателен начиная с `L2` и содержит бизнес-заказчика, участников, исходные материалы, проблему/возможность, идею, эффект, сроки, бюджет, риски, ограничения, коммуникации, критерии успеха и открытые вопросы.
- `/SpecKit Init` сам анализирует `.specify/memory/constitution.md`, сопоставляет факты с `.specify/workflows/speckit/class-workflow.yml`, рекомендует конкретный класс `L1 - Proof of Concept`, `L2 - Pilot` или `L3 - Production Solution` и записывает его в `.specify/project.yml`. Если фактов достаточно для `high` или `medium` confidence, Init не задает пользователю вопрос со свободным выбором класса; вопросы допустимы только для недостающих или конфликтующих классификационных фактов либо для явного override. Недостающие runtime-настройки Init задаются вопросами на русском языке; когда среда Codex поддерживает интерактивный выбор, варианты показываются как кнопки, а при запуске Init выводится подсказка про режим `"План"`. Отдельной команды `/SpecKit Classify` в активном контуре нет; изменение класса выполняется повторным `/SpecKit Init` с сохранением остальных проектных настроек. После Init для всех классов обязателен явный `/SpecKit Specify`.
- `/SpecKit Init` обязателен для всех классов инициатив. Для самого Init gate проверяет готовность и согласование конституции, а для всех последующих class-aware команд требует заполненный `initiative.class` и workflow overlay в `.specify/project.yml`.
- `speckit-class-gate` живет в `.agents/skills/speckit-class-gate` и подключен через `.specify/extensions.yml` как mandatory hook перед `specify`, `clarify`, `plan`, `analyze`, `tasks` и `implement`; overlay отвечает за профиль класса, а не за enforcement.
- `/SpecKit Clarify` задает до 5 уточняющих вопросов за один запуск, предлагает рекомендуемый вариант и варианты ответов на русском языке, сохраняя технические маркеры Spec Kit без перевода; когда среда Codex поддерживает интерактивный выбор, варианты показываются как кнопки. Повторные запуски в тот же день записываются отдельными заголовками `### Session YYYY-MM-DD #2`, `#3` и так далее.
- `/SpecKit Plan` дополнительно создает `specs/<feature>/test-plan.md` и завершает работу подробным русскоязычным отчетом по выполненному планированию, ключевым архитектурным решениям, gates, рискам и следующему шагу; `/SpecKit Tasks` обязан превратить проверки каждого Story-инкремента и финального решения в задачи до реализации.
- `/SpecKit Specify` является обязательным первым feature-шагом после `/SpecKit Init`: он создает `specs/<feature>/spec.md`. Количество пользовательских историй в `spec.md` следует смыслу feature, а не форме шаблона.
- `spec-kit-jira` установлен как часть шаблона в `.specify/extensions/jira`; перед `/SpecKit Init` автоматически срабатывает legacy hook `speckit-jira-constitution-sync`, после `/SpecKit Tasks` - `speckit-jira-background-trace`, после `/SpecKit Implement` - `speckit-jira-background-sync`, после успешной архивации конкретной спеки - `speckit-jira-complete-spec`. Эти hooks обращаются к Jira только если `.specify/project.yml` содержит `integrations.jira.sync_enabled: true`.
- `spec-kit-confluence` установлен как часть шаблона в `.specify/extensions/confluence`; перед `/SpecKit Plan` автоматически срабатывает `speckit-confluence-background-spec-sync`, который создает или обновляет Confluence-зеркало только для `specs/<feature>/spec.md`. Ручные команды `speckit.confluence.write`, `speckit.confluence.read` и `speckit.confluence.update` доступны через Codex skills. Запись и обновление Confluence разрешены только если `.specify/project.yml` содержит `integrations.confluence.sync_enabled: true`.
- Confluence-зеркало публикуется на русском языке: синхронизатор переводит пользовательский текст `spec.md`, но сохраняет технические идентификаторы, пути, команды, URL, code blocks, IDs и source refs без смысловой порчи. Локальный `spec.md` не изменяется.
- Корневая страница Confluence для дочерних страниц спецификаций задается в `.specify/project.yml` ключом `integrations.confluence.spec_root_page_url`. Старые разделы конституции `## Корневая страница Confluence` / `## Confluence root page` поддерживаются только как fallback при миграции. Если URL не задан, Confluence hook не блокирует `/SpecKit Plan`, а пишет skip trace.
- Основные следы Confluence-интеграции: `specs/<feature>/confluence-mapping.json` со ссылкой на страницу и `specs/<feature>/confluence-sync-trace.json`; fallback при невозможности определить feature - `.specify/traces/confluence/background-spec-sync.json`.
- `spec-kit-archive` установлен как часть шаблона в `.specify/extensions/archive`; для `L1` после `/SpecKit Implement`, а для `L2` и `L3` после successful `/SpecKit Converge` и `speckit.verify.run` обязательно запускается `speckit.archive.run` с текущим feature directory и без scope-флагов вроде `--plan-only`, поэтому архивируются все поддерживаемые project memory artifacts.
- `/SpecKit Converge` обязателен для `L2` и `L3` после `/SpecKit Implement`; если converge добавил новые tasks, downstream hooks текущего запуска останавливаются до повторного implement/converge цикла.
- `spec-kit-verify` установлен как часть шаблона в `.specify/extensions/verify`; для `L2` и `L3` после successful `/SpecKit Converge` обязательно запускается `speckit.verify.run` перед Jira status sync и archive.
- `spec-kit-critique` установлен как часть шаблона в `.specify/extensions/critique`; для `L3` после `/SpecKit Plan` обязательно запускается `speckit.critique.run`, который создает `specs/<feature>/critiques/critique-*.md` перед переходом к tasks.
- Базовая Jira-проекция настроена на проект `TPL`: конституция создает/обновляет `Stage`, feature spec создает/обновляет `Epic`, фазы и задачи не синхронизируются.
- Описание `Stage` рендерится из конституции в Jira wiki markup через `.specify/extensions/jira/renderers/constitution-stage.jira`, а не отправляется сырым Markdown.
- `Epic` связывается со `Stage` через поле `Epic.Stage` (`customfield_18723`, обнаружено через Atlassian MCP).
- Jira hooks не требуют ручного вызова: если `.specify/project.yml` отключает Jira sync или `.specify/extensions/jira/jira-config.yml` не содержит `project.key` и не задан `SPECKIT_JIRA_PROJECT_KEY`, они не блокируют основной workflow и пишут trace со статусом `skipped`.
- После архивации `speckit-jira-complete-spec` переводит feature-level Jira issue спеки в `Done`, используя `workflow.done_transition` / `workflow.done_status` из `.specify/extensions/jira/jira-config.yml`; по умолчанию это `Done`.
- Основные следы Jira-интеграции: `.specify/jira-constitution-mapping.json`, `.specify/traces/jira/constitution-sync.json`, `specs/<feature>/jira-mapping.json`, `specs/<feature>/jira-trace.json`, `specs/<feature>/jira-sync-trace.json`, `specs/<feature>/jira-completion-trace.json`; fallback при невозможности определить feature - `.specify/traces/jira/*.json`.
