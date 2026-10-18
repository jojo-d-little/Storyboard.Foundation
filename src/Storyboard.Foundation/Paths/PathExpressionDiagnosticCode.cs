namespace Storyboard.Foundation.Paths;

/// <summary>Identifies the outcome or failure reason for resolving a portable path expression.</summary>
public enum PathExpressionDiagnosticCode
{
    /// <summary>The expression resolved successfully.</summary>
    None = 0,
    /// <summary>The expression was empty or whitespace.</summary>
    EmptyExpression,
    /// <summary>An environment-variable token was malformed.</summary>
    MalformedToken,
    /// <summary>An environment variable in the expression was not defined.</summary>
    UnresolvedVariable,
    /// <summary>The caller disallowed an absolute path.</summary>
    AbsolutePathNotAllowed,
    /// <summary>The caller disallowed a relative path.</summary>
    RelativePathNotAllowed,
    /// <summary>The caller disallowed a file URI.</summary>
    FileUriNotAllowed,
    /// <summary>The expression contains an unsupported non-file URI scheme.</summary>
    UnsupportedUri,
    /// <summary>The expanded value could not be used as a filesystem path.</summary>
    InvalidPath,
    /// <summary>A relative path requires an explicit base directory, but none was supplied.</summary>
    BaseDirectoryRequired
}
