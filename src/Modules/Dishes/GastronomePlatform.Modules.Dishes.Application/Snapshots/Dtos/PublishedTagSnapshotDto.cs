namespace GastronomePlatform.Modules.Dishes.Application.Snapshots.Dtos
{
    /// <summary>
    /// Снепшот привязки блюда к тегу.
    /// </summary>
    /// <remarks>
    /// MVP-формат: только <see cref="Id"/>. Имя тега не денормализуется в снепшот —
    /// резолвится потребителем через справочник <c>dishes.Tags</c>.
    /// При переходе на денормализованный формат (UC-DSH-052 либо Этап 8+ rebuild snapshot)
    /// сюда добавится <c>Name</c> как nullable-поле для обратной совместимости.
    /// </remarks>
    /// <param name="Id">Идентификатор тега в справочнике <c>dishes.Tags</c>.</param>
    public sealed record PublishedTagSnapshotDto(Guid Id);
}
