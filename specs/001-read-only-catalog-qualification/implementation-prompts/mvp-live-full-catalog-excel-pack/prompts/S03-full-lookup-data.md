# S03 worker — full lookup registry and values

Model: `gpt-5.6-sol`; reasoning: `high`.

На accepted S02 metadata реализуй полный ordered `SelectQuery` системного `Lookup`,
точное связывание `LookupRecordId`/`SysEntitySchemaUId`/schema layer и чтение всех
обнаруженных lookup collections. Для каждой записи/колонки сохрани RecordId,
Null|EmptyString|Value, typed/canonical value, reference RecordId и source fingerprint.

Общий pager обязан ловить duplicate/overlap/gap/loop/empty-middle/nonempty-after-
terminal/max limits и собирать safe telemetry. Неподдержанный тип нельзя пропускать.
Tests-first на несколько нестандартных lookup schemas и все negative paging/value cases;
только fake HTTP, без raw values в logs/evidence.

Не реализуй двухпроходную qualification и Excel. Верни diff/evidence и полный контракт
данных для S04.
