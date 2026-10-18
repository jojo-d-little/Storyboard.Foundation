namespace Storyboard.Foundation.Config;

/// <summary>
/// Central policy for stack-scale defaults and clamping bounds.
/// Provides defaults, valid bounds, and normalization for stack-scale settings.
/// </summary>
public static class StackScalePolicy
{
    /// <summary>The default increase applied between successive stack layers.</summary>
    public const double DefaultStackScaleStep = 0.08d;
    /// <summary>The default minimum scale ratio for a stack layer.</summary>
    public const double DefaultMinStackScale = 0.50d;

    /// <summary>The smallest supported stack scale step.</summary>
    public const double MinStackScaleStep = 0.01d;
    /// <summary>The largest supported stack scale step.</summary>
    public const double MaxStackScaleStep = 0.20d;

    /// <summary>The smallest supported minimum stack scale ratio.</summary>
    public const double MinMinStackScale = 0.25d;
    /// <summary>The largest supported minimum stack scale ratio.</summary>
    public const double MaxMinStackScale = 1.00d;

    /// <summary>Checks whether a value is finite and within the supported stack-step range.</summary>
    /// <param name="value">The stack scale step to check.</param>
    /// <returns><see langword="true"/> when the value is within the inclusive bounds.</returns>
    public static bool IsStackScaleStepWithinBounds(double value)
    {
        return double.IsFinite(value)
            && value >= MinStackScaleStep
            && value <= MaxStackScaleStep;
    }

    /// <summary>Checks whether a value is finite and within the supported minimum-scale range.</summary>
    /// <param name="value">The minimum stack scale ratio to check.</param>
    /// <returns><see langword="true"/> when the value is within the inclusive bounds.</returns>
    public static bool IsMinStackScaleWithinBounds(double value)
    {
        return double.IsFinite(value)
            && value >= MinMinStackScale
            && value <= MaxMinStackScale;
    }

    /// <summary>Clamps a finite value to the supported stack-step range.</summary>
    /// <param name="value">The stack scale step to clamp.</param>
    /// <returns>The clamped value, or <see cref="DefaultStackScaleStep"/> if non-finite.</returns>
    public static double ClampStackScaleStep(double value)
    {
        if (!double.IsFinite(value))
        {
            return DefaultStackScaleStep;
        }

        return Math.Clamp(value, MinStackScaleStep, MaxStackScaleStep);
    }

    /// <summary>Clamps a finite value to the supported minimum-scale range.</summary>
    /// <param name="value">The minimum stack scale ratio to clamp.</param>
    /// <returns>The clamped value, or <see cref="DefaultMinStackScale"/> if non-finite.</returns>
    public static double ClampMinStackScale(double value)
    {
        if (!double.IsFinite(value))
        {
            return DefaultMinStackScale;
        }

        return Math.Clamp(value, MinMinStackScale, MaxMinStackScale);
    }

    /// <summary>Normalizes a stack scale step, using the default for non-finite or non-positive input.</summary>
    /// <param name="value">The configured stack scale step.</param>
    /// <returns>A value within the supported range.</returns>
    public static double NormalizeStackScaleStepOrDefault(double value)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            return DefaultStackScaleStep;
        }

        return ClampStackScaleStep(value);
    }

    /// <summary>Normalizes a minimum stack scale, using the default for non-finite or non-positive input.</summary>
    /// <param name="value">The configured minimum stack scale ratio.</param>
    /// <returns>A value within the supported range.</returns>
    public static double NormalizeMinStackScaleOrDefault(double value)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            return DefaultMinStackScale;
        }

        return ClampMinStackScale(value);
    }
}
