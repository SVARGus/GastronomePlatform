namespace GastronomePlatform.Modules.Dishes.Application.Commands.DeleteOrDeactivateCategory
{
    /// <summary>
    /// Результат команды <see cref="DeleteOrDeactivateCategoryCommand"/> —
    /// сообщает клиенту, что произошло с категорией.
    /// </summary>
    /// <param name="WasDeleted"><see langword="true"/> — категория физически удалена;
    /// <see langword="false"/> — переведена в <c>IsActive = false</c>.</param>
    public sealed record DeleteOrDeactivateCategoryResult(bool WasDeleted);
}
