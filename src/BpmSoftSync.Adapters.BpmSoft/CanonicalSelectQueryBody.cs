using System.Text;

namespace BpmSoftSync.Adapters.BpmSoft;

public sealed class CanonicalSelectQueryBody : IDisposable
{
    private byte[]? _utf8Json;

    private CanonicalSelectQueryBody(byte[] utf8Json) => _utf8Json = utf8Json;

    public static CanonicalSelectQueryBody Parse(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        if (!ReadEndpointAllowlist.HasCanonicalSelectQueryBody(bytes))
        {
            Array.Clear(bytes);
            throw new BpmSoftTransportException(BpmSoftTransportError.EndpointNotAllowlisted, "ENDPOINT_NOT_ALLOWLISTED: SelectQuery body is not canonical.");
        }
        return new CanonicalSelectQueryBody(bytes);
    }

    internal byte[] DetachBody()
    {
        return Interlocked.Exchange(ref _utf8Json, null) ?? throw new ObjectDisposedException(nameof(CanonicalSelectQueryBody));
    }

    public void Dispose()
    {
        var bytes = Interlocked.Exchange(ref _utf8Json, null);
        if (bytes is not null) Array.Clear(bytes);
    }
}
