using GastronomePlatform.Common.Domain.Events;

namespace GastronomePlatform.Modules.Dishes.Domain.Events
{
    /// <summary>
    /// Доменное событие — блюдо опубликовано (или перепубликовано). Поднимается в
    /// <c>Dish.Publish(...)</c> после успешного перехода в статус <c>Published</c>.
    /// На Этапе 5+ может быть подписчиком для инвалидации кэшей и уведомлений подписчикам автора.
    /// </summary>
    /// <param name="DishId">Идентификатор опубликованного блюда.</param>
    /// <param name="AuthorUserId">Идентификатор автора блюда.</param>
    public sealed record DishPublishedEvent(Guid DishId, Guid AuthorUserId) : IDomainEvent
    {
        /// <inheritdoc/>
        public Guid EventId { get; init; } = Guid.NewGuid();

        /// <inheritdoc/>
        public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    }
}
