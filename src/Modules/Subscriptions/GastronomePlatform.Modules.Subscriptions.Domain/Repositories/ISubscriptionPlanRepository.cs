using GastronomePlatform.Modules.Subscriptions.Domain.Entities;

namespace GastronomePlatform.Modules.Subscriptions.Domain.Repositories
{
    /// <summary>
    /// Репозиторий каталога тарифных планов (<see cref="SubscriptionPlan"/>).
    /// </summary>
    /// <remarks>
    /// Набор методов расширяется по мере появления UC-потребителей: сейчас покрывает
    /// UC-SUB-001 (Create + pre-check уникальности <see cref="SubscriptionPlan.TechnicalName"/>),
    /// UC-SUB-004 (проверка существования плана перед добавлением оффера),
    /// UC-SUB-007 (загрузка плана с составом грантов под <see cref="SubscriptionPlan.SetGrants"/>)
    /// и UC-SUB-040 (витрина каталога).
    /// </remarks>
    public interface ISubscriptionPlanRepository
    {
        /// <summary>
        /// Проверяет, существует ли активный план с указанным
        /// <see cref="SubscriptionPlan.TechnicalName"/>.
        /// Учитываются все планы, у которых <c>TechnicalName</c> задан
        /// (partial UNIQUE-индекс в БД покрывает <c>WHERE "TechnicalName" IS NOT NULL</c>).
        /// </summary>
        /// <param name="technicalName">Системное имя для проверки.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see langword="true"/>, если план с таким <c>TechnicalName</c> уже существует;
        /// иначе <see langword="false"/>.
        /// </returns>
        Task<bool> TechnicalNameExistsAsync(string technicalName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает план по идентификатору без загрузки связанных сущностей
        /// (<c>Grants</c>, <c>PlanPrices</c>). Достаточно для проверки существования плана
        /// (UC-SUB-004) без лишнего join-а.
        /// </summary>
        /// <param name="planId">Идентификатор плана.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// План, если найден; иначе <see langword="null"/>.
        /// </returns>
        Task<SubscriptionPlan?> GetByIdAsync(Guid planId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает план с загруженным составом грантов (<c>Grants</c>).
        /// Загрузка нужна для UC-SUB-007 <see cref="SubscriptionPlan.SetGrants"/> —
        /// метод внутри вызывает <c>_grants.Clear()</c>, и EF change tracker
        /// должен видеть удаляемые <c>PlanGrant</c>-строки, иначе они «зависнут».
        /// </summary>
        /// <param name="planId">Идентификатор плана.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// План с <c>Grants</c>, если найден; иначе <see langword="null"/>.
        /// </returns>
        Task<SubscriptionPlan?> GetByIdWithGrantsAsync(Guid planId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает все планы каталога с загруженным составом грантов (<c>Grants</c>).
        /// Выборка витрины UC-SUB-040.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Фильтр доступности намеренно **не** применяется в запросе: отбор выполняет
        /// вызывающий код через <see cref="SubscriptionPlan.IsAvailableAt"/>. Правило
        /// видимости плана живёт в Domain в единственном экземпляре, и частичная
        /// копия условия в SQL при его изменении дала бы молча неверный результат.
        /// Каталог измеряется единицами планов, поэтому загрузка целиком дешевле
        /// риска расхождения.
        /// </para>
        /// <para>
        /// Офферы не подгружаются: <c>PlanPrice</c> не входит в композицию
        /// <see cref="SubscriptionPlan"/> и навигации к нему у плана нет. Их загружает
        /// <c>IPlanPriceRepository.ListByPlanIdsAsync</c> отдельным запросом.
        /// </para>
        /// </remarks>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// Список планов с составом грантов. Пустой список, если каталог пуст.
        /// </returns>
        Task<IReadOnlyList<SubscriptionPlan>> ListWithGrantsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавляет новый план в хранилище.
        /// </summary>
        /// <param name="plan">План для сохранения.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task AddAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохраняет изменения в хранилище (Unit of Work).
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
