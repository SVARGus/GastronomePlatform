using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;
using GastronomePlatform.Modules.Dishes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Repositories
{
    /// <summary>
    /// Реализация <see cref="IMeasureUnitRepository"/> через EF Core.
    /// </summary>
    /// <remarks>
    /// Создан вместе с первыми UC-потребителями справочника (UC-DSH-030..031 —
    /// нужен <see cref="IMeasureUnitRepository.GetByIdAsync"/> для проверки
    /// существования единицы при добавлении/обновлении ингредиента рецепта).
    /// Все методы реализованы прямолинейно — справочник на Этапе 2 наполняется
    /// seed-данными, runtime-кода для добавления единиц пока нет.
    /// </remarks>
    public sealed class MeasureUnitRepository : IMeasureUnitRepository
    {
        private readonly DishesDbContext _context;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="MeasureUnitRepository"/>.
        /// </summary>
        /// <param name="context">Контекст базы данных модуля Dishes.</param>
        public MeasureUnitRepository(DishesDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public async Task<MeasureUnit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.MeasureUnits
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        /// <inheritdoc/>
        public async Task<MeasureUnit?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
            => await _context.MeasureUnits
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

        /// <inheritdoc/>
        public async Task<IReadOnlyList<MeasureUnit>> ListAllAsync(CancellationToken cancellationToken = default)
            => await _context.MeasureUnits
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);
    }
}
