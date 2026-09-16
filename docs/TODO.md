# TODO

## Apply MVP discovery

- [ ] Владелец выбирает минимальный candidate kind для Apply MVP. Пока все write kinds `DENIED`.
- [ ] Для выбранного kind описать version-specific BPMSoft write contract: endpoint, canonical payload, permission model, expected response, exact read-back и manual recovery boundary.
- [ ] Описать immutable `compare-plan.json` admission: canonical schema, duplicate/unknown field rejection, plan/input/workbook/target staleness checks и exact plan ordering.
- [ ] Определить mandatory preflight outcomes: positive, permission denied, stale state, missing/ambiguous identity, timeout/HTTP failure, malformed response и read-back mismatch.
- [ ] Спроектировать deny-by-default dispatcher без delete, rollback, index, structural или hidden write endpoint.
- [ ] Определить two-person/human gates, backup confirmation, stop-first-error semantics, per-operation audit и no-automatic-retry/recovery policy.
- [ ] Отдельно исследовать `LookupReference`: `ReferenceRecordId` format, target existence, rights, reference graph и dependencies до любого write.
- [ ] Подготовить synthetic/fake-transport tests и fault injection. Реальные write probes только после owner decision и отдельного authorization.
- [ ] Добавить regression test для workbook, сохранённой desktop Excel: shared strings, normal theme/docProps parts и combined validation `sqref`.
- [ ] Закрыть независимо `CATALOG_ORDER_OR_PAGING_UNQUALIFIED` и решить судьбу strict full-catalog qualification; Compare evidence это не заменяет.
