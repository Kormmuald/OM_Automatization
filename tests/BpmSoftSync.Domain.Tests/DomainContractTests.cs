using BpmSoftSync.Domain;

namespace BpmSoftSync.Domain.Tests;

public static class DomainContractTests
{
    public static void RequiredContractsExist()
    {
        var blocker = new Blocker(BlockerCode.EndpointNotAllowlisted, "endpoint", "safe", "recover", "stop");
        var result = SafeResult.Blocked(blocker);
        if (result.IsSuccess || result.Reason != "safe" || typeof(EndpointClassification).GetProperties().Any(property => property.Name is "Name" or "Code"))
        {
            throw new InvalidOperationException("Domain safe contracts are invalid.");
        }
    }
}
