using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Domain.Events;
using GastronomePlatform.Common.Domain.Primitives;
using MediatR;

namespace GastronomePlatform.Common.Infrastructure.Services
{
    /// <summary>
    /// Реализация <see cref="IDomainEventDispatcher"/> через MediatR <see cref="IPublisher"/>.
    /// </summary>
    /// <remarks>
    /// Сначала копирует и очищает накопленные в агрегате события, затем публикует их
    /// последовательно. Такой порядок защищает от рекурсивных «петель»: подписчик
    /// (<see cref="INotificationHandler{TNotification}"/>) может модифицировать тот же
    /// агрегат и поднять новые события — они будут опубликованы только при следующем
    /// явном вызове <see cref="DispatchAsync{TId}"/>.
    /// </remarks>
    public sealed class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IPublisher _publisher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DomainEventDispatcher"/>.
        /// </summary>
        /// <param name="publisher">MediatR-издатель уведомлений.</param>
        public DomainEventDispatcher(IPublisher publisher)
        {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        /// <inheritdoc/>
        public async Task DispatchAsync<TId>(AggregateRoot<TId> aggregate, CancellationToken cancellationToken = default)
            where TId : notnull
        {
            ArgumentNullException.ThrowIfNull(aggregate);

            List<IDomainEvent> events = aggregate.DomainEvents.ToList();
            aggregate.ClearDomainEvents();

            foreach (IDomainEvent domainEvent in events)
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }
        }
    }
}
