using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;
using GastronomePlatform.Modules.Dishes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Repositories
{
    /// <summary>
    /// Реализация <see cref="IIngredientSpecRepository"/> через EF Core.
    /// </summary>
    /// <remarks>
    /// Создан вместе с первыми UC-потребителями (UC-DSH-030..031 — нужен
    /// <see cref="IIngredientSpecRepository.GetByIdAsync"/> для проверки
    /// существования и соответствия родительскому ингредиенту при привязке
    /// сорта к позиции рецепта). На Этапе 2 <see cref="IngredientSpec"/> — stub:
    /// в UI выбор сорта не предлагается, admin-команды отсутствуют. Реализация
    /// прямолинейная: каждый метод — короткий EF-запрос.
    /// </remarks>
    public sealed class IngredientSpecRepository : IIngredientSpecRepository
    {
        private readonly DishesDbContext _context;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="IngredientSpecRepository"/>.
        /// </summary>
        /// <param name="context">Контекст базы данных модуля Dishes.</param>
        public IngredientSpecRepository(DishesDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public async Task<IngredientSpec?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.IngredientSpecs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        /// <inheritdoc/>
        public async Task<IReadOnlyList<IngredientSpec>> GetByIngredientIdAsync(
            Guid ingredientId,
            CancellationToken cancellationToken = default)
            => await _context.IngredientSpecs
                .AsNoTracking()
                .Where(x => x.IngredientId == ingredientId)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task AddAsync(IngredientSpec ingredientSpec, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(ingredientSpec);
            await _context.IngredientSpecs.AddAsync(ingredientSpec, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);
    }
}
