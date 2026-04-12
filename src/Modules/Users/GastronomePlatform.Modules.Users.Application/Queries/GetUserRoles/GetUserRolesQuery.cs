using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Users.Application.Queries.GetUserRoles
{
    /// <summary>
    /// Запрос роли пользователя по идентификатору.
    /// </summary>
    /// <param name="UserId">Идентификатор пользователя.</param>
    public sealed record GetUserRolesQuery(Guid UserId) : IQuery<IReadOnlyCollection<string>>;
}
