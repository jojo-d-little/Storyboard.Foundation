namespace Storyboard.Foundation.Paths;

/// <summary>Describes a configured asset root that contains a file.</summary>
/// <param name="Root">The matching root, normalized to an absolute path.</param>
/// <param name="RelativePath">The file path relative to the root.</param>
/// <param name="Depth">The root depth used to order matches, with deeper roots first.</param>
public sealed record AssetRootMatch(AssetRoot Root, string RelativePath, int Depth);
