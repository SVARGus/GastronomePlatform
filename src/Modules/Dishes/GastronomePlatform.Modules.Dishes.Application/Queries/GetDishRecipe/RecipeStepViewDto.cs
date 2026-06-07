namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishRecipe
{
    /// <summary>
    /// Шаг рецепта в публичном представлении (UC-DSH-052), упорядоченный по <see cref="Order"/>.
    /// </summary>
    /// <param name="Id">Идентификатор шага в рамках агрегата. UI использует для якорей/навигации.</param>
    /// <param name="Order">Порядковый номер шага в рамках рецепта (1..N).</param>
    /// <param name="Title">Короткий заголовок шага. <see langword="null"/>, если не задан.</param>
    /// <param name="Description">Основной текст шага.</param>
    /// <param name="ImageMediaId">Идентификатор иллюстрации в Media. <see langword="null"/>, если не задан.</param>
    /// <param name="VideoUrl">URL внешнего видео. <see langword="null"/>, если не задан.</param>
    /// <param name="TemperatureCelsius">Температура приготовления в градусах Цельсия. <see langword="null"/>, если не задана.</param>
    /// <param name="TimerMinutes">Время для UI-таймера в минутах. <see langword="null"/>, если не задано.</param>
    public sealed record RecipeStepViewDto(
        Guid Id,
        int Order,
        string? Title,
        string Description,
        Guid? ImageMediaId,
        string? VideoUrl,
        int? TemperatureCelsius,
        int? TimerMinutes);
}
