using GrantOS.Sentinel.Application.Options;
using Microsoft.Extensions.Options;

namespace GrantOS.Sentinel.Infrastructure.Services;

/// <summary>
/// Resolves filesystem paths and enforces configured directory boundaries, including symlink
/// targets. This service performs no filesystem mutation.
/// </summary>
public sealed class FileSystemPathPolicy
{
    private readonly string[] _allowedRoots;
    private readonly StringComparison _pathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    public FileSystemPathPolicy(IOptions<AgentOptions> options)
    {
        _allowedRoots = options.Value.AllowedDirectories
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(path, Directory.GetCurrentDirectory()))
            .Where(Directory.Exists)
            .Select(ResolveExistingPath)
            .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<string> AllowedRoots => _allowedRoots;

    public bool TryResolve(string? requestedPath, out string resolvedPath, out string error)
    {
        resolvedPath = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(requestedPath))
        {
            error = "A path is required.";
            return false;
        }

        if (_allowedRoots.Length == 0)
        {
            error = "Filesystem access is disabled because Agent:AllowedDirectories is empty.";
            return false;
        }

        string candidate;
        try
        {
            candidate = Path.GetFullPath(requestedPath, Directory.GetCurrentDirectory());
            candidate = ResolveExistingPath(candidate);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException)
        {
            error = $"The path is invalid: {ex.Message}";
            return false;
        }

        if (!_allowedRoots.Any(root => IsWithin(candidate, root)))
        {
            error = $"Path '{candidate}' is outside the configured allowed directories.";
            return false;
        }

        resolvedPath = candidate;
        return true;
    }

    private bool IsWithin(string candidate, string root)
    {
        if (string.Equals(candidate, root, _pathComparison))
            return true;

        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;
        return candidate.StartsWith(rootWithSeparator, _pathComparison);
    }

    private static string ResolveExistingPath(string fullPath)
    {
        var root = Path.GetPathRoot(fullPath)
                   ?? throw new ArgumentException("The path does not have a filesystem root.", nameof(fullPath));
        var relative = Path.GetRelativePath(root, fullPath);
        if (relative == ".")
            return root;

        var segments = relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        var current = root;

        for (var index = 0; index < segments.Length; index++)
        {
            current = Path.Combine(current, segments[index]);
            FileSystemInfo? item = Directory.Exists(current)
                ? new DirectoryInfo(current)
                : File.Exists(current)
                    ? new FileInfo(current)
                    : null;

            if (item is null)
            {
                for (index++; index < segments.Length; index++)
                    current = Path.Combine(current, segments[index]);
                break;
            }

            if (item.LinkTarget is not null)
                current = item.ResolveLinkTarget(returnFinalTarget: true)?.FullName
                          ?? throw new IOException($"Could not resolve symbolic link '{item.FullName}'.");
        }

        return Path.GetFullPath(current);
    }
}
