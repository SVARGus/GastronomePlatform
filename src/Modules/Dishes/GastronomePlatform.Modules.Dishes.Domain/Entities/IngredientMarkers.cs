using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Domain.Entities
{
    /// <summary>
    /// Денормализованные маркеры справочного ингредиента, нужные при пересчёте
    /// агрегата <see cref="Dish"/>: маска аллергенов и маска конфликтующих
    /// диетических меток.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Используется в <see cref="Dish.RecalculateDishMarkers"/> и в
    /// <see cref="Dish.SetDietLabels"/> — Application-handler собирает словарь
    /// <c>IngredientId → IngredientMarkers</c> через
    /// <c>IIngredientRepository.GetMarkersByIdsAsync</c> и передаёт его в Domain.
    /// </para>
    /// <para>
    /// Объединение двух маркеров в один record — одна Application-поездка
    /// в БД, один проход по составу рецепта на стороне Domain. Расширяется
    /// добавлением нового параметра, если появится третий маркер.
    /// </para>
    /// </remarks>
    /// <param name="Allergens">Маска аллергенов ингредиента (значение из <see cref="Ingredient.AllergenType"/>),
    /// либо <see cref="AllergenType.None"/>, если ингредиент не помечен аллергеном.</param>
    /// <param name="DietConflicts">Маска диетических меток, с которыми ингредиент конфликтует
    /// (значение из <see cref="Ingredient.DietConflictsMask"/>). Если бит установлен,
    /// блюдо с этим ингредиентом не может нести соответствующую диетическую метку.</param>
    public sealed record IngredientMarkers(
        AllergenType Allergens,
        DietLabels DietConflicts);
}
