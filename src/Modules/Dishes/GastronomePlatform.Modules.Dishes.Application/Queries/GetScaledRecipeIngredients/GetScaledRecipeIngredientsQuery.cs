using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetScaledRecipeIngredients
{
    /// <summary>
    /// Запрос пересчёта ингредиентов рецепта на указанное число порций (UC-DSH-056).
    /// Требует аутентификации (<c>VALID_ACTOR</c>). Источник данных —
    /// <c>Dish.PublishedVersionData</c> (snapshot); рабочая версия не отдаётся.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Доступно авторизованным пользователям с грантом <c>PortionCalculator</c>
    /// (POL-004); автор блюда и admin проходят без проверки подписки.
    /// </para>
    /// <para>
    /// Текущая реализация выполняет только линейное масштабирование <c>Quantity</c>:
    /// <c>scaled = original * (servings / servingsDefault)</c>. Конвертация единиц
    /// (Mass ↔ Volume через <c>Ingredient.DensityApprox</c>) отложена — клиент получает
    /// тот же <c>MeasureUnitId</c>, что в snapshot.
    /// </para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="Servings">Желаемое число порций (≥ 1).</param>
    public sealed record GetScaledRecipeIngredientsQuery(
        Guid DishId,
        int Servings) : IQuery<GetScaledRecipeIngredientsResult>;
}
