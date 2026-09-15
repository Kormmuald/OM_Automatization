# S02 worker — full WorkspaceInventory and object model

Model: `gpt-5.6-terra`; reasoning: `high`.

Используя accepted S01 transport, реализуй полный `GetWorkspaceItems` inventory и
`GetSchema` traversal всех поддержанных EntitySchema/package layers. Сохраняй точные
typed identities, parent, own/inherited columns, types, required/indexed, references,
indexes и ordered members. Одинаковые display names не объединять. Unknown shapes —
lossless safe envelope или scoped blocker, без default/drop.

Index relation только `schema.indexes[].columns[].columnUId`; `ActualIndexed` отдельно.
Tests-first: multi-package/name collisions, inheritance, references, composite indexes,
unknown/malformed/unreadable items, completeness counts. Только fake HTTP.

Не читать lookup records/values, не делать Pass A/B и Excel. Верни diff и evidence.
