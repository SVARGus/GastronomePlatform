using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Users.Application.Commands.UpdateLocation
{
    /// <summary>
    /// Команда обновления местоположения пользователя.
    /// </summary>
    /// <param name="UserId">Идентификатор пользователя.</param>
    /// <param name="Country">Страна.</param>
    /// <param name="Region">Регион или область.</param>
    /// <param name="City">Город.</param>
    public sealed record UpdateLocationCommand(
        Guid UserId,
        string? Country,
        string? Region,
        string? City) : ICommand;
}
