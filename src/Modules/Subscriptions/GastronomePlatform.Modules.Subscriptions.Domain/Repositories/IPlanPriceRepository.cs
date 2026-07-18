using GastronomePlatform.Modules.Subscriptions.Domain.Entities;

namespace GastronomePlatform.Modules.Subscriptions.Domain.Repositories
{
    /// <summary>
    /// Репозиторий офферов каталога (<see cref="PlanPrice"/>).
    /// </summary>
    /// <remarks>
    /// Отдельный репозиторий, а не расширение <see cref="ISubscriptionPlanRepository"/>:
    /// <see cref="PlanPrice"/> не является композицией <see cref="SubscriptionPlan"/>
    /// в Domain (у плана нет <c>_prices</c>, нет метода <c>AddPrice</c>) — это независимая
    /// сущность каталога, связанная с планом только по FK <c>PlanId</c>.
    /// Сейчас покрывает UC-SUB-004 (AddOffer) — минимальный набор операций.
    /// UC-SUB-005 (UpdateOffer) и UC-SUB-006 (SetPricing) добавят методы обхода
    /// цепочек <c>RenewsAs</c>/<c>Fallback</c> для cycle-detection.
    /// </remarks>
    public interface IPlanPriceRepository
    {
        /// <summary>
        /// Возвращает оффер по идентификатору. Используется для проверки принадлежности
        /// целевого оффера перехода (<c>RenewsAsPriceId</c>/<c>FallbackPriceId</c>)
        /// тому же плану.
        /// </summary>
        /// <param name="priceId">Идентификатор оффера.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// Оффер, если найден; иначе <see langword="null"/>.
        /// </returns>
        Task<PlanPrice?> GetByIdAsync(Guid priceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает все офферы указанных планов одним запросом.
        /// Парная операция к <c>ISubscriptionPlanRepository.ListWithGrantsAsync</c>
        /// в витрине каталога (UC-SUB-040).
        /// </summary>
        /// <remarks>
        /// Фильтр покупаемости в запросе **не** применяется — отбор выполняет
        /// вызывающий код через <see cref="PlanPrice.IsPurchasableAt"/>, чтобы правило
        /// оставалось в Domain в единственном экземпляре. Пустой вход даёт пустой
        /// результат без обращения к БД.
        /// </remarks>
        /// <param name="planIds">Идентификаторы планов.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// Список офферов, принадлежащих указанным планам. Порядок не гарантируется.
        /// </returns>
        Task<IReadOnlyList<PlanPrice>> ListByPlanIdsAsync(
            IReadOnlyCollection<Guid> planIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавляет новый оффер в хранилище.
        /// </summary>
        /// <param name="price">Оффер для сохранения.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task AddAsync(PlanPrice price, CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохраняет изменения в хранилище (Unit of Work).
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
