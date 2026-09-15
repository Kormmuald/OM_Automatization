using BpmSoftSync.Application;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Testing;

public sealed class RequestCapture
{
    public int ReadCount { get; private set; }
    public int PromptCount { get; private set; }
    public void RecordRead() => ReadCount++;
    public void RecordPrompt() => PromptCount++;
}

public sealed class FakeCatalogSource(RequestCapture capture) : ICatalogSource
{
    public ValueTask<CatalogPassInput> ReadPassAsync(string exactTargetAlias, string scopeDescriptorHash, int passNumber, CancellationToken cancellationToken = default)
    {
        capture.RecordRead();
        return ValueTask.FromResult(new CatalogPassInput("fake", exactTargetAlias, scopeDescriptorHash, ["fake-version"], new CollectionDefinition("fake", "id", 4), [new CatalogPage("0", ["one"], true)], [], [], [], 1));
    }
}
