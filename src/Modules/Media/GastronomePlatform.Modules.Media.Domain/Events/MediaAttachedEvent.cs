using GastronomePlatform.Common.Domain.Events;

namespace GastronomePlatform.Modules.Media.Domain.Events
{
    /// <summary>
    /// Доменное событие — медиафайл привязан к сущности-владельцу
    /// (Dish, RecipeStep, UserAvatar, …). Поднимается в <c>MediaFile.AttachToEntity(...)</c>.
    /// </summary>
    /// <param name="MediaId">Идентификатор привязываемого файла.</param>
    /// <param name="EntityType">Тип сущности-владельца (значение из <c>MediaEntityTypes</c>).</param>
    /// <param name="EntityId">Идентификатор сущности в её собственном домене.</param>
    public sealed record MediaAttachedEvent(
        Guid MediaId,
        string EntityType,
        Guid EntityId) : IDomainEvent
    {
        /// <inheritdoc/>
        public Guid EventId { get; init; } = Guid.NewGuid();

        /// <inheritdoc/>
        public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    }
}
