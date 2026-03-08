namespace GastronomePlatform.Common.Application.Abstractions
{
    /// <summary>
    /// Предоставляет информацию о текущем пользователе.
    /// Реализация получает данные из HTTP-контекста (JWT claims). 
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// Идентификатор текущего пользователя. Null для гостей.
        /// </summary>
        Guid? UserId { get; }

        /// <summary>
        /// Email текущего пользователя. Null для гостей.
        /// </summary>
        string? UserEmail { get; }

        /// <summary>
        /// Признак аутентификации текущего пользователя.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Роли текущего пользователя.
        /// </summary>
        IReadOnlyCollection<string> Roles { get; }

        /// <summary>
        /// Проверяет, имеет ли текущий пользователь указанную роль.
        /// </summary>
        bool IsInRole(string role);
    }
}
