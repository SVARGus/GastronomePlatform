using GastronomePlatform.Common.Domain.Events;

namespace GastronomePlatform.Modules.Auth.Domain.Events
{
    /// <summary>
    /// Доменное событие — пользователь успешно зарегистрирован.
    /// Публикуется модулем Auth, обрабатывается модулем Users
    /// для создания профиля пользователя.
    /// </summary>
    public sealed record UserRegisteredEvent : IDomainEvent
    {
        /// <inheritdoc/>
        public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;

        /// <inheritdoc/>
        public Guid EventId { get; } = Guid.NewGuid();

        /// <summary>
        /// Идентификатор зарегистрированного пользователя.
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// Email пользователя.
        /// </summary>
        public string Email { get; init; } = string.Empty;

        /// <summary>
        /// Никнейм пользователя.
        /// </summary>
        public string UserName { get; init; } = string.Empty;

        /// <summary>
        /// Номер телефона пользователя (опционально).
        /// </summary>
        public string? PhoneNumber { get; init; }
    }
}
