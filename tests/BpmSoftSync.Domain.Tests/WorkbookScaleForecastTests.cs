using BpmSoftSync.Domain;

namespace BpmSoftSync.Domain.Tests;
public static class WorkbookScaleForecastTests
{
    public static void IsDiagnosticOnlyAndFailsClosedForUnknownLimits()
    {
        Assert(WorkbookScaleForecast.Create(4, 10).Status == WorkbookScaleForecastStatus.DiagnosticOnly, "Known counts must be diagnostic only.");
        Assert(WorkbookScaleForecast.Create(4, null).Reason == "WORKBOOK_SCALE_DECISION_REQUIRED", "Absent limit must require decision.");
        Assert(WorkbookScaleForecast.Create(11, 10).Reason == "WORKBOOK_SCALE_DECISION_REQUIRED", "Over-limit forecast must require decision.");
        Assert(WorkbookScaleForecast.Create(new Dictionary<string, int> { ["LookupValues"] = 11, ["Schemas"] = 2 }, 10).Status == WorkbookScaleForecastStatus.DecisionRequired, "Largest projected worksheet did not fail closed.");
    }
    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
