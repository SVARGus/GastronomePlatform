using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Domain.Repositories
{
    /// <summary>
    /// Репозиторий для работы со справочником сортов/видов ингредиентов.
    /// На текущий момент — минимальный набор операций (Stub).
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
        /// Получает словарь «Id ингредиента → маска аллергенов» для указанного набора Id.
        /// Используется в Application Handler'ах модификации состава рецепта
        /// (UC-DSH-030, UC-DSH-031, UC-DSH-032) перед вызовом
        /// <c>Dish.RecalculateAllergens(...)</c>.
        /// </summary>
        /// <param name="ingredientIds">Список идентификаторов ингредиентов.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// Словарь Id → <see cref="AllergenType"/>. Ингредиенты без аллергенов
        /// попадают в словарь со значением <see cref="AllergenType.None"/>.
        /// Идентификаторы, не найденные в БД, в результирующем словаре отсутствуют.
        /// </returns>
        Task<IReadOnlyDictionary<Guid, AllergenType>> GetAllergensByIdsAsync(
            IReadOnlyCollection<Guid> ingredientIds,
            CancellationToken cancellationToken = default);

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
