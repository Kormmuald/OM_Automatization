# Owner decision — workbook editability and reference precedence

**Date:** 2026-09-05  
**Status:** APPROVED CONTRACT CORRECTION; not G5 acceptance

Владелец явно разрешил:

- одновременно заполнять `LookupValues.ReferenceRecordId` и `ReferenceDraftRowToken`;
- при будущей загрузке использовать валидный непустой `ReferenceRecordId`, иначе однозначно разрешённый через read-back mapping `ReferenceDraftRowToken`, иначе пустую ссылку;
- блокировать загрузку при некорректном непустом `ReferenceRecordId`, не трактуя его как отсутствие GUID;
- применять Excel sheet protection только к листам, которые полностью read-only;
- оставлять mixed-листы `Schemas`, `Columns`, `LookupRegistry` и `LookupValues` целиком редактируемыми, сохраняя parser/compare как обязательный hard gate для недопустимых изменений.

Это решение отменяет прежнее требование workbook-level mutual exclusion для `ReferenceRecordId` / `ReferenceDraftRowToken` и прежнюю cell-level protection mixed-листов. Оно не разрешает BPMSoft Write/Manage, index load/apply, изменение `SyncOM/` или закрытие G5.

Владелец также повторно разрешил один раз проигнорировать 20%-ный usage threshold для запуска следующего субагента. Исключение одноразовое и не меняет постоянную gate policy.
