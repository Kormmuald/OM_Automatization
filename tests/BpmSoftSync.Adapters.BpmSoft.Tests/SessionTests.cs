using BpmSoftSync.Adapters.BpmSoft;

namespace BpmSoftSync.Adapters.BpmSoft.Tests;

public static class SessionTests
{
    public static void EphemeralStateIsClearedOnDispose()
    {
        using var session = new InMemoryReadOnlySession("test-password".AsSpan(), "test-cookie", "test-csrf");
        if (!session.HasEphemeralState)
        {
            throw new InvalidOperationException("Session did not keep ephemeral state in memory.");
        }

        session.Dispose();
        if (session.HasEphemeralState || typeof(InMemoryReadOnlySession).GetProperties().Any(property => property.Name.Contains("password", StringComparison.OrdinalIgnoreCase) || property.Name.Contains("cookie", StringComparison.OrdinalIgnoreCase) || property.Name.Contains("csrf", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Session state survived disposal or is exposed for serialization.");
        }
    }
}
