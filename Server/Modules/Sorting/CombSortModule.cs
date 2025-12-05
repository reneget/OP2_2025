namespace Server.Modules.Sorting;

/// <summary>
/// Модуль сортировки расчёсткой (Comb Sort)
/// </summary>
public class CombSortModule
{
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
}

