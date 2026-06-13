using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.ReorderRecipeSteps
{
    /// <summary>
    /// Команда переупорядочивания шагов рецепта (UC-DSH-023).
    /// </summary>
    /// <remarks>
    /// <para>Авторизация — POL-001 (автор или Admin). Проверка в Handler-е.</para>
    /// <para>Принимает полный список идентификаторов шагов рецепта в желаемом порядке.
    /// Список должен содержать все шаги без дубликатов и без посторонних идентификаторов.
    /// Полноту покрытия проверяет Domain (<c>Recipe.ReorderSteps</c>).</para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="OrderedStepIds">Полный список идентификаторов шагов в желаемом порядке.</param>
    public sealed record ReorderRecipeStepsCommand(
        Guid DishId,
        IReadOnlyList<Guid> OrderedStepIds) : ICommand;
}
