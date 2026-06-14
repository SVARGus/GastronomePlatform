using System.Text.RegularExpressions;

namespace GastronomePlatform.Modules.Dishes.Application.Helpers
{
    /// <summary>
    /// Нормализует имена тегов перед поиском и созданием в справочнике
    /// (UC-DSH-008 SetTags). Преобразование: <c>Trim</c>, приведение к нижнему
    /// регистру, схлопывание подряд идущих пробельных символов в один пробел.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Назначение — дедупликация семантически одинаковых вариантов написания:
    /// «Без глютена», «без глютена», «без  глютена  » → «без глютена».
    /// Хранится в <c>Tag.NormalizedName</c> с уникальным индексом.
    /// </para>
    /// <para>
    /// Транслитерация (например, «вегетарианский» ↔ «vegetarianskiy») намеренно
    /// не выполняется — на Этапе 2 разные алфавиты считаются разными тегами.
    /// Слияние эквивалентных тегов — отдельный admin-UC `UC-DSH-132 MergeTags` (Этап 8+).
    /// </para>
    /// </remarks>
    public static class TagNameNormalizer
    {
        // Подряд идущие пробельные символы (включая табуляции и неразрывные пробелы)
        // заменяются на один обычный пробел.
        private static readonly Regex _whitespaceRegex = new(@"\s+", RegexOptions.Compiled);

        /// <summary>
        /// Возвращает нормализованную форму имени тега.
        /// </summary>
        /// <param name="name">Имя тега, как ввёл пользователь.</param>
        /// <returns>
        /// Trim + lower + collapse-whitespace представление; пустая строка для
        /// <see langword="null"/> или входа, состоящего только из пробельных символов.
        /// </returns>
        public static string Normalize(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            string trimmed = name.Trim();
            string collapsed = _whitespaceRegex.Replace(trimmed, " ");
            return collapsed.ToLowerInvariant();
        }
    }
}
