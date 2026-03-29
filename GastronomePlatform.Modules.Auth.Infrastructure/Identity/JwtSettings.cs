namespace GastronomePlatform.Modules.Auth.Infrastructure.Identity
{
    /// <summary>
    /// Настройка JWT-токена, загружаемые из конфигурации приложения.
    /// </summary>
    public sealed class JwtSettings
    {
        /// <summary>
        /// Секция в appsettings.json.
        /// </summary>
        public const string SECTION_NAME = "JwtSettings";

        /// <summary>
        /// Секретный ключ для подписи токена. Минимум 32 символа.
        /// </summary>
        public string Secret { get; init; } = string.Empty;

        /// <summary>
        /// Издатель токена (обычно URL сервиса).
        /// </summary>
        public string Issuer { get; init; } = string.Empty;

        /// <summary>
        /// Аудитория токена (кто может использовать).
        /// </summary>
        public string Audience { get; init; } = string.Empty;

        /// <summary>
        /// Время жизни access token в минутах.
        /// </summary>
        public int AccessTokenExpiryMinutes { get; init; } = 15;

        /// <summary>
        /// Время жизни refresh token в днях.
        /// </summary>
        public int RefreshTokenExpiryDays { get; init; } = 30;
    }
}
