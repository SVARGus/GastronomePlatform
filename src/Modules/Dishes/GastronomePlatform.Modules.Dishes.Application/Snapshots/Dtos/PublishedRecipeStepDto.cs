namespace GastronomePlatform.Modules.Dishes.Application.Snapshots.Dtos
{
    /// <summary>
    /// Снепшот одного шага рецепта на момент публикации.
    /// </summary>
    /// <param name="Id">Идентификатор шага в рамках агрегата. Сохраняется для возможности
    /// связать публичный шаг с рабочей версией (например, при админском rebuild снепшота).</param>
    /// <param name="Order">Порядковый номер шага в рамках рецепта (1..N).</param>
    /// <param name="Title">Короткий заголовок шага. <see langword="null"/>, если не задан.</param>
    /// <param name="Description">Основной текст шага.</param>
    /// <param name="ImageMediaId">Идентификатор иллюстрации в Media. <see langword="null"/>, если не задан.</param>
    /// <param name="VideoUrl">URL внешнего видео. <see langword="null"/>, если не задан.</param>
    /// <param name="TemperatureCelsius">Температура приготовления в градусах Цельсия. <see langword="null"/>, если не задана.</param>
    /// <param name="TimerMinutes">Время для UI-таймера в минутах. <see langword="null"/>, если не задано.</param>
    public sealed record PublishedRecipeStepDto(
        Guid Id,
        int Order,
        string? Title,
        string Description,
        Guid? ImageMediaId,
        string? VideoUrl,
        int? TemperatureCelsius,
        int? TimerMinutes);
}
