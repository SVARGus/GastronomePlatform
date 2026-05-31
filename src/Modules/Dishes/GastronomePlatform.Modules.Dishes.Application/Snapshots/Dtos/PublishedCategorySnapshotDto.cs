namespace GastronomePlatform.Modules.Dishes.Application.Snapshots.Dtos
{
    /// <summary>
    /// Снепшот привязки блюда к категории каталога.
    /// </summary>
    /// <remarks>
    /// MVP-формат: только <see cref="Id"/>. Имя и slug категории не денормализуются в снепшот —
    /// резолвятся потребителем (например, snapshot-веткой UC-DSH-050) через справочник.
    /// При переходе на денормализованный формат (UC-DSH-052 либо Этап 8+ rebuild snapshot)
    /// сюда добавятся <c>Name</c> и <c>Slug</c> как nullable-поля для обратной совместимости.
    /// </remarks>
    /// <param name="Id">Идентификатор категории в справочнике <c>dishes.Categories</c>.</param>
    public sealed record PublishedCategorySnapshotDto(Guid Id);
}
