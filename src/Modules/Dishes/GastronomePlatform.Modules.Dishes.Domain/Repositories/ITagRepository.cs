using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Domain.Repositories
{
    /// <summary>
    /// Репозиторий для работы со справочником пользовательских тегов.
    /// </summary>
    public interface ITagRepository
    {
        /// <summary>
        /// Находит тег по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор тега.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Tag"/>, если запись найдена; иначе <see langword="null"/>.
        /// </returns>
        Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Находит тег по нормализованному имени.
        /// Используется для дедупликации перед созданием нового тега
        /// (UC-DSH-008 SetTags): «Без глютена» и «без глютена» должны мапиться на одну запись.
        /// </summary>
        /// <param name="normalizedName">Нормализованное имя тега (lowercase + trim + транслит).</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Tag"/>, если запись с таким нормализованным именем найдена;
        /// иначе <see langword="null"/>.
        /// </returns>
        Task<Tag?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавляет новый тег в хранилище.
        /// </summary>
        /// <param name="tag">Тег для сохранения.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task AddAsync(Tag tag, CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохраняет изменения в хранилище (Unit of Work).
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
