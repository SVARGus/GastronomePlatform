namespace GastronomePlatform.Common.Domain.Primitives
{
    /// <summary>
    /// Базовый класс для всех доменных сущностей.
    /// Сущности обладают уникальным идентификатором и сравниваются по нему.
    /// </summary>
    /// <typeparam name="TId">Тип идентификатора сущности (Guid, int, strongly-typed Id)</typeparam>
    // TODO: При необходимости заменить Guid на strongly-typed Id (например, DishId, OrderId)
    public abstract class Entity<TId> : IEquatable<Entity<TId>> where TId : notnull
    {
        /// <summary>
        /// Уникальный идентификатор сущности.
        /// </summary>
        public TId Id { get; protected init; }

        /// <summary>
        /// Конструктор без параметров для EF Core.
        /// EF Core использует его при материализации объектов из БД.
        /// </summary>
        protected Entity() { }

        /// <summary>
        /// Конструктор для создания сущности с известным идентификатором.
        /// </summary>
        /// <param name="id">Идентификатор сущности</param>
        protected Entity(TId id)
        {
            if (id is null)
                throw new ArgumentNullException(nameof(id), "Id не может быть пустым");

            Id = id;
        }

        #region Equality Members

        /// <summary>
        /// Сравнение с другой сущностью по идентификатору.
        /// </summary>
        public bool Equals(Entity<TId>? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (GetType() != other.GetType())
                return false;

            return Id.Equals(other.Id);
        }

        /// <summary>
        /// Переопределение Equals для object.
        /// </summary>
        public override bool Equals(object? obj)
        {
            return obj is Entity<TId> other && Equals(other);
        }

        /// <summary>
        /// Хэш-код строится только на основе Id.
        /// </summary>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion

        #region Operators

        /// <summary>
        /// Оператор равенства.
        /// </summary>
        public static bool operator == (Entity<TId>? left, Entity<TId>? right)
        {
            if (left is null && right is null)
                return true;

            if (left is null || right is null)
                return false;

            return left.Equals(right);
        }

        /// <summary>
        /// Оператор неравенства.
        /// </summary>
        public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
        {
            return !(left == right);
        }

        #endregion
    }
}
