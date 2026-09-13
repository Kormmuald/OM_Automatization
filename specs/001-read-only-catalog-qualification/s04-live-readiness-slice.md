# S04 — готовность общего live read-only пути

## Статус и назначение

Это отдельный corrective slice активной Feature 001
`001-read-only-catalog-qualification`. Он подготовлен по результатам
[`Проверка причины недореализации feature 001.md`](../../Проверка%20причины%20недореализации%20feature%20001.md).
Исходный отчёт и все файлы в `preparation/docs/product-specs/` остаются неизменяемыми
источниками. Документ не меняет отметки в действующем `tasks.md`, не заменяет его и не
является разрешением на подключение к BPMSoft.

**Результат S04:** один production application workflow может получить каталог либо от
sanitized fixture adapter, либо от HTTP adapter, и в обоих случаях проходит один и тот же
validated reader → qualification/reconciliation → append-only evidence pipeline. HTTP
adapter и интерактивная сессия доказываются только локальным fake `HttpMessageHandler`;
реальный target, credentials и HTTP-вызовы не используются при разработке и тестировании.

S04 закрывает пропущенную техническую предпосылку T046. Он также устраняет остатки
S01–S03, без которых нельзя достоверно закрыть T025–T045. Сам T046 остаётся отдельной
ручной задачей после успешных offline checks; отдельное сохраняемое разрешение не требуется.

## Проблема, которую устраняет slice

Текущий `catalog validate-offline` выполняет только проверку fixture, один capture вызов и
возвращает `HUMAN_REVIEW_REQUIRED`. Текущий `catalog qualify` только отказывает без
авторизации. Поэтому существующие компоненты reader, reconciliation, forecast и run store
не образуют исполнимого production workflow, а live transport/source отсутствует.

Дополнительно должны быть устранены доказанные расхождения:

- `research.md` определяет четыре endpoint ID, а код добавляет `GET_PACKAGES`; exact runtime
  contract должен быть один и покрыт тестами;
- `ICatalogSource` пока не возвращает типизированный результат прохода, а
  `IReadOnlyTransport` не переносит validated request/response boundary;
- `TargetFingerprint` не использует оговорённый canonical JSON;
- `AppendOnlyRunStore.StoreAsync` создаёт новый root на каждую запись, а durable paths не
  замкнуты на единый `RunId` и terminal seal;
- `catalog diagnose --run` не подключён к `Program.Main`;
- текущие E2E/security tests вручную соединяют helpers вместо запуска production composition
  root и не могут служить evidence T043–T045.

## Граница работ

### В рамках S04

- Исправление неполного S01/S02/S03 поведения, тестов и evidence contracts, необходимых
  для фактического закрытия T025–T045.
- Нормализованный exact `ReadEndpointAllowlist/v1`, request factory и узкий HTTP adapter.
- Typed target/scope configuration, admission flow и in-memory interactive session boundary.
- Общий composition root для `catalog validate-offline`, будущего `catalog qualify` и
  `catalog diagnose --run`; fixture и HTTP являются сменными adapter-реализациями одного
  application port.
- Полный двухпроходный reader/inventory/fingerprint/reconciliation pipeline, один `RunId`,
  strict schema + scanner для каждого durable path и безопасный terminal seal.
- Расширенная offline E2E, adversarial, metamorphic и leakage matrix с independently
  calculated expected manifests/hashes.
- Выполнимый handoff: offline procedure, conditional manual procedure и recovery steps.

### Вне рамок S04

- Запрос, ввод или сохранение реальных credentials; подключение к real BPMSoft; browser,
  Excel, compare, Apply, Git и любые Write/Manage действия.
- Выполнение T046, live acceptance, снятие `FULL_CATALOG_NOT_QUALIFIED`,
  `INDEX_SYNC_UNRESOLVED` либо выдача Apply permission.
- Запуск live test без ручного interactive invocation оператора или прямой текущей просьбы
  пользователя в доступном чате.

## Обязательная модель исполнения

```text
CLI command
    │
    ├─ invocation boundary (manual terminal | direct current user request)
    │     └─ offline/automatic path: zero credential prompt, zero HTTP send
    │
    └─ CatalogQualificationUseCase
          ├─ ICatalogSource: FixtureCatalogSource | HttpCatalogSource
          ├─ Pass A: classified requests → typed reader → inventory/manifests/fingerprint
          ├─ Pass B: тот же target/scope и тот же reader
          ├─ reconciliation: exactly two passes; no Pass C/retry
          └─ one RunId: journal + validated/scanned evidence + terminal seal
                 └─ HUMAN_REVIEW_REQUIRED или named blocker
```

`HttpCatalogSource` не может создавать альтернативный путь к `HttpClient`. Каждый request
сначала создаётся canonical request factory и классифицируется allowlist. Redirects,
alternate host, query/fragment/path traversal, неразрешённый method/path/body и любые
неучтённые sends должны останавливаться до отправки. Login допустим только как отдельный
session-handshake после ручного invocation/direct current request и только в in-memory session.

## Решения и human gates

| Gate | Кто / когда | Разрешённое действие | Что запрещено до выполнения |
|---|---|---|---|
| G-1: S04 offline readiness | implementation agent в строго заданной последовательности `S04-001…S04-021 → S04-022 → S04-023 (preparatory/control only) → T043 + T044 → T045` | после S04-022 и контрольного S04-023 однократно выполнить T043 и T044 на completed composition root, затем создать единственный T045 evidence package | считать старый PASS достаточным, пропускать S04-022 или S04-023, отмечать T025–T045 без полного evidence либо считать S04-023 выполнением T043/T044 или созданием T045 |
| G-2: manual live invocation | оператор после G-1 либо пользователь в текущем доступном чате | вручную запустить `catalog qualify` для exact target/scope либо прямо запросить agent-run | automatic agent start, credentials вне terminal prompt, любой write |
| G-3: evidence review | человек после T046 | review safe evidence | трактовать `HUMAN_REVIEW_REQUIRED` как acceptance или Apply permission |

До G-2 допускаются только offline/fake-HTTP tests. Они должны доказывать zero prompt/send
на offline и automatic paths; вручную вызванный interactive CLI может перейти к terminal
credential prompt без `AuthorizationReference`.

## Инварианты и критерии готовности

1. Fixture и fake-HTTP режимы проходят один use case; тесты не подменяют reconciliation,
   manifests, fingerprint или run store прямыми helper-вызовами.
2. Каждый Pass использует один validated immutable target/scope snapshot. Pass A и Pass B
   обязаны использовать тот же reader contract; любое различие — terminal named blocker,
   `RetryCount = 0` и no Pass C.
3. Reader проверяет declared stable ordering, cursor progress, duplicates, overlaps, gaps,
   empty-middle, nonempty-after-terminal, loop и max-pages. Unknown shape становится
   lossless envelope либо blocker, но не default/drop.
4. Fingerprint строится из canonical JSON согласно Decision 2 в `research.md` и не содержит
   credentials, cookies, CSRF, raw lookup values либо raw response.
5. Все artifacts одного запуска содержатся под одним `runs/yyyy/MM/dd/<RunId>/`; journal,
   manifests, reconciliation, scan/schema results и outcome ссылаются на этот `RunId`.
   Schema/scanner failure создаёт безопасный blocked terminal record, но не success seal.
6. `catalog diagnose --run <RunId>` читает только validated safe records, поддерживает
   bounded paging и никогда не раскрывает secret/raw value.
7. Clean fixture produces exactly two reconciled passes and `HUMAN_REVIEW_REQUIRED`; every
   negative fixture produces its declared blocker, bounded termination and zero write calls.
8. T045 содержит запуск новой сборки: fixture IDs and SHA-256, command lines and exit codes,
   run-tree hashes, reconciliation/scan/schema summaries, test results and failures. Старое
   `offline-validation.md` нельзя переиспользовать как evidence изменённой реализации.

## Артефакты и ожидаемое изменение архитектуры

| Область | Требуемый результат |
|---|---|
| `BpmSoftSync.Application` | Typed `ICatalogSource`/use case, immutable qualification request, manual-invocation policy and controlled session/reader/run lifecycle; нет HTTP/console types в Domain. |
| `BpmSoftSync.Adapters.BpmSoft` | Exact allowlist, canonical request factory, fake-handler-testable `HttpCatalogSource`, response parser в typed catalog data, in-memory session adapter и send/prompt spies. |
| `BpmSoftSync.Domain` | Canonical JSON fingerprint, complete reader/paging/inventory/reconciliation contracts and terminal outcomes. |
| `BpmSoftSync.Adapters.FileSystem` | Per-run append-only writer/journal, schema/scanner before every durable write and seal, safe reader for diagnosis. |
| `BpmSoftSync.Cli` | Composition root wires the shared workflow; `qualify` is available only through a manual interactive invocation or direct current user request; `diagnose --run` is dispatchable. |
| `tests/fixtures/read-only` | Complete manifest-controlled fixture matrix, including exact endpoint, malformed response, all paging mutations, target change, schema and leakage canaries. |
| `docs/read-only-handoff` | Separate offline runbook and conditional manual procedure; neither contains secrets nor permits automatic agent start. |

## Зависимость с T025–T046

После выполнения задач из [`s04-live-readiness-tasks.md`](s04-live-readiness-tasks.md)
обязательна единственная последовательность исполнения:
`S04-001…S04-021 → S04-022 → S04-023 (preparatory/control only) → T043 + T044 → T045`.
S04-022 допускает индивидуальную проверку completion исходных T025–T042 по их собственным
criteria. S04-023 только подготавливает и контролирует fresh-evidence protocol; он не
запускает T043/T044 и не создаёт T045 evidence package. Только после него T043 и T044
выполняются заново на production workflow, а единственный новый evidence package создаётся
как T045 после обоих успешных reruns. T046 не входит в S04: он остаётся отдельным human-gated
действием и может начаться только после завершения G-1 и ручного G-2.

```text
S04-001…S04-005 (contracts + S01/S02 corrections)
  → S04-006…S04-014 (complete S03 + evidence lifecycle)
  → S04-015…S04-021 (fake HTTP live-ready workflow)
  → S04-022 (individual T025–T042 evidence review)
  → S04-023 (preparatory/control only; does not run T043/T044 or create T045 evidence)
  → T043 + T044 rerun → T045 sole new evidence package
  → G-2 manual invocation/direct current request → T046 manual read-only verification → human review
```

## Неподтверждённые допущения

- Endpoint set, login response shape, CSRF handling and catalog response schema remain
  candidates until represented by sanitized captures and fake-handler tests; they must not be
  expanded during implementation by endpoint-name or HTTP-method heuristics.
- Manual invocation/direct current request is the sole live-test admission rule. It is not a
  persistent credential, not an `AuthorizationReference` and not an authorization for write.
- S04 makes the live path technically testable offline; it does not prove target compatibility,
  catalog completeness or a real BPMSoft result.
