using GastronomePlatform.Modules.Auth.Domain.Entities;

namespace GastronomePlatform.Modules.Auth.Domain.Repositories
{
    /// <summary>
    /// Репозиторий для работы с refresh-токенами.
    /// </summary>
    public interface IRefreshTokenRepository
    {
        /// <summary>
        /// Находит токен по его строковому значению.
        /// Возвращает null если токен не найден.
        /// </summary>
        /// <param name="token">Строковое значение токена.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        ///<see cref="RefreshToken"/> если токен найден; иначе <see langword="null"/>
        /// </returns>
        Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавляет новый refresh-токен в хранилище.
        /// </summary>
        /// <param name="refreshToken">Токен для сохранения.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохраняет изменения в хранилище (Unit of Work).
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Удаляет все неактивные токены пользователя.
        /// Вызывается при каждом входе для предотвращения накопления мусора.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task DeleteInactiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
