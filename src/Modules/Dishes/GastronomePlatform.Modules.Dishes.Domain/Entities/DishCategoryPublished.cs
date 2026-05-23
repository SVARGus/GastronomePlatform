namespace GastronomePlatform.Modules.Dishes.Domain.Entities
{
    /// <summary>
    /// Связь опубликованной версии блюда с категорией. Заполняется при
    /// <c>Dish.Publish(...)</c> синхронно с <see cref="Dish.PublishedVersionData"/>,
    /// очищается при <c>Unpublish</c> / <c>Archive</c>.
    /// </summary>
    /// <remarks>
    /// Используется в каталожном фильтре (UC-DSH-054 SearchDishes) — посетители
    /// видят категории по этой таблице, а не по <see cref="DishCategory"/>.
    /// Composite-ключ <c>(DishId, CategoryId)</c>. Изменения этой таблицы НЕ задевают
    /// <see cref="Dish.UpdatedAt"/> — это техническая денормализация.
    /// </remarks>
    public sealed class DishCategoryPublished : IEquatable<DishCategoryPublished>
    {
        /// <summary>Идентификатор блюда. Часть composite PK.</summary>
        public Guid DishId { get; private set; }

        /// <summary>Идентификатор категории. Часть composite PK.</summary>
        public Guid CategoryId { get; private set; }

        /// <summary>Конструктор без параметров для EF Core.</summary>
        private DishCategoryPublished() { }

        /// <summary>
        /// Создаёт опубликованную связь блюда с категорией. Вызывается только
        /// из <c>Dish.Publish(...)</c>.
        /// </summary>
        /// <param name="dishId">Идентификатор блюда.</param>
        /// <param name="categoryId">Идентификатор категории.</param>
        internal DishCategoryPublished(Guid dishId, Guid categoryId)
        {
            DishId = dishId;
            CategoryId = categoryId;
        }

        /// <inheritdoc/>
        public bool Equals(DishCategoryPublished? other)
        {
            return other is not null
                && DishId == other.DishId
                && CategoryId == other.CategoryId;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is DishCategoryPublished other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(DishId, CategoryId);
    }
}
