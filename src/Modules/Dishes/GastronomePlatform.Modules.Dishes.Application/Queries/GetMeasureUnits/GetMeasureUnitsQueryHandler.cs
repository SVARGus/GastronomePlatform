using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetMeasureUnits
{
    /// <summary>
    /// Обработчик запроса <see cref="GetMeasureUnitsQuery"/> (UC-DSH-064).
    /// </summary>
    public sealed class GetMeasureUnitsQueryHandler
        : IQueryHandler<GetMeasureUnitsQuery, IReadOnlyList<MeasureUnitDto>>
    {
        private readonly IMeasureUnitRepository _measureUnitRepository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetMeasureUnitsQueryHandler"/>.
        /// </summary>
        /// <param name="measureUnitRepository">Репозиторий единиц измерения.</param>
        public GetMeasureUnitsQueryHandler(IMeasureUnitRepository measureUnitRepository)
        {
            _measureUnitRepository = measureUnitRepository
                ?? throw new ArgumentNullException(nameof(measureUnitRepository));
        }

        /// <inheritdoc/>
        public async Task<Result<IReadOnlyList<MeasureUnitDto>>> Handle(
            GetMeasureUnitsQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<MeasureUnit> units =
                await _measureUnitRepository.ListAllAsync(cancellationToken);

            IReadOnlyList<MeasureUnitDto> dtos = units
                .OrderBy(u => u.Type)
                .ThenBy(u => u.ConversionToBase)
                .ThenBy(u => u.Code)
                .Select(u => new MeasureUnitDto(
                    Id: u.Id,
                    Code: u.Code,
                    NameRu: u.NameRu,
                    Type: u.Type,
                    ConversionToBase: u.ConversionToBase,
                    IsBase: u.IsBase))
                .ToList();

            return Result<IReadOnlyList<MeasureUnitDto>>.Success(dtos);
        }
    }
}
