using System.Text;

namespace Storyboard.Foundation.Paths;

public static class PathExpressionResolver
{
    public static PathExpressionResolutionResult Resolve(
        string? expression,
        PathExpressionOptions? options = null)
    {
        options ??= new PathExpressionOptions();
        var raw = expression ?? string.Empty;

        if (string.IsNullOrWhiteSpace(expression))
        {
            return Failure(raw, raw, PathExpressionDiagnosticCode.EmptyExpression, "The path expression is empty.");
        }

        if (!TryExpandEnvironmentTokens(expression, out var expanded, out var diagnosticCode, out var diagnosticMessage))
        {
            return Failure(raw, expression, diagnosticCode, diagnosticMessage);
        }

        var looksLikeFileUri = expanded.StartsWith("file:", StringComparison.OrdinalIgnoreCase);
        Uri? uri = null;
        if (looksLikeFileUri && Uri.TryCreate(expanded, UriKind.Absolute, out uri) && uri is not null && uri.IsFile)
        {
            if (!options.AllowFileUris)
            {
                return Failure(raw, expanded, PathExpressionDiagnosticCode.FileUriNotAllowed, "File URIs are not permitted by this caller.");
            }

            expanded = uri!.LocalPath;
        }
        else if (!Path.IsPathRooted(expanded) && Uri.TryCreate(expanded, UriKind.Absolute, out var otherUri) && !string.IsNullOrEmpty(otherUri.Scheme))
        {
            return Failure(raw, expanded, PathExpressionDiagnosticCode.UnsupportedUri, $"The URI scheme '{otherUri.Scheme}' is not a supported path expression.");
        }

        bool isAbsolute;
        try
        {
            isAbsolute = Path.IsPathRooted(expanded);
        }
        catch (ArgumentException)
        {
            return Failure(raw, expanded, PathExpressionDiagnosticCode.InvalidPath, "The expanded value is not a valid path.");
        }

        if (isAbsolute && !options.AllowAbsolutePaths)
        {
            return Failure(raw, expanded, PathExpressionDiagnosticCode.AbsolutePathNotAllowed, "Absolute paths are not permitted by this caller.");
        }

        if (!isAbsolute && !options.AllowRelativePaths)
        {
            return Failure(raw, expanded, PathExpressionDiagnosticCode.RelativePathNotAllowed, "Relative paths are not permitted by this caller.");
        }

        string? baseDirectory = null;
        if (!isAbsolute)
        {
            if (options.RequireBaseDirectoryForRelativePaths && string.IsNullOrWhiteSpace(options.BaseDirectory))
            {
                return Failure(raw, expanded, PathExpressionDiagnosticCode.BaseDirectoryRequired, "A base directory is required for relative path expressions.");
            }

            baseDirectory = string.IsNullOrWhiteSpace(options.BaseDirectory)
                ? Environment.CurrentDirectory
                : Path.GetFullPath(options.BaseDirectory);
        }
        else if (!string.IsNullOrWhiteSpace(options.BaseDirectory))
        {
            baseDirectory = Path.GetFullPath(options.BaseDirectory);
        }

        try
        {
            var physicalPath = isAbsolute
                ? Path.GetFullPath(expanded)
                : Path.GetFullPath(expanded, baseDirectory!);

            return new PathExpressionResolutionResult
            {
                RawExpression = raw,
                ExpandedExpression = expanded,
                PhysicalPath = physicalPath,
                BaseDirectory = baseDirectory,
                DiagnosticCode = PathExpressionDiagnosticCode.None,
                DiagnosticMessage = "Path expression resolved successfully."
            };
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException)
        {
            return Failure(raw, expanded, PathExpressionDiagnosticCode.InvalidPath, ex.Message);
        }
    }

    private static bool TryExpandEnvironmentTokens(
        string expression,
        out string expanded,
        out PathExpressionDiagnosticCode diagnosticCode,
        out string diagnosticMessage)
    {
        var builder = new StringBuilder(expression.Length);
        for (var index = 0; index < expression.Length; index++)
        {
            if (expression[index] != '%')
            {
                builder.Append(expression[index]);
                continue;
            }

            var closingIndex = expression.IndexOf('%', index + 1);
            if (closingIndex < 0)
            {
                expanded = expression;
                diagnosticCode = PathExpressionDiagnosticCode.MalformedToken;
                diagnosticMessage = "An environment-variable token is missing its closing '%'.";
                return false;
            }

            var name = expression[(index + 1)..closingIndex];
            if (name.Length == 0 || !IsValidVariableName(name))
            {
                expanded = expression;
                diagnosticCode = PathExpressionDiagnosticCode.MalformedToken;
                diagnosticMessage = $"The environment-variable token '%{name}%' has an invalid name.";
                return false;
            }

            var value = Environment.GetEnvironmentVariable(name);
            if (value is null)
            {
                expanded = expression;
                diagnosticCode = PathExpressionDiagnosticCode.UnresolvedVariable;
                diagnosticMessage = $"Environment variable '{name}' is not defined; the expression was not treated as a relative path.";
                return false;
            }

            builder.Append(value);
            index = closingIndex;
        }

        expanded = builder.ToString();
        diagnosticCode = PathExpressionDiagnosticCode.None;
        diagnosticMessage = string.Empty;
        return true;
    }

    private static bool IsValidVariableName(string name)
    {
        if (!char.IsLetter(name[0]) && name[0] != '_')
        {
            return false;
        }

        return name.Skip(1).All(character => char.IsLetterOrDigit(character) || character == '_');
    }

    private static PathExpressionResolutionResult Failure(
        string raw,
        string expanded,
        PathExpressionDiagnosticCode code,
        string message) => new()
        {
            RawExpression = raw,
            ExpandedExpression = expanded,
            PhysicalPath = null,
            BaseDirectory = null,
            DiagnosticCode = code,
            DiagnosticMessage = message
        };
}
