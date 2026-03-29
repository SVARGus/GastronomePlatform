namespace GastronomePlatform.Common.Domain.Constants
{
    /// <summary>
    /// Константы ролей пользователей платформы.
    /// Используются для авторизации во всех модулях.
    /// При переходе к микросервисам каждый сервис
    /// определяет нужные роли локально.
    /// </summary>
    public static class PlatformRoles
    {
        /// <summary>Обычный зарегистрированный пользователь.</summary>
        public const string USER = "User";

        /// <summary>Пользователь с премиум-подпиской.</summary>
        public const string PREMIUM = "Premium";

        /// <summary>Самозанятый повар, принимает заказы.</summary>
        public const string CHEF = "Chef";

        /// <summary>Ресторан, управляет поварами и меню.</summary>
        public const string RESTAURANT = "Restaurant";

        /// <summary>Администратор платформы.</summary>
        public const string ADMIN = "Admin";
    }
}
