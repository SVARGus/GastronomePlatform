using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Domain.Repositories
{
    /// <summary>
    /// Репозиторий для работы с записями пищевой ценности (КБЖУ).
    /// Создаётся вместе с родительской сущностью (Ingredient/Recipe/IngredientSpec)
    /// и обращений извне модуля Dishes не имеет.
    /// </summary>
    public interface INutritionRepository
    {
        /// <summary>
        /// Находит запись пищевой ценности по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор записи КБЖУ.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Nutrition"/>, если запись найдена; иначе <see langword="null"/>.
        /// </returns>
        Task<Nutrition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавляет новую запись пищевой ценности в хранилище.
        /// </summary>
        /// <param name="nutrition">Запись КБЖУ для сохранения.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task AddAsync(Nutrition nutrition, CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохраняет изменения в хранилище (Unit of Work).
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
