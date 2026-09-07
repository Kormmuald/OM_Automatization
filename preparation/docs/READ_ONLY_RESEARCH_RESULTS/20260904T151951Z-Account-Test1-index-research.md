# P3-C: read-only evidence по непустым индексам `Account/Test1`

Дата evidence: 2026-09-04 15:19:51 UTC.  
Роль: read-only researcher.  
Статус исследования: **достаточно для bounded simple non-empty index contract; исходный runtime verdict `NOT_CONFIRMED` вызван ошибочным own-only oracle**.  
Граница: отчёт не разрешает full-catalog pull, `.xlsx`, Write/Manage, compile/save/create/update/delete или изменение source contracts.

## 1. Краткий вывод

Ответ локального BPMSoft 1.8 для замещающей схемы `Account` из package `Test1` содержит ровно два индекса:

1. `Index1`: `isUnique=true`, один член, `columnUId=60cc5643-4ee2-4adf-b76b-06000ad0b067`. Этот UId точно совпадает с колонкой `Code` базовой схемы `Account`.
2. `Index2`: `isUnique=false`, один член, `columnUId=7c81a01e-f59b-47df-830c-8e830f1bf889`. Этот UId точно совпадает с колонкой `Name` базовой схемы `Account`.

Следовательно, owner-observed facts «UNIQUE index на `Code`» и «NON-UNIQUE index на `Name`» подтверждены read-only response. Не подтвердилось только ошибочное дополнительное предположение prototype, что `Code` и `Name` должны быть own columns именно в слое `Account/Test1`.

У `Account/Test1` `schema.columns` имеет length 0, а `schema.inheritedColumns` — length 34. Это ожидаемо для замещающего слоя: он не создаёт новые `Code` и `Name`, а наследует существующие колонки родительской `Account` и добавляет собственные index definitions, ссылающиеся на устойчивые `ColumnUId` наследованных колонок.

## 2. Источники

Основной immutable evidence:

```text
PrototypeReadOnlyPull/bin/Release/net10.0/probe-output/20260904T151951Z/
```

Предыдущее успешное bounded evidence для сверки идентичности колонок:

```text
PrototypeReadOnlyPull/bin/Release/net10.0/probe-output/20260904T132710Z/
```

Терминологическая памятка `docs/REFERENCES/creatio-terrasoft-manual-section-registration/README.md` используется только для различения record `Id` и configuration `UId`. Она не доказывает index/GetSchema contract BPMSoft 1.8; исторический SQL не выполнялся.

## 3. Criterion-to-evidence matrix

| Criterion | Evidence | Проверка | Result |
|---|---|---|---|
| Exact immutable file set | `20260904T151951Z/artefact-manifest.sha256.json` + directory listing | Ровно 9 файлов; manifest перечисляет остальные 8 и исключает себя | **CONFIRMED** |
| Exact-byte integrity | `artefact-manifest.sha256.json` | Для всех 8 entries пересчитаны byte length и SHA-256; все совпали; UTF-8 BOM отсутствует | **CONFIRMED** |
| Manifest identity | `artefact-manifest.sha256.json` | SHA-256 самого manifest: `dcb01e65e09cbf1e6f06be2e21b61f5147c1c8808d50865712ec11dd558870bb` | **RECORDED** |
| Redaction | `redaction-check.json` + независимый marker scan | `Passed=true`, 9 prospective files, 0 forbidden hits; secret-bearing JSON keys и suspicious filenames не найдены | **CONFIRMED** |
| Bounded target selection | `workspace-selection-Account-Test1.mapping.json` | `MatchCount=1`, `type=3`, `Name=Account`, `PackageName=Test1`, workspace UId `05c46595-729a-4668-94bc-56061dad4fb9` | **CONFIRMED** |
| Read scope | `index-probe-request-contract.json`, `run-result.json` | One `GetWorkspaceItems`, one `GetSchema`; SelectQuery/full catalog/write/Excel/alternative retry listed as not run | **CONFIRMED AS RECORDED** |
| Response structure | `schema-Account-Test1.response-shape.json` | `/schema/columns` length 0; `/schema/inheritedColumns` length 34; `/schema/indexes` length 2; scalar response values redacted | **CONFIRMED** |
| Safe index shape | `schema-Account-Test1.indexes.shape.json` | Два index objects; каждый имеет `name`, `uId`, `isUnique`, `isAutoName`, `columns[1]`; unsafe scalar paths отсутствуют | **CONFIRMED** |
| Simple UNIQUE index | `indexes.shape.json`, paths `/schema/indexes/0/*` | `name=Index1`, `isUnique=true`, `columns` length 1, member `columnUId=60cc...b067` | **CONFIRMED** |
| Simple NON-UNIQUE index | `indexes.shape.json`, paths `/schema/indexes/1/*` | `name=Index2`, `isUnique=false`, `columns` length 1, member `columnUId=7c81...f889` | **CONFIRMED** |
| Index member → `Code` | `indexes.shape.json` + `20260904T132710Z/schema-Account-Base.mapping.json` | Index1 `columnUId` exact equals Base Account `Code.ColumnUId` | **CONFIRMED** |
| Index member → `Name` | те же sources | Index2 `columnUId` exact equals Base Account `Name.ColumnUId` | **CONFIRMED** |
| Inheritance model | current `response-shape.json` + prior Base/extension mappings | Test1 own=0/inherited=34; previous Base has same 34 flattened columns, and previous extension already shows Code/Name as inherited with the same UIds | **CONFIRMED FOR SAMPLED SCHEMAS** |
| Prototype candidate verdict | `schema-Account-Test1.index-candidates.mapping.json`, `own-columns.mapping.json`, `run-result.json` | Runtime candidate matcher получил no matches, поскольку lookup set включал только own columns with null UIds | **FALSE NEGATIVE EXPLAINED** |
| Current inherited-column `indexed` flags | Current full response shape redacts scalar values; own-only mapping не сохраняет inherited details | Текущие `Code.indexed`/`Name.indexed` в Test1 независимо не восстановимы | **NOT CONFIRMED; NOT REQUIRED FOR INDEX ARRAY CONTRACT** |
| Composite/full-catalog behavior | Not-run list | Не исследовалось | **NOT RUN** |

## 4. Exact observed index contract

Для этого bounded sample подтверждена следующая форма `GetSchema.schema.indexes`:

```text
schema.indexes[]
  uId                 GUID index identity observed in response
  name                technical index name
  isUnique            boolean uniqueness flag
  isAutoName          boolean observed flag
  columns[]           index members
    uId               GUID identity index-member object
    name              technical index-member name
    orderDirection    numeric observed value
    columnUId         relation to schema column UId
```

Observed values:

| Index | Index UId | isUnique | isAutoName | Members | Member name | Member UId | orderDirection | columnUId | Resolved column |
|---|---|---:|---:|---:|---|---|---:|---|---|
| `Index1` | `03d8954e-6967-41b5-88c7-0a08a802880e` | `true` | `false` | 1 | `IndexColumn_Code` | `979a0bb4-edaa-7f8a-a98c-3d25b6173e22` | 0 | `60cc5643-4ee2-4adf-b76b-06000ad0b067` | `Account.Code` |
| `Index2` | `bace03c9-8747-4f26-970c-96cc3940543e` | `false` | `false` | 1 | `IndexColumn_Name` | `d30c533f-7d5a-66e6-aa70-38bc46747428` | 0 | `7c81a01e-f59b-47df-830c-8e830f1bf889` | `Account.Name` |

`columns[].uId` и `columns[].columnUId` — разные идентификаторы: первый относится к объекту-члену индекса, второй связывает этот член с колонкой схемы. Именно `columnUId`, а не имя `IndexColumn_*`, является доказанной relation identity.

`orderDirection=0` зафиксирован как observed numeric value. Его более широкая enum-семантика и поведение descending index в этом тесте не исследовались.

## 5. Почему `Code` и `Name` inherited

Простыми словами, package `Test1` содержит ещё один слой схемы с кодовым именем `Account`. Базовый слой уже определил `Code` и `Name`. Test1 не копирует и не создаёт эти колонки заново, поэтому в его GetSchema-response они находятся в `inheritedColumns`, а `columns` остаётся пустым.

Индекс при этом принадлежит Test1-слою и может ссылаться на колонку, пришедшую от родителя. BPMSoft сохраняет эту ссылку как исходный `ColumnUId` базовой колонки. Поэтому одновременно верны два утверждения:

- index definition является новым для `Account/Test1`;
- его target column `Code` или `Name` является inherited в этом слое и own в базовой `Account`.

Это также объясняет повторяющиеся `SchemaName=Account` при разных schema UIds: package layers имеют разные configuration identities, хотя эффективная runtime schema носит одно имя.

## 6. Что именно prototype проверил неверно

`own-columns.mapping.json` показывает для обеих колонок:

```text
OwnMatchCount = 0
InheritedMatchCount = 1
ColumnUId = null
Indexed = null
```

Prototype сначала искал `Code` и `Name` только в `schema.columns[]`. После нулевого own match он намеренно не извлёк их UIds из `schema.inheritedColumns[]`. Затем candidate matcher сравнивал `schema.indexes[].columns[].columnUId` только с этим пустым own-only набором. Поэтому оба реальных index members получили `MatchedColumnNames=[]`, а run — `CODE_INDEX_CANDIDATE_COUNT_NOT_ONE` и `NAME_INDEX_CANDIDATE_COUNT_NOT_ONE`.

Иными словами, response не был противоречив: ошибочным было проверочное требование `Ownership=Own` для index target. Оно смешало принадлежность index definition текущему replacement layer с ownership самой target column.

## 7. Reconciliation с предыдущим evidence

`20260904T132710Z/schema-Account-Base.mapping.json`:

- `Code.ColumnUId = 60cc5643-4ee2-4adf-b76b-06000ad0b067`, ownership `Own`;
- `Name.ColumnUId = 7c81a01e-f59b-47df-830c-8e830f1bf889`, ownership `Own`.

`20260904T132710Z/schema-Account-extension.mapping.json` показывает те же UIds как `Inherited`, что независимо подтверждает стабильность identities между package layers.

Предыдущие `Indexed` values (`Code=false`, `Name=true`) относятся к более раннему Base/extension evidence и не являются текущим read-back Test1 после UI-создания индексов. Они не опровергают наличие двух explicit `schema.indexes` в новом response. Более того, несовпадение подчёркивает, что workbook `Indexes` должен строиться из explicit index collection, а `Columns.ActualIndexed` нельзя без отдельной проверки считать простой производной от наличия любого index в replacement layer.

## 8. Достаточность и ограничения

**Повторный read-only запуск для фиксации simple non-empty index contract не нужен.** Текущий safe evidence содержит property names, exact paths, two index identities, unique flags, one-member cardinality и exact `columnUId` relations, которые независимо разрешаются в `Code` и `Name` через предыдущее safe Base mapping.

Можно source-backed зафиксировать:

- `IndexName <- schema.indexes[].name`;
- `IsUnique <- schema.indexes[].isUnique`;
- member ordinal из позиции в `schema.indexes[].columns[]`;
- `ColumnUId <- schema.indexes[].columns[].columnUId`;
- simple index = `columns.length == 1`;
- target column может быть `Own` или `Inherited` в текущем package layer.

Нельзя обобщать этот single-schema sample на composite indexes, auto-named indexes, все schema kinds или full catalog. Не доказана текущая semantics `column.indexed` для inherited column, если индекс объявлен replacement layer. Если владельцу потребуется отдельно закрыть именно `Columns.ActualIndexed`, это будет другой bounded question и новый pre-change gate; для текущего index-contract решения он не требуется.

## 9. Воспроизводимые команды

Все команды read-only и не используют secrets.

### 9.1. Exact set, byte lengths, SHA-256 и BOM

```powershell
$E = 'C:\CodingAgents\codex\projects\OM_Automatization\preparation\PrototypeReadOnlyPull\bin\Release\net10.0\probe-output\20260904T151951Z'
$M = Get-Content -LiteralPath "$E\artefact-manifest.sha256.json" -Raw | ConvertFrom-Json

$actual = @(Get-ChildItem -LiteralPath $E -File | Sort-Object Name)
$expected = @($M.Files.FileName) + 'artefact-manifest.sha256.json'
($actual.Name -join '|') -ceq (($expected | Sort-Object) -join '|')

foreach ($item in $M.Files) {
    $path = Join-Path $E $item.FileName
    $bytes = [IO.File]::ReadAllBytes($path)
    [pscustomobject]@{
        File = $item.FileName
        LengthMatch = $bytes.Length -eq $item.ByteLength
        HashMatch = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ceq $item.Sha256
        HasUtf8Bom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xef -and $bytes[1] -eq 0xbb -and $bytes[2] -eq 0xbf
    }
}

Get-FileHash -Algorithm SHA256 -LiteralPath "$E\artefact-manifest.sha256.json"
```

Expected: exact set `True`; восемь раз `LengthMatch=True`, `HashMatch=True`, `HasUtf8Bom=False`; manifest hash `DCB01E65E09CBF1E6F06BE2E21B61F5147C1C8808D50865712EC11DD558870BB`.

### 9.2. Структура response и indexes

```powershell
$responseShape = Get-Content -LiteralPath "$E\schema-Account-Test1.response-shape.json" -Raw | ConvertFrom-Json
$responseShape.Nodes |
  Where-Object Path -in '/schema/columns','/schema/inheritedColumns','/schema/indexes' |
  Select-Object Path, Kind, ArrayLength

$indexes = Get-Content -LiteralPath "$E\schema-Account-Test1.indexes.shape.json" -Raw | ConvertFrom-Json
$indexes.Nodes |
  Where-Object SafeValue -ne $null |
  Select-Object Path, Kind, SafeValue, SafeValueClass
```

Expected lengths: own columns 0, inherited columns 34, indexes 2.

### 9.3. ColumnUId reconciliation

```powershell
$Old = 'C:\CodingAgents\codex\projects\OM_Automatization\preparation\PrototypeReadOnlyPull\bin\Release\net10.0\probe-output\20260904T132710Z'
$base = Get-Content -LiteralPath "$Old\schema-Account-Base.mapping.json" -Raw | ConvertFrom-Json
$extension = Get-Content -LiteralPath "$Old\schema-Account-extension.mapping.json" -Raw | ConvertFrom-Json

$base.Columns | Where-Object ColumnName -in 'Code','Name'
$extension.Columns | Where-Object ColumnName -in 'Code','Name'

$indexColumnUIds = $indexes.Nodes |
  Where-Object Path -match '/schema/indexes/\d+/columns/0/columnUId$' |
  Select-Object -ExpandProperty SafeValue

$indexColumnUIds
$indexColumnUIds[0] -ceq ($base.Columns | Where-Object ColumnName -eq 'Code').ColumnUId
$indexColumnUIds[1] -ceq ($base.Columns | Where-Object ColumnName -eq 'Name').ColumnUId
```

Expected: обе проверки `True`.

### 9.4. Independent redaction marker scan

```powershell
$pattern = '"(password|userpassword|authorization|cookie|bpmcsrf|csrf|session(token|id)?|headers?)"\s*:'
Get-ChildItem -LiteralPath $E -File |
  Select-String -Pattern $pattern -CaseSensitive:$false
```

Expected: no output.

## 10. Not run / unchanged

- BPMSoft/login requests при подготовке этого отчёта не выполнялись.
- Evidence files не изменялись.
- `Program.cs`, contracts, SDD, handoff и prompts не изменялись.
- SelectQuery, full-catalog pull, Write/Manage, compile/save/create/update/delete, `.xlsx`, Google input и `SyncOM` не выполнялись.

