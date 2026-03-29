using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Auth.Application.DTOs;

namespace GastronomePlatform.Modules.Auth.Application.Commands.Login
{
    /// <summary>
    /// Команда аутентификации пользователя по логину и паролю.
    /// </summary>
    /// <param name="Login">Логин при авторизации (Email, Phone или UserName)</param>
    /// <param name="Password">Пароль</param>
    public sealed record LoginCommand(string Login, string Password) : ICommand<LoginResponse>;
}
