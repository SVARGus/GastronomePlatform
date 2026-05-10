using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Domain.Repositories
{
    /// <summary>
    /// Репозиторий для работы со справочником ингредиентов.
    /// </summary>
    public interface IIngredientRepository
    {
        /// <summary>
        /// Находит ингредиент по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор ингредиента.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Ingredient"/>, если запись найдена; иначе <see langword="null"/>.
        /// </returns>
        Task<Ingredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Находит ингредиент по точному совпадению названия.
        /// Используется для проверки уникальности перед созданием новой записи (UC-DSH-111).
        /// </summary>
        /// <param name="name">Название ингредиента.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Ingredient"/>, если запись с таким названием найдена; иначе <see langword="null"/>.
        /// </returns>
        Task<Ingredient?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает все активные ингредиенты (<see cref="Ingredient.IsActive"/> = <see langword="true"/>).
        /// Используется для admin-каталога и autocomplete-подсказок при добавлении ингредиента в рецепт.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Список активных ингредиентов, доступный только для чтения.</returns>
        Task<IReadOnlyList<Ingredient>> ListActiveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавляет новый ингредиент в хранилище. Вызывается из admin-команды UC-DSH-111.
        /// </summary>
        /// <param name="ingredient">Ингредиент для сохранения.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task AddAsync(Ingredient ingredient, CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохраняет изменения в хранилище (Unit of Work).
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
