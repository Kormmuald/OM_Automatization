function myFunction() {
  var spreadsheet = SpreadsheetApp.getActive();
  spreadsheet.getActiveRange().setDataValidation(SpreadsheetApp.newDataValidation()
  .setAllowInvalid(false)
  .requireValueInList(['Вариант 1', 'Вариант 2'], true)
  .build());
  spreadsheet.getActiveRange().setDataValidation(SpreadsheetApp.newDataValidation()
  .setAllowInvalid(false)
  .requireValueInRange(spreadsheet.getRange('SheetInfo2!$B2:$AZ2'), true)
  .build());
  var destinationRange = spreadsheet.getActiveRange().offset(0, 0, 49);
  spreadsheet.getActiveRange().autoFill(destinationRange, SpreadsheetApp.AutoFillSeries.DEFAULT_SERIES);
  spreadsheet.getCurrentCell().offset(13, 0).activate();
};