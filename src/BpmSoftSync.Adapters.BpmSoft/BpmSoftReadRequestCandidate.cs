using System.Text;

namespace BpmSoftSync.Adapters.BpmSoft;

internal sealed class BpmSoftReadRequestCandidate : IDisposable
{
    private byte[]? _body;

    private BpmSoftReadRequestCandidate(string requestId, HttpMethod method, Uri requestUri, byte[] body)
    {
        RequestId = requestId;
        Method = method;
        RequestUri = requestUri;
        _body = body;
    }

    public string RequestId { get; }
    public HttpMethod Method { get; }
    public Uri RequestUri { get; }
    public ReadOnlyMemory<byte> Body => _body ?? throw new ObjectDisposedException(nameof(BpmSoftReadRequestCandidate));

    internal static BpmSoftReadRequestCandidate Create(BpmSoftTargetOrigin origin, string requestId, byte[] body) =>
        new(requestId, HttpMethod.Post, new Uri(origin.Uri, ReadEndpointAllowlist.PathFor(requestId)), body);

    internal static BpmSoftReadRequestCandidate ForContractTest(string requestId, HttpMethod method, Uri requestUri, string body) =>
        new(requestId, method, requestUri, Encoding.UTF8.GetBytes(body));

    internal byte[] DetachBody() => Interlocked.Exchange(ref _body, null) ?? throw new ObjectDisposedException(nameof(BpmSoftReadRequestCandidate));

    public void Dispose()
    {
        var body = Interlocked.Exchange(ref _body, null);
        if (body is not null) Array.Clear(body);
    }
}
