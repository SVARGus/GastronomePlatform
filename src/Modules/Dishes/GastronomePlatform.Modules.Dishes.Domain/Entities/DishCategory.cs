namespace GastronomePlatform.Modules.Dishes.Domain.Entities
{
    /// <summary>
    /// Связь блюда с категорией для рабочей версии — позиция в M:M-таблице
    /// между <see cref="Dish"/> и <see cref="Category"/>. Часть агрегата <see cref="Dish"/>.
    /// </summary>
    /// <remarks>
    /// Не наследует <c>Entity&lt;TId&gt;</c> — у типа нет surrogate-идентификатора,
    /// сущность определяется composite-ключом <c>(DishId, CategoryId)</c>.
    /// <see cref="Equals(DishCategory)"/> и <see cref="GetHashCode"/> переопределены
    /// по обоим полям.
    /// <para>
    /// Создание возможно только из <c>Dish.SetCategories(...)</c>; внешний код
    /// читает коллекцию <see cref="Dish.Categories"/> и не управляет ею напрямую.
    /// </para>
    /// </remarks>
    public sealed class DishCategory : IEquatable<DishCategory>
    {
        /// <summary>Идентификатор блюда. Часть composite PK.</summary>
        public Guid DishId { get; private set; }

        /// <summary>Идентификатор категории. Часть composite PK.</summary>
        public Guid CategoryId { get; private set; }

        /// <summary>Конструктор без параметров для EF Core.</summary>
        private DishCategory() { }

        /// <summary>
        /// Создаёт связь блюда с категорией. Вызывается только из <c>Dish.SetCategories(...)</c>.
        /// </summary>
        /// <param name="dishId">Идентификатор блюда.</param>
        /// <param name="categoryId">Идентификатор категории.</param>
        internal DishCategory(Guid dishId, Guid categoryId)
        {
            DishId = dishId;
            CategoryId = categoryId;
        }

        /// <inheritdoc/>
        public bool Equals(DishCategory? other)
        {
            return other is not null
                && DishId == other.DishId
                && CategoryId == other.CategoryId;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is DishCategory other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(DishId, CategoryId);
    }
}
