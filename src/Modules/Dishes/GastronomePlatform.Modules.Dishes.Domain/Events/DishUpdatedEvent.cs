using GastronomePlatform.Common.Domain.Events;

namespace GastronomePlatform.Modules.Dishes.Domain.Events
{
    /// <summary>
    /// Доменное событие — данные блюда были изменены автором.
    /// Поднимается на любом update-методе <c>Dish</c> (<c>UpdateCard</c>,
    /// <c>ChangeMainImage</c>, <c>UpdateHistory</c>, <c>MarkAsUpdated</c>).
    /// </summary>
    /// <param name="DishId">Идентификатор изменённого блюда.</param>
    /// <param name="AuthorUserId">Идентификатор автора блюда.</param>
    public sealed record DishUpdatedEvent(Guid DishId, Guid AuthorUserId) : IDomainEvent
    {
        /// <inheritdoc/>
        public Guid EventId { get; init; } = Guid.NewGuid();

        /// <inheritdoc/>
        public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    }
}
