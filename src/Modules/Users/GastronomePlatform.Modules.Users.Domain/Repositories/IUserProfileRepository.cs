using GastronomePlatform.Modules.Users.Domain.Entities;

namespace GastronomePlatform.Modules.Users.Domain.Repositories
{
    /// <summary>
    /// Репозиторий для работы с персональными данными пользователя.
    /// </summary>
    public interface IUserProfileRepository
    {
        /// <summary>
        /// Находит профиль пользователя по идентификатору.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="UserProfile"/> если профиль найден; иначе <see langword="null"/>.
        /// </returns>
        Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверяет существование профиля пользователя.
        /// Используется для защиты от дублирования при обработке <c>UserRegisteredEvent</c>.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see langword="true"/> если профиль существует; иначе <see langword="false"/>.
        /// </returns>
        Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавляет новый профиль пользователя в хранилище.
        /// Вызывается при обработке <c>UserRegisteredEvent</c>.
        /// </summary>
        /// <param name="userProfile">Профиль для сохранения.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task AddAsync(UserProfile userProfile, CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохраняет изменения в хранилище (Unit of Work).
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
