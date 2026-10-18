namespace Storyboard.Foundation.Actions;

/// <summary>
/// Validates the structure and references used by Storyboard action scripts.
/// </summary>
public static class ActionScriptSyntaxValidator
{
    private static readonly HashSet<string> ReservedWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "if",
        "else",
        "then",
        "endif",
        "set",
        "echo",
        "is",
        "true",
        "false",
        "equals",
        "not",
        "less",
        "than",
        "greater",
        "or",
        "equal"
    };

    /// <summary>
    /// Validates IF/ELSE block structure and references used in a script.
    /// </summary>
    /// <param name="scriptText">The script text to validate.</param>
    /// <param name="inScopeReferences">Reference tokens available to the script; comparison is case-insensitive.</param>
    /// <returns>A result containing any validation errors.</returns>
    public static ActionScriptSyntaxValidationResult Validate(string scriptText, IEnumerable<string>? inScopeReferences = null)
    {
        var result = new ActionScriptSyntaxValidationResult();
        var stack = new Stack<IfBlockState>();
        var referenceSet = BuildReferenceSet(inScopeReferences);

        var lines = scriptText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (IsIf(line))
            {
                stack.Push(new IfBlockState { StartLineNumber = lineNumber });
            }
            else if (IsElseIf(line))
            {
                if (stack.Count == 0)
                {
                    result.Errors.Add($"Line {lineNumber}: ELSE IF found without a matching IF.");
                }
                else if (stack.Peek().HasElse)
                {
                    result.Errors.Add($"Line {lineNumber}: ELSE IF cannot appear after ELSE in the same IF block.");
                }
            }
            else if (IsElse(line))
            {
                if (stack.Count == 0)
                {
                    result.Errors.Add($"Line {lineNumber}: ELSE found without a matching IF.");
                }
                else
                {
                    var current = stack.Peek();
                    if (current.HasElse)
                    {
                        result.Errors.Add($"Line {lineNumber}: Duplicate ELSE in the same IF block.");
                    }
                    else
                    {
                        current.HasElse = true;
                    }
                }
            }
            else if (IsEnd(line))
            {
                result.Errors.Add($"Line {lineNumber}: Use ENDIF to close IF/ELSE blocks.");
            }
            else if (IsEndIf(line))
            {
                if (stack.Count == 0)
                {
                    result.Errors.Add($"Line {lineNumber}: ENDIF found without a matching IF.");
                }
                else
                {
                    stack.Pop();
                }
            }

            ValidateLineReferences(line, lineNumber, referenceSet, result);
        }

        while (stack.Count > 0)
        {
            var unclosed = stack.Pop();
            result.Errors.Add($"Line {unclosed.StartLineNumber}: IF block is missing ENDIF.");
        }

        return result;
    }

    /// <summary>
    /// Collects distinct reference tokens used by script expressions and placeholders.
    /// </summary>
    /// <param name="scriptText">Script text to inspect; <see langword="null"/> is treated as empty text.</param>
    /// <returns>Distinct tokens in first-seen order.</returns>
    public static IReadOnlyList<string> CollectReferenceTokens(string? scriptText)
    {
        var tokens = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var script = scriptText ?? string.Empty;
        var lines = script.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var rawLine = lines[i] ?? string.Empty;
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(rawLine, "\\{([^{}]+)\\}"))
            {
                AddToken(match.Groups[1].Value, tokens, seen);
            }

            var leadingKeyword = line.TrimStart();
            if (!leadingKeyword.StartsWith("IF", StringComparison.OrdinalIgnoreCase)
                && !leadingKeyword.StartsWith("ELSE IF", StringComparison.OrdinalIgnoreCase)
                && !leadingKeyword.StartsWith("SET", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var scrubbed = System.Text.RegularExpressions.Regex.Replace(line, "\"[^\"]*\"", " ");
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(scrubbed, "(?<!@)\\b[A-Za-z_][A-Za-z0-9_]*(?:[\\.,][A-Za-z_][A-Za-z0-9_]*)?\\b"))
            {
                var token = match.Value.Trim();
                if (ReservedWords.Contains(token))
                {
                    continue;
                }

                AddToken(token, tokens, seen);
            }
        }

        return tokens;
    }

    private static HashSet<string> BuildReferenceSet(IEnumerable<string>? inScopeReferences)
    {
        if (inScopeReferences is null)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return inScopeReferences
            .Where(static reference => !string.IsNullOrWhiteSpace(reference))
            .Select(static reference => reference.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static void ValidateLineReferences(string line, int lineNumber, HashSet<string> references, ActionScriptSyntaxValidationResult result)
    {
        if (line.Contains("{{", StringComparison.Ordinal) || line.Contains("}}", StringComparison.Ordinal))
        {
            result.Errors.Add($"Line {lineNumber}: Double braces '{{' or '}}' are not allowed.");
        }

        // {variableRef} usage in any script line.
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(line, "\\{([^{}]+)\\}"))
        {
            var token = match.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            if (!references.Contains(token))
            {
                result.Errors.Add($"Line {lineNumber}: Unknown reference '{{{token}}}' (not found in scope).");
            }
        }

        // Bare references in IF/ELSE IF/SET lines, excluding function names and reserved words.
        var leadingKeyword = line.TrimStart();
        if (!leadingKeyword.StartsWith("IF", StringComparison.OrdinalIgnoreCase)
            && !leadingKeyword.StartsWith("ELSE IF", StringComparison.OrdinalIgnoreCase)
            && !leadingKeyword.StartsWith("SET", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var scrubbed = System.Text.RegularExpressions.Regex.Replace(line, "\"[^\"]*\"", " ");
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(scrubbed, "(?<!@)\\b[A-Za-z_][A-Za-z0-9_]*(?:[\\.,][A-Za-z_][A-Za-z0-9_]*)?\\b"))
        {
            var token = match.Value;
            if (ReservedWords.Contains(token))
            {
                continue;
            }

            if (!references.Contains(token))
            {
                result.Errors.Add($"Line {lineNumber}: Unknown reference '{token}' (not found in scope).");
            }
        }
    }

    private static bool IsIf(string line)
    {
        return MatchesSingleKeyword(line, "IF");
    }

    private static bool IsElseIf(string line)
    {
        return MatchesKeywordPair(line, "ELSE", "IF")
               || MatchesSingleKeyword(line, "ELSEIF");
    }

    private static bool IsElse(string line)
    {
        return MatchesSingleKeyword(line, "ELSE");
    }

    private static bool IsEndIf(string line)
    {
        return MatchesSingleKeyword(line, "ENDIF")
               || MatchesKeywordPair(line, "END", "IF");
    }

    private static bool IsEnd(string line)
    {
        return MatchesSingleKeyword(line, "END");
    }

    private static bool MatchesSingleKeyword(string line, string keyword)
    {
        if (!line.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (line.Length == keyword.Length)
        {
            return true;
        }

        return !IsIdentifierChar(line[keyword.Length]);
    }

    private static bool MatchesKeywordPair(string line, string first, string second)
    {
        if (!line.StartsWith(first, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var index = first.Length;
        if (index >= line.Length || !char.IsWhiteSpace(line[index]))
        {
            return false;
        }

        while (index < line.Length && char.IsWhiteSpace(line[index]))
        {
            index++;
        }

        if (index >= line.Length)
        {
            return false;
        }

        if (!line.AsSpan(index).StartsWith(second, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        index += second.Length;
        return index == line.Length || !IsIdentifierChar(line[index]);
    }

    private static bool IsIdentifierChar(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }

    private static void AddToken(string? candidate, ICollection<string> tokens, ISet<string> seen)
    {
        var normalized = candidate?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized)
            || !seen.Add(normalized))
        {
            return;
        }

        tokens.Add(normalized);
    }

    private sealed class IfBlockState
    {
        public int StartLineNumber { get; init; }

        public bool HasElse { get; set; }
    }
}

