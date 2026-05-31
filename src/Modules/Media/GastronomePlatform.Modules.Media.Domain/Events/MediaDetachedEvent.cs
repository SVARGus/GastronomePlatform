using GastronomePlatform.Common.Domain.Events;

namespace GastronomePlatform.Modules.Media.Domain.Events
{
    /// <summary>
    /// Доменное событие — медиафайл отвязан от сущности-владельца. Поднимается
    /// в <c>MediaFile.DetachFromEntity(...)</c>. После отвязки файл становится
    /// orphan, его <c>ExpiresAt</c> переустанавливается, и при истечении срока
    /// он удаляется фоновой задачей UC-MED-210 (Этап 8+).
    /// </summary>
    /// <param name="MediaId">Идентификатор отвязываемого файла.</param>
    /// <param name="PreviousEntityType">Тип сущности, к которой файл был привязан.</param>
    /// <param name="PreviousEntityId">Идентификатор сущности, к которой файл был привязан.</param>
    public sealed record MediaDetachedEvent(
        Guid MediaId,
        string PreviousEntityType,
        Guid PreviousEntityId) : IDomainEvent
    {
        /// <inheritdoc/>
        public Guid EventId { get; init; } = Guid.NewGuid();

        /// <inheritdoc/>
        public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    }
}
