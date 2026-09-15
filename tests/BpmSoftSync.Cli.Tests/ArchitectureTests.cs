namespace BpmSoftSync.Cli.Tests;
public static class ArchitectureTests
{
    public static void DomainAndApplicationDoNotDependOnForbiddenAdapters()
    {
        // Typed evidence labels (for example Excel table timestamps) are permitted;
        // adapter implementations and transports are not.
        var forbidden = new[] { "HttpClient", "Browser", "Git" };
        foreach (var directory in new[] { Path.Combine("src", "BpmSoftSync.Domain"), Path.Combine("src", "BpmSoftSync.Application") })
            foreach (var file in Directory.EnumerateFiles(directory, "*.cs"))
                if (forbidden.Any(word => File.ReadAllText(file).Contains(word, StringComparison.Ordinal))) throw new InvalidOperationException("Forbidden architecture dependency: " + Path.GetFileName(file));
    }

    public static void ExactlyOneHttpSendBoundaryAndNoGetPackagesAllowance()
    {
        var adapterDirectory = Path.Combine("src", "BpmSoftSync.Adapters.BpmSoft");
        var sourceFiles = Directory.EnumerateFiles(adapterDirectory, "*.cs").ToArray();
        var httpClientOwners = sourceFiles
            .Where(file => File.ReadAllText(file).Contains("new HttpClient(", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToArray();
        if (!httpClientOwners.SequenceEqual(new[] { "BpmSoftReadTransport.cs" }, StringComparer.Ordinal))
            throw new InvalidOperationException("BPMSoft adapter has a second HTTP entrypoint.");

        var sendOwners = sourceFiles
            .Where(file => File.ReadAllText(file).Contains("_client.SendAsync(", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToArray();
        if (!sendOwners.SequenceEqual(new[] { "BpmSoftReadTransport.cs" }, StringComparer.Ordinal))
            throw new InvalidOperationException("BPMSoft adapter has a second HTTP send boundary.");

        var allowlist = File.ReadAllText(Path.Combine(adapterDirectory, "ReadEndpointAllowlist.cs"));
        if (allowlist.Contains("GET_PACKAGES", StringComparison.Ordinal) || allowlist.Contains("GetPackages", StringComparison.Ordinal))
            throw new InvalidOperationException("GET_PACKAGES remains allowlisted.");
        var transport = File.ReadAllText(Path.Combine(adapterDirectory, "BpmSoftReadTransport.cs"));
        if (!transport.Contains("AllowAutoRedirect = false", StringComparison.Ordinal))
            throw new InvalidOperationException("Production HTTP handler does not disable redirects.");
    }
}
