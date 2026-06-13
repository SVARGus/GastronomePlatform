using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.RemoveRecipeStep
{
    /// <summary>
    /// Команда удаления шага рецепта (UC-DSH-022).
    /// </summary>
    /// <remarks>
    /// <para>Авторизация — POL-001 (автор или Admin). Проверка в Handler-е.</para>
    /// <para>После удаления Domain (<c>Recipe.RemoveStep</c>) автоматически пересчитывает
    /// <c>Order</c> оставшихся шагов с 1 до N. Detach связанной иллюстрации через
    /// межмодульный <c>IMediaService</c> на текущем этапе не выполняется — сервис ещё
    /// не реализован, orphan-вопрос вынесен в TODO.</para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="StepId">Идентификатор шага рецепта.</param>
    public sealed record RemoveRecipeStepCommand(Guid DishId, Guid StepId) : ICommand;
}
