using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Users.Application.DTOs;

namespace GastronomePlatform.Modules.Users.Application.Queries.GetPublicProfile
{
    /// <summary>
    /// Запрос публичного профиля пользователя по идентификатору (анонимный).
    /// В отличие от <c>GetProfileQuery</c> (полный профиль для владельца),
    /// возвращает урезанное <see cref="PublicUserProfileDto"/> без контактных
    /// и чувствительных данных.
    /// </summary>
    /// <param name="UserId">Идентификатор пользователя.</param>
    public sealed record GetPublicProfileQuery(Guid UserId) : IQuery<PublicUserProfileDto>;
}
