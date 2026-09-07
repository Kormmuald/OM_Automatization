# Терминологическая ссылка: ручная регистрация раздела Creatio/Terrasoft

## Статус источника

- Локальная копия исходной статьи: `source-community-creatio.html.txt`.
- Исходный URL, сохранённый в HTML: `https://community.terrasoft.ru/questions/ruchnaya-registraciya-razdela`.
- Вопрос опубликован 30 мая 2017 года; технический ответ явно отмечает, что алгоритм проверялся на Terrasoft 7.9.2.
- Исходный локальный файл имел дату изменения 2021-07-30 и был добавлен в проект без изменения байтов.
- SHA-256 исходника и копии: `d3c09e25f773bd92788af1a475b566f95f89ff85f99de414d3b96c41363393a1`.

Расширение сохранённой копии намеренно изменено с `.html` на `.html.txt`: оригинальная страница содержит сторонние analytics/tracking scripts. Companion-folder со скриптами, стилями и аватарами в проект не копировался. Файл используется только как текстовый справочный источник и не должен исполняться как web page.

## Как применять к BPMSoft

По решению владельца терминология Creatio/Terrasoft из этой статьи применима к BPMSoft с подстановкой названия продукта. Это помогает понимать назначение системных колонок, но не заменяет evidence конкретной локальной версии BPMSoft 1.8.0.14107.

При расхождении приоритет имеют:

1. воспроизводимое strictly read-only evidence локального BPMSoft;
2. metadata/IL конкретных локальных assemblies;
3. утверждённый владельцем workbook contract;
4. эта историческая статья как терминологическая подсказка.

## Полезные соответствия из статьи

| Поле | Объяснение обычным языком |
|---|---|
| `SysSchema.UId` | UId схемы: устойчивый идентификатор самой схемы, на который ссылаются другие системные записи. |
| `SysModuleEntity.SysEntitySchemaUId` | UId объектной схемы раздела из `SysSchema`; это не `Id` записи `SysModuleEntity`. |
| `SysModuleEdit.SysModuleEntityId` | `Id` отдельной записи в таблице `SysModuleEntity`. |
| `SysModuleEdit.CardSchemaUId` | UId схемы страницы редактирования из `SysSchema`. |
| `SysModule.SectionModuleSchemaUId` | UId схемы модуля раздела, например `SectionModuleV2`. |
| `SysModule.SectionSchemaUId` | UId схемы страницы раздела из `SysSchema`. |
| `SysModule.SysModuleEntityId` | `Id` записи `SysModuleEntity`, а не UId объектной схемы. |
| `SysModule.CardModuleUId` | В позднем комментарии указано, что отсутствие этого значения нарушало формирование ссылки на карточку раздела. Точная современная семантика требует отдельной проверки. |

Главный вывод для текущего проекта: `Id` записи системной таблицы и `UId` схемы — разные идентификаторы с разными ролями. Нельзя подменять один другим только потому, что оба имеют формат GUID.

## Что источник подтверждает для текущего contract

- Использование `SchemaUId` / `SysSchema.UId` как relation identity для configuration metadata согласуется с исторической системной моделью.
- `SysEntitySchemaUId` означает ссылку на UId entity schema, а не ID registry-записи.
- `LookupRecordId`, `SysModuleEntityId` и другие поля с суффиксом `Id` могут идентифицировать записи своих системных таблиц и не равны UId связанной схемы.
- Решение P2-B остаётся корректным: значение `GetSchema.schema.id` сохраняется как защищённое диагностическое `GetSchemaSchemaIdCandidate`, пока его точная связь с `SysSchema.Id` не доказана независимым evidence.

## Чего статья не доказывает

- Она не доказывает, что `GetSchema.schema.id` в BPMSoft 1.8 равен `SysSchema.Id`.
- Она не описывает JSON contract `GetSchema` или `SelectQuery` локальной версии BPMSoft.
- Она не доказывает mapping extensions, indexes, lookup registry или write responses.
- Приведённые в статье SQL `insert` — исторический пример ручной регистрации раздела, а не инструкция текущему проекту. Выполнять эти запросы запрещено текущим read-only scope.
- Статья не открывает Write/Manage, delete, backup, `.xlsx` или production-tool gates.

## Правило дальнейшего использования

При разборе незнакомой BPMSoft-колонки сначала искать её терминологическое объяснение в этой статье и настоящей памятке, затем обязательно проверять смысл по local BPMSoft evidence. В contract фиксировать только доказанную семантику; если подтверждена лишь форма или имя поля, использовать явную пометку `Candidate` и не применять его в identity, relations, fingerprints или change plans.
