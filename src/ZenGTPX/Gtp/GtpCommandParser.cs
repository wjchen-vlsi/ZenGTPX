namespace ZenGTPX.Gtp;

public static class GtpCommandParser
{
    public static GtpCommand? Parse(string line)
    {
        line = line.TrimStart('\uFEFF');
        if (line.StartsWith("ï»¿", StringComparison.Ordinal))
        {
            line = line[3..];
        }

        while (line.Length > 0 && !IsGtpLineStart(line[0]))
        {
            line = line[1..];
        }

        var commentStart = line.IndexOf('#');
        if (commentStart >= 0)
        {
            line = line[..commentStart];
        }

        var tokens = line
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length == 0)
        {
            return null;
        }

        string? id = null;
        var commandIndex = 0;
        if (IsCommandId(tokens[0]))
        {
            id = tokens[0];
            commandIndex = 1;
        }

        if (commandIndex >= tokens.Length)
        {
            return null;
        }

        var name = tokens[commandIndex].ToLowerInvariant();
        var arguments = tokens[(commandIndex + 1)..];
        return new GtpCommand(id, name, arguments);
    }

    private static bool IsCommandId(string token)
    {
        foreach (var character in token)
        {
            if (!char.IsAsciiDigit(character))
            {
                return false;
            }
        }

        return token.Length > 0;
    }

    private static bool IsGtpLineStart(char character)
    {
        return char.IsAsciiLetterOrDigit(character) || character == '#';
    }
}
