using GastronomePlatform.Common.Domain.Events;
using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Domain.Events
{
    /// <summary>
    /// Доменное событие — биты <c>Dish.DietLabelsMask</c> были автоматически сняты
    /// при перерасчёте по текущему составу <c>Recipe.Ingredients</c>. Поднимается
    /// из <see cref="Entities.Dish.RecalculateDishMarkers"/>, если итоговая маска
    /// отличается от исходной.
    /// </summary>
    /// <remarks>
    /// Используется UI/подсистемой уведомлений (Этап 5+), чтобы сообщить автору
    /// о причине снятия меток («Метка Vegan снята из-за добавления свинины»).
    /// На текущем этапе у события подписчиков нет — оно регистрируется в коллекции
    /// доменных событий агрегата для будущей доставки.
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="AuthorUserId">Идентификатор автора блюда.</param>
    /// <param name="RemovedLabels">Битовая маска снятых меток (всегда не равна
    /// <see cref="DietLabels.None"/> — иначе событие не поднимается).</param>
    public sealed record DishDietLabelsAutoCorrectedEvent(
        Guid DishId,
        Guid AuthorUserId,
        DietLabels RemovedLabels) : IDomainEvent
    {
        /// <inheritdoc/>
        public Guid EventId { get; init; } = Guid.NewGuid();

        /// <inheritdoc/>
        public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    }
}
