namespace GastronomePlatform.Modules.Media.Application.Abstractions
{
    /// <summary>
    /// Абстракция санитайзера SVG-файлов.
    /// Удаляет опасные элементы (<c>&lt;script&gt;</c>, <c>on*</c>-атрибуты и т.п.)
    /// для предотвращения XSS при прямой отдаче SVG клиенту.
    /// Реализация в Infrastructure использует <c>HtmlSanitizer</c>.
    /// </summary>
    public interface ISvgSanitizer
    {
        /// <summary>
        /// Санирует SVG-разметку: удаляет скрипты и опасные атрибуты.
        /// </summary>
        /// <param name="svgContent">Содержимое SVG-файла в виде строки UTF-8.</param>
        /// <returns>Санированное SVG-содержимое.</returns>
        string Sanitize(string svgContent);
    }
}
