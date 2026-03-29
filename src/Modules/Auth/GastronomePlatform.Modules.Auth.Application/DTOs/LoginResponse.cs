namespace GastronomePlatform.Modules.Auth.Application.DTOs
{
    /// <summary>
    /// Короткосрочный и долгосрочный токен при успешной авторизации.
    /// </summary>
    /// <param name="AccessToken">Короткоживущий JWT-токен</param>
    /// <param name="RefreshToken">Долгосрочный токен для обновления access token.</param>
    /// <param name="ExpiresAt">Дата и время истечения access token (UTC).</param>
    public sealed record LoginResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);
}
