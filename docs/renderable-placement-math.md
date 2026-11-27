# Renderable placement math

Storyboard.Foundation.Spatial.RenderablePlacementMath converts a bitmap placement authored relative to an object's unrotated footprint into the offset a renderer applies to the room-rotated footprint origin. It contains no renderer or UI-framework types.

It also composes a child physical footprint into its parent's rigidly rotated room placement and resolves the inverse movement transform. These methods operate on footprint AABB origins and physical angles; bitmap fitting remains a separate calculation.

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

## Parent and child structural placement

`ComposeChildFootprintPlacement` accepts the parent room-space AABB origin, its accumulated absolute physical angle and unrotated footprint dimensions, followed by the child's parent-local AABB origin after its relative turn, relative physical angle and unrotated footprint dimensions. It returns `StructuralFootprintPlacement`, containing the child room-space AABB origin and accumulated absolute physical angle. The local origin is in the parent's unrotated footprint frame; the room origin is in room pixels.

`ResolveChildFootprintLocalOrigin` is the inverse. Give it the same parent geometry and the child's room-space AABB origin, relative angle and footprint dimensions. It returns the child AABB origin in the parent's unrotated footprint frame. It inverts footprint placement only: do not pass a bitmap draw origin. This pair does not resolve parent inheritance, read state, convert cells, apply base-image scale, situational scale, or presentation cues.

Let `P` be the parent's room AABB origin, `theta` its absolute angle, `Fp/Fc` the parent/child unrotated footprint rectangles, `L` the child's parent-local AABB origin after its relative turn `phi`, `b(F,a)` the minimum corner of footprint `F` rotated by `a`, and `R(a)` screen-space clockwise rotation. Then:

```text
childAngle = theta + phi
childRoomOrigin = P + R(theta) × (L - b(Fc, phi)) - b(Fp, theta) + b(Fc, childAngle)

L = R(-theta) × (childRoomOrigin - P + b(Fp, theta) - b(Fc, childAngle)) + b(Fc, phi)
```

This is equivalent to attaching the child to the parent's unrotated footprint and rotating the assembled shapes together. For nested parts, compose each edge using the resolved parent AABB origin, absolute physical angle and original parent footprint size.

All values are pixels in the same already-resolved geometry space. Footprint dimensions must be finite and non-negative; coordinates and angles must be finite, and the accumulated angle must remain finite. The new methods throw `ArgumentOutOfRangeException` for invalid inputs or a non-finite calculated result. Finite angles are accumulated as supplied and normalized modulo 360 only for rotation; equivalent angles produce equivalent geometry. Exact quarter turns use exact cardinal sine/cosine values, preventing grid-boundary drift without rounding fractional or non-cardinal coordinates. Existing object helper fallback behavior for invalid bitmap dimensions, offsets and angles is unchanged.

Foundation applies no scale policy. Current Designer treatment multiplies parent-relative offsets by the parent base-image scale, while Engine offsets remain room pixels. The confirmed fixture uses unit scale; this API does not establish non-unit preview/runtime parity or change either consumer's existing policy.

Keep physical angle separate from bitmap-local fit. For a child, first use `ComputeIconLocalOffset(phi, imageLocalAngle, ...)` to fit the bitmap inside the parent-local assembly, then compose the complete assembly. The equivalent room-space bitmap fit uses the child's accumulated `childAngle`; do not feed image-local rotation, parent base-image scale or situational scale into physical footprint composition.

### Confirmed Elevator fixture

For parent AABB origin `(80,160)`, absolute angle `270°`, footprint `400 × 400`, and child footprint `80 × 80` with relative angle `0°`:

| Child | Parent-local AABB origin | Composed room AABB origin |
| --- | --- | --- |
| Left closed | `(120,320)` | `(400,360)` |
| Right closed | `(200,320)` | `(400,280)` |
| Left after 40 px west move | `(120,280)` | `(360,360)` |

The existing child bitmap fits remain separate. With local fit `(11,-2)`, local image angle `180°` and native image size `67 × 82`, the left bitmap origin is `(480,362)`. The right fit `(1,-2)` with `65 × 82` yields `(480,294)`. Inverting the moved left child room AABB origin returns parent-local `(120,280)`.
