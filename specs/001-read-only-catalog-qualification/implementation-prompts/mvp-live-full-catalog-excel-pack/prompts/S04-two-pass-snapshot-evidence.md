# S04 worker — exact-two-pass snapshot and evidence

Model: `gpt-5.6-sol`; reasoning: `high`.

Собери один application use case поверх S01–S03: Pass A фиксирует sealed scope и свежо
читает полный catalog; Pass B ровно один раз независимо перечитывает тот же scope без
reuse/cache A. Сверяй identities, counts, page manifests, schema/value component hashes,
unsupported set, version evidence и `TargetFingerprint/v1`.

После совпадения создай in-memory `QualifiedCatalogSnapshot/v1`; при расхождении —
terminal blocker, RetryCount=0, без Pass C. Реализуй один append-only RunId lifecycle,
typed evidence, scan-before-write/seal, safe diagnostics и workbook-scale forecast.
Raw lookup values могут жить в snapshot memory, но не в audit/evidence.

Tests-first: independent read counters, cache trap, target mutation, canonical hashes,
run collisions, canaries, failure evidence. Только fake transport. Excel adapter не
реализовывать. Верни diff/evidence и snapshot contract для S05.
