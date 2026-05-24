using System.Text;
using System.Text.RegularExpressions;
using GastronomePlatform.Common.Application.Abstractions;

namespace GastronomePlatform.Common.Infrastructure.Services
{
    /// <summary>
    /// Реализация <see cref="ISlugGenerator"/> для Этапа 2: ASCII-only.
    /// </summary>
    /// <remarks>
    /// Алгоритм:
    /// <list type="number">
    ///   <item>Приведение к нижнему регистру (<see cref="string.ToLowerInvariant"/>).</item>
    ///   <item>Посимвольный транслит кириллицы в латиницу по таблице (упрощённый ГОСТ).</item>
    ///   <item>Замена всех символов вне <c>a-z 0-9</c> на дефисы.</item>
    ///   <item>Схлопывание последовательных дефисов в один (<c>Regex "-+"</c>).</item>
    ///   <item>Обрезка дефисов с краёв.</item>
    ///   <item>Ограничение длины в 200 символов (запас под суффикс <c>-N</c>; БД-лимит 220).</item>
    /// </list>
    /// Мультиязычные slug-и (Этап 8+) — см. TODO 2.15 в <c>private_TODO-будущие-этапы.md</c>.
    /// </remarks>
    public sealed class SlugGenerator : ISlugGenerator
    {
        private const int MAX_LENGTH = 200;

        private static readonly IReadOnlyDictionary<char, string> _translitMap = new Dictionary<char, string>
        {
            { 'а', "a" }, { 'б', "b" }, { 'в', "v" }, { 'г', "g" }, { 'д', "d" },
            { 'е', "e" }, { 'ё', "yo" }, { 'ж', "zh" }, { 'з', "z" }, { 'и', "i" },
            { 'й', "y" }, { 'к', "k" }, { 'л', "l" }, { 'м', "m" }, { 'н', "n" },
            { 'о', "o" }, { 'п', "p" }, { 'р', "r" }, { 'с', "s" }, { 'т', "t" },
            { 'у', "u" }, { 'ф', "f" }, { 'х', "h" }, { 'ц', "ts" }, { 'ч', "ch" },
            { 'ш', "sh" }, { 'щ', "sch" }, { 'ъ', "" }, { 'ы', "y" }, { 'ь', "" },
            { 'э', "e" }, { 'ю', "yu" }, { 'я', "ya" }
        };

        private static readonly Regex _consecutiveDashes = new("-+", RegexOptions.Compiled);

        /// <inheritdoc/>
        public string Generate(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return string.Empty;
            }

            var lowered = source.ToLowerInvariant();
            var builder = new StringBuilder(lowered.Length);

            foreach (var ch in lowered)
            {
                if (_translitMap.TryGetValue(ch, out var replacement))
                {
                    builder.Append(replacement);
                }
                else if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
                {
                    builder.Append(ch);
                }
                else
                {
                    builder.Append('-');
                }
            }

            var collapsed = _consecutiveDashes.Replace(builder.ToString(), "-");
            var trimmed = collapsed.Trim('-');

            if (trimmed.Length > MAX_LENGTH)
            {
                trimmed = trimmed[..MAX_LENGTH].TrimEnd('-');
            }

            return trimmed;
        }
    }
}
