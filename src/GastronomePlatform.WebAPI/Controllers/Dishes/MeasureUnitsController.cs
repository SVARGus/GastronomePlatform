using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Queries.GetMeasureUnits;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GastronomePlatform.WebAPI.Controllers.Dishes
{
    /// <summary>
    /// Контроллер справочника единиц измерения модуля Dishes (UC-DSH-064).
    /// Эндпоинт публичный.
    /// </summary>
    [ApiController]
    [Route("api/measure-units")]
    public sealed class MeasureUnitsController : ApiController
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="MeasureUnitsController"/>.
        /// </summary>
        /// <param name="sender">Отправитель MediatR.</param>
        public MeasureUnitsController(ISender sender) : base(sender) { }

        /// <summary>
        /// Возвращает полный справочник единиц измерения (UC-DSH-064).
        /// Сортировка: по типу, затем по коэффициенту пересчёта, затем по коду.
        /// </summary>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns><c>200 OK</c> со списком <see cref="MeasureUnitDto"/>.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllAsync(CancellationToken ct)
        {
            Result<IReadOnlyList<MeasureUnitDto>> result =
                await Sender.Send(new GetMeasureUnitsQuery(), ct);
            return MapResult(result);
        }
    }
}
