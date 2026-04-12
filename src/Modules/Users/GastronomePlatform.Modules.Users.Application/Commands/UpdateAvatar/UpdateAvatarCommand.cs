using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Users.Application.Commands.UpdateAvatar
{
    /// <summary>
    /// Команда обновления аватара пользователя.
    /// </summary>
    /// <param name="UserId">Идентификатор пользователя.</param>
    /// <param name="AvatarMediaId">
    /// Идентификатор медиафайла из модуля Media.
    /// <see langword="null"/> — удалить аватар.
    /// </param>
    public sealed record UpdateAvatarCommand(Guid UserId, Guid? AvatarMediaId) : ICommand;
}
