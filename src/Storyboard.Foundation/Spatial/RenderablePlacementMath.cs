namespace Storyboard.Foundation.Spatial;

/// <summary>
/// Renderer-agnostic placement math for footprint-relative object icon placement.
/// This helper is free of UI-framework types so hosts can share one deterministic transform pipeline.
/// </summary>
public static class RenderablePlacementMath
{
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
        var radians = NormalizeDegrees(rotationDegrees) * (Math.PI / 180d);
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
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
}
