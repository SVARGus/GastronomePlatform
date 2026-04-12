using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Users.Application.DTOs;

namespace GastronomePlatform.Modules.Users.Application.Queries.GetProfile
{
    /// <summary>
    /// Запрос профиля пользователя по идентификатору.
    /// </summary>
    /// <param name="UserId">Идентификатор пользователя.</param>
    public sealed record GetProfileQuery(Guid UserId) : IQuery<UserProfileDto>;
}
