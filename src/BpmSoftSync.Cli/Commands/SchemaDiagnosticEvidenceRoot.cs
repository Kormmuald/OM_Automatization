namespace BpmSoftSync.Cli.Commands;

/// <summary>
/// Resolves a diagnostic root only below the dedicated user-local base. Existing
/// ancestors are inspected physically: a reparse point is rejected rather than
/// followed, so a lexical user-local path cannot escape through a junction/link.
/// Newly-created descendants are rechecked by the evidence store before use.
/// </summary>
internal static class SchemaDiagnosticEvidenceRoot
{
    internal static string Default => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BpmSoftSync", "diagnostic-evidence");

    internal static bool TryResolve(string? requested, out string root)
    {
        root = string.Empty;
        try
        {
            if (requested is not null && (!Path.IsPathRooted(requested) || string.IsNullOrWhiteSpace(requested))) return false;
            var candidate = Normalize(string.IsNullOrWhiteSpace(requested) ? Default : requested);
            var allowedBase = Normalize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BpmSoftSync"));
            var workspace = Normalize(FindWorkspaceRoot(Environment.CurrentDirectory));
            var temp = Normalize(Path.GetTempPath());
            var windows = Normalize(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
            var system = Normalize(Environment.GetFolderPath(Environment.SpecialFolder.System));
            var user = Normalize(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            var volume = Path.GetPathRoot(candidate) ?? string.Empty;
            if (candidate.Equals(volume, StringComparison.OrdinalIgnoreCase)
                || !IsNested(candidate, user)
                || !IsNested(candidate, allowedBase)
                || new[] { workspace, temp, windows, system }.Any(denied => candidate.Equals(denied, StringComparison.OrdinalIgnoreCase) || IsNested(candidate, denied))
                || !ExistingAncestorsArePhysicalDirectories(candidate)) return false;
            root = candidate;
            return true;
        }
        catch { return false; }
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

    private static string Normalize(string path)
    {
        var full = Path.GetFullPath(path);
        var volume = Path.GetPathRoot(full) ?? string.Empty;
        return string.Equals(full, volume, StringComparison.OrdinalIgnoreCase) ? volume : full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static bool ExistingAncestorsArePhysicalDirectories(string candidate)
    {
        var volume = Path.GetPathRoot(candidate);
        if (string.IsNullOrWhiteSpace(volume)) return false;
        if (!IsPhysicalDirectory(volume)) return false;
        var relative = candidate[volume.Length..];
        var current = volume;
        foreach (var segment in relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (File.Exists(current)) return false;
            if (!Directory.Exists(current)) continue;
            if (!IsPhysicalDirectory(current)) return false;
        }
        return true;
    }

    private static bool IsPhysicalDirectory(string path)
    {
        if (!Directory.Exists(path) || File.Exists(path)) return false;
        var directory = new DirectoryInfo(path);
        return (directory.Attributes & FileAttributes.ReparsePoint) == 0 && directory.LinkTarget is null;
    }

    private static bool IsNested(string candidate, string parent) => candidate.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || candidate.StartsWith(parent + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
