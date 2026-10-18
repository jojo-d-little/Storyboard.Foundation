using System.Text.Json;
using System.Text.Json.Serialization;

namespace Storyboard.Foundation.Serialization;

/// <summary>Creates the shared JSON serialization settings used by Storyboard products.</summary>
public static class StoryboardJsonSerializerOptions
{
    /// <summary>Creates a fresh set of shared JSON options.</summary>
    /// <param name="writeIndented">Whether JSON output should be indented.</param>
    /// <param name="propertyNameCaseInsensitive">Whether JSON property matching should ignore case.</param>
    /// <returns>Options using camel-case property names, omitting null values, and serializing enums by name.</returns>
    public static JsonSerializerOptions Create(bool writeIndented = true, bool propertyNameCaseInsensitive = false)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = propertyNameCaseInsensitive,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = writeIndented
        };

        // Enforce enum names in JSON to keep contracts resilient to enum member reordering.
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
