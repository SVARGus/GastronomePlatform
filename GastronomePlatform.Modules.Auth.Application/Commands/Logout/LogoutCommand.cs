using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Auth.Application.Commands.Logout
{
    /// <summary>
    /// Команда завершения сессии пользователя.
    /// Отзывает указанный refresh token, делая его недействительным.
    /// </summary>
    /// <param name="RefreshToken">Строковое значение текущего refresh token.</param>
    public sealed record LogoutCommand(string RefreshToken) : ICommand;
}
