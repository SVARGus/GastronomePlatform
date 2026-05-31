using GastronomePlatform.Modules.Media.Domain.Entities;

namespace GastronomePlatform.Modules.Media.Domain.Repositories
{
    /// <summary>
    /// Репозиторий для работы с агрегатом <see cref="MediaFile"/>.
    /// </summary>
    /// <remarks>
    /// Состав методов наращивается по мере появления UC-потребителей. На bootstrap-этапе
    /// модуля присутствуют только базовые: загрузка по идентификатору, добавление,
    /// фиксация Unit of Work. Списочные запросы (<c>ListByOwnerAsync</c>,
    /// <c>ListByEntityAsync</c>, <c>GetByIdWithThumbnailsAsync</c>) будут добавлены
    /// при реализации соответствующих UC.
    /// </remarks>
    public interface IMediaFileRepository
    {
        /// <summary>
        /// Находит медиафайл по идентификатору. Миниатюры не подгружаются.
        /// Используется в UC, где Thumbnail-коллекция не требуется (UC-MED-202
        /// AttachToEntity, UC-MED-203 DetachFromEntity, UC-MED-005 DeleteOwnFile).
        /// </summary>
        /// <param name="id">Идентификатор файла.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="MediaFile"/>, если запись найдена; иначе <see langword="null"/>.
        /// </returns>
        Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавляет новую запись медиафайла в хранилище. Вызывается из UC-MED-001 / UC-MED-101
        /// после успешного <see cref="MediaFile.Upload"/>.
        /// </summary>
        /// <param name="mediaFile">Медиафайл для сохранения.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task AddAsync(MediaFile mediaFile, CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохраняет изменения в хранилище (Unit of Work).
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
