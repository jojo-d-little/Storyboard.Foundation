# Storyboard.Foundation

Storyboard.Foundation contains small, framework-independent utilities shared by Storyboard products.

The package contains:

- Storyboard.Foundation.Paths: strict resolution for %ENVIRONMENT_VARIABLE% path expressions and deterministic asset-root containment helpers.
- Storyboard.Foundation.Config.StackScalePolicy: shared stack scale defaults and bounds.
- Storyboard.Foundation.Spatial.RenderablePlacementMath: renderer-independent footprint and bitmap placement calculations. See the [placement design note](docs/renderable-placement-math.md).
- Storyboard.Foundation.Directions.Direction10Formatting: formatting for the Direction10 contract enum.
- Storyboard.Foundation.Actions.ActionScriptSyntaxValidator and ActionScriptSyntaxValidationResult: action script syntax and reference validation.
- Storyboard.Foundation.Serialization.StoryboardJsonSerializerOptions: the shared JSON serializer configuration.

Foundation does not depend on GameEngine, Designer, or UI frameworks. The direction formatter uses the Storyboard.Contracts package for its Direction10 type.

The package includes XML API documentation and the placement design note.
