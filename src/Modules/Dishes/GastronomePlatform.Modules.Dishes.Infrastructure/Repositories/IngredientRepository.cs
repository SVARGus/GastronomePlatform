using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;
using GastronomePlatform.Modules.Dishes.Domain.Enums;
using GastronomePlatform.Modules.Dishes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Repositories
{
    /// <summary>
    /// Реализация <see cref="IIngredientRepository"/> через EF Core.
    /// </summary>
    /// <remarks>
    /// Создан вместе с первым UC-потребителем (UC-DSH-009 SetDietLabels — нужен
    /// <see cref="IIngredientRepository.GetMarkersByIdsAsync"/>). По мере появления
    /// admin-команд UC-DSH-110/111 и UC-DSH-030..032 реализация расширится — сейчас
    /// она минимальна: только методы, которые реально вызываются. Прочие методы
    /// интерфейса возвращают результат прямого EF-запроса.
    /// </remarks>
    public sealed class IngredientRepository : IIngredientRepository
    {
        private readonly DishesDbContext _context;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="IngredientRepository"/>.
        /// </summary>
        /// <param name="context">Контекст базы данных модуля Dishes.</param>
        public IngredientRepository(DishesDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public async Task<Ingredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.Ingredients.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        /// <inheritdoc/>
        public async Task<Ingredient?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
            => await _context.Ingredients.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Ingredient>> ListActiveAsync(CancellationToken cancellationToken = default)
            => await _context.Ingredients
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task AddAsync(Ingredient ingredient, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(ingredient);
            await _context.Ingredients.AddAsync(ingredient, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyDictionary<Guid, IngredientMarkers>> GetMarkersByIdsAsync(
            IReadOnlyCollection<Guid> ingredientIds,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(ingredientIds);

            if (ingredientIds.Count == 0)
            {
                return new Dictionary<Guid, IngredientMarkers>(capacity: 0);
            }

            // Выбираем только нужные колонки — IngredientMarkers содержит два маркера,
            // остальные поля Ingredient (Name, Description и т.д.) для перерасчёта не нужны.
            var rows = await _context.Ingredients
                .AsNoTracking()
                .Where(x => ingredientIds.Contains(x.Id))
                .Select(x => new { x.Id, x.AllergenType, x.DietConflictsMask })
                .ToListAsync(cancellationToken);

            Dictionary<Guid, IngredientMarkers> result = new(capacity: rows.Count);
            foreach (var row in rows)
            {
                result[row.Id] = new IngredientMarkers(
                    Allergens: row.AllergenType ?? AllergenType.None,
                    DietConflicts: row.DietConflictsMask);
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);
    }
}
