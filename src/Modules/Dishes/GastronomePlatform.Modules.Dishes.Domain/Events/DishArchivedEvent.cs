using GastronomePlatform.Common.Domain.Events;

namespace GastronomePlatform.Modules.Dishes.Domain.Events
{
    /// <summary>
    /// Доменное событие — блюдо архивировано (мягкое удаление).
    /// Поднимается в <c>Dish.Archive(...)</c>.
    /// </summary>
    /// <param name="DishId">Идентификатор архивированного блюда.</param>
    /// <param name="AuthorUserId">Идентификатор автора блюда.</param>
    public sealed record DishArchivedEvent(Guid DishId, Guid AuthorUserId) : IDomainEvent
    {
        /// <inheritdoc/>
        public Guid EventId { get; init; } = Guid.NewGuid();

        /// <inheritdoc/>
        public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    }
}
