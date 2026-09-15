using System.Net;

namespace BpmSoftSync.Adapters.BpmSoft;

internal sealed class InMemoryReadOnlySession : IDisposable
{
    private CookieContainer? _cookies;
    private char[]? _csrf;

    private InMemoryReadOnlySession(CookieContainer cookies, char[] csrf)
    {
        _cookies = cookies;
        _csrf = csrf;
    }

    public bool HasEphemeralState => _cookies is not null && _csrf is not null;

    internal static InMemoryReadOnlySession Create(BpmSoftTargetOrigin origin, IEnumerable<string> setCookieHeaders)
    {
        var cookies = new CookieContainer();
        try
        {
            foreach (var header in setCookieHeaders) cookies.SetCookies(origin.Uri, header);
        }
        catch (CookieException)
        {
            throw new BpmSoftTransportException(BpmSoftTransportError.CsrfCookieMissing, "CSRF_COOKIE_MISSING: login did not establish a valid session cookie.");
        }

        var csrf = cookies.GetCookies(origin.Uri)["BPMCSRF"]?.Value;
        if (string.IsNullOrWhiteSpace(csrf))
            throw new BpmSoftTransportException(BpmSoftTransportError.CsrfCookieMissing, "CSRF_COOKIE_MISSING: successful login did not provide BPMCSRF.");
        return new InMemoryReadOnlySession(cookies, csrf.ToCharArray());
    }

    internal void Apply(HttpRequestMessage request, BpmSoftTargetOrigin origin)
    {
        var cookies = _cookies ?? throw new BpmSoftTransportException(BpmSoftTransportError.SessionDisposed, "SESSION_DISPOSED: session material is unavailable.");
        var csrf = _csrf ?? throw new BpmSoftTransportException(BpmSoftTransportError.SessionDisposed, "SESSION_DISPOSED: session material is unavailable.");
        var cookieHeader = cookies.GetCookieHeader(origin.Uri);
        if (string.IsNullOrWhiteSpace(cookieHeader))
            throw new BpmSoftTransportException(BpmSoftTransportError.SessionRequired, "SESSION_REQUIRED: read request has no session cookie.");
        request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        request.Headers.TryAddWithoutValidation("BPMCSRF", new string(csrf));
    }

    public void Dispose()
    {
        _cookies = null;
        var csrf = Interlocked.Exchange(ref _csrf, null);
        if (csrf is not null) Array.Clear(csrf);
    }
}
