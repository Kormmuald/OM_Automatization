# Промпт переноса спецификации 01

Перенеси неизменяемую исходную спецификацию
`preparation/docs/product-specs/local-bpmsoft-synchronizer/01-read-only-catalog-spec.md`
в корпоративный Spec Kit как отдельную L2 feature безопасного read-only
адаптера и qualification полного каталога. Изменять исходный файл, переносить
его в `specs/` или задавать его как `SPECIFY_FEATURE_DIRECTORY` запрещено.

Перед работой примени `speckit-class-gate` для `/SpecKit Specify` и прочитай
`.specify/project.yml`, `.specify/memory/constitution.md`,
`.specify/workflows/speckit/class-workflow.yml` и overlay `l2-pilot`.
Создай feature specification только в `specs/<feature>/spec.md` на русском
языке. Jira и Confluence отключены; не создавай интеграционные mapping/trace.
Создай для feature новую отдельную папку `specs/<feature>/`; в ней далее должны
жить только артефакты этой feature: checklist, research, data-model (если
применимо), quickstart, plan и tasks. В текущем Specify-запуске создавай лишь
`spec.md`; следующие артефакты появятся в той же папке на соответствующих L2-шагах.

Сохрани тезисную суть `READ-001`…`READ-012`, разрешив только редакционную
реструктуризацию. Включи в feature:

- интерактивную аутентификацию и запрет сохранения password/cookies/CSRF;
- deny-by-default read endpoint allowlist и контрактное доказательство отсутствия
  HTTP writes;
- детерминированный pageable reader с защитой от пропусков, дубликатов и
  бесконечного выполнения;
- сохранение package-layer identity, lossless inventory неизвестных структур и
  ограниченные blockers вместо догадок;
- чтение `Indexes` только из `schema.indexes[].columns[].columnUId`;
- двойной qualification run, fingerprint и evidence с границами масштаба.

Сформулируй L2-границы: ограниченный каталог/стенд, безопасный fallback при
неизвестном состоянии, измеримые success/stop criteria и явное решение человека
после пилота. Исключи Excel pair, compare, plan, Apply, browser write и Git
finalization. Не объявляй full-catalog допуск уже пройденным: это результат
пилотной проверки. Не выдумывай endpoint, данные, сроки или evidence.

После `/SpecKit Specify` не планируй и не реализуй: обозначь `/SpecKit Clarify`
как следующий обязательный шаг L2.
