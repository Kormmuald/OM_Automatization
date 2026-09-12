using BpmSoftSync.Application;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Testing;

public sealed record CapturedRequest(string EndpointId, string Method, string Path);

public sealed class RequestCapture
{
    private readonly List<CapturedRequest> _requests = [];

    public IReadOnlyList<CapturedRequest> Requests => _requests;

    public int RejectedSendCount { get; private set; }

    public int WriteCallCount => _requests.Count(request => request.Method is not "POST" and not "GET");

    public void Record(EndpointClassification endpoint) => _requests.Add(new(endpoint.EndpointId, endpoint.Method, endpoint.Path));

    public void RecordRejected() => RejectedSendCount++;
}

public sealed class FakeReadOnlyTransport(RequestCapture capture) : IReadOnlyTransport
{
    public ValueTask<SafeResult> SendAsync(EndpointClassification endpoint, CancellationToken cancellationToken = default)
    {
        if (endpoint.AllowlistVersion != EndpointClassification.ExactAllowlistVersion)
        {
            capture.RecordRejected();
            return ValueTask.FromResult(SafeResult.Blocked(new Blocker(BlockerCode.EndpointNotAllowlisted, "endpoint", "ENDPOINT_NOT_ALLOWLISTED", "Use an exact ReadEndpointAllowlist/v1 entry.", "Stop the offline run.")));
        }

        capture.Record(endpoint);
        return ValueTask.FromResult(SafeResult.SuccessForHumanReview("offline fixture"));
    }
}
