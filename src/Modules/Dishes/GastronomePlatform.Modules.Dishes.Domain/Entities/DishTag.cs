namespace GastronomePlatform.Modules.Dishes.Domain.Entities
{
    /// <summary>
    /// Связь блюда с тегом для рабочей версии — позиция в M:M-таблице между
    /// <see cref="Dish"/> и <see cref="Tag"/>. Часть агрегата <see cref="Dish"/>.
    /// </summary>
    /// <remarks>
    /// Не наследует <c>Entity&lt;TId&gt;</c> — composite-ключ <c>(DishId, TagId)</c>.
    /// Создание возможно только из <c>Dish.SetTags(...)</c>.
    /// </remarks>
    public sealed class DishTag : IEquatable<DishTag>
    {
        /// <summary>Идентификатор блюда. Часть composite PK.</summary>
        public Guid DishId { get; private set; }

        /// <summary>Идентификатор тега. Часть composite PK.</summary>
        public Guid TagId { get; private set; }

        /// <summary>Конструктор без параметров для EF Core.</summary>
        private DishTag() { }

        /// <summary>
        /// Создаёт связь блюда с тегом. Вызывается только из <c>Dish.SetTags(...)</c>.
        /// </summary>
        /// <param name="dishId">Идентификатор блюда.</param>
        /// <param name="tagId">Идентификатор тега.</param>
        internal DishTag(Guid dishId, Guid tagId)
        {
            DishId = dishId;
            TagId = tagId;
        }

        /// <inheritdoc/>
        public bool Equals(DishTag? other)
        {
            return other is not null
                && DishId == other.DishId
                && TagId == other.TagId;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is DishTag other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(DishId, TagId);
    }
}
