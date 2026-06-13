using GastronomePlatform.Common.Domain.Primitives;

namespace GastronomePlatform.Common.Application.Abstractions
{
    /// <summary>
    /// Сервис публикации доменных событий, накопленных в корне агрегата.
    /// </summary>
    /// <remarks>
    /// Выносит из командных хендлеров повторяющуюся последовательность
    /// «копия <see cref="AggregateRoot{TId}.DomainEvents"/> → <see cref="AggregateRoot{TId}.ClearDomainEvents"/> →
    /// публикация через <see cref="MediatR.IPublisher"/>». Хендлер вызывает
    /// <see cref="DispatchAsync{TId}"/> после успешного <c>SaveChangesAsync</c>.
    /// </remarks>
    public interface IDomainEventDispatcher
    {
        /// <summary>
        /// Публикует все накопленные в агрегате доменные события и очищает их список.
        /// </summary>
        /// <typeparam name="TId">Тип идентификатора корня агрегата.</typeparam>
        /// <param name="aggregate">Корень агрегата, в котором накопились события.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task DispatchAsync<TId>(AggregateRoot<TId> aggregate, CancellationToken cancellationToken = default)
            where TId : notnull;
    }
}
