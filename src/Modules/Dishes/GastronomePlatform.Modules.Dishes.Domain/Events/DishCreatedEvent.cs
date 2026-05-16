using GastronomePlatform.Common.Domain.Events;

namespace GastronomePlatform.Modules.Dishes.Domain.Events
{
    /// <summary>
    /// Доменное событие — создано новое блюдо в статусе <c>Draft</c>.
    /// Поднимается в <c>Dish.Create(...)</c>.
    /// </summary>
    /// <param name="DishId">Идентификатор созданного блюда.</param>
    /// <param name="AuthorUserId">Идентификатор автора блюда.</param>
    public sealed record DishCreatedEvent(Guid DishId, Guid AuthorUserId) : IDomainEvent
    {
        /// <inheritdoc/>
        public Guid EventId { get; init; } = Guid.NewGuid();

        /// <inheritdoc/>
        public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    }
}
