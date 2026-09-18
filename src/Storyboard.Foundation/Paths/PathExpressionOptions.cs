namespace Storyboard.Foundation.Paths;

public sealed record PathExpressionOptions
{
    public bool AllowAbsolutePaths { get; init; } = true;
    public bool AllowRelativePaths { get; init; } = true;
    public bool AllowFileUris { get; init; }
    public string? BaseDirectory { get; init; }
    public bool RequireBaseDirectoryForRelativePaths { get; init; }
}
