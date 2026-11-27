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
    ("preserves existing object placement behavior and fallbacks", PreservesExistingObjectPlacementBehavior),
    ("resolves the hosted position for every room-image slot", ResolvesEveryRoomImageSlot),
    ("preserves hosted quarter-turn and legacy rotation behavior", PreservesRoomImageRotationBehavior),
    ("preserves fractional coordinates and exposes native bitmap corners", PreservesFractionalPlacementAndCorners),
    ("rejects invalid room-image placement inputs", RejectsInvalidPlacementInputs),
    ("composes and inverts the confirmed Elevator placements", ConfirmsElevatorStructuralPlacement),
    ("matches independently transformed corners and quarter-turn round trips", ChecksStructuralPlacementCornersAndRoundTrips),
    ("composes nested fractional structural rotations", ChecksNestedStructuralPlacement),
    ("rejects invalid structural placement inputs", RejectsInvalidStructuralPlacementInputs)
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

static void PreservesExistingObjectPlacementBehavior()
{
    var transformed = RenderablePlacementMath.TransformLocalPointWithinRotatedFootprint(90, 20, 10, 100, 60);
    AssertNear(transformed.X, 50, "existing rotated footprint X");
    AssertNear(transformed.Y, 20, "existing rotated footprint Y");

    var icon = RenderablePlacementMath.ComputeIconLocalOffset(90, 90, 5, 7, 100, 60, 40, 20);
    AssertNear(icon.X, 53, "existing object icon offset X");
    AssertNear(icon.Y, 25, "existing object icon offset Y");

    var invalidOffset = RenderablePlacementMath.ComputeIconLocalOffset(0, 0, double.NaN, 7, 100, 60, 40, 20);
    Assert(invalidOffset == (0, 0), "existing non-finite offset fallback changed");
    var invalidPoint = RenderablePlacementMath.TransformLocalPointWithinRotatedFootprint(90, 20, 10, -1, 60);
    Assert(invalidPoint == (20, 10), "existing invalid footprint fallback changed");
    var nonFiniteRotation = RenderablePlacementMath.TransformLocalPointWithinRotatedFootprint(double.NaN, 20, 10, 100, 60);
    AssertNear(nonFiniteRotation.X, 20, "existing non-finite rotation fallback X");
    AssertNear(nonFiniteRotation.Y, 10, "existing non-finite rotation fallback Y");
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

static void ConfirmsElevatorStructuralPlacement()
{
    var left = RenderablePlacementMath.ComposeChildFootprintPlacement(
        80, 160, 270, 400, 400, 120, 320, 0, 80, 80);
    var right = RenderablePlacementMath.ComposeChildFootprintPlacement(
        80, 160, 270, 400, 400, 200, 320, 0, 80, 80);
    AssertNear(left.RoomOriginX, 400, "Elevator left child AABB X");
    AssertNear(left.RoomOriginY, 360, "Elevator left child AABB Y");
    AssertNear(right.RoomOriginX, 400, "Elevator right child AABB X");
    AssertNear(right.RoomOriginY, 280, "Elevator right child AABB Y");
    AssertNear(left.AbsoluteRotationDegrees, 270, "Elevator child physical rotation");

    var leftFit = RenderablePlacementMath.ComputeIconLocalOffset(270, 180, 11, -2, 80, 80, 67, 82);
    var rightFit = RenderablePlacementMath.ComputeIconLocalOffset(270, 180, 1, -2, 80, 80, 65, 82);
    AssertNear(left.RoomOriginX + leftFit.X, 480, "Elevator left closed bitmap X");
    AssertNear(left.RoomOriginY + leftFit.Y, 362, "Elevator left closed bitmap Y");
    AssertNear(right.RoomOriginX + rightFit.X, 480, "Elevator right closed bitmap X");
    AssertNear(right.RoomOriginY + rightFit.Y, 294, "Elevator right closed bitmap Y");

    var movedLeft = RenderablePlacementMath.ResolveChildFootprintLocalOrigin(
        80, 160, 270, 400, 400, 360, 360, 0, 80, 80);
    AssertNear(movedLeft.X, 120, "moved left inverse local X");
    AssertNear(movedLeft.Y, 280, "moved left inverse local Y");
    AssertNear(360 + leftFit.X, 440, "moved left bitmap X");
    AssertNear(360 + leftFit.Y, 362, "moved left bitmap Y");
}

static void ChecksStructuralPlacementCornersAndRoundTrips()
{
    foreach (var parentTurn in new[] { 0d, 90d, 180d, 270d })
    {
        foreach (var childTurn in new[] { 0d, 90d, 180d, 270d })
        {
            const double parentX = 17.5;
            const double parentY = -31.25;
            const double parentWidth = 310;
            const double parentHeight = 190;
            const double childX = -42.75;
            const double childY = 126.125;
            const double childWidth = 80;
            const double childHeight = 45;

            var placement = RenderablePlacementMath.ComposeChildFootprintPlacement(
                parentX, parentY, parentTurn, parentWidth, parentHeight,
                childX, childY, childTurn, childWidth, childHeight);
            var recovered = RenderablePlacementMath.ResolveChildFootprintLocalOrigin(
                parentX, parentY, parentTurn, parentWidth, parentHeight,
                placement.RoomOriginX, placement.RoomOriginY, childTurn, childWidth, childHeight);
            AssertNear(recovered.X, childX, $"round-trip X at {parentTurn}/{childTurn}");
            AssertNear(recovered.Y, childY, $"round-trip Y at {parentTurn}/{childTurn}");
            AssertNear(placement.AbsoluteRotationDegrees, parentTurn + childTurn, "accumulated physical rotation");

            var parentMin = IndependentBoundsMinimum(parentWidth, parentHeight, parentTurn);
            var childLocalMin = IndependentBoundsMinimum(childWidth, childHeight, childTurn);
            var childRoomMin = IndependentBoundsMinimum(childWidth, childHeight, parentTurn + childTurn);
            foreach (var corner in RectCorners(childWidth, childHeight))
            {
                // Independent assembly: recover the child's physical corner in the parent's
                // original frame, then rotate that entire point with the parent.
                var childPhysicalOrigin = (X: childX - childLocalMin.X, Y: childY - childLocalMin.Y);
                var childTurnedCorner = IndependentRotate(corner.X, corner.Y, childTurn);
                var inParentFrame = IndependentRotate(
                    childPhysicalOrigin.X + childTurnedCorner.X,
                    childPhysicalOrigin.Y + childTurnedCorner.Y,
                    parentTurn);
                var assembled = (
                    X: parentX + inParentFrame.X - parentMin.X,
                    Y: parentY + inParentFrame.Y - parentMin.Y);

                var absoluteTurnedCorner = IndependentRotate(corner.X, corner.Y, parentTurn + childTurn);
                var fromComposedBounds = (
                    X: placement.RoomOriginX + absoluteTurnedCorner.X - childRoomMin.X,
                    Y: placement.RoomOriginY + absoluteTurnedCorner.Y - childRoomMin.Y);
                AssertNear(fromComposedBounds.X, assembled.X, $"assembled corner X at {parentTurn}/{childTurn}");
                AssertNear(fromComposedBounds.Y, assembled.Y, $"assembled corner Y at {parentTurn}/{childTurn}");
            }
        }
    }

    var boundary = RenderablePlacementMath.ComposeChildFootprintPlacement(
        80, 160, 270, 400, 400, 120, 320, 0, 80, 80);
    Assert(boundary.RoomOriginX == 400 && boundary.RoomOriginY == 360,
        "cardinal placement introduced floating-point grid-boundary drift");
}

static void ChecksNestedStructuralPlacement()
{
    var child = RenderablePlacementMath.ComposeChildFootprintPlacement(
        21.25, -40.5, 123.5, 300, 170, -25.75, 65.125, 37.25, 101, 59);
    var grandchild = RenderablePlacementMath.ComposeChildFootprintPlacement(
        child.RoomOriginX, child.RoomOriginY, child.AbsoluteRotationDegrees, 101, 59,
        130.125, -15.875, -54.5, 43, 97);

    AssertNear(child.AbsoluteRotationDegrees, 160.75, "nested child accumulated angle");
    AssertNear(grandchild.AbsoluteRotationDegrees, 106.25, "nested grandchild accumulated angle");

    var childOriginInRoot = (-25.75 - IndependentBoundsMinimum(101, 59, 37.25).X,
        65.125 - IndependentBoundsMinimum(101, 59, 37.25).Y);
    var grandchildOriginInChild = (130.125 - IndependentBoundsMinimum(43, 97, -54.5).X,
        -15.875 - IndependentBoundsMinimum(43, 97, -54.5).Y);
    var rootMin = IndependentBoundsMinimum(300, 170, 123.5);
    foreach (var corner in RectCorners(43, 97))
    {
        var grandchildTurned = IndependentRotate(corner.X, corner.Y, -54.5);
        var inChild = (
            X: grandchildOriginInChild.Item1 + grandchildTurned.X,
            Y: grandchildOriginInChild.Item2 + grandchildTurned.Y);
        var turnedByChild = IndependentRotate(inChild.X, inChild.Y, 37.25);
        var inRoot = (
            X: childOriginInRoot.Item1 + turnedByChild.X,
            Y: childOriginInRoot.Item2 + turnedByChild.Y);
        var inRoom = IndependentRotate(inRoot.X, inRoot.Y, 123.5);
        var assembled = (X: 21.25 + inRoom.X - rootMin.X, Y: -40.5 + inRoom.Y - rootMin.Y);

        var fullTurn = IndependentRotate(corner.X, corner.Y, 106.25);
        var fullMin = IndependentBoundsMinimum(43, 97, 106.25);
        var fromResolved = (
            X: grandchild.RoomOriginX + fullTurn.X - fullMin.X,
            Y: grandchild.RoomOriginY + fullTurn.Y - fullMin.Y);
        AssertNear(fromResolved.X, assembled.X, "nested independently assembled corner X");
        AssertNear(fromResolved.Y, assembled.Y, "nested independently assembled corner Y");
    }

    var restoredLocal = RenderablePlacementMath.ResolveChildFootprintLocalOrigin(
        child.RoomOriginX, child.RoomOriginY, child.AbsoluteRotationDegrees, 101, 59,
        grandchild.RoomOriginX, grandchild.RoomOriginY, -54.5, 43, 97);
    AssertNear(restoredLocal.X, 130.125, "nested inverse X");
    AssertNear(restoredLocal.Y, -15.875, "nested inverse Y");

    // Relative and parent turns remain physical angles; image-local fit stays a separate existing API.
    var fractional = RenderablePlacementMath.ComposeChildFootprintPlacement(
        -100.25, 19.75, 37.25, 240, 90, -18.625, 120.375, -13.5, 63, 109);
    var fractionalInverse = RenderablePlacementMath.ResolveChildFootprintLocalOrigin(
        -100.25, 19.75, 37.25, 240, 90, fractional.RoomOriginX, fractional.RoomOriginY, -13.5, 63, 109);
    AssertNear(fractionalInverse.X, -18.625, "fractional non-cardinal inverse X");
    AssertNear(fractionalInverse.Y, 120.375, "fractional non-cardinal inverse Y");
}

static void RejectsInvalidStructuralPlacementInputs()
{
    ExpectOutOfRange(() => RenderablePlacementMath.ComposeChildFootprintPlacement(
        double.NaN, 0, 0, 10, 10, 0, 0, 0, 1, 1), "non-finite parent origin");
    ExpectOutOfRange(() => RenderablePlacementMath.ComposeChildFootprintPlacement(
        0, 0, 0, -1, 10, 0, 0, 0, 1, 1), "negative parent footprint");
    ExpectOutOfRange(() => RenderablePlacementMath.ResolveChildFootprintLocalOrigin(
        0, 0, 0, 10, 10, 0, 0, double.PositiveInfinity, 1, 1), "non-finite child angle");
}

static (double X, double Y) IndependentRotate(double x, double y, double degrees)
{
    var radians = degrees * (Math.PI / 180d);
    return (x * Math.Cos(radians) - y * Math.Sin(radians), x * Math.Sin(radians) + y * Math.Cos(radians));
}

static (double X, double Y) IndependentBoundsMinimum(double width, double height, double degrees)
{
    var points = new[]
    {
        (X: 0d, Y: 0d),
        IndependentRotate(width, 0, degrees),
        IndependentRotate(width, height, degrees),
        IndependentRotate(0, height, degrees)
    };
    return (points.Min(point => point.X), points.Min(point => point.Y));
}

static (double X, double Y)[] RectCorners(double width, double height) =>
[
    (0, 0),
    (width, 0),
    (width, height),
    (0, height)
];

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
