# Актуальный handoff — Feature 001: read-only catalog qualification

**Дата:** 2026-09-16
**Active feature:** `001-read-only-catalog-qualification` (подтверждён
`.specify/feature.json`).

## Текущий статус

Строгая Feature 001 **не квалифицирована**: Cycle 3 завершён controlled full live run,
run `75e23f1f-d177-439a-b53e-ce566e13845c` остановился fail-closed на
`CATALOG_ORDER_OR_PAGING_UNQUALIFIED` (`RetryCount=0`). Зафиксирован только
lookup pagination/offset blocker; это не доказательство причины в BPMSoft и не
разрешает ослаблять ordering/paging contract.

Pass A не был успешно завершён, поэтому Pass B, reconciliation-success,
qualified snapshot, output и canonical Model/Lookup Excel pair не выполнялись.
Excel paths/hashes, pair/read-back/OOXML checks и human approval отсутствуют.
`FULL_CATALOG_NOT_QUALIFIED` и `INDEX_SYNC_UNRESOLVED` остаются открытыми.

Отдельный ручной режим `catalog export-best-effort` 2026-09-16 успешно создал
пару Excel без ошибки Excel recovery. Это не strict qualification, не
доказательство полноты и не разрешение Compare/Apply.

## Временные допущения best-effort export

- Используется один legacy-style `SelectQuery` с большим `rowCount`; paging,
  `rowsOffset`, Pass A/B, reconciliation и доказательство полноты временно
  выключены. Выгрузка может быть неполной.
- Неизвестный BPMSoft column type либо значение, не соответствующее заявленному
  типу, сериализуется как text/canonical JSON только в Lookup workbook. Это не
  typed semantic mapping.
- Ответ `SelectQuery` с `success != true` для отдельного lookup пропускает
  только этот lookup, а не останавливает весь best-effort export. Поэтому
  Lookup workbook может быть неполным.
- Классическими считаются только `AccountType`, `ActivityCategory`,
  `ActivityPriority`, `ActivityResult`, `ActivityStatus`, `AddressType`.
  Это пересечение 54 имён legacy Google Sheets с 109 именами текущего
  `LookupRegistry`; templates, profiles и прочие registry entries исключены
  из export и будущего update scope.
- Excel ограничивает строку ячейки 32 767 символами. Более длинные значения
  заменяются префиксом и маркером `TRUNCATED_FOR_EXCEL` с исходной длиной и
  SHA-256; исходное значение не восстанавливается из workbook.
- В best-effort сохраняются OOXML validation, sheet/header checks и pair
  binding. Полное canonical projection read-back equality не применяется,
  потому что этот режим намеренно допускает преобразование long-cell values.

Последняя успешная пара создана только в user-local path
`%LOCALAPPDATA%/BpmSoftSync/best-effort-1455f0b565e745c1b25df435e5aea92a/`:
`BPMSoft.ModelCatalog.xlsx` SHA-256
`82e0f5569ca499f8f7e989d01d23b363d5b4230e0a452d250876864816db0b1a`,
`BPMSoft.LookupCatalog.xlsx` SHA-256
`2c0828c409552718f6b6dc0130164bda480514ff5210fe0a9ccb2a776cde0f27`.
В Git не включать output, raw lookup values либо credentials.

## Safe evidence controlled run

User-local sealed run root:
`%LOCALAPPDATA%/BpmSoftSync/controlled-full-20260915-001/runs/2026/09/15/75e23f1f-d177-439a-b53e-ce566e13845c/`.

- `.sealed` содержит closed marker `blocked-terminal`; SHA-256 файла:
  `006399676594510e9cad733a52725666c06cc257c818df3177d7a398608b56ee`.
- `run-journal.json` SHA-256:
  `54eaa941a5e22af464d7bf75f8bbb9fd95ac07c8f10f187e66fa8375552ae1f7`.
- `evidence/reconciliation.json`: payload digest
  `311387b82a4f38a02bb9fad089e13184a7b10b223f625b499393e16c731f8e02`,
  file SHA-256
  `0d91a166ea3d7bf7f6636a940a7c0071dae1822ddfe7a7297632889e3010b5e2`.
- `evidence/blocked-terminal.json`: payload digest
  `ad1e72525775b61bf43e51141d793ffbee9a4008302c7caf4211a7f44252d6e4`,
  file SHA-256
  `60650e540d232a6ed91b39cc16f3c6b263801797ac459e6a520f8f4fcae7fbbd`.

В repo переносить содержимое user-local evidence нельзя. В документации
допустимы только приведённые closed metadata и digests; credentials, URL,
raw response, lookup values и иные scalar values не записываются.

## История cycle

- Cycle 1: added bounded read-only diagnostic route и sealed safe structural
  evidence без ослабления strict qualification contract.
- Cycle 2: added closed H-005 discriminator и single deterministic
  `SCHEMA_GET` for bounded diagnostics; результат поддержал, но не доказал
  различие opaque `package.id` и GUID `package.uId`.
- Cycle 3: H-005 semantic remediation и offline revalidation прошли, после чего
  controlled full run дошёл до lookup paging/offset blocker. Это новый terminal
  blocker, а не successful qualification.

Baseline commit: `e7babcb2a6d5be243e860d8f6089543dec7bc752`
(`codex: baseline before live schema remediation`). Remediation commit должен
содержать только Feature 001 production/tests/docs changes; user-local evidence,
output, `bin/`, `obj/`, temporary data, credentials и несвязанные dirty paths в
него не входят.

## Допустимые следующие решения человека

1. Отдельно исследовать и исправить lookup ordering/paging contract с focused
   offline characterization и independent validation; без live rerun.
2. Провести независимый security/process review текущей observability и
   controlled-run evidence, затем получить новое явное решение о следующем
   ограниченном действии.
3. Остановить Feature 001 как unqualified и сохранить текущие commits/evidence
   без нового live доступа.

До нового явного решения человека запрещены retry/rerun, `SELECT_QUERY`, lookup
replay, Pass B/C, Excel publication, Write/Manage/Compare/Apply, browser write,
SQL mutation и Git push.
