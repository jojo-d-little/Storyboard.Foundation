namespace Storyboard.Foundation.Paths;

public sealed record AssetRoot(string VariableName, string RootPath)
{
    public string FullRootPath => Path.GetFullPath(RootPath);
}
