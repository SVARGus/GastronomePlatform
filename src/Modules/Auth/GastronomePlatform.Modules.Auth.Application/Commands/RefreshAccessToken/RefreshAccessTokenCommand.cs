using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Auth.Application.DTOs;

namespace GastronomePlatform.Modules.Auth.Application.Commands.RefreshAccessToken
{
    /// <summary>
    /// Команда обновления пары токенов по действующему refresh token.
    /// </summary>
    /// <param name="RefreshToken">Строковое значение текущего refresh token.</param>
    public sealed record RefreshAccessTokenCommand(string RefreshToken) : ICommand<LoginResponse>;
}
