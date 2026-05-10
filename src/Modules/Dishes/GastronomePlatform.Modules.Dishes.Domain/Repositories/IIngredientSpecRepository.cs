using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Domain.Repositories
{
    /// <summary>
    /// Репозиторий для работы со справочником сортов/видов ингредиентов.
    /// На Этапе 2 — минимальный набор операций (Stub).
    /// </summary>
    public interface IIngredientSpecRepository
    {
        /// <summary>
        /// Находит сорт ингредиента по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор сорта.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="IngredientSpec"/>, если запись найдена; иначе <see langword="null"/>.
        /// </returns>
        Task<IngredientSpec?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает все сорта для указанного родительского ингредиента.
        /// Используется при отображении списка сортов в карточке ингредиента (UC расширения, Этап 8+).
        /// </summary>
        /// <param name="ingredientId">Идентификатор родительского ингредиента.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Список сортов, доступный только для чтения. Пустой, если сортов нет.</returns>
        Task<IReadOnlyList<IngredientSpec>> GetByIngredientIdAsync(Guid ingredientId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавляет новый сорт ингредиента в хранилище.
        /// </summary>
        /// <param name="ingredientSpec">Сорт для сохранения.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task AddAsync(IngredientSpec ingredientSpec, CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохраняет изменения в хранилище (Unit of Work).
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
