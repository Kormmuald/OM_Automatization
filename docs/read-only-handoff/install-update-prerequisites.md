# Установка и prerequisites

Установите .NET 10 SDK и соберите `BpmSoftSync.sln` в Release. Offline suite
использует только tracked sanitized fixtures/fake HTTP; BPMSoft, Excel, browser, Git,
URL и credentials для неё не нужны.

Live qualification не относится к установке. Она требует отдельно принятого S06
offline evidence, нового human decision и terminal-only ввода URL/login/password.
Не сохраняйте credentials, cookies, CSRF или authorization data. Текущий S07 result —
terminal blocker `SCHEMA_INVENTORY_UNQUALIFIED` / `UNKNOWN_SHAPE_UNQUALIFIED`.
