# S02 offline validation summary

Scope: only sanitized fixtures and the capture-only transport. No live BPMSoft target,
credential prompt, HTTP client, Excel, compare, Apply, browser, or Git action was used.

## Result

- `dotnet build BpmSoftSync.sln --configuration Release --no-restore`: PASS, zero warnings and zero errors.
- `BpmSoftSync.Domain.Tests`: PASS. Negative paging fixtures terminate with
  `CATALOG_ORDER_OR_PAGING_UNQUALIFIED`; the clean fixture yields two canonical page
  manifests whose ordinal, SHA-256 progress-token digest, count, first/last identity
  digests, ordered identity digest, and response-size bucket match independent safe
  vectors. Every manifest-approved sanitized fixture has a lowercase SHA-256 digest
  that is recomputed from its file by the offline runner.
- `BpmSoftSync.Adapters.BpmSoft.Tests`: PASS. The unknown-shape structural envelope
  records JSON kind, scalar class/length, and digests only. `RAW_LOOKUP_CANARY` is not
  retained in the envelope. A missing envelope produces no inventory item and the named
  `UnknownShapeUnqualified` / `UNKNOWN_SHAPE_ENVELOPE_REQUIRED` blocker. Index relations
  use only `columns[].columnUId` and retain `INDEX_SYNC_UNRESOLVED`.
- `TargetFingerprint/v1`: PASS metamorphic checks. Reordered source entries and two
  semantically equivalent JSON sources with different named-property order preserve the
  digest; changed identity, page manifest, support status, or version evidence changes it.
  Missing version evidence is represented as `TARGET_VERSION_METADATA_UNAVAILABLE`, never guessed.
- `LegacyDispositionManifest/v1`: PASS for `reuse-semantics`, `rewrite`, `drop`, and
  `defer`; unknown disposition is rejected as `LEGACY_DISPOSITION_INVALID`.

## Retained blockers and boundaries

- `FULL_CATALOG_NOT_QUALIFIED` remains open.
- No two-pass qualification, retry, Pass C, live authorization, or real network run exists
  in S02.
- `INDEX_SYNC_UNRESOLVED` is read-only evidence and cannot produce index plan/load/apply.

Fixture and fingerprint digests are deterministic SHA-256 values calculated from canonical
safe inputs; the fixtures contain only synthetic identifiers and structural test data. The
offline runners cover the declared typed support statuses, but this remains evidence-only:
it does not qualify the full catalog.
