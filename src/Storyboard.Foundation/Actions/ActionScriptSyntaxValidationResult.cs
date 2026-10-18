namespace Storyboard.Foundation.Actions;

/// <summary>
/// Contains the outcome of action-script syntax validation.
/// </summary>
public sealed class ActionScriptSyntaxValidationResult
{
    /// <summary>Gets whether validation completed without errors.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets the validation errors, in encounter order.
    /// </summary>
    public List<string> Errors { get; } = new();
}
