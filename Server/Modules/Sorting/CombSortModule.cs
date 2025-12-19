namespace Server.Modules.Sorting;

/// <summary>
/// Результат сортировки с метаданными
/// </summary>
public class SortResult
{
    public int[] SortedArray { get; set; } = new int[0];
    public int InitialGap { get; set; }
    public long ExecutionTimeMs { get; set; }
    public DateTime CompletionTime { get; set; }
}

/// <summary>
/// Модуль сортировки расчёсткой (Comb Sort)
/// </summary>
public class CombSortModule
{
    // Коэффициент сжатия для алгоритма расчёстки
    private const double ShrinkFactor = 1.3;

    /// <summary>
    /// Сортирует массив чисел методом расчёстки
    /// </summary>
    /// <param name="array">Массив для сортировки</param>
    /// <returns>Отсортированный массив</returns>
    public int[] Sort(int[] array)
    {
        if (array == null || array.Length == 0)
            return new int[0];
        
        if (array.Length <= 1)
            return (int[])array.Clone();

        var sortedArray = (int[])array.Clone();
        int gap = sortedArray.Length;
        bool swapped = true;

        while (gap > 1 || swapped)
        {
            // Уменьшаем gap на каждом проходе
            gap = Math.Max(1, (int)(gap / ShrinkFactor));
            swapped = false;

            for (int i = 0; i + gap < sortedArray.Length; i++)
            {
                if (sortedArray[i] > sortedArray[i + gap])
                {
                    (sortedArray[i], sortedArray[i + gap]) = (sortedArray[i + gap], sortedArray[i]);
                    swapped = true;
                }
            }
        }

        return sortedArray;
    }

    /// <summary>
    /// Сортирует массив с указанием направления
    /// </summary>
    /// <param name="array">Массив для сортировки</param>
    /// <param name="ascending">true для сортировки по возрастанию, false для убывания</param>
    /// <returns>Отсортированный массив</returns>
    public int[] Sort(int[] array, bool ascending)
    {
        var sorted = Sort(array);
        if (!ascending)
        {
            Array.Reverse(sorted);
        }
        return sorted;
    }

    /// <summary>
    /// Сортирует массив с возвратом метаданных (шаг отбрасывания, время выполнения)
    /// </summary>
    /// <param name="array">Массив для сортировки</param>
    /// <param name="ascending">true для сортировки по возрастанию, false для убывания</param>
    /// <param name="customGap">Пользовательский шаг отбрасывания (опционально)</param>
    /// <returns>Результат сортировки с метаданными</returns>
    public SortResult SortWithMetadata(int[] array, bool ascending = true, int? customGap = null)
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        if (array == null || array.Length == 0)
        {
            return new SortResult
            {
                SortedArray = new int[0],
                InitialGap = 0,
                ExecutionTimeMs = 0,
                CompletionTime = DateTime.UtcNow
            };
        }
        
        if (array.Length <= 1)
        {
            return new SortResult
            {
                SortedArray = (int[])array.Clone(),
                InitialGap = array.Length,
                ExecutionTimeMs = 0,
                CompletionTime = DateTime.UtcNow
            };
        }

        var sortedArray = (int[])array.Clone();
        
        // Определяем начальный шаг отбрасывания
        int initialGap;
        if (customGap.HasValue && customGap.Value > 0 && customGap.Value <= sortedArray.Length)
        {
            initialGap = customGap.Value;
        }
        else
        {
            // Оптимальный шаг на основе размера массива
            initialGap = (int)(sortedArray.Length / ShrinkFactor);
            if (initialGap < 1) initialGap = sortedArray.Length;
        }

        int gap = initialGap;
        bool swapped = true;

        while (gap > 1 || swapped)
        {
            // Уменьшаем gap на каждом проходе
            gap = Math.Max(1, (int)(gap / ShrinkFactor));
            swapped = false;

            for (int i = 0; i + gap < sortedArray.Length; i++)
            {
                if (ascending ? sortedArray[i] > sortedArray[i + gap] : sortedArray[i] < sortedArray[i + gap])
                {
                    (sortedArray[i], sortedArray[i + gap]) = (sortedArray[i + gap], sortedArray[i]);
                    swapped = true;
                }
            }
        }

        stopwatch.Stop();
        var completionTime = DateTime.UtcNow;

        return new SortResult
        {
            SortedArray = sortedArray,
            InitialGap = initialGap,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
            CompletionTime = completionTime
        };
    }
}

