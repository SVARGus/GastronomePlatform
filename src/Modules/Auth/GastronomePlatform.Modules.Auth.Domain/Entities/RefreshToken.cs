using GastronomePlatform.Common.Domain.Primitives;

namespace GastronomePlatform.Modules.Auth.Domain.Entities
{
    /// <summary>
    /// Refresh-токен для обновления JWR access-токена.
    /// Хранится в БД, привязан к конкретному пользователю.
    /// </summary>
    public sealed class RefreshToken : Entity<Guid>
    {
        /// <summary>
        /// Значение токена (криптографически случайная строка).
        /// </summary>
        public string Token { get; private set; } = string.Empty;

        /// <summary>
        /// Идентификатор пользователя-владельца токена.
        /// </summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// Дата и время истечения токена (UTC).
        /// </summary>
        public DateTimeOffset ExpiresAt { get; private set; }

        /// <summary>
        /// Дата и время отзыва токена (UTC). Null — токен не отозван.
        /// </summary>
        public DateTimeOffset? RevokedAt { get; private set; }

        /// <summary>
        /// Токен активен — не истёк и не отозван.
        /// </summary>
        public bool IsActive => RevokedAt is null && DateTimeOffset.UtcNow < ExpiresAt;

        /// <summary>
        /// Конструктор без параметров для EF Core.
        /// </summary>
        private RefreshToken() : base() { }

        private RefreshToken(Guid id, string token, Guid userId, DateTimeOffset expiresAt)
            : base(id)
        {
            Token = token;
            UserId = userId;
            ExpiresAt = expiresAt;
        }

        /// <summary>
        /// Создаёт новый refresh-токен.
        /// </summary>
        /// <param name="token">Значение токена.</param>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="expiresAt">Дата истечения (UTC).</param>
        /// <returns>Новый экземпляр <see cref="RefreshToken"/>.</returns>
        public static RefreshToken Create(string token, Guid userId, DateTimeOffset expiresAt)
        {
            return new RefreshToken(Guid.NewGuid(), token, userId, expiresAt);
        }

        /// <summary>
        /// Отзывает токен. После отзыва токен становится неактивным.
        /// </summary>
        public void Revoke(DateTimeOffset revokedAt)
        {
            if (!IsActive)
            {
                return;
            }

            RevokedAt = revokedAt;
        }
    }
}
