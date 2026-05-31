using GastronomePlatform.Common.Domain.Events;
using GastronomePlatform.Modules.Media.Domain.Enums;

namespace GastronomePlatform.Modules.Media.Domain.Events
{
    /// <summary>
    /// Доменное событие — медиафайл успешно загружен в хранилище и зарегистрирован в БД.
    /// Поднимается в <c>MediaFile.Upload(...)</c>. На Этапе 8+ может стать триггером
    /// асинхронной генерации миниатюр (UC-MED-213).
    /// </summary>
    /// <param name="MediaId">Идентификатор созданного файла.</param>
    /// <param name="OwnerUserId">Идентификатор владельца. <see langword="null"/> для системных файлов.</param>
    /// <param name="DataCategory">Категория данных файла.</param>
    /// <param name="ContentType">MIME-тип файла.</param>
    /// <param name="SizeBytes">Размер исходного файла в байтах.</param>
    public sealed record MediaUploadedEvent(
        Guid MediaId,
        Guid? OwnerUserId,
        MediaDataCategory DataCategory,
        string ContentType,
        long SizeBytes) : IDomainEvent
    {
        /// <inheritdoc/>
        public Guid EventId { get; init; } = Guid.NewGuid();

        /// <inheritdoc/>
        public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    }
}
