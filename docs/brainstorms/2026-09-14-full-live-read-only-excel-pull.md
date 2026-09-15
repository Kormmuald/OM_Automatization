---
date: 2026-09-14
topic: full-live-read-only-excel-pull
---

# Полная read-only выгрузка BPMSoft в Excel

## Что строим

Feature 001 расширяется управляемой ручной CLI-выгрузкой полного текущего
состояния объектной модели и справочников из локального BPMSoft. Результат каждой
успешной выгрузки — новая датированная пара Excel-книг в отдельной output-папке.
Существующие canonical `BPMSoft.ModelCatalog.xlsx` и `BPMSoft.LookupCatalog.xlsx`
никогда не перезаписываются.

## Почему этот подход

Исторический `WorkbookDeliveryTool` уже демонстрирует раздельную цепочку
read-only capture → проверенный source → Excel generation, но охватывает только
bounded baseline. Новая CLI должна расширить именно эту цепочку до полного
каталога, а не использовать legacy Google Sheets/SyncOM.

## Ключевые решения

- Запуск — только вручную в terminal; credentials вводит пользователь и они остаются
  в памяти процесса.
- Разрешены только read endpoints; Write/Manage/Apply/compare и browser workflow
  не входят в эту возможность.
- Reader обязан использовать declared order, paging, duplicate/skip/termination
  guards и fail-closed обработку неизвестных response shapes.
- Новая Excel-пара размещается в отдельной датированной output-папке; overwrite
  existing books запрещён.
- Реальный стенд не является автоматическим test fixture: automated tests используют
  sanitized fixtures/fake handler, а ручной live run получает отдельные stop criteria
  и evidence protocol.

## Открытые вопросы для планирования

- Точные declared scope metadata и формат output-пути.
- Полный endpoint/request matrix, подтверждённый владельцем для всех catalog kinds.
- Лимиты paging/scale, критерии полноты и модель безопасного evidence без raw values.
- Отдельный ручной G-2 protocol для первого live capture.

## Следующий шаг

Обновить mutable Feature 001 specification и затем провести class-aware
Clarify → Plan → Tasks для нового live read-only scope. Immutable
`preparation/docs/product-specs/` остаётся источником и не изменяется.
