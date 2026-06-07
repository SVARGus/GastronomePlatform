namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishRecipe
{
    /// <summary>
    /// Времена этапов приготовления в публичном представлении рецепта (UC-DSH-052).
    /// </summary>
    /// <param name="PrepTimeMinutes">Время подготовки в минутах. <see langword="null"/>, если не задано.</param>
    /// <param name="CookTimeMinutes">Время основного приготовления в минутах. <see langword="null"/>, если не задано.</param>
    /// <param name="RestTimeMinutes">Время отдыха в минутах. <see langword="null"/>, если не задано.</param>
    /// <param name="ActiveTimeMinutes">Время активной работы повара в минутах. <see langword="null"/>, если не задано.</param>
    /// <param name="TotalTimeMinutes">Общее время приготовления в минутах. Единственное обязательное поле.</param>
    /// <param name="IsTotalManual">
    /// <see langword="true"/> — общее время задано автором вручную; <see langword="false"/> —
    /// вычислено автоматически как сумма Prep + Cook + Rest.
    /// </param>
    public sealed record TimingViewDto(
        int? PrepTimeMinutes,
        int? CookTimeMinutes,
        int? RestTimeMinutes,
        int? ActiveTimeMinutes,
        int TotalTimeMinutes,
        bool IsTotalManual);
}
