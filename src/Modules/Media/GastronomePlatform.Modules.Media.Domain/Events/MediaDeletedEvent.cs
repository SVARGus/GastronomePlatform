using GastronomePlatform.Common.Domain.Events;

namespace GastronomePlatform.Modules.Media.Domain.Events
{
    /// <summary>
    /// Доменное событие — медиафайл soft-удалён (статус переведён в <c>Deleted</c>).
    /// Поднимается в <c>MediaFile.SoftDelete(...)</c>. Физическое удаление файла
    /// из хранилища выполняется фоновой задачей UC-MED-211 на Этапе 8+.
    /// </summary>
    /// <param name="MediaId">Идентификатор удалённого файла.</param>
    public sealed record MediaDeletedEvent(Guid MediaId) : IDomainEvent
    {
        /// <inheritdoc/>
        public Guid EventId { get; init; } = Guid.NewGuid();

        /// <inheritdoc/>
        public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    }
}
