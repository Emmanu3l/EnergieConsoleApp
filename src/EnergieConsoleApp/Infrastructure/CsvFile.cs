namespace EnergieConsoleApp.Infrastructure;

// The assessment's CSV format is deliberately restricted: semicolons, no quoting.
internal static class CsvFile
{
    public static IEnumerable<T> Read<T>(string path, string header, Func<string[], T> parse)
    {
        using var reader = new StreamReader(path);
        if (reader.ReadLine() != header)
            throw new FormatException($"{path}, line 1: expected header '{header}'.");

        int columnCount = header.Split(';').Length;
        int lineNumber = 1;
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            T value;
            try
            {
                if (line.Contains('"'))
                    throw new FormatException("Quoted fields are not supported.");
                var columns = line.Split(';').Select(value => value.Trim()).ToArray();
                if (columns.Length != columnCount)
                    throw new FormatException($"Expected {columnCount} columns, found {columns.Length}.");
                value = parse(columns);
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                throw new FormatException($"{path}, line {lineNumber}: {ex.Message}", ex);
            }
            yield return value;
        }
    }

    public static string Required(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new FormatException($"{field} is required.");
        return value;
    }

    public static string Choice(string value, string field, params string[] choices)
    {
        if (!choices.Contains(value, StringComparer.OrdinalIgnoreCase))
            throw new FormatException($"{field} must be one of: {string.Join(", ", choices)}.");
        return value;
    }
}
