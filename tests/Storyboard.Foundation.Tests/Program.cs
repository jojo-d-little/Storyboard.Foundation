using Storyboard.Foundation.Paths;

var tests = new (string Name, Action Body)[]
{
    ("expands token and resolves relative path", ExpandsToken),
    ("rejects unresolved token without relative fallback", RejectsUnresolvedToken),
    ("rejects malformed token", RejectsMalformedToken),
    ("enforces absolute and relative policies", EnforcesPolicies),
    ("enforces file URI policy", EnforcesFileUriPolicy),
    ("chooses deepest containing root", ChoosesDeepestRoot),
    ("orders equal-depth roots deterministically", OrdersRootsDeterministically)
};

var failures = new List<string>();
foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failures.Add($"FAIL {test.Name}: {exception.Message}");
        Console.WriteLine(failures[^1]);
    }
}

return failures.Count == 0 ? 0 : 1;

static void ExpandsToken()
{
    const string variable = "STORYBOARD_FOUNDATION_TEST_ROOT";
    var root = Path.Combine(Path.GetTempPath(), "storyboard-foundation-test");
    Environment.SetEnvironmentVariable(variable, root);
    var result = PathExpressionResolver.Resolve($"%{variable}%/child.txt");
    Assert(result.IsSuccess, result.DiagnosticMessage);
    Assert(result.ExpandedExpression == $"{root}/child.txt", "expanded expression mismatch");
    Assert(result.PhysicalPath == Path.GetFullPath(Path.Combine(root, "child.txt")), "physical path mismatch");
}

static void RejectsUnresolvedToken()
{
    var result = PathExpressionResolver.Resolve("%STORYBOARD_FOUNDATION_MISSING%/child.txt");
    Assert(!result.IsSuccess, "unresolved token unexpectedly succeeded");
    Assert(result.DiagnosticCode == PathExpressionDiagnosticCode.UnresolvedVariable, "wrong diagnostic code");
    Assert(result.PhysicalPath is null, "unresolved token produced a physical path");
}

static void RejectsMalformedToken()
{
    var result = PathExpressionResolver.Resolve("%BROKEN/child.txt");
    Assert(result.DiagnosticCode == PathExpressionDiagnosticCode.MalformedToken, "wrong diagnostic code");
}

static void EnforcesPolicies()
{
    var absolute = PathExpressionResolver.Resolve(Path.GetFullPath("absolute.txt"), new PathExpressionOptions { AllowAbsolutePaths = false });
    Assert(absolute.DiagnosticCode == PathExpressionDiagnosticCode.AbsolutePathNotAllowed, "absolute policy not enforced");

    var relative = PathExpressionResolver.Resolve("relative.txt", new PathExpressionOptions { AllowRelativePaths = false });
    Assert(relative.DiagnosticCode == PathExpressionDiagnosticCode.RelativePathNotAllowed, "relative policy not enforced");
}

static void EnforcesFileUriPolicy()
{
    var uri = new Uri(Path.GetFullPath("file.txt"));
    var rejected = PathExpressionResolver.Resolve(uri.AbsoluteUri);
    Assert(rejected.DiagnosticCode == PathExpressionDiagnosticCode.FileUriNotAllowed, "file URI policy not enforced");

    var accepted = PathExpressionResolver.Resolve(uri.AbsoluteUri, new PathExpressionOptions { AllowFileUris = true });
    Assert(accepted.IsSuccess, accepted.DiagnosticMessage);
}

static void ChoosesDeepestRoot()
{
    var root = Path.Combine(Path.GetTempPath(), "storyboard-root");
    var nested = Path.Combine(root, "nested");
    var file = Path.Combine(nested, "file.png");
    var matches = AssetRootMatcher.FindContainingRoots(file, new[]
    {
        new AssetRoot("STORYBOARD_ASSET_SOURCE_ROOT", root),
        new AssetRoot("STORYBOARD_ASSET_SOURCE_ROOT_NESTED", nested)
    });
    Assert(matches.Count == 2, "expected two containing roots");
    Assert(matches[0].Root.VariableName == "STORYBOARD_ASSET_SOURCE_ROOT_NESTED", "deepest root was not first");
    Assert(matches[0].RelativePath == "file.png", "relative path mismatch");
}

static void OrdersRootsDeterministically()
{
    var roots = AssetRootMatcher.OrderRoots(new[]
    {
        new AssetRoot("ZED", Path.Combine(Path.GetTempPath(), "same")),
        new AssetRoot("ALPHA", Path.Combine(Path.GetTempPath(), "same"))
    });
    Assert(roots[0].VariableName == "ALPHA", "equal-depth roots were not ordered by variable name");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
