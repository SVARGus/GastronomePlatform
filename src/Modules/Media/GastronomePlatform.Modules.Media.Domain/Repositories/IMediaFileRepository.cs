using GastronomePlatform.Modules.Media.Domain.Entities;
using GastronomePlatform.Modules.Media.Domain.Enums;

namespace GastronomePlatform.Modules.Media.Domain.Repositories
{
    /// <summary>
    /// Репозиторий для работы с агрегатом <see cref="MediaFile"/>.
    /// </summary>
    public interface IMediaFileRepository
    {
        /// <summary>
        /// Находит медиафайл по идентификатору. Миниатюры не подгружаются.
        /// Используется в UC, где Thumbnail-коллекция не требуется (UC-MED-005, 202, 203).
        /// </summary>
        /// <param name="id">Идентификатор файла.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns><see cref="MediaFile"/> если найден; иначе <see langword="null"/>.</returns>
        Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Находит медиафайл вместе с коллекцией миниатюр.
        /// Используется в UC-MED-002 (GetFile), UC-MED-003 (GetThumbnail), UC-MED-004 (GetMetadata).
        /// </summary>
        /// <param name="id">Идентификатор файла.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns><see cref="MediaFile"/> с загруженными <see cref="MediaFile.Thumbnails"/>; или <see langword="null"/>.</returns>
        Task<MediaFile?> GetByIdWithThumbnailsAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает страницу файлов указанного владельца с опциональными фильтрами.
        /// Используется в UC-MED-103 (GetUserFiles, только Admin).
        /// </summary>
        /// <param name="ownerUserId">Идентификатор владельца.</param>
        /// <param name="status">Фильтр по статусу. <see langword="null"/> — все статусы.</param>
        /// <param name="entityType">Фильтр по типу сущности. <see langword="null"/> — все типы.</param>
        /// <param name="page">Номер страницы (начиная с 1).</param>
        /// <param name="pageSize">Количество записей на странице.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Список <see cref="MediaFile"/> без миниатюр для текущей страницы.</returns>
        Task<IReadOnlyList<MediaFile>> ListByOwnerAsync(
            Guid ownerUserId,
            MediaStatus? status,
            string? entityType,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Batch-загрузка файлов по набору идентификаторов.
        /// Используется в UC-MED-201 (GetMetadataBatch).
        /// Файлы, не найденные в БД, отсутствуют в результирующем словаре.
        /// </summary>
        /// <param name="ids">Набор идентификаторов.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Словарь <c>id → MediaFile</c> только для найденных записей.</returns>
        Task<IReadOnlyDictionary<Guid, MediaFile>> GetBatchByIdsAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает все файлы, привязанные к указанной сущности.
        /// Используется в UC-MED-204 (DeleteByEntity).
        /// </summary>
        /// <param name="entityType">Тип сущности.</param>
        /// <param name="entityId">Идентификатор сущности.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Список всех активных (не Deleted) <see cref="MediaFile"/> сущности.</returns>
        Task<IReadOnlyList<MediaFile>> ListByEntityAsync(
            string entityType,
            Guid entityId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавляет новую запись медиафайла. Вызывается из UC-MED-001 / UC-MED-101.
        /// </summary>
        /// <param name="mediaFile">Медиафайл для сохранения.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task AddAsync(MediaFile mediaFile, CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохраняет изменения (Unit of Work).
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
