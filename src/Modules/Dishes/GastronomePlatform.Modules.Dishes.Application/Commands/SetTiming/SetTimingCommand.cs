using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetTiming
{
    /// <summary>
    /// Команда установки тайминга рецепта (UC-DSH-040). Полная замена значений
    /// <c>Timing</c> блюда: <c>Prep</c>, <c>Cook</c>, <c>Rest</c>, <c>Active</c>,
    /// <c>Total</c> и признака <c>IsTotalManual</c>.
    /// </summary>
    /// <remarks>
    /// <para>Авторизация — POL-001 (автор или Admin). Проверка в Handler-е.</para>
    /// <para>
    /// Семантика — replace всего набора полей <c>Timing</c>. Если <c>IsTotalManual = false</c>,
    /// <c>TotalTimeMinutes</c> вычисляется автоматически как сумма Prep + Cook + Rest;
    /// присланное значение игнорируется.
    /// </para>
    /// <para>
    /// Правка не трогает <c>PublishedVersionData</c>: чтобы изменения отразились
    /// в публичной версии — нужен явный UC-DSH-004 PublishDish.
    /// </para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="PrepTimeMinutes">Время подготовки в минутах. <see langword="null"/> — не задано.</param>
    /// <param name="CookTimeMinutes">Время основного приготовления в минутах. <see langword="null"/> — не задано.</param>
    /// <param name="RestTimeMinutes">Время отдыха в минутах. <see langword="null"/> — не задано.</param>
    /// <param name="ActiveTimeMinutes">Время активной работы повара в минутах. <see langword="null"/> — не задано.</param>
    /// <param name="TotalTimeMinutes">Общее время в минутах. Используется только при <c>IsTotalManual = true</c>.</param>
    /// <param name="IsTotalManual">Если <see langword="true"/> — общее время задано вручную; иначе вычисляется автоматически.</param>
    public sealed record SetTimingCommand(
        Guid DishId,
        int? PrepTimeMinutes,
        int? CookTimeMinutes,
        int? RestTimeMinutes,
        int? ActiveTimeMinutes,
        int TotalTimeMinutes,
        bool IsTotalManual) : ICommand;
}
