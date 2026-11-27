namespace Storyboard.Foundation.Spatial;

/// <summary>
/// Renderer-agnostic placement math for footprint-relative object icon placement.
/// This helper is free of UI-framework types so hosts can share one deterministic transform pipeline.
/// </summary>
public static class RenderablePlacementMath
{
    /// <summary>
    /// Composes a child footprint's parent-local AABB origin and relative physical angle into room coordinates.
    /// </summary>
    /// <param name="parentRoomOriginX">Parent footprint's room-space AABB origin X, in pixels.</param>
    /// <param name="parentRoomOriginY">Parent footprint's room-space AABB origin Y, in pixels.</param>
    /// <param name="parentAbsoluteRotationDegrees">Parent's accumulated absolute physical angle in clockwise screen degrees.</param>
    /// <param name="parentFootprintWidthPixels">Parent's unrotated physical footprint width in pixels.</param>
    /// <param name="parentFootprintHeightPixels">Parent's unrotated physical footprint height in pixels.</param>
    /// <param name="childParentLocalOriginX">Child footprint's AABB origin in the parent's unrotated footprint frame, in pixels.</param>
    /// <param name="childParentLocalOriginY">Child footprint's AABB origin in the parent's unrotated footprint frame, in pixels.</param>
    /// <param name="childRelativeRotationDegrees">Child's physical angle relative to the parent, clockwise in screen coordinates.</param>
    /// <param name="childFootprintWidthPixels">Child's unrotated physical footprint width in pixels.</param>
    /// <param name="childFootprintHeightPixels">Child's unrotated physical footprint height in pixels.</param>
    /// <returns>The child's room-space AABB origin and accumulated absolute physical angle.</returns>
    /// <exception cref="ArgumentOutOfRangeException">An input coordinate or angle is non-finite, a footprint dimension is negative or non-finite, or accumulated angle overflows.</exception>
    /// <remarks>
    /// The local child origin is already the origin of the child's AABB after its relative turn. Bitmap-local fit and image-local
    /// rotation are separate operations and remain the responsibility of <see cref="ComputeIconLocalOffset"/>.
    /// </remarks>
    public static StructuralFootprintPlacement ComposeChildFootprintPlacement(
        double parentRoomOriginX,
        double parentRoomOriginY,
        double parentAbsoluteRotationDegrees,
        double parentFootprintWidthPixels,
        double parentFootprintHeightPixels,
        double childParentLocalOriginX,
        double childParentLocalOriginY,
        double childRelativeRotationDegrees,
        double childFootprintWidthPixels,
        double childFootprintHeightPixels)
    {
        ValidateStructuralPlacementInputs(
            parentRoomOriginX,
            parentRoomOriginY,
            parentAbsoluteRotationDegrees,
            parentFootprintWidthPixels,
            parentFootprintHeightPixels,
            childParentLocalOriginX,
            childParentLocalOriginY,
            childRelativeRotationDegrees,
            childFootprintWidthPixels,
            childFootprintHeightPixels);

        var childAbsoluteRotationDegrees = parentAbsoluteRotationDegrees + childRelativeRotationDegrees;
        if (!double.IsFinite(childAbsoluteRotationDegrees))
        {
            throw new ArgumentOutOfRangeException(nameof(childRelativeRotationDegrees), "The accumulated physical angle must remain finite.");
        }

        var parentBoundsMinimum = GetRotatedFootprintMinimum(parentFootprintWidthPixels, parentFootprintHeightPixels, parentAbsoluteRotationDegrees);
        var childLocalBoundsMinimum = GetRotatedFootprintMinimum(childFootprintWidthPixels, childFootprintHeightPixels, childRelativeRotationDegrees);
        var childRoomBoundsMinimum = GetRotatedFootprintMinimum(childFootprintWidthPixels, childFootprintHeightPixels, childAbsoluteRotationDegrees);
        var childOriginBeforeParentTurnX = childParentLocalOriginX - childLocalBoundsMinimum.X;
        var childOriginBeforeParentTurnY = childParentLocalOriginY - childLocalBoundsMinimum.Y;
        var rotatedChildOrigin = Rotate(childOriginBeforeParentTurnX, childOriginBeforeParentTurnY, parentAbsoluteRotationDegrees);

        var roomX = parentRoomOriginX + rotatedChildOrigin.X - parentBoundsMinimum.X + childRoomBoundsMinimum.X;
        var roomY = parentRoomOriginY + rotatedChildOrigin.Y - parentBoundsMinimum.Y + childRoomBoundsMinimum.Y;
        ValidateStructuralResult(roomX, nameof(childParentLocalOriginX));
        ValidateStructuralResult(roomY, nameof(childParentLocalOriginY));

        return new StructuralFootprintPlacement(roomX, roomY, childAbsoluteRotationDegrees);
    }

    /// <summary>
    /// Resolves a child's parent-local AABB origin from its room-space AABB origin using the inverse composition.
    /// </summary>
    /// <param name="parentRoomOriginX">Parent footprint's room-space AABB origin X, in pixels.</param>
    /// <param name="parentRoomOriginY">Parent footprint's room-space AABB origin Y, in pixels.</param>
    /// <param name="parentAbsoluteRotationDegrees">Parent's accumulated absolute physical angle in clockwise screen degrees.</param>
    /// <param name="parentFootprintWidthPixels">Parent's unrotated physical footprint width in pixels.</param>
    /// <param name="parentFootprintHeightPixels">Parent's unrotated physical footprint height in pixels.</param>
    /// <param name="childRoomOriginX">Child footprint's room-space AABB origin X, in pixels.</param>
    /// <param name="childRoomOriginY">Child footprint's room-space AABB origin Y, in pixels.</param>
    /// <param name="childRelativeRotationDegrees">Child's physical angle relative to the parent, clockwise in screen coordinates.</param>
    /// <param name="childFootprintWidthPixels">Child's unrotated physical footprint width in pixels.</param>
    /// <param name="childFootprintHeightPixels">Child's unrotated physical footprint height in pixels.</param>
    /// <returns>The child AABB origin (X, Y) in the parent's unrotated footprint frame, after the child's relative turn.</returns>
    /// <exception cref="ArgumentOutOfRangeException">An input coordinate or angle is non-finite, a footprint dimension is negative or non-finite, or accumulated angle overflows.</exception>
    /// <remarks>
    /// This is the inverse of <see cref="ComposeChildFootprintPlacement"/> for the same geometry. Use it for footprint anchors,
    /// not bitmap draw origins. Angles equivalent modulo 360 degrees produce equivalent coordinates.
    /// </remarks>
    public static (double X, double Y) ResolveChildFootprintLocalOrigin(
        double parentRoomOriginX,
        double parentRoomOriginY,
        double parentAbsoluteRotationDegrees,
        double parentFootprintWidthPixels,
        double parentFootprintHeightPixels,
        double childRoomOriginX,
        double childRoomOriginY,
        double childRelativeRotationDegrees,
        double childFootprintWidthPixels,
        double childFootprintHeightPixels)
    {
        ValidateStructuralPlacementInputs(
            parentRoomOriginX,
            parentRoomOriginY,
            parentAbsoluteRotationDegrees,
            parentFootprintWidthPixels,
            parentFootprintHeightPixels,
            childRoomOriginX,
            childRoomOriginY,
            childRelativeRotationDegrees,
            childFootprintWidthPixels,
            childFootprintHeightPixels);

        var childAbsoluteRotationDegrees = parentAbsoluteRotationDegrees + childRelativeRotationDegrees;
        if (!double.IsFinite(childAbsoluteRotationDegrees))
        {
            throw new ArgumentOutOfRangeException(nameof(childRelativeRotationDegrees), "The accumulated physical angle must remain finite.");
        }

        var parentBoundsMinimum = GetRotatedFootprintMinimum(parentFootprintWidthPixels, parentFootprintHeightPixels, parentAbsoluteRotationDegrees);
        var childLocalBoundsMinimum = GetRotatedFootprintMinimum(childFootprintWidthPixels, childFootprintHeightPixels, childRelativeRotationDegrees);
        var childRoomBoundsMinimum = GetRotatedFootprintMinimum(childFootprintWidthPixels, childFootprintHeightPixels, childAbsoluteRotationDegrees);

        var relativeRoomX = childRoomOriginX - parentRoomOriginX + parentBoundsMinimum.X - childRoomBoundsMinimum.X;
        var relativeRoomY = childRoomOriginY - parentRoomOriginY + parentBoundsMinimum.Y - childRoomBoundsMinimum.Y;
        var unrotatedRelative = Rotate(relativeRoomX, relativeRoomY, -parentAbsoluteRotationDegrees);
        var localX = unrotatedRelative.X + childLocalBoundsMinimum.X;
        var localY = unrotatedRelative.Y + childLocalBoundsMinimum.Y;
        ValidateStructuralResult(localX, nameof(childRoomOriginX));
        ValidateStructuralResult(localY, nameof(childRoomOriginY));
        return (localX, localY);
    }

    /// <summary>
    /// Transforms a footprint-local point into the rotated footprint's room-relative bounds.
    /// Positive degrees follow the engine's screen-space clockwise rotation convention.
    /// </summary>
    /// <param name="rotationDegrees">Rotation applied in screen coordinates.</param>
    /// <param name="localX">Point X coordinate in the unrotated footprint-local frame.</param>
    /// <param name="localY">Point Y coordinate in the unrotated footprint-local frame.</param>
    /// <param name="footprintWidthPixels">Unrotated footprint width in pixels.</param>
    /// <param name="footprintHeightPixels">Unrotated footprint height in pixels.</param>
    /// <returns>The point rebased into the axis-aligned bounds of the rotated footprint.</returns>
    public static (double X, double Y) TransformLocalPointWithinRotatedFootprint(
        double rotationDegrees,
        double localX,
        double localY,
        double footprintWidthPixels,
        double footprintHeightPixels)
    {
        if (!double.IsFinite(localX) || !double.IsFinite(localY)
            || !double.IsFinite(footprintWidthPixels) || !double.IsFinite(footprintHeightPixels)
            || footprintWidthPixels < 0 || footprintHeightPixels < 0)
        {
            return (localX, localY);
        }

        var rotation = NormalizeDegrees(rotationDegrees);
        var rotatedPoint = Rotate(localX, localY, rotation);
        var corners = new[]
        {
            Rotate(0, 0, rotation),
            Rotate(footprintWidthPixels, 0, rotation),
            Rotate(0, footprintHeightPixels, rotation),
            Rotate(footprintWidthPixels, footprintHeightPixels, rotation)
        };
        var minX = corners.Min(static corner => corner.X);
        var minY = corners.Min(static corner => corner.Y);
        var x = rotatedPoint.X - minX;
        var y = rotatedPoint.Y - minY;
        return (NormalizeNearZero(x), NormalizeNearZero(y));
    }

    /// <summary>
    /// Computes the renderer-root offset for a bitmap whose scaled, locally rotated
    /// bounds are normalized to the footprint's top-left before the local offset is
    /// applied. The room rotation then rotates that footprint-local placement.
    /// </summary>
    /// <param name="roomRotationDegrees">Rotation of the physical object and its footprint.</param>
    /// <param name="localRotationDegrees">Appearance rotation of the selected bitmap.</param>
    /// <param name="localOffsetX">Post-local-rotation translation in footprint-local pixels.</param>
    /// <param name="localOffsetY">Post-local-rotation translation in footprint-local pixels.</param>
    /// <param name="footprintWidthPixels">Unrotated physical footprint width, before room rotation.</param>
    /// <param name="footprintHeightPixels">Unrotated physical footprint height, before room rotation.</param>
    /// <param name="renderedIconWidthPixels">Full bitmap width after authored base scale, including transparent pixels.</param>
    /// <param name="renderedIconHeightPixels">Full bitmap height after authored base scale, including transparent pixels.</param>
    /// <returns>The offset to add to the room-rotated footprint origin for the renderer's bitmap origin.</returns>
    public static (double X, double Y) ComputeIconLocalOffset(
        double roomRotationDegrees,
        double localRotationDegrees,
        double localOffsetX,
        double localOffsetY,
        double footprintWidthPixels,
        double footprintHeightPixels,
        double renderedIconWidthPixels,
        double renderedIconHeightPixels)
    {
        if (!double.IsFinite(localOffsetX) || !double.IsFinite(localOffsetY))
        {
            return (0, 0);
        }

        var localBoundsTopLeft = double.IsFinite(renderedIconWidthPixels)
                                 && renderedIconWidthPixels > 0
                                 && double.IsFinite(renderedIconHeightPixels)
                                 && renderedIconHeightPixels > 0
            ? GetRotatedBoundsTopLeft(renderedIconWidthPixels, renderedIconHeightPixels, localRotationDegrees)
            : (X: 0d, Y: 0d);

        // First normalize the full bitmap's bounds after base scale and local rotation.
        // The authored offset is then applied in footprint-local axes, so local image
        // rotation never changes the meaning or direction of the offset.
        var normalizedLocalImageOriginX = localOffsetX - localBoundsTopLeft.X;
        var normalizedLocalImageOriginY = localOffsetY - localBoundsTopLeft.Y;

        // The room rotation transforms both the normalized image placement and the
        // footprint. Use the unrotated physical footprint to rebase its origin.
        return TransformLocalPointWithinRotatedFootprint(
            roomRotationDegrees,
            normalizedLocalImageOriginX,
            normalizedLocalImageOriginY,
            footprintWidthPixels,
            footprintHeightPixels);
    }

    /// <summary>Normalizes an angle to the range [0, 360); non-finite values normalize to zero.</summary>
    /// <param name="degrees">The angle in degrees.</param>
    /// <returns>An equivalent angle in the range [0, 360).</returns>
    public static double NormalizeDegrees(double degrees)
    {
        if (!double.IsFinite(degrees))
        {
            return 0;
        }

        var normalized = degrees % 360d;
        return normalized < 0 ? normalized + 360d : normalized;
    }

    private static (double X, double Y) Rotate(double x, double y, double rotationDegrees)
    {
        var normalized = NormalizeDegrees(rotationDegrees);
        var quarterTurns = normalized / 90d;
        double cos;
        double sin;
        if (quarterTurns == Math.Truncate(quarterTurns))
        {
            (cos, sin) = ((int)quarterTurns % 4) switch
            {
                0 => (1d, 0d),
                1 => (0d, 1d),
                2 => (-1d, 0d),
                _ => (0d, -1d)
            };
        }
        else
        {
            var radians = normalized * (Math.PI / 180d);
            cos = Math.Cos(radians);
            sin = Math.Sin(radians);
        }

        return (
            X: (x * cos) - (y * sin),
            Y: (x * sin) + (y * cos));
    }

    private static (double X, double Y) GetRotatedBoundsTopLeft(double width, double height, double rotationDegrees)
    {
        var corners = new[]
        {
            Rotate(0, 0, rotationDegrees),
            Rotate(width, 0, rotationDegrees),
            Rotate(0, height, rotationDegrees),
            Rotate(width, height, rotationDegrees)
        };

        return (
            NormalizeNearZero(corners.Min(static corner => corner.X)),
            NormalizeNearZero(corners.Min(static corner => corner.Y)));
    }

    private static double NormalizeNearZero(double value) => Math.Abs(value) < 1e-10 ? 0 : value;

    private static void ValidateStructuralPlacementInputs(
        double parentRoomOriginX,
        double parentRoomOriginY,
        double parentRotationDegrees,
        double parentFootprintWidthPixels,
        double parentFootprintHeightPixels,
        double childOriginX,
        double childOriginY,
        double childRelativeRotationDegrees,
        double childFootprintWidthPixels,
        double childFootprintHeightPixels)
    {
        ValidateFinite(parentRoomOriginX, nameof(parentRoomOriginX));
        ValidateFinite(parentRoomOriginY, nameof(parentRoomOriginY));
        ValidateFinite(parentRotationDegrees, nameof(parentRotationDegrees));
        ValidateFinite(childOriginX, nameof(childOriginX));
        ValidateFinite(childOriginY, nameof(childOriginY));
        ValidateFinite(childRelativeRotationDegrees, nameof(childRelativeRotationDegrees));
        ValidateFootprintDimension(parentFootprintWidthPixels, nameof(parentFootprintWidthPixels));
        ValidateFootprintDimension(parentFootprintHeightPixels, nameof(parentFootprintHeightPixels));
        ValidateFootprintDimension(childFootprintWidthPixels, nameof(childFootprintWidthPixels));
        ValidateFootprintDimension(childFootprintHeightPixels, nameof(childFootprintHeightPixels));
    }

    private static void ValidateFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "A finite value is required.");
        }
    }

    private static void ValidateFootprintDimension(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "A finite non-negative footprint dimension is required.");
        }
    }

    private static void ValidateStructuralResult(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, "The resolved coordinate must remain finite.");
        }
    }

    private static (double X, double Y) GetRotatedFootprintMinimum(double width, double height, double rotationDegrees)
    {
        var topRight = Rotate(width, 0, rotationDegrees);
        var bottomRight = Rotate(width, height, rotationDegrees);
        var bottomLeft = Rotate(0, height, rotationDegrees);
        return (
            Math.Min(0, Math.Min(topRight.X, Math.Min(bottomRight.X, bottomLeft.X))),
            Math.Min(0, Math.Min(topRight.Y, Math.Min(bottomRight.Y, bottomLeft.Y))));
    }

}

/// <summary>
/// A physical footprint's room-space axis-aligned bounds origin and accumulated clockwise rotation.
/// </summary>
public readonly record struct StructuralFootprintPlacement
{
    /// <summary>
    /// Creates a resolved structural footprint placement.
    /// </summary>
    /// <param name="roomOriginX">AABB origin X in room pixels.</param>
    /// <param name="roomOriginY">AABB origin Y in room pixels.</param>
    /// <param name="absoluteRotationDegrees">Accumulated absolute physical angle in clockwise screen degrees.</param>
    public StructuralFootprintPlacement(double roomOriginX, double roomOriginY, double absoluteRotationDegrees)
    {
        RoomOriginX = roomOriginX;
        RoomOriginY = roomOriginY;
        AbsoluteRotationDegrees = absoluteRotationDegrees;
    }

    /// <summary>Gets the footprint AABB origin X in room pixels.</summary>
    public double RoomOriginX { get; }

    /// <summary>Gets the footprint AABB origin Y in room pixels.</summary>
    public double RoomOriginY { get; }

    /// <summary>Gets the accumulated absolute physical angle in clockwise screen degrees.</summary>
    public double AbsoluteRotationDegrees { get; }
}
