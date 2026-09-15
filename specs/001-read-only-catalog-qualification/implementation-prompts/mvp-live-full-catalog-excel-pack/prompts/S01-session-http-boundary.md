# S01 worker — interactive session and HTTP boundary

Model: `gpt-5.6-sol`; reasoning: `high`.

Реализуй только production target/origin policy, terminal-only credential input,
ephemeral cookie/CSRF session, exact four-endpoint allowlist, canonical request factory,
redirect/alternate-host denial, bounded HTTP reading/timeouts и typed responses.
Перенеси semantics `PrototypeReadOnlyPull` без runtime dependency и port-by-copy.

Tests-first через fake `HttpMessageHandler`: login success/failure, CSRF absence,
method/path/body/origin mismatch rejected before send, redirect rejected, disposal clears
session, secrets absent from args/log/evidence. Реальный стенд не использовать в S01.

Не реализуй full schema/lookup traversal, two-pass orchestration или Excel. Верни diff,
tests/commands/results и точные deferred interfaces для S02.
