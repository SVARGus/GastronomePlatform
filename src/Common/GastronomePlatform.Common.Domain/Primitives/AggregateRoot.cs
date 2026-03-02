using GastronomePlatform.Common.Domain.Events;

namespace GastronomePlatform.Common.Domain.Primitives
{
    /// <summary>
    /// Базовый класс для корня агрегата.
    /// Агрегат — это кластер связанных сущностей, изменяемых как единое целое.
    /// Корень агрегата накапливает доменные события для последующего диспатчинга.
    /// </summary>
    /// <typeparam name="TId">Тип идентификатора сущности</typeparam>
    public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
    {
        /// <summary>
        /// Внутренняя коллекция доменных событий, произошедших в рамках агрегата.
        /// </summary>
        private readonly List<IDomainEvent> _domainEvents = [];

        /// <summary>
        /// Конструктор для создания агрегата с известным идентификатором.
        /// </summary>
        /// <param name="id">Идентификатор корневой сущности</param>
        protected AggregateRoot(TId id) : base(id) { }

        /// <summary>
        /// Конструктор без параметров для EF Core
        /// </summary>
        protected AggregateRoot() { }

        /// <summary>
        /// Список доменных событий, произошедших в рамках агрегата.
        /// Возвращает копию, защищённую от внешней модификации.
        /// </summary>
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        /// <summary>
        /// Регистрирует доменное событие.
        /// Вызывается только внутри агрегата при изменении состояния.
        /// </summary>
        /// <param name="domainEvent">Доменное событие для публикации</param>
        protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

        /// <summary>
        /// Очищает список доменных событий.
        /// Вызывается инфраструктурным слоем после успешного диспатчинга событий.
        /// </summary>
        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}
