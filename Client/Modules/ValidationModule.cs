namespace Client.Modules;

/// <summary>
/// Модуль валидации входных данных
/// </summary>
public static class ValidationModule
{
    /// <summary>
    /// Валидирует строку с массивом чисел
    /// </summary>
    /// <param name="input">Строка с числами, разделенными пробелами или запятыми</param>
    /// <returns>Массив целых чисел или null, если валидация не прошла</returns>
    public static int[]? ValidateArray(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        // Разделяем по пробелам и запятым
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

            // Удаляем квадратные скобки, если есть
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

    /// <summary>
    /// Валидирует путь к файлу
    /// </summary>
    /// <param name="filePath">Путь к файлу</param>
    /// <returns>true, если путь валиден и файл существует</returns>
    public static bool ValidateFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        try
        {
            // Проверяем, что путь абсолютный
            if (!Path.IsPathRooted(filePath))
            {
                return false;
            }

            // Проверяем существование файла
            return File.Exists(filePath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Валидирует формат данных в файле
    /// </summary>
    /// <param name="filePath">Путь к файлу</param>
    /// <returns>true, если формат валиден</returns>
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

