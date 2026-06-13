using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UpdateRecipeStep
{
    /// <summary>
    /// Команда обновления существующего шага рецепта (UC-DSH-021).
    /// </summary>
    /// <remarks>
    /// <para>Авторизация — POL-001 (автор или Admin). Проверка в Handler-е.</para>
    /// <para>Обновление атомарное: все поля присваиваются одной операцией.
    /// <c>null</c> в опциональных полях означает «очистить». Порядок шага не меняется
    /// (это отдельный UC-DSH-023).</para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="StepId">Идентификатор шага рецепта.</param>
    /// <param name="Description">Основной текст шага. 10..4000 символов.</param>
    /// <param name="Title">Короткий заголовок шага. <c>null</c> — очистить.</param>
    /// <param name="ImageMediaId">Идентификатор иллюстрации шага в Media. <c>null</c> — очистить.</param>
    /// <param name="VideoUrl">URL внешнего видео. <c>null</c> — очистить.</param>
    /// <param name="TemperatureCelsius">Температура приготовления. <c>null</c> — очистить.</param>
    /// <param name="TimerMinutes">Время для UI-таймера в минутах. <c>null</c> — очистить.</param>
    public sealed record UpdateRecipeStepCommand(
        Guid DishId,
        Guid StepId,
        string Description,
        string? Title,
        Guid? ImageMediaId,
        string? VideoUrl,
        int? TemperatureCelsius,
        int? TimerMinutes) : ICommand;
}
