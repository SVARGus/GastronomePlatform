using GastronomePlatform.Modules.Subscriptions.Domain.Entities;

namespace GastronomePlatform.Modules.Subscriptions.Domain.Repositories
{
    /// <summary>
    /// Репозиторий каталога тарифных планов (<see cref="SubscriptionPlan"/>).
    /// </summary>
    /// <remarks>
    /// Bootstrap-этап модуля (Phase A) — минимальный набор под UC-SUB-001
    /// (Create) и pre-check уникальности <see cref="SubscriptionPlan.TechnicalName"/>.
    /// Операции чтения витрины (UC-SUB-040) и редактирования плана (UC-SUB-002/003)
    /// добавляются по мере появления соответствующих UC.
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
