namespace Storyboard.Foundation.Paths;

public sealed record PathExpressionResolutionResult
{
    public required string RawExpression { get; init; }
    public required string ExpandedExpression { get; init; }
    public string? PhysicalPath { get; init; }
    public string? BaseDirectory { get; init; }
    public required PathExpressionDiagnosticCode DiagnosticCode { get; init; }
    public required string DiagnosticMessage { get; init; }
    public bool IsSuccess => DiagnosticCode == PathExpressionDiagnosticCode.None;
}
