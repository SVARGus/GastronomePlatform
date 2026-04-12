using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Users.Application.Commands.ChangeEmail
{
    /// <summary>
    /// Команда изменения email пользователя.
    /// </summary>
    /// <param name="UserId">Идентификатор пользователя.</param>
    /// <param name="NewEmail">Новый адрес электронной почты.</param>
    public sealed record ChangeEmailCommand(Guid UserId, string NewEmail) : ICommand;
}
