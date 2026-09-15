# Актуальный handoff — Feature 001: read-only catalog qualification

**Дата:** 2026-09-15
**Active feature:** `001-read-only-catalog-qualification` (подтверждён
`.specify/feature.json`).

## Статус

Два последовательных remediation cycle исчерпаны. Feature 001 **не принята и
не квалифицирована**. Историческая полная live-попытка S07 завершилась
fail-closed: `SCHEMA_INVENTORY_UNQUALIFIED` /
`UNKNOWN_SHAPE_UNQUALIFIED`, exit `2`, `RetryCount=0`; Pass A не завершился,
Pass B, reconciliation, snapshot и Excel-пара не выполнялись.

Baseline commit: `e7babcb2a6d5be243e860d8f6089543dec7bc752`
(`codex: baseline before live schema remediation`). Последующий remediation
commit должен содержать только Feature 001 production/tests/docs changes;
user-local evidence, output, `bin/`, `obj/`, temporary data, credentials и
несвязанные dirty paths в него не входят.

## Что доказано ограниченно

- Cycle 1 создал отдельный bounded read-only diagnostic route с sealed safe
  structural evidence; strict full-qualification contract не ослаблялся.
- Cycle 2 добавил closed companion discriminator для H-005 и исправил bounded
  source так, чтобы после `AUTH_LOGIN` → `WORKSPACE_ITEMS` выполнялась не более
  чем одна детерминированная `SCHEMA_GET`. Offline build и шесть suites в
  cycle reports завершились `exit 0`; это не live qualification.
- Bounded live-attempt Cycle 2 сохранила only safe sealed artifact:
  `%LOCALAPPDATA%/BpmSoftSync/diagnostic-evidence/diagnostic-runs/2026/09/15/c18fb82f1ab83de5a644beba1b442eaf6ae70b6a/`
  (token `c18fb82f1ab83de5a644beba1b442eaf6ae70b6a`; только
  `.sealed` и `diagnostic-terminal.json`). Содержимое не копируется в repo.
- Результат поддерживает H-005 — возможное разделение opaque `package.id` и
  GUID `package.uId` — но не доказывает её. Ни identity rewrite, ни acceptance
  unknown shape не разрешены.

## Не доказано и запрещено

- `FULL_CATALOG_NOT_QUALIFIED` и `INDEX_SYNC_UNRESOLVED` остаются открытыми.
- Нет qualified snapshot, canonical Model/Lookup Excel pair, Excel paths/hashes,
  pair/read-back/OOXML checks или human approval.
- Никакие последующие live attempts, retry/rerun, `SELECT_QUERY`, lookup,
  Pass B/C, Excel publication, Write/Manage/Compare/Apply, browser write, SQL
  mutation или Git push не выполняются без нового явного решения человека.

## Safety/process note

Во время Cycle 2 raw-like diagnostic material было выведено только transient
tool output вне approved safe-evidence route. Оно не внесено в repo-документы
и не считается evidence. Последующая работа обязана опираться только на closed
safe categories и sealed user-local artifacts; credentials и raw values не
записываются в repo, prompts или reports.

## Evidence и документы

- Mutable журнал: `verification/live-remediation/README.md`.
- Register гипотез: `verification/live-remediation/hypotheses.md`.
- Cycle reports: `verification/live-remediation/cycle-01-*.md` и
  `verification/live-remediation/cycle-02-*.md`.
- Исторический S07/S08 evidence: `verification/mvp-slices/S07/` и `S08/`.

## Допустимые дальнейшие решения человека

1. Отдельный contract-design/remediation cycle для H-005: доказать collision-safe
   identity semantics и two-pass equality до любой production rewrite.
2. Независимый security/process review bounded diagnostic observability, затем
   новый явный decision о строго ограниченном следующем действии.
3. Остановить Feature 001 как unqualified и сохранить commits/evidence без
   дополнительного live доступа.
