namespace BpmSoftSync.Adapters.BpmSoft;

public sealed class InMemoryReadOnlySession : IDisposable
{
    private char[]? _password;
    private string? _cookie;
    private string? _csrf;

    public InMemoryReadOnlySession(ReadOnlySpan<char> password, string cookie, string csrf)
    {
        _password = password.ToArray();
        _cookie = cookie;
        _csrf = csrf;
    }

    public bool HasEphemeralState => _password is not null && _cookie is not null && _csrf is not null;

    public void Dispose()
    {
        if (_password is not null)
        {
            Array.Clear(_password);
        }

        _password = null;
        _cookie = null;
        _csrf = null;
    }
}
