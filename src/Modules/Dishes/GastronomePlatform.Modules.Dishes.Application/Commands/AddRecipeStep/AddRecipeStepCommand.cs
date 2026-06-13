using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.AddRecipeStep
{
    /// <summary>
    /// Команда добавления нового шага в рецепт блюда (UC-DSH-020).
    /// </summary>
    /// <remarks>
    /// <para>Авторизация — POL-001 (автор или Admin). Проверка в Handler-е.</para>
    /// <para>Порядковый номер <c>Order</c> назначается доменным методом
    /// <c>Recipe.AddStep</c> автоматически как <c>max+1</c>.</para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="Description">Основной текст шага. 10..4000 символов.</param>
    /// <param name="Title">Короткий заголовок шага. Опционально, до 200 символов.</param>
    /// <param name="ImageMediaId">Идентификатор иллюстрации шага в Media. Опционально.</param>
    /// <param name="VideoUrl">URL внешнего видео (http/https). Опционально, до 500 символов.</param>
    /// <param name="TemperatureCelsius">Температура приготовления в градусах Цельсия. Опционально, диапазон −30..300.</param>
    /// <param name="TimerMinutes">Время для UI-таймера в минутах. Опционально, диапазон 1..1440.</param>
    public sealed record AddRecipeStepCommand(
        Guid DishId,
        string Description,
        string? Title,
        Guid? ImageMediaId,
        string? VideoUrl,
        int? TemperatureCelsius,
        int? TimerMinutes) : ICommand<AddRecipeStepResult>;
}
