using BpmSoftSync.Domain;

namespace BpmSoftSync.Application;

public sealed class ManualInvocationPolicy : IInvocationPolicy
{
    public SafeResult Check(InvocationSource? source, bool interactive)
    {
        if (interactive && source is InvocationSource.ManualTerminal or InvocationSource.DirectCurrentChatRequest)
            return SafeResult.SuccessForHumanReview("manual-read-only-invocation");
        return SafeResult.Blocked(new Blocker(BlockerCode.FullCatalogNotQualified, "invocation", "FULL_CATALOG_NOT_QUALIFIED", "Start an interactive manual command or make a direct current-chat request.", "Do not prompt for credentials or send HTTP; stop."));
    }
}
