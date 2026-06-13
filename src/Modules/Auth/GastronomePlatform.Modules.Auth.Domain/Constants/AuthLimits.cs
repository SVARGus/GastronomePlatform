namespace GastronomePlatform.Modules.Auth.Domain.Constants
{
    /// <summary>
    /// Лимиты длины полей пользовательских данных модуля Auth.
    /// </summary>
    /// <remarks>
    /// Сам класс пользователя (<c>ApplicationUser</c>) лежит в Infrastructure (Identity);
    /// Domain не зависит от Infrastructure. Поэтому константы вынесены в отдельный
    /// доменный класс — единый источник правды для валидаторов команд (<c>Register</c>,
    /// <c>Login</c>, ...) и для будущего <c>IAuthUserService</c>.
    /// <para>
    /// Аналогичные ограничения дублируются в <c>UserProfile</c> (модуль Users) —
    /// это допустимо: у каждой доменной области свой источник правды,
    /// зеркалирование значений 1:1 — известное и контролируемое.
    /// </para>
    /// </remarks>
    public static class AuthLimits
    {
        /// <summary>Максимальная длина email (совпадает с дефолтом Identity).</summary>
        public const int MAX_EMAIL_LENGTH = 256;

        /// <summary>Максимальная длина строки логина (email / phone / userName в одном поле).</summary>
        public const int MAX_LOGIN_LENGTH = 256;

        /// <summary>Минимальная длина никнейма.</summary>
        public const int MIN_USER_NAME_LENGTH = 3;

        /// <summary>Максимальная длина никнейма.</summary>
        public const int MAX_USER_NAME_LENGTH = 100;

        /// <summary>Минимальная длина пароля.</summary>
        public const int MIN_PASSWORD_LENGTH = 8;

        /// <summary>Максимальная длина пароля.</summary>
        public const int MAX_PASSWORD_LENGTH = 100;

        /// <summary>Максимальная длина номера телефона.</summary>
        public const int MAX_PHONE_LENGTH = 50;
    }
}
