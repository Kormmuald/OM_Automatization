# Formula Validation Report — S05 synthetic pair

**Files**: `generated-fake-data-pair-current/BPMSoft.ModelCatalog.xlsx`, `generated-fake-data-pair-current/BPMSoft.LookupCatalog.xlsx`  
**Date**: 2026-09-14  
**Model sheets checked**: Readme, Manifest, WorkspaceInventory, Schemas, Columns, Indexes, ValidationLists, PullConflicts  
**Lookup sheets checked**: Readme, Manifest, LookupRegistry, LookupValues, ValidationLists, PullConflicts  
**Total worksheet formulas scanned**: 0

## Tier 1 — Static validation

**Status**: PASS  
**Tool**: `python -X utf8 .../formula_check.py` direct OOXML scan plus production `WorkbookPackageInspector`.

Commands actually executed for Model and Lookup respectively: `python -X utf8 C:/Users/Evgenii_2/.codex/skills/minimax-xlsx/scripts/formula_check.py <file> --json`; both actual exits were `0`.

No cached Excel errors, broken worksheet formula references, formula cells, external relationships, VBA, connections, forbidden parts, broken relationships, missing content-type declarations, invalid style/protection semantics or invalid auto-filter extents were detected. Data-validation formulas use only workbook-local defined names backed by the hidden `ValidationLists` sheet.

## Tier 2 — Dynamic validation

**Status**: SKIPPED  
**Tool**: LibreOffice headless is not available on this Windows host. Actual command: `python -X utf8 C:/Users/Evgenii_2/.codex/skills/minimax-xlsx/scripts/libreoffice_recalc.py --check`; actual exit `2`, output `LibreOffice NOT available`. Exit `2` is the tool's documented not-installed/skip status; exit `1` would mean an installed runtime failed.

The generated contract deliberately contains zero worksheet formulas. Runtime recalculation therefore has no calculation graph to evaluate; the missing LibreOffice check is still recorded explicitly and does not replace Tier 1/read-back validation.

## Read-back

**Status**: PASS  
**Tools**: production `WorkbookPairReader` and explicit `python -X utf8 C:/Users/Evgenii_2/.codex/skills/minimax-xlsx/scripts/xlsx_reader.py <file>` commands.

`xlsx_reader.py` opened both files successfully (exit `0`) and reported the exact 8-sheet Model and 6-sheet Lookup inventories. The Model workbook contains 16 column rows including the inherited-column case; the Lookup workbook contains 13 typed/null/empty/reference value rows. Its data-quality notices are expected for contract-required blank ID/reference/editable cells, sparse validation-list columns and mixed text in key/value control sheets; they are not OOXML or formula failures.

## Summary

- Errors found: 0.
- Auto-fixed: 0.
- Human review required: independent S05 alignment review of the implementation and generated pair.
- Final validation status: PASS for worker evidence; no self-acceptance is asserted.
