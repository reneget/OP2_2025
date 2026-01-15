namespace Client.Modules;

public static class ValidationModule
{
    public static int[]? ValidateArray(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var parts = input.Split([' ', ',', ';', '\t'], StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length == 0)
            return null;

        var numbers = new List<int>();
        
        foreach (var part in parts)
        {
            var trimmed = part.Trim().Trim('[', ']');
            
            if (int.TryParse(trimmed, out var number))
            {
                numbers.Add(number);
            }
        }

        return numbers.Count == 0 ? null : numbers.ToArray();
    }

    public static bool ValidateFilePath(string? filePath)
    {
        return !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath);
    }
}