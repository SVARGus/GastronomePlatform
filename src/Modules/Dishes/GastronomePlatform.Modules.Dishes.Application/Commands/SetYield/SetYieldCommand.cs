using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetYield
{
    /// <summary>
    /// Команда установки выхода рецепта (UC-DSH-041). Полная замена значений
    /// <c>Yield</c>: <c>QuantityTotal</c>, <c>YieldUnit</c>, <c>ServingsCount</c>,
    /// <c>GramsPerServing</c>.
    /// </summary>
    /// <remarks>
    /// <para>Авторизация — POL-001 (автор или Admin). Проверка в Handler-е.</para>
    /// <para>
    /// Семантика — replace. <c>ServingsCount</c> — не меньше 1, <c>QuantityTotal</c>
    /// и <c>GramsPerServing</c> — неотрицательные. Domain дополнительно проверит
    /// инварианты и при нарушении вернёт <c>DISHES.INVALID_YIELD</c> (HTTP 409).
    /// </para>
    /// <para>
    /// Правка не трогает <c>PublishedVersionData</c>.
    /// </para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="QuantityTotal">Общее количество готового продукта.</param>
    /// <param name="YieldUnit">Единица выхода (граммы, килограммы, миллилитры, литры, штуки, порции).</param>
    /// <param name="ServingsCount">Количество порций (≥ 1).</param>
    /// <param name="GramsPerServing">Вес одной порции в граммах. <see langword="null"/> — не задано.</param>
    public sealed record SetYieldCommand(
        Guid DishId,
        decimal QuantityTotal,
        YieldUnit YieldUnit,
        int ServingsCount,
        decimal? GramsPerServing) : ICommand;
}
