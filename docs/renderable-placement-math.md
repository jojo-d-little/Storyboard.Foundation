# Renderable placement math

Storyboard.Foundation.Spatial.RenderablePlacementMath converts a bitmap placement authored relative to an object's unrotated footprint into the offset a renderer applies to the room-rotated footprint origin. It contains no renderer or UI-framework types.

## Coordinate system

- Coordinates are measured in pixels, with the origin at the top-left and positive X rightward and positive Y downward.
- The footprint-local frame is the object's physical footprint before room rotation.
- A room rotation applies to both the physical footprint and the authored placement. Positive angles use the screen-space clockwise convention. The math uses x' = x cos θ - y sin θ and y' = x sin θ + y cos θ.
- A bitmap's local rotation is its appearance rotation. Its scaled full rectangular bounds are normalized to a top-left at (0, 0) after rotation; the authored local offset is then added in footprint-local axes.
- Returned positions are rebased into the top-left of the axis-aligned bounds of the rotated footprint. Add the returned offset to that footprint bounds origin to obtain the renderer bitmap origin.

## Inputs and calculation

ComputeIconLocalOffset takes room and local rotation in degrees; a post-local-rotation X/Y offset in footprint-local pixels; the unrotated footprint width and height; and the rendered bitmap width and height after authored base scaling. Bitmap dimensions include transparent pixels because placement is based on the complete image rectangle.

The calculation has two stages:

1. Rotate the bitmap rectangle around its origin by the local rotation. Find the minimum rotated X and Y among its four corners, then subtract those minima from the authored local offset. This gives the bitmap origin after the rotated rectangle has been normalized to its own top-left.
2. Rotate that normalized origin by the room rotation. Rotate all four footprint corners, find the footprint bounds minimum X and Y, then subtract those minima. The result is the position relative to the rotated footprint bounds top-left.

TransformLocalPointWithinRotatedFootprint performs stage two directly for a supplied footprint-local point. NormalizeDegrees maps finite angles into [0, 360) and maps non-finite angles to zero.

## Worked examples

### Room rotation

For a 100 × 60 footprint, a point at (20, 10) and a room rotation of 90° produce the rotated point (-10, 20). The rotated footprint corners span X from -60 to 0 and Y from 0 to 100. Rebasing by the bounds minimum gives (50, 20).

### Local bitmap rotation and offset

For a bitmap rendered at 40 × 20, local rotation 90° gives rotated bounds with minimum (-20, 0). With an authored offset of (5, 7), normalization gives a footprint-local bitmap origin of (25, 7). On the same 100 × 60 footprint at room rotation 90°, this becomes (-7, 25) before footprint rebasing, then (53, 25) relative to the rotated footprint bounds.

## Assumptions and edge cases

- Angles may be any finite number of degrees; negative values and values above 360 are normalized. Non-finite angles behave as zero rotation.
- A non-finite local offset passed to ComputeIconLocalOffset returns (0, 0).
- Invalid bitmap dimensions (non-finite, zero, or negative) skip bitmap-bounds normalization and treat the local bitmap origin as (0, 0) before applying room rotation.
- A non-finite or negative footprint dimension, or a non-finite input point, causes TransformLocalPointWithinRotatedFootprint to return the input point unchanged. ComputeIconLocalOffset uses that same fallback for invalid footprint dimensions.
- Floating-point residue with absolute magnitude below 1e-10 is normalized to zero to avoid tiny values at right-angle rotations.
- The calculation does not inspect bitmap alpha bounds, clip to the footprint, or apply scaling itself. Callers supply already-scaled full bitmap dimensions and handle clipping/render transforms separately.
