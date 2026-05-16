using GastronomePlatform.Common.Domain.Events;

namespace GastronomePlatform.Modules.Dishes.Domain.Events
{
    /// <summary>
    /// Доменное событие — блюдо снято с публикации автором. Поднимается в
    /// <c>Dish.Unpublish(...)</c>. На Этапе 5+ может быть подписчиком
    /// для инвалидации кэшей публичных страниц.
    /// </summary>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="AuthorUserId">Идентификатор автора блюда.</param>
    public sealed record DishUnpublishedEvent(Guid DishId, Guid AuthorUserId) : IDomainEvent
    {
        /// <inheritdoc/>
        public Guid EventId { get; init; } = Guid.NewGuid();

        /// <inheritdoc/>
        public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    }
}
