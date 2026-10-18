namespace Storyboard.Foundation.Paths;

/// <summary>Orders asset roots and finds roots containing a filesystem path.</summary>
public static class AssetRootMatcher
{
    /// <summary>Normalizes and orders roots by depth descending, then variable name ordinally.</summary>
    /// <param name="roots">The roots to normalize and order.</param>
    /// <returns>Normalized roots, with deepest paths first.</returns>
    public static IReadOnlyList<AssetRoot> OrderRoots(IEnumerable<AssetRoot> roots) =>
        roots
            .Select(root => new AssetRoot(root.VariableName, Path.GetFullPath(root.RootPath)))
            .OrderByDescending(root => RootDepth(root.FullRootPath))
            .ThenBy(root => root.VariableName, StringComparer.Ordinal)
            .ToArray();

    /// <summary>Finds every configured root containing a file, with deepest matches first.</summary>
    /// <param name="filePath">The file path to match.</param>
    /// <param name="roots">The candidate roots.</param>
    /// <returns>All containing roots and their relative paths, ordered by depth and variable name.</returns>
    public static IReadOnlyList<AssetRootMatch> FindContainingRoots(string filePath, IEnumerable<AssetRoot> roots)
    {
        var fullFilePath = Path.GetFullPath(filePath);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        return OrderRoots(roots)
            .Select(root =>
            {
                var fullRootPath = EnsureTrailingSeparator(root.FullRootPath);
                if (!fullFilePath.StartsWith(fullRootPath, comparison))
                {
                    return null;
                }

                var relativePath = Path.GetRelativePath(root.FullRootPath, fullFilePath);
                return new AssetRootMatch(root, relativePath, RootDepth(root.FullRootPath));
            })
            .Where(match => match is not null)
            .Cast<AssetRootMatch>()
            .ToArray();
    }

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;

    private static int RootDepth(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)
            .Length;
}
