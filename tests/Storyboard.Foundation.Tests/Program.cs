using Storyboard.Foundation.Paths;
using Storyboard.Foundation.Spatial;
using Storyboard.Shared.RuntimeContracts.Enums;

var tests = new (string Name, Action Body)[]
{
    ("expands token and resolves relative path", ExpandsToken),
    ("rejects unresolved token without relative fallback", RejectsUnresolvedToken),
    ("rejects malformed token", RejectsMalformedToken),
    ("enforces absolute and relative policies", EnforcesPolicies),
    ("enforces file URI policy", EnforcesFileUriPolicy),
    ("chooses deepest containing root", ChoosesDeepestRoot),
    ("orders equal-depth roots deterministically", OrdersRootsDeterministically),
    ("resolves the hosted position for every room-image slot", ResolvesEveryRoomImageSlot),
    ("preserves hosted quarter-turn and legacy rotation behavior", PreservesRoomImageRotationBehavior),
    ("preserves fractional coordinates and exposes native bitmap corners", PreservesFractionalPlacementAndCorners),
    ("rejects invalid placement inputs", RejectsInvalidPlacementInputs)
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

static void ResolvesEveryRoomImageSlot()
{
    var cases = new (RoomImageSlot Slot, double AnchorX, double AnchorY, double OffsetX, double OffsetY)[]
    {
        (RoomImageSlot.Default, 800, 400, -200, -100),
        (RoomImageSlot.Down, 0, 0, 0, 0),
        (RoomImageSlot.North, 800, 0, -200, 0),
        (RoomImageSlot.NorthEast, 1600, 0, -400, 0),
        (RoomImageSlot.East, 1600, 400, -400, -100),
        (RoomImageSlot.SouthEast, 1600, 800, -400, -200),
        (RoomImageSlot.South, 800, 800, -200, -200),
        (RoomImageSlot.SouthWest, 0, 800, 0, -200),
        (RoomImageSlot.West, 0, 400, 0, -100),
        (RoomImageSlot.NorthWest, 0, 0, 0, 0),
        (RoomImageSlot.Up, 800, 400, -200, -100)
    };

    foreach (var item in cases)
    {
        var result = RoomImagePlacementMath.Resolve(item.Slot, 1600, 800, 400, 200, 0, 0, 0);
        AssertNear(result.AnchorX, item.AnchorX, $"{item.Slot} anchor X");
        AssertNear(result.AnchorY, item.AnchorY, $"{item.Slot} anchor Y");
        AssertNear(result.IconOffsetX, item.OffsetX, $"{item.Slot} icon offset X");
        AssertNear(result.IconOffsetY, item.OffsetY, $"{item.Slot} icon offset Y");
        AssertNear(result.X, item.AnchorX + item.OffsetX, $"{item.Slot} X sum");
        AssertNear(result.Y, item.AnchorY + item.OffsetY, $"{item.Slot} Y sum");
    }

    var adjusted = RoomImagePlacementMath.Resolve(RoomImageSlot.Down, 1600, 800, 400, 200, -20.5, 35.25, 0);
    AssertNear(adjusted.X, -20.5, "negative fractional authored X offset");
    AssertNear(adjusted.Y, 35.25, "fractional authored Y offset");
}

static void PreservesRoomImageRotationBehavior()
{
    var expectedDown = new (double Rotation, double X, double Y)[]
    {
        (0, 0, 0),
        (90, 200, 0),
        (180, 400, 200),
        (270, 0, 400)
    };
    foreach (var item in expectedDown)
    {
        var result = RoomImagePlacementMath.Resolve(RoomImageSlot.Down, 1600, 800, 400, 200, 0, 0, item.Rotation);
        AssertNear(result.X, item.X, $"Down {item.Rotation} X");
        AssertNear(result.Y, item.Y, $"Down {item.Rotation} Y");
    }

    var northQuarterTurn = RoomImagePlacementMath.Resolve(RoomImageSlot.North, 1600, 800, 400, 200, 30, 15, 90);
    AssertNear(northQuarterTurn.AnchorX, 800, "North 90 anchor X");
    AssertNear(northQuarterTurn.IconOffsetX, 130, "North 90 normalized X offset");
    AssertNear(northQuarterTurn.IconOffsetY, 15, "North 90 normalized Y offset");
    AssertNear(northQuarterTurn.X, 930, "North 90 draw X");

    var positive = RoomImagePlacementMath.Resolve(RoomImageSlot.North, 1600, 800, 400, 200, 13.25, -7.5, 270);
    var equivalentNegative = RoomImagePlacementMath.Resolve(RoomImageSlot.North, 1600, 800, 400, 200, 13.25, -7.5, -90);
    AssertNear(positive.X, equivalentNegative.X, "equivalent rotation X");
    AssertNear(positive.Y, equivalentNegative.Y, "equivalent rotation Y");

    var legacy = RoomImagePlacementMath.Resolve(RoomImageSlot.North, 1600, 800, 400, 200, 30, 15, 30);
    AssertNear(legacy.X, 830, "legacy 30 degree hosted X");
    AssertNear(legacy.Y, 15, "legacy 30 degree hosted Y");
}

static void PreservesFractionalPlacementAndCorners()
{
    var odd = RoomImagePlacementMath.Resolve(RoomImageSlot.Default, 101, 99, 7, 5, 0, 0, 0);
    AssertNear(odd.AnchorX, 50.5, "odd room half-pixel anchor X");
    AssertNear(odd.AnchorY, 49.5, "odd room half-pixel anchor Y");
    AssertNear(odd.IconOffsetX, -3.5, "odd bitmap half-pixel offset X");
    AssertNear(odd.IconOffsetY, -2.5, "odd bitmap half-pixel offset Y");
    Assert(odd.Corners.Count == 4, "expected all four full-bitmap corners");

    var rotated = RoomImagePlacementMath.Resolve(RoomImageSlot.Down, 1600, 800, 400, 200, 0, 0, 90);
    AssertPoint(rotated.Corners[0], 200, 0, "rotated top-left");
    AssertPoint(rotated.Corners[1], 200, 400, "rotated top-right");
    AssertPoint(rotated.Corners[2], 0, 400, "rotated bottom-right");
    AssertPoint(rotated.Corners[3], 0, 0, "rotated bottom-left");

    var unequal = RoomImagePlacementMath.Resolve(RoomImageSlot.East, 1600, 800, 701, 333, 12.5, -3.25, 0);
    AssertNear(unequal.X, 1600 - 701 + 12.5, "scale-neutral native-pixel placement");
    AssertNear(unequal.Y, 400 - 166.5 - 3.25, "unequal image height placement");
    AssertNear(unequal.X, unequal.AnchorX + unequal.IconOffsetX, "unequal placement X sum invariant");
    AssertNear(unequal.Y, unequal.AnchorY + unequal.IconOffsetY, "unequal placement Y sum invariant");
}

static void RejectsInvalidPlacementInputs()
{
    ExpectOutOfRange(() => RoomImagePlacementMath.Resolve(RoomImageSlot.Down, 100, 100, 0, 20, 0, 0, 0), "unreadable/zero bitmap width");
    ExpectOutOfRange(() => RoomImagePlacementMath.Resolve(RoomImageSlot.Down, double.NaN, 100, 20, 20, 0, 0, 0), "unknown room width");
    ExpectOutOfRange(() => RoomImagePlacementMath.Resolve(RoomImageSlot.Down, 100, 100, 20, 20, double.PositiveInfinity, 0, 0), "non-finite authored offset");
    ExpectOutOfRange(() => RoomImagePlacementMath.Resolve(RoomImageSlot.Down, 100, 100, 20, 20, 0, 0, double.NaN), "non-finite legacy rotation");
    ExpectOutOfRange(() => RoomImagePlacementMath.Resolve((RoomImageSlot)999, 100, 100, 20, 20, 0, 0, 0), "unknown slot");
}

static void ExpectOutOfRange(Action action, string description)
{
    try
    {
        action();
    }
    catch (ArgumentOutOfRangeException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected invalid {description} to be rejected.");
}

static void AssertNear(double actual, double expected, string description)
{
    Assert(Math.Abs(actual - expected) <= 1e-9, $"{description}: expected {expected}, got {actual}");
}

static void AssertPoint((double X, double Y) actual, double x, double y, string description)
{
    AssertNear(actual.X, x, $"{description} X");
    AssertNear(actual.Y, y, $"{description} Y");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
