using BpmSoftSync.Application;
using BpmSoftSync.Cli.Commands;

namespace BpmSoftSync.Cli.Tests;
public static class AuthorizationGateTests
{
    public static async Task MissingAuthorizationNeverPromptsOrSendsAsync()
    {
        var result = await new CatalogQualifyCommand(new ManualInvocationPolicy()).ExecuteAsync(["--scope", "fixture"] , new StringWriter());
        if (result.IsSuccess || result.Reason != "QUALIFY_COMMAND_INVALID") throw new InvalidOperationException("Automatic invocation reached the terminal prompt.");
    }
    public static async Task OnlyManualInvocationMayReachTerminalPromptAsync()
    {
        var result = await new CatalogQualifyCommand(new ManualInvocationPolicy()).ExecuteAsync(["--target", "fixture", "--scope", "full", "--manual"], new StringWriter());
        if (result.IsSuccess || result.Reason != "LIVE_ADMISSION_REQUIRED") throw new InvalidOperationException("Manual invocation bypassed the live admission boundary.");
    }
}
