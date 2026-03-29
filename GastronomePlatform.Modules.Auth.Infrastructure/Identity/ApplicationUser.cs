using Microsoft.AspNetCore.Identity;

namespace GastronomePlatform.Modules.Auth.Infrastructure.Identity
{
    /// <summary>
    /// Пользователь системы аутентификации.
    /// Расширяет <see cref="IdentityUser{TKey}"/> полями специфичными для платформы.
    /// Живёт в Infrastructure — Domain знает о пользователе только через Guid.
    /// </summary>
    public sealed class ApplicationUser : IdentityUser<Guid>
    {
        /// <summary>
        /// Дата и время регистрации пользователя (UTC).
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Признак деактивации аккаунта администратором.
        /// Деактивированный пользователь не может войти в систему.
        /// </summary>
        public bool IsDeactivated { get; set; }
    }
}
