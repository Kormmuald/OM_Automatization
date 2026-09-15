# Cycle 3 — H-005: semantic rewrite opaque `package.id`

**Дата:** 2026-09-15
**Статус:** offline implementation evidence; не является independent review,
live-admission или квалификацией target.

## Принятое узкое правило

`schema.package.id` принимается только как непустая JSON-строка. Это opaque
provenance, а не GUID и не ключ join/identity. Сразу после него
`schema.package.uId` обязан пройти `GuidString`; именно этот GUID является
единственной primary package identity. Любое missing/null/whitespace/non-string
`package.id`, а также missing/invalid `package.uId`, останавливает parsing
fail-closed с закрытым structural diagnostic.

## Изменённые инварианты

- `PackageLayerIdentity.PrimaryIdentityKey` состоит только из `package.uId`;
  package name и layer kind остаются описательными metadata.
- Raw opaque scalar удерживается только в in-memory provenance. Для component
  change detection используется односторонний SHA-256 digest; raw scalar не
  включён в stable identity, TargetFingerprint input, evidence, CLI или Excel
  cells.
- `CatalogPassBuilder` не строит pass, если у schema нет non-empty opaque
  provenance или typed package GUID. Дубликат пары `(schema.uId, package.uId)`
  также terminally blocks pass.
- Изменение opaque provenance между независимыми Pass A/B меняет безопасный
  component/fingerprint digest, поэтому reconciliation возвращает
  `TARGET_STATE_CHANGED_DURING_QUALIFICATION`, без retry или Pass C.

## Offline proof

- adapter tests: opaque и GUID-looking строки accepted только с valid
  `package.uId`; missing/null/whitespace/non-string `id` и invalid `uId`
  fail-close без scalar leakage;
- domain tests: primary identity — только GUID, два разных opaque provenance
  не попадают в key;
- A/B test: differing opaque provenance does not merge into a snapshot;
  durable qualification evidence excludes the canary;
- Excel projection test: package provenance canaries absent from both workbook
  projections;
- existing fingerprint assertions and the added A/B case prove that provenance
  changes are detected through safe digests rather than raw values.

Ни live/network/auth/credential action, Excel publication, retry/rerun,
`SELECT_QUERY`, Write/Manage/Compare/Apply, browser/SQL mutation, commit или
push в этом cycle не выполнялись. Full catalog remains unqualified until a
separate independent review and a new explicit human decision.
