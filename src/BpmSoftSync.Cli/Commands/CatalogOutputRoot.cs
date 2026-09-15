namespace BpmSoftSync.Cli.Commands;

internal static class CatalogOutputRoot
{
    internal static string Default => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BpmSoftSync");

    internal static bool TryResolve(string? requested, out string root)
    {
        root = string.Empty;
        try
        {
            var candidate = NormalizeDirectory(string.IsNullOrWhiteSpace(requested) ? Default : requested);
            var workspace = FindWorkspaceRoot(Environment.CurrentDirectory);
            var denied = new[]
            {
                NormalizeDirectory(workspace),
                NormalizeDirectory(Path.GetTempPath())
            };
            if (denied.Any(path => candidate.Equals(path, StringComparison.OrdinalIgnoreCase) || IsNested(candidate, path))) return false;
            root = candidate;
            return true;
        }
        catch (Exception) when (requested is not null) { return false; }
    }

    private static string FindWorkspaceRoot(string current)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(current));
        while (directory.Parent is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")) || File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))) return directory.FullName;
            directory = directory.Parent;
        }
        return Path.GetFullPath(current);
    }

    private static string NormalizeDirectory(string path)
    {
        var full = Path.GetFullPath(path);
        var root = Path.GetPathRoot(full) ?? string.Empty;
        return string.Equals(full, root, StringComparison.OrdinalIgnoreCase)
            ? root
            : full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static bool IsNested(string candidate, string root) =>
        candidate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
        candidate.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
