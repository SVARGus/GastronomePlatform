namespace GastronomePlatform.Common.Application.Validation
{
    /// <summary>
    /// Общие проверки URL-строк для слоя Application.
    /// </summary>
    /// <remarks>
    /// Используется в FluentValidation-валидаторах команд, принимающих
    /// пользовательские URL (например, <c>RecipeStep.VideoUrl</c>).
    /// Не делает сетевых запросов — только парсинг строки и проверку схемы.
    /// </remarks>
    public static class UrlValidator
    {
        /// <summary>
        /// Возвращает <see langword="true"/>, если строка является валидным
        /// absolute URI со схемой <c>http</c> или <c>https</c>.
        /// </summary>
        /// <param name="url">Проверяемая строка. Допускаются <see langword="null"/>
        /// и пробельные значения — оба возвращают <see langword="false"/>.</param>
        /// <returns>
        /// <see langword="true"/> — строка парсится как absolute URI со схемой http/https;
        /// <see langword="false"/> — мусор, относительный URI, опасная схема
        /// (<c>javascript:</c>, <c>data:</c>, <c>file:</c> и т.п.).
        /// </returns>
        public static bool IsValidHttpUrl(string? url) =>
            !string.IsNullOrWhiteSpace(url)
            && Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
