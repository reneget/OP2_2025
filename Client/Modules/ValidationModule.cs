namespace Client.Modules;

public static class ValidationModule
{
    public static int[]? ValidateArray(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var parts = input.Split(new[] { ' ', ',', ';', '\t', '\n', '\r' }, 
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return null;
        }

        var numbers = new List<int>();
        
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            trimmed = trimmed.Trim('[', ']');

            if (int.TryParse(trimmed, out var number))
            {
                numbers.Add(number);
            }
            else
            {
                Console.WriteLine($"⚠️  Предупреждение: '{trimmed}' не является целым числом и будет пропущено.");
            }
        }

        if (numbers.Count == 0)
        {
            return null;
        }

        return numbers.ToArray();
    }

    public static bool ValidateFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        try
        {
            if (!Path.IsPathRooted(filePath))
            {
                return false;
            }

            return File.Exists(filePath);
        }
        catch
        {
            return false;
        }
    }

    public static bool ValidateFileFormat(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            var array = ValidateArray(content);
            return array != null && array.Length > 0;
        }
        catch
        {
            return false;
        }
    }
}

