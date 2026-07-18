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

        public static readonly Error PhoneAlreadyTaken =
            Error.Conflict("AUTH.PHONE_TAKEN", "Этот телефон уже используется.");

        public static readonly Error UserNameAlreadyTaken =
            Error.Conflict("AUTH.USERNAME_TAKEN", "Данный логин уже используется.");

        public static readonly Error UserNotFound =
            Error.NotFound("AUTH.USER_NOT_FOUND", "Пользователь не найден.");

        /// <summary>
        /// Учётной записи не назначено ни одной роли. Аномалия данных: логин
        /// и пароль верны, но выпустить осмысленный токен не из чего.
        /// Отдельный код вместо <see cref="InvalidCredentials"/> — чтобы причина
        /// была видна без доступа к логам сервера и не выглядела как забытый пароль.
        /// </summary>
        public static readonly Error UserHasNoRoles =
            Error.Forbidden("AUTH.USER_HAS_NO_ROLES",
                "Учётной записи не назначено ни одной роли. Обратитесь к администратору платформы.");

        public static readonly Error TokenExpired =
            Error.Failure("AUTH.TOKEN_EXPIRED", "Токен истёк.");

        public static readonly Error InvalidToken =
            Error.Failure("AUTH.INVALID_TOKEN", "Токен недействителен.");

        public static readonly Error RegistrationFailed =
            Error.Failure("AUTH.REGISTRATION_FAILED", "Не удалось создать пользователя.");

        public static readonly Error RoleNotFound =
            Error.NotFound("AUTH.ROLE_NOT_FOUND", "Роль не найдена.");

        public static readonly Error RoleAssignmentFailed =
            Error.Failure("AUTH.ROLE_ASSIGNMENT_FAILED", "Не удалось изменить набор ролей пользователя.");
    }
}
