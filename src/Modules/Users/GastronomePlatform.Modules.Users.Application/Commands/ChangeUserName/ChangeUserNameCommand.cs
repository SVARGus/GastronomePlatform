using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Users.Application.Commands.ChangeUserName
{
    /// <summary>
    /// Команда изменения никнейма пользователя.
    /// </summary>
    /// <param name="UserId">Идентификатор пользователя.</param>
    /// <param name="NewUserName">Новый никнейм.</param>
    public sealed record ChangeUserNameCommand(Guid UserId, string NewUserName) : ICommand;
}
