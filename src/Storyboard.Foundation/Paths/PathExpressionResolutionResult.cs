namespace Storyboard.Foundation.Paths;

/// <summary>Contains the resolved path or diagnostic information when resolution fails.</summary>
public sealed record PathExpressionResolutionResult
{
    /// <summary>Gets the original expression supplied by the caller.</summary>
    public required string RawExpression { get; init; }
    /// <summary>Gets the expression after environment-variable expansion or URI conversion.</summary>
    public required string ExpandedExpression { get; init; }
    /// <summary>Gets the normalized physical path when resolution succeeds; otherwise <see langword="null"/>.</summary>
    public string? PhysicalPath { get; init; }
    /// <summary>Gets the base directory used for resolution, when applicable.</summary>
    public string? BaseDirectory { get; init; }
    /// <summary>Gets the resolution diagnostic code.</summary>
    public required PathExpressionDiagnosticCode DiagnosticCode { get; init; }
    /// <summary>Gets a message describing the resolution outcome.</summary>
    public required string DiagnosticMessage { get; init; }
    /// <summary>Gets whether the expression resolved successfully.</summary>
    public bool IsSuccess => DiagnosticCode == PathExpressionDiagnosticCode.None;
}
