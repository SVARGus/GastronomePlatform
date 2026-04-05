using GastronomePlatform.Common.Domain.Results;

namespace GastronomePlatform.Modules.Users.Domain.Errors
{
    /// <summary>
    /// Доменные ошибки модуля Users.
    /// </summary>
    public static class UsersErrors
    {
        public static readonly Error ProfileNotFound =
            Error.NotFound("USERS.PROFILE_NOT_FOUND", "Профиль пользователя не найден.");

        public static readonly Error ProfileAlreadyExists =
            Error.Conflict("USERS.PROFILE_ALREADY_EXISTS", "Профиль пользователя уже существует.");

        public static readonly Error NotAuthorized =
            Error.Forbidden("USERS.NOT_AUTHORIZED", "Недостаточно прав для выполнения операции.");
    }
}
