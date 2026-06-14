using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos
{
    /// <summary>
    /// DTO единицы измерения для заполнения dropdown-ов в UI
    /// (UC-DSH-064 GetMeasureUnits).
    /// </summary>
    /// <param name="Id">Идентификатор единицы измерения.</param>
    /// <param name="Code">Уникальное кодовое обозначение (латиницей).</param>
    /// <param name="NameRu">Название на русском.</param>
    /// <param name="Type">Тип единицы (Mass / Volume / Pinch / Piece).</param>
    /// <param name="ConversionToBase">Коэффициент пересчёта к базовой единице своего типа.</param>
    /// <param name="IsBase">Является ли базовой в своём типе.</param>
    public sealed record MeasureUnitDto(
        Guid Id,
        string Code,
        string NameRu,
        MeasureUnitType Type,
        decimal ConversionToBase,
        bool IsBase);
}
