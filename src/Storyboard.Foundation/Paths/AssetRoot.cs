namespace Storyboard.Foundation.Paths;

/// <summary>Defines a named root directory that can own portable asset paths.</summary>
/// <param name="VariableName">The name of the environment variable identifying the root.</param>
/// <param name="RootPath">The configured filesystem path for the root.</param>
public sealed record AssetRoot(string VariableName, string RootPath)
{
    /// <summary>Gets the root path in normalized absolute form.</summary>
    public string FullRootPath => Path.GetFullPath(RootPath);
}
