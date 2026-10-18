namespace Storyboard.Foundation.Paths;

/// <summary>Specifies caller policy for resolving a portable path expression.</summary>
public sealed record PathExpressionOptions
{
    /// <summary>Gets or sets whether absolute filesystem paths are allowed.</summary>
    public bool AllowAbsolutePaths { get; init; } = true;
    /// <summary>Gets or sets whether relative filesystem paths are allowed.</summary>
    public bool AllowRelativePaths { get; init; } = true;
    /// <summary>Gets or sets whether file URIs are allowed.</summary>
    public bool AllowFileUris { get; init; }
    /// <summary>Gets or sets the directory used to resolve a relative expression.</summary>
    public string? BaseDirectory { get; init; }
    /// <summary>Gets or sets whether relative expressions require an explicit <see cref="BaseDirectory"/>.</summary>
    public bool RequireBaseDirectoryForRelativePaths { get; init; }
}
