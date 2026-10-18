using Storyboard.Shared.RuntimeContracts.Enums;

namespace Storyboard.Foundation.Directions;

/// <summary>
/// Formats and parses shared <see cref="Direction10"/> values.
/// </summary>
public static class Direction10Formatting
{
    /// <summary>Returns whether a direction names a usable traversal direction rather than <see cref="Direction10.Unknown"/>.</summary>
    /// <param name="direction">The direction to check.</param>
    /// <returns><see langword="true"/> for one of the ten supported directions.</returns>
    public static bool IsUsable(Direction10 direction) => direction is
        Direction10.North or Direction10.NorthEast or Direction10.East or Direction10.SouthEast
        or Direction10.South or Direction10.SouthWest or Direction10.West or Direction10.NorthWest
        or Direction10.Up or Direction10.Down;

    /// <summary>
    /// Formats a command direction as the canonical token expected by movement gateways.
    /// </summary>
    /// <param name="direction">The direction to format.</param>
    /// <returns>A canonical uppercase token, or an empty string for an unsupported value.</returns>
    public static string ToCanonicalToken(this Direction10 direction)
    {
        return direction switch
        {
            Direction10.North => "N",
            Direction10.NorthEast => "NE",
            Direction10.East => "E",
            Direction10.SouthEast => "SE",
            Direction10.South => "S",
            Direction10.SouthWest => "SW",
            Direction10.West => "W",
            Direction10.NorthWest => "NW",
            Direction10.Up => "UP",
            Direction10.Down => "DOWN",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Formats a direction as its lowercase token.
    /// </summary>
    /// <param name="direction">The direction to format.</param>
    /// <returns>A lowercase token, or an empty string for an unsupported value.</returns>
    public static string ToToken(this Direction10 direction)
    {
        return direction switch
        {
            Direction10.North => "north",
            Direction10.NorthEast => "northeast",
            Direction10.East => "east",
            Direction10.SouthEast => "southeast",
            Direction10.South => "south",
            Direction10.SouthWest => "southwest",
            Direction10.West => "west",
            Direction10.NorthWest => "northwest",
            Direction10.Up => "up",
            Direction10.Down => "down",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Formats a direction as a display label.
    /// </summary>
    /// <param name="direction">The direction to format.</param>
    /// <returns>A title-cased label, or <c>Unknown</c> for an unsupported value.</returns>
    public static string ToDisplayLabel(this Direction10 direction)
    {
        return direction switch
        {
            Direction10.North => "North",
            Direction10.NorthEast => "North East",
            Direction10.East => "East",
            Direction10.SouthEast => "South East",
            Direction10.South => "South",
            Direction10.SouthWest => "South West",
            Direction10.West => "West",
            Direction10.NorthWest => "North West",
            Direction10.Up => "Up",
            Direction10.Down => "Down",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Parses a direction name or accepted abbreviation, ignoring surrounding whitespace and case.
    /// </summary>
    /// <param name="token">The text to parse.</param>
    /// <param name="direction">Receives the parsed direction, or <see cref="Direction10.Unknown"/> on failure.</param>
    /// <returns><see langword="true"/> if the token is recognized.</returns>
    public static bool TryParseToken(string? token, out Direction10 direction)
    {
        var normalized = token?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            direction = Direction10.Unknown;
            return false;
        }

        switch (normalized.ToLowerInvariant())
        {
            case "north":
            case "n":
                direction = Direction10.North;
                return true;
            case "northeast":
            case "ne":
                direction = Direction10.NorthEast;
                return true;
            case "east":
            case "e":
            case "right":
                direction = Direction10.East;
                return true;
            case "southeast":
            case "se":
                direction = Direction10.SouthEast;
                return true;
            case "south":
            case "s":
                direction = Direction10.South;
                return true;
            case "southwest":
            case "sw":
                direction = Direction10.SouthWest;
                return true;
            case "west":
            case "w":
            case "left":
                direction = Direction10.West;
                return true;
            case "northwest":
            case "nw":
                direction = Direction10.NorthWest;
                return true;
            case "up":
            case "u":
                direction = Direction10.Up;
                return true;
            case "down":
            case "d":
                direction = Direction10.Down;
                return true;
            default:
                direction = Direction10.Unknown;
                return false;
        }
    }
}

