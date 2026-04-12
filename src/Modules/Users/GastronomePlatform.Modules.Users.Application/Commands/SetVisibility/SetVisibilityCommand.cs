using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Users.Application.Commands.SetVisibility
{
    /// <summary>
    /// Команда изменения видимости профиля пользователя.
    /// </summary>
    /// <param name="UserId">Идентификатор пользователя.</param>
    /// <param name="IsPublic">
    /// <see langword="true"/> — профиль публичный;
    /// <see langword="false"/> — профиль скрыт.
    /// </param>
    public sealed record SetVisibilityCommand(Guid UserId, bool IsPublic) : ICommand;
}
