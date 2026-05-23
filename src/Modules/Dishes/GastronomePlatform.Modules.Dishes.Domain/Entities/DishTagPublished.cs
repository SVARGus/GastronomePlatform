namespace GastronomePlatform.Modules.Dishes.Domain.Entities
{
    /// <summary>
    /// Связь опубликованной версии блюда с тегом. Заполняется при
    /// <c>Dish.Publish(...)</c> синхронно с <see cref="Dish.PublishedVersionData"/>,
    /// очищается при <c>Unpublish</c> / <c>Archive</c>.
    /// </summary>
    /// <remarks>
    /// Используется в каталожном фильтре (UC-DSH-054 SearchDishes).
    /// Composite-ключ <c>(DishId, TagId)</c>. Изменения этой таблицы НЕ задевают
    /// <see cref="Dish.UpdatedAt"/> — это техническая денормализация.
    /// </remarks>
    public sealed class DishTagPublished : IEquatable<DishTagPublished>
    {
        /// <summary>Идентификатор блюда. Часть composite PK.</summary>
        public Guid DishId { get; private set; }

        /// <summary>Идентификатор тега. Часть composite PK.</summary>
        public Guid TagId { get; private set; }

        /// <summary>Конструктор без параметров для EF Core.</summary>
        private DishTagPublished() { }

        /// <summary>
        /// Создаёт опубликованную связь блюда с тегом. Вызывается только
        /// из <c>Dish.Publish(...)</c>.
        /// </summary>
        /// <param name="dishId">Идентификатор блюда.</param>
        /// <param name="tagId">Идентификатор тега.</param>
        internal DishTagPublished(Guid dishId, Guid tagId)
        {
            DishId = dishId;
            TagId = tagId;
        }

        /// <inheritdoc/>
        public bool Equals(DishTagPublished? other)
        {
            return other is not null
                && DishId == other.DishId
                && TagId == other.TagId;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is DishTagPublished other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(DishId, TagId);
    }
}
