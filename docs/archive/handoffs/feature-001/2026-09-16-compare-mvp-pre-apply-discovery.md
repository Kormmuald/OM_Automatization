# Актуальный handoff — ограниченный Compare MVP после Feature 001

**Дата:** 2026-09-16
**Активный контекст:** `.specify/feature.json` по-прежнему указывает
`001-read-only-catalog-qualification`; это не переключает проект на строгую
Feature 003 и не квалифицирует Feature 001.

## Статус и решение пользователя

Строгая Feature 001 остаётся **неквалифицированной**. Её последний controlled
full run остановился на `CATALOG_ORDER_OR_PAGING_UNQUALIFIED`; strict baseline
и qualified full-catalog Excel pair отсутствуют. Полная история и безопасные
evidence перенесены в
`docs/archive/handoffs/feature-001/2026-09-16-pre-compare-mvp-user-decision.md`.

Пользователь явно разрешил отдельный прагматичный **Compare MVP**: он использует
последнюю user-local пару `catalog export-best-effort` как практический вход,
но не называет её qualified или безопасным full-catalog baseline. Это не
разрешает strict qualification, Apply, writeback, browser write, SQL mutation,
автоматический merge, Git push или delete.

Входом является последняя пара из user-local best-effort export, указанная в
архивном handoff. Её нельзя копировать в репозиторий, коммитить либо раскрывать
содержимое значений в чат, логи, tests, отчёты или handoff.

## Зафиксированные границы Compare MVP

- Расширяется существующая .NET CLI/domain/application/adapter архитектура;
  отдельное приложение не создаётся.
- В памяти процесса используются реальные значения Excel и BPMSoft для точного
  сравнения. Реальные значения не встраиваются в исходный код или fixtures и
  не печатаются в консоль, human report либо handoff.
- Поддерживается минимальный набор: `Columns.DesiredRequired` для существующих
  `Own`-колонок с `DesiredState=Active`, а также типизированные scalar changes
  `LookupValues.Value` для существующих `Text`, `Integer`, `Decimal`,
  `Boolean`, `DateTime`, `Date`, `Time` и `Guid`. `ValueState` + `Value`
  образуют одно намерение и различают `Null`, `EmptyString` и `Value`.
- `ValueKind` не изменяется. Для `LookupReference` Compare MVP принимает
  `ReferenceRecordId` как непрозрачный текстовый desired value, сохраняет его
  как текст в local plan и сравнивает без построения reference graph. Он не
  проверяет GUID format, существование target record либо dependencies: это
  обязательный будущий preflight Apply. `ReferenceDraftRowToken` не
  поддерживается и остаётся blocker/report item.
- Изменения `DesiredState`, `DesiredIndexed`, `DesiredPackageName`, lookup
  registry, comment, reference-полей, новых/удалённых строк и structural
  изменений не становятся операциями плана. Они разбираются и отражаются как
  отдельные blocker/report items с корректирующим действием.
- Независимые blocker items не прекращают разбор остальных строк. Итог может
  быть `completed_with_blockers` и содержать частичный план только из
  подтверждённых поддержанных операций. Нечитаемая/несвязываемая пара остаётся
  глобальным безопасным отказом.
- Команда MVP создаёт в новом user-local output-каталоге безопасный
  `compare-report.md` и единый локальный `compare-plan.json`.
- `compare-report.md` не содержит raw lookup values. `compare-plan.json`
  содержит полные нормализованные реальные desired values только планируемых
  операций, а также hashes и bindings, чтобы будущий Apply мог использовать
  его без повторного вывода значений. В нём запрещены credentials, cookies,
  URL, write endpoint и скрытая Apply-команда.
- `compare-plan.json` — конфиденциальный локальный артефакт: он исключается из
  Git, отчётов, логов, чата, tests и handoff. Пример плана в документации
  использует только синтетические значения.

## Обязательная проверка

До live-доступа implementation agent обязан завершить локальную сборку и
минимум один synthetic happy-path и blocker smoke/integration test.

Перед **каждым** отдельным запуском CLI, подключающимся к BPMSoft, agent
запрашивает у пользователя одноразовое явное разрешение с целью, точной
командой/режимом, ожидаемыми read endpoint IDs, подтверждением отсутствия
записи в BPMSoft и Excel, и критерием полезного безопасного результата.
Credentials пользователь вводит только интерактивно в терминале. После запуска
agent фиксирует только safe status/counts/blockers/hashes/пути не-секретных
артефактов и перед следующим запуском снова получает новое разрешение.

Happy path считается подтверждённым только после разрешённого live запуска:
Excel-пара прочитана, актуальное состояние получено, diff/report и JSON plan
сформированы, записей не было.

## Экспорт справочников: текущая политика и остаточный риск

2026-09-16 жёсткий allowlist заменён на policy export **всех** справочников из
`LookupRegistry`, кроме трёх явных технических имён, подтверждённых человеком:
`SocialAccount`, `Calendar`, `EmailTemplate`. Изменение ограничено
`LookupExportExclusions` и локально прошло сборку и unit check; live export
после него не выполнялся.

Best-effort export по-прежнему не является qualified full-catalog baseline:
он использует legacy single query с ограничением и может пропустить lookup при
ошибке чтения. Расширенный охват справочников может выявить новые проблемы
типа, размера либо paging; их нужно разбирать отдельно, без автоматического
retry и без ослабления strict qualification contract.

## Следующее действие

Реализацию выполняет отдельный агент по согласованному пользовательскому
промпту. Никакой live-run не разрешён этим handoff сам по себе.
