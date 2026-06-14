using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetNutrition
{
    /// <summary>
    /// Команда установки КБЖУ рецепта (UC-DSH-042). Полная замена значений
    /// <c>Nutrition</c>: метод расчёта, калорийность, БЖУ и опциональные сахара,
    /// насыщенные жиры, клетчатка, соль. Если у рецепта ещё нет записи КБЖУ —
    /// она создаётся; если есть — перезаписывается.
    /// </summary>
    /// <remarks>
    /// <para>Авторизация — POL-001 (автор или Admin). Проверка в Handler-е.</para>
    /// <para>
    /// Все значения должны быть неотрицательны. Опциональные согласованные
    /// инварианты — <c>Sugar ≤ Carbs</c> и <c>SaturatedFats ≤ Fats</c> — проверяются
    /// валидатором FluentValidation. Domain (<c>Nutrition.Update</c>) валидацию не дублирует.
    /// </para>
    /// <para>
    /// Правка не трогает <c>PublishedVersionData</c>.
    /// </para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="CalcMethod">Способ расчёта: на 100 г или на порцию.</param>
    /// <param name="Calories">Калорийность, ккал.</param>
    /// <param name="Proteins">Белки, г.</param>
    /// <param name="Fats">Жиры, г.</param>
    /// <param name="SaturatedFats">Насыщенные жиры, г. Опционально; должны быть ≤ <paramref name="Fats"/>.</param>
    /// <param name="Carbs">Углеводы, г.</param>
    /// <param name="Sugar">Сахара, г. Опционально; должны быть ≤ <paramref name="Carbs"/>.</param>
    /// <param name="Fiber">Клетчатка, г. Опционально.</param>
    /// <param name="Salt">Соль, г. Опционально.</param>
    public sealed record SetNutritionCommand(
        Guid DishId,
        NutritionCalcMethod CalcMethod,
        decimal Calories,
        decimal Proteins,
        decimal Fats,
        decimal? SaturatedFats,
        decimal Carbs,
        decimal? Sugar,
        decimal? Fiber,
        decimal? Salt) : ICommand;
}
