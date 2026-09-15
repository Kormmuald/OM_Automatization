# Data model: Feature 001 full catalog and output pair

| Entity | Required fields / relations | Boundary |
| --- | --- | --- |
| `ScopeDescriptor` | target alias/origin policy, allowlist version, full collection registry, query/order/limits, snapshot/projection versions | sealed after Pass A; safe hash only in evidence |
| `CatalogPass` | ordinal A/B, full workspace/schema/lookup content, manifests, telemetry, component/value digests, fingerprint, unsupported set | B reads again; it cannot use A as data input |
| `WorkspaceInventoryItem` | typed workspace and `PackageLayerIdentity`, type, `Structured|InventoryOnly|Unreadable|Unsupported`, reason/envelope | no name-only merge |
| `EntitySchema` | schema/package IDs, own/inherited columns, references, `Indexes[]` and ordinal members | index relation only from `columns[].columnUId` |
| `LookupRecord` / `NormalizedLookupValue` | registry ID/schema package context; record/column IDs, state, kind, canonical value, reference ID, fingerprints | values are in-memory snapshot/output only |
| `PageManifest` | ordinal, token hash, count, first/last identity, digest, size bucket | validates ordering and terminal sequence |
| `TargetFingerprint` | versioned canonical digest and safe component evidence | no raw values or secrets |
| `QualifiedCatalogSnapshot` | equal A/B scope and reconciliation, B materialized content, identities, diagnostics, scale, pair/run identity | domain/application has no adapter types |
| `WorkbookPair` | `PairId`, `RunId`, two staged paths, manifests/hashes/projection summary | both validate before atomic publish |
| `Run` / `EvidenceEnvelope` | unique date-root, safe metadata, gate outcomes, append-only journal/audit/evidence | scanner/schema before every durable write |
| `Blocker` | code, scope, safe reason, recovery, next permitted action | never carries raw response/secret/cell value |

## State transitions

```text
Created -> OfflineValidated
ManualInvocation -> SessionEstablished -> PassA(seal scope) -> PassB -> Reconciled
Reconciled(equal) -> QualifiedSnapshot -> PairStaged -> PairValidated -> PairPublished -> HumanReviewRequired
PassA/PassB -> PagingOrShapeBlocked | TargetChanged -> SealedBlocked (no retry)
S06 accepted + current user confirmation -> S07 LiveRunOnce -> HumanDecision
```

`HumanReviewRequired` and `HumanDecision` do not grant Apply or change unresolved index/write gates.
