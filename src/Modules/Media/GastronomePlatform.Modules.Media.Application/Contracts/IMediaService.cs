using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Media.Domain.Enums;

namespace GastronomePlatform.Modules.Media.Application.Contracts
{
    /// <summary>
    /// Публичный контракт модуля Media для межмодульного взаимодействия.
    /// При выделении Media в микросервис заменяется на HTTP-клиент без изменений у потребителей.
    /// </summary>
    public interface IMediaService
    {
        /// <summary>
        /// Возвращает метаданные файла без его содержимого.
        /// </summary>
        /// <param name="mediaId">Идентификатор медиафайла.</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>
        /// <see cref="MediaMetadataDto"/> при успехе; <c>MEDIA.NOT_FOUND</c> если файл не найден.
        /// </returns>
        Task<Result<MediaMetadataDto>> GetMetadataAsync(
            Guid mediaId,
            CancellationToken ct = default);

        /// <summary>
        /// Batch-запрос метаданных для минимизации N+1 при формировании каталогов.
        /// </summary>
        /// <param name="mediaIds">Коллекция идентификаторов медиафайлов.</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>
        /// Словарь <c>mediaId → MediaMetadataDto</c> только для найденных файлов.
        /// Отсутствующие идентификаторы не включаются в результат (не являются ошибкой).
        /// </returns>
        Task<Result<IReadOnlyDictionary<Guid, MediaMetadataDto>>> GetMetadataBatchAsync(
            IReadOnlyCollection<Guid> mediaIds,
            CancellationToken ct = default);

        /// <summary>
        /// Привязывает медиафайл к бизнес-сущности (eager attach).
        /// Вызывается из Handler-ов при создании или обновлении сущностей.
        /// </summary>
        /// <param name="mediaId">Идентификатор медиафайла.</param>
        /// <param name="actorUserId">Идентификатор пользователя, выполняющего операцию.</param>
        /// <param name="entityType">Тип сущности (константа из <c>MediaEntityTypes</c>).</param>
        /// <param name="entityId">Идентификатор сущности в её домене.</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>
        /// <see cref="Result.Success"/> при успехе;
        /// <c>MEDIA.NOT_FOUND</c>, <c>MEDIA.NOT_READY</c>, <c>MEDIA.ALREADY_ATTACHED</c>,
        /// <c>MEDIA.NOT_OWNED</c>, <c>MEDIA.UNKNOWN_ENTITY_TYPE</c> при нарушении инвариантов.
        /// </returns>
        Task<Result> AttachToEntityAsync(
            Guid mediaId,
            Guid actorUserId,
            string entityType,
            Guid entityId,
            CancellationToken ct = default);

        /// <summary>
        /// Отвязывает медиафайл от сущности. После отвязки файл становится orphan
        /// и удаляется фоновой задачей по истечении <c>ExpiresAt</c>.
        /// </summary>
        /// <param name="mediaId">Идентификатор медиафайла.</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>
        /// <see cref="Result.Success"/> при успехе; <c>MEDIA.NOT_FOUND</c> если файл не найден.
        /// </returns>
        Task<Result> DetachFromEntityAsync(
            Guid mediaId,
            CancellationToken ct = default);

        /// <summary>
        /// Каскадно удаляет все медиафайлы, привязанные к указанной сущности.
        /// Вызывается при удалении Dish, UserProfile и других сущностей.
        /// </summary>
        /// <param name="entityType">Тип сущности (константа из <c>MediaEntityTypes</c>).</param>
        /// <param name="entityId">Идентификатор удаляемой сущности.</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns><see cref="Result.Success"/> при успехе или если файлов не найдено.</returns>
        Task<Result> DeleteByEntityAsync(
            string entityType,
            Guid entityId,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Метаданные медиафайла — DTO для межмодульного взаимодействия.
    /// </summary>
    /// <param name="Id">Идентификатор медиафайла.</param>
    /// <param name="OwnerUserId">Идентификатор владельца. <c>null</c> — системный файл.</param>
    /// <param name="DataCategory">Категория данных: <c>Public</c> или <c>Personal</c>.</param>
    /// <param name="EntityType">Тип привязанной сущности. <c>null</c> — файл-сирота.</param>
    /// <param name="EntityId">Идентификатор привязанной сущности. <c>null</c> — файл-сирота.</param>
    /// <param name="Width">Ширина изображения в пикселях. <c>null</c> для видео.</param>
    /// <param name="Height">Высота изображения в пикселях. <c>null</c> для видео.</param>
    /// <param name="Status">Текущий статус жизненного цикла файла.</param>
    /// <param name="ContentType">MIME-тип файла.</param>
    public sealed record MediaMetadataDto(
        Guid Id,
        Guid? OwnerUserId,
        MediaDataCategory DataCategory,
        string? EntityType,
        Guid? EntityId,
        int? Width,
        int? Height,
        MediaStatus Status,
        string ContentType);
}
