using Storyboard.Shared.RuntimeContracts.Enums;

namespace Storyboard.Foundation.Spatial;

/// <summary>
/// Resolves authored room-image slot placement into the explicit room-pixel transform consumed by preview and export.
/// </summary>
public static class RoomImagePlacementMath
{
    private const double AnchorComponentThreshold = 0.0001d;

    /// <summary>
    /// Resolves a room image's logical slot anchor, normalized bitmap offset, draw origin, and transformed bitmap corners.
    /// </summary>
    /// <param name="slot">The existing room-image slot whose hosted placement is being preserved.</param>
    /// <param name="roomWidthPixels">The known effective room-image width in pixels.</param>
    /// <param name="roomHeightPixels">The known effective room-image height in pixels.</param>
    /// <param name="bitmapWidthPixels">The full native bitmap width in pixels, including transparent borders.</param>
    /// <param name="bitmapHeightPixels">The full native bitmap height in pixels, including transparent borders.</param>
    /// <param name="authoredOffsetX">The authored horizontal pixel adjustment from the slot placement.</param>
    /// <param name="authoredOffsetY">The authored vertical pixel adjustment from the slot placement.</param>
    /// <param name="rotationDegrees">The screen-space clockwise image rotation in degrees.</param>
    /// <returns>The six resolved placement coordinates and the four transformed native bitmap corners.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A slot is unknown, a dimension is not finite and positive, an authored offset is non-finite, or rotation is non-finite.</exception>
    /// <remarks>
    /// Dimensions must be resolved by the caller from the same image bytes used for preview/export. This method performs no file I/O
    /// and never substitutes room dimensions when bitmap dimensions are unavailable. Any finite legacy rotation is retained.
    /// </remarks>
    public static RoomImagePlacementResult Resolve(
        RoomImageSlot slot,
        double roomWidthPixels,
        double roomHeightPixels,
        double bitmapWidthPixels,
        double bitmapHeightPixels,
        double authoredOffsetX,
        double authoredOffsetY,
        double rotationDegrees)
    {
        if (!Enum.IsDefined(slot))
        {
            throw new ArgumentOutOfRangeException(nameof(slot), slot, "The room-image slot is not recognized.");
        }

        ValidateDimension(roomWidthPixels, nameof(roomWidthPixels));
        ValidateDimension(roomHeightPixels, nameof(roomHeightPixels));
        ValidateDimension(bitmapWidthPixels, nameof(bitmapWidthPixels));
        ValidateDimension(bitmapHeightPixels, nameof(bitmapHeightPixels));
        ValidateFinite(authoredOffsetX, nameof(authoredOffsetX));
        ValidateFinite(authoredOffsetY, nameof(authoredOffsetY));
        ValidateFinite(rotationDegrees, nameof(rotationDegrees));

        var (inwardX, inwardY) = GetSlotInwardVector(slot);
        var radians = NormalizeDegrees(rotationDegrees) * (Math.PI / 180d);
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);

        // This is the inverse rotation used by the current hosted texture-anchor calculation.
        var localInwardX = (inwardX * cos) + (inwardY * sin);
        var localInwardY = (-inwardX * sin) + (inwardY * cos);
        var bitmapAnchorX = ResolveBitmapAnchorComponent(localInwardX);
        var bitmapAnchorY = ResolveBitmapAnchorComponent(localInwardY);
        var anchorX = GetSlotAnchorX(slot, roomWidthPixels);
        var anchorY = GetSlotAnchorY(slot, roomHeightPixels);

        var rotatedAnchor = Rotate(bitmapAnchorX * bitmapWidthPixels, bitmapAnchorY * bitmapHeightPixels, radians);
        var iconOffsetX = NormalizeNearZero(authoredOffsetX - rotatedAnchor.X);
        var iconOffsetY = NormalizeNearZero(authoredOffsetY - rotatedAnchor.Y);
        var drawX = NormalizeNearZero(anchorX + iconOffsetX);
        var drawY = NormalizeNearZero(anchorY + iconOffsetY);

        var corners = new[]
        {
            TransformCorner(0, 0),
            TransformCorner(bitmapWidthPixels, 0),
            TransformCorner(bitmapWidthPixels, bitmapHeightPixels),
            TransformCorner(0, bitmapHeightPixels)
        };

        return new RoomImagePlacementResult(anchorX, anchorY, iconOffsetX, iconOffsetY, drawX, drawY, Array.AsReadOnly(corners));

        (double X, double Y) TransformCorner(double x, double y)
        {
            var rotated = Rotate(x, y, radians);
            return (NormalizeNearZero(drawX + rotated.X), NormalizeNearZero(drawY + rotated.Y));
        }
    }

    private static (double X, double Y) GetSlotInwardVector(RoomImageSlot slot) => slot switch
    {
        RoomImageSlot.Down or RoomImageSlot.NorthWest => (1, 1),
        RoomImageSlot.North => (0, 1),
        RoomImageSlot.NorthEast => (-1, 1),
        RoomImageSlot.East => (-1, 0),
        RoomImageSlot.SouthEast => (-1, -1),
        RoomImageSlot.South => (0, -1),
        RoomImageSlot.SouthWest => (1, -1),
        RoomImageSlot.West => (1, 0),
        _ => (0, 0)
    };

    private static double GetSlotAnchorX(RoomImageSlot slot, double roomWidthPixels) => slot switch
    {
        RoomImageSlot.Down or RoomImageSlot.West or RoomImageSlot.SouthWest or RoomImageSlot.NorthWest => 0,
        RoomImageSlot.NorthEast or RoomImageSlot.East or RoomImageSlot.SouthEast => roomWidthPixels,
        _ => roomWidthPixels / 2d
    };

    private static double GetSlotAnchorY(RoomImageSlot slot, double roomHeightPixels) => slot switch
    {
        RoomImageSlot.Down or RoomImageSlot.North or RoomImageSlot.NorthEast or RoomImageSlot.NorthWest => 0,
        RoomImageSlot.SouthEast or RoomImageSlot.South or RoomImageSlot.SouthWest => roomHeightPixels,
        _ => roomHeightPixels / 2d
    };

    private static double ResolveBitmapAnchorComponent(double component)
    {
        if (Math.Abs(component) <= AnchorComponentThreshold)
        {
            return 0.5d;
        }

        return component > 0 ? 0d : 1d;
    }

    private static double NormalizeDegrees(double degrees)
    {
        var normalized = degrees % 360d;
        return normalized < 0 ? normalized + 360d : normalized;
    }

    private static (double X, double Y) Rotate(double x, double y, double radians)
    {
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        return (x * cos - y * sin, x * sin + y * cos);
    }

    private static void ValidateDimension(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "A known finite positive pixel dimension is required.");
        }
    }

    private static void ValidateFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "A finite value is required.");
        }
    }

    private static double NormalizeNearZero(double value) => Math.Abs(value) < 1e-10 ? 0d : value;
}

/// <summary>
/// The resolved room-pixel placement and transformed full-bitmap corners for one room image.
/// </summary>
/// <param name="AnchorX">Logical slot anchor X in room pixels.</param>
/// <param name="AnchorY">Logical slot anchor Y in room pixels.</param>
/// <param name="IconOffsetX">Complete normalized artwork offset X from the logical slot anchor.</param>
/// <param name="IconOffsetY">Complete normalized artwork offset Y from the logical slot anchor.</param>
/// <param name="X">Bitmap draw origin X, equal to AnchorX plus IconOffsetX.</param>
/// <param name="Y">Bitmap draw origin Y, equal to AnchorY plus IconOffsetY.</param>
/// <param name="Corners">Full native bitmap corners transformed into room pixels in top-left, top-right, bottom-right, bottom-left order.</param>
public sealed record RoomImagePlacementResult(
    double AnchorX,
    double AnchorY,
    double IconOffsetX,
    double IconOffsetY,
    double X,
    double Y,
    IReadOnlyList<(double X, double Y)> Corners);
