using GastronomePlatform.Common.Domain.Results;

namespace GastronomePlatform.Modules.Auth.Domain.Errors
{
    /// <summary>
    /// Доменные ошибки модуля аутентификации.
    /// </summary>
    public static class AuthErrors
    {
        public static readonly Error InvalidCredentials =
            Error.Failure("AUTH.INVALID_CREDENTIALS", "Неверный логин или пароль.");

        public static readonly Error EmailAlreadyTaken =
            Error.Conflict("AUTH.EMAIL_TAKEN", "Этот email уже используется.");

        public static readonly Error UserNotFound =
            Error.NotFound("AUTH.USER_NOT_FOUND", "Пользователь не найден.");

        public static readonly Error TokenExpired =
            Error.Failure("AUTH.TOKEN_EXPIRED", "Токен истёк.");

        public static readonly Error InvalidToken =
            Error.Failure("AUTH.INVALID_TOKEN", "Токен недействителен.");
    }
}
