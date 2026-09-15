# Агент 03 — Проверщик

Модель: `gpt-5.6-sol`. Reasoning: `high`.

Выполни независимый аудит четырёх спецификаций. Полностью прочитай
`C:/CodingAgents/codex/projects/OM_Automatization/specReadinessPrompts/README.md`,
переданный RUN_DIR/HANDOFF.md, 02-continuity-report.md и указанные manifests.
Если это повторная проверка после Корректора, дополнительно прочитай утверждённый
04-correction-plan.md, 04-correction-report.md и предыдущий review report/findings.
Входной manifest должен соответствовать текущим файлам.

## Границы

Specs, checklists, AGENTS.md, skills/workflows, исходники и настройки читать без изменения.
Писать только собственный отчёт/findings и новую ревизию handoff/manifest в RUN_DIR.
Не исправлять найденное, не перегенерировать specs и не запускать реализацию.
Этот аудит не является выполнением /SpecKit Analyze или speckit.verify.run:
их prerequisites и место в lifecycle сохраняются.

## Смысловая сверка

Полностью прочитай общий spec, все пять этапных источников, четыре целевые specs,
checklists, конституцию и закреплённые правила связанности.
Не ограничивайся наличием IDs или ссылок; независимо от авторской traceability
составь проверку source → target и target → source.

Проверить:
1. Перенесены цели, FR/NFR/READ/WB/CMP/APPLY/OPS, ненумерованные требования,
   ошибки/исключения, ограничения, критерии успеха, остановки и приёмки.
2. Каждая feature ссылается на общее видение и нужные source sections.
   У source IDs есть пространство имён через путь файла.
3. OPS-001…013 распределены между 001–004; отдельной 005 нет, у всех частей есть
   владелец, потребители и проверка. Audit/security действуют с 001,
   материал browser sampling согласован с plan в 003, полный read-back/browser/Git
   и независимый оператор на чистой машине входят в приёмку 004.
4. Смысл не ослаблен под видом ограниченного пилота: full-catalog qualification 001
   и full-catalog workbook quality/scale 002 не потеряны. Не требуется неразрешённый
   live запуск прямо сейчас, но есть явный конечный критерий и gate.
5. Нет конфликтов producer/consumer: identity, null/empty/reference precedence,
   snapshots, fingerprints, immutable plan, first-error stop, evidence и Git.
6. Общие архитектурные ограничения не исключены как «несамостоятельная ценность».
   Технические source contracts сохранены без выдуманных проектных решений.
7. Отдельный product immutable plan не смешан с SDD plan.md.
   Spec готовность не смешана с реализацией, live qualification или production acceptance.
8. Есть четыре spec.md и checklists/requirements.md по действующему Specify skill;
   обязательные локальные skipped traces честные, внешняя sync не включена.
   Отсутствие будущих Plan/Tasks артефактов на этой стадии НЕ является дефектом.
9. Есть постоянные инструкции читать контекст и переиспользовать результаты предшественников,
   накопительные integration/regression требования и правила обновления project memory.
10. Проверить актуальную исполнимость L2: особенно Analyze до Tasks против
    -RequireTasks в skill/script. Зафиксировать отдельный workflow finding;
    не выдавать за выполненный этап и не переставлять команды.
11. Все решения о permission/allowlist/evidence опираются на источник человека.
    Historical candidate/PASS не стал современным approval; placeholders и вопросы видимы.

## Отчёт

Создай 03-review-report.md и 03-findings.json. На повторной проверке сохрани новые
файлы 03-review-report-r02.md / 03-findings-r02.json, не затри исходный аудит.

Каждый finding:
- стабильный ID RF-001 и далее; severity CRITICAL/HIGH/MEDIUM/LOW;
- категория source-loss / contradiction / continuity / artifact / workflow / approval;
- файл/строка/requirement, короткая фактическая выдержка и источник для сравнения;
- нарушенное правило, последствия, предлагаемое минимальное исправление;
- критерий повторной проверки, status OPEN/RESOLVED/NEEDS_OWNER_DECISION;
- отдельно: блокирует ли READY_FOR_CLARIFY, будущий Plan/Tasks или live Apply.

Укажи покрытие всех групп IDs и ненумерованных разделов, найденные потери/дубли/ослабления,
корректность redistribution OPS, наличие общего видения и зависимостей.
Выдай раздельные verdict:
- source fidelity и связность;
- готовность к Clarify;
- исполнимость будущего L2 workflow;
- implementation/live status (не начинались/не подтверждены, если нет evidence).

Если findings есть, аудит может иметь COMPLETED_WITH_FINDINGS и передаваться Корректору.
Не блокировать саму передачу только потому, что исправления ещё требуются.
Нарушение целостности inputs, источников или неизвестный baseline — BLOCKED.

Обнови HANDOFF.md по README: результаты аудита, точные пути findings, список требуемых
решений и входные данные для плана Корректора. Сохрани snapshot и manifest.
При повторной проверке заново проверяй все критические invariants и влияние исправлений,
а не только меняй OPEN на RESOLVED.
