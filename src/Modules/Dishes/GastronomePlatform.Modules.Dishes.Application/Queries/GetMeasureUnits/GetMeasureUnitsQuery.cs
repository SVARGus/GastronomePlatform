using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetMeasureUnits
{
    /// <summary>
    /// Запрос полного справочника единиц измерения (UC-DSH-064).
    /// Анонимный публичный эндпоинт. Используется для заполнения dropdown-ов в UI.
    /// </summary>
    public sealed record GetMeasureUnitsQuery() : IQuery<IReadOnlyList<MeasureUnitDto>>;
}
