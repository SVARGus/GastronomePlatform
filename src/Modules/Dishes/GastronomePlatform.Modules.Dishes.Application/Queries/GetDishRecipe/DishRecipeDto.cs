namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishRecipe
{
    /// <summary>
    /// Публичное представление рецепта блюда (UC-DSH-052) — обёртка над
    /// <see cref="RecipeViewDto"/> с метаданными о слое-источнике.
    /// </summary>
    /// <remarks>
    /// Симметрично <see cref="GastronomePlatform.Modules.Dishes.Application.Queries.GetDishById.DishDetailDto"/>:
    /// клиент по флагу <see cref="IsPublishedVersion"/> понимает, из какого слоя
    /// пришли данные, и по флагу <see cref="HasUnsavedChanges"/> (только для автора/admin)
    /// — есть ли несохранённые правки в рабочем слое относительно публичной версии.
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда. Повторяется в теле ответа для
    /// самодостаточности (UI может кэшировать DishRecipeDto без отдельного знания об URL).</param>
    /// <param name="IsPublishedVersion">
    /// <see langword="true"/>, если данные получены из jsonb-снепшота <c>Dish.PublishedVersionData</c>;
    /// <see langword="false"/>, если из основных таблиц (рабочая версия, доступная только автору/admin).
    /// </param>
    /// <param name="HasUnsavedChanges">
    /// Для автора/admin: <see langword="true"/>, если <c>Dish.UpdatedAt &gt; Dish.PublishedAt</c> —
    /// в рабочей версии есть несохранённые правки. Для остальных пользователей —
    /// <see langword="null"/> (приватная информация о состоянии редактирования не утекает).
    /// </param>
    /// <param name="Recipe">Рецепт со всеми вложенными сущностями.</param>
    public sealed record DishRecipeDto(
        Guid DishId,
        bool IsPublishedVersion,
        bool? HasUnsavedChanges,
        RecipeViewDto Recipe);
}
