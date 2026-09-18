namespace Storyboard.Foundation.Paths;

public enum PathExpressionDiagnosticCode
{
    None = 0,
    EmptyExpression,
    MalformedToken,
    UnresolvedVariable,
    AbsolutePathNotAllowed,
    RelativePathNotAllowed,
    FileUriNotAllowed,
    UnsupportedUri,
    InvalidPath,
    BaseDirectoryRequired
}
