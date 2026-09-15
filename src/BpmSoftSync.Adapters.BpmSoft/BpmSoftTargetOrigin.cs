using System.Text.Json.Serialization;

namespace BpmSoftSync.Adapters.BpmSoft;

public sealed class BpmSoftTargetOrigin
{
    private BpmSoftTargetOrigin(Uri uri) => Uri = uri;

    [JsonIgnore]
    public Uri Uri { get; }

    public static BpmSoftTargetOrigin ParseInteractive(string input)
    {
        if (string.IsNullOrWhiteSpace(input) ||
            !System.Uri.TryCreate(input.Trim(), UriKind.Absolute, out var parsed) ||
            (parsed.Scheme != System.Uri.UriSchemeHttps && (parsed.Scheme != System.Uri.UriSchemeHttp || !parsed.IsLoopback)) ||
            !string.IsNullOrEmpty(parsed.UserInfo) ||
            parsed.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(parsed.Query) ||
            !string.IsNullOrEmpty(parsed.Fragment))
        {
            throw new BpmSoftTransportException(
                BpmSoftTransportError.TargetOriginNotAllowed,
                "TARGET_ORIGIN_NOT_ALLOWED: enter an HTTPS origin, or a loopback HTTP(S) origin, without credentials, path, query, or fragment.");
        }

        return new BpmSoftTargetOrigin(new Uri(parsed.GetLeftPart(UriPartial.Authority).TrimEnd('/') + "/"));
    }

    internal bool IsExactOrigin(Uri candidate)
    {
        if (!candidate.IsAbsoluteUri || !string.IsNullOrEmpty(candidate.Query) || !string.IsNullOrEmpty(candidate.Fragment)) return false;
        return string.Equals(Uri.GetLeftPart(UriPartial.Authority), candidate.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString() => "[REDACTED BPMSoft target origin]";
}
