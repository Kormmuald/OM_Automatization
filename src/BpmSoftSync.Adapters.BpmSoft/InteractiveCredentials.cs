using System.Text.Json;
using System.Text.Json.Serialization;

namespace BpmSoftSync.Adapters.BpmSoft;

public sealed class InteractiveCredentials : IDisposable
{
    private char[]? _userName;
    private char[]? _password;

    private InteractiveCredentials(BpmSoftTargetOrigin targetOrigin, ReadOnlySpan<char> userName, ReadOnlySpan<char> password)
    {
        TargetOrigin = targetOrigin;
        _userName = userName.ToArray();
        _password = password.ToArray();
    }

    [JsonIgnore]
    public BpmSoftTargetOrigin TargetOrigin { get; }
    [JsonIgnore]
    public bool HasEphemeralState => _userName is not null && _password is not null;

    internal static InteractiveCredentials Create(BpmSoftTargetOrigin targetOrigin, ReadOnlySpan<char> userName, ReadOnlySpan<char> password)
    {
        if (userName.IsEmpty || password.IsEmpty)
            throw new BpmSoftTransportException(BpmSoftTransportError.InteractiveTerminalRequired, "INTERACTIVE_CREDENTIALS_REQUIRED: login and password must be entered in the terminal.");
        return new InteractiveCredentials(targetOrigin, userName, password);
    }

    internal static InteractiveCredentials CreateForTesting(string targetOrigin, string userName, ReadOnlySpan<char> password) =>
        Create(BpmSoftTargetOrigin.ParseInteractive(targetOrigin), userName.AsSpan(), password);

    internal byte[] CreateLoginBody(int timeZoneOffset)
    {
        var userName = _userName ?? throw new ObjectDisposedException(nameof(InteractiveCredentials));
        var password = _password ?? throw new ObjectDisposedException(nameof(InteractiveCredentials));
        using var stream = new MemoryStream();
        byte[] result;
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("UserName", userName);
            writer.WriteString("UserPassword", password);
            writer.WriteNumber("TimeZoneOffset", timeZoneOffset);
            writer.WriteEndObject();
        }
        result = stream.ToArray();
        if (stream.TryGetBuffer(out var buffer) && buffer.Array is not null) Array.Clear(buffer.Array);
        return result;
    }

    public void Dispose()
    {
        var userName = Interlocked.Exchange(ref _userName, null);
        var password = Interlocked.Exchange(ref _password, null);
        if (userName is not null) Array.Clear(userName);
        if (password is not null) Array.Clear(password);
    }

    public override string ToString() => "[REDACTED interactive credentials]";
}
