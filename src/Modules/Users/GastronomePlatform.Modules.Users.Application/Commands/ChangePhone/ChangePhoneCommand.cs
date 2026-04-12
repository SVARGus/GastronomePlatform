using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Users.Application.Commands.ChangePhone
{
    /// <summary>
    /// Команда изменения номера телефона пользователя.
    /// </summary>
    /// <param name="UserId">Идентификатор пользователя.</param>
    /// <param name="NewPhone">Новый номер телефона.</param>
    public sealed record ChangePhoneCommand(Guid UserId, string NewPhone) : ICommand;
}
