using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Auth.Application.Commands.Register
{
    /// <summary>
    /// Команда регистрации нового пользователя.
    /// </summary>
    /// <param name="Email">Адрес электронной почты.</param>
    /// <param name="UserName">Никнейм пользователя.</param>
    /// <param name="Password">Пароль в открытом виде.</param>
    /// <param name="Phone">Номер телефона (опционально).</param>
    public sealed record RegisterCommand(string Email, string UserName, string Password, string? Phone) : ICommand;
}
