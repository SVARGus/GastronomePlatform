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
        /// Возвращает словарь маркеров (аллергены + конфликтующие диет-метки) для
        /// набора ингредиентов справочника. Используется агрегатом <see cref="Dish"/>
        /// при перерасчёте денормализованных полей <c>AllergensMask</c> и
        /// <c>DietLabelsMask</c>, а также при валидации UC-DSH-009 SetDietLabels.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Запросом одной поездкой собираются оба маркера — это уменьшает
        /// количество round-trip-ов и упрощает Domain-сигнатуру:
        /// <c>RecalculateDishMarkers</c> и <c>SetDietLabels</c> принимают
        /// словарь одного и того же типа.
        /// </para>
        /// <para>
        /// Идентификаторы, отсутствующие в БД (например, ингредиент уже удалён),
        /// в результирующий словарь не попадают. Для них агрегат интерпретирует
        /// маркеры как <see cref="GastronomePlatform.Modules.Dishes.Domain.Enums.AllergenType.None"/>
        /// и <see cref="GastronomePlatform.Modules.Dishes.Domain.Enums.DietLabels.None"/>
        /// (отсутствие маркеров — самая консервативная интерпретация).
        /// </para>
        /// </remarks>
        /// <param name="ingredientIds">Уникальные идентификаторы catalog-ингредиентов
        /// рецепта. Пустая коллекция допустима — возвращается пустой словарь.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Словарь <c>IngredientId → IngredientMarkers</c>.</returns>
        Task<IReadOnlyDictionary<Guid, IngredientMarkers>> GetMarkersByIdsAsync(
            IReadOnlyCollection<Guid> ingredientIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохраняет изменения в хранилище (Unit of Work).
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
