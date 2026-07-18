using System.Security.Claims;

namespace GastronomePlatform.Modules.Auth.Application.Abstractions
{
    /// <summary>
    /// Сервис генерации и валидации JWT-токенов.
    /// </summary>
    public interface IJwtService
    {
        /// <summary>Время жизни access token в минутах.</summary>
        int AccessTokenExpiryMinutes { get; }

        /// <summary>Время жизни refresh token в днях.</summary>
        int RefreshTokenExpiryDays { get; }

        /// <summary>
        /// Генерирует короткоживущий JWT access token с claims пользователя.
        /// </summary>
        /// <remarks>
        /// Роли передаются коллекцией: каждая попадает в токен отдельным claim-ом
        /// <c>role</c>. Тип параметра намеренно не <c>params string[]</c> — иначе
        /// оставалась бы возможность передать одну роль там, где их несколько,
        /// и потерять остальные незаметно.
        /// </remarks>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="email">Email пользователя.</param>
        /// <param name="roles">Роли пользователя. Пустая коллекция допустима — токен выпускается без claim-ов роли.</param>
        /// <returns>Подписанный JWT access token.</returns>
        string GenerateAccessToken(Guid userId, string email, IReadOnlyCollection<string> roles);

        /// <summary>
        /// Генерирует долгоживущий refresh token.
        /// Криптографически случайная строка, не JWT.
        /// </summary>
        /// <returns>Строковое значение refresh token.</returns>
        string GenerateRefreshToken();

        /// <summary>
        /// Извлекает claims из просроченного access token.
        /// Используется при обновлении токена (RefreshToken use case).
        /// Возвращает null если токен недействителен.
        /// </summary>
        /// <param name="accessToken">Просроченный JWT access token.</param>
        /// <returns>
        /// <see cref="ClaimsPrincipal"/> с claims токена;
        /// иначе <see langword="null"/>.
        /// </returns>
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string accessToken);
    }
}
