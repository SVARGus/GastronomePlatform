using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Domain.Repositories
{
    /// <summary>
    /// Репозиторий для работы с агрегатом блюда.
    /// </summary>
    public interface IDishRepository
    {
        /// <summary>
        /// Находит блюдо по идентификатору — только корневые поля Dish, без
        /// <see cref="Dish.Recipe"/> и подколлекций. Используется в командах модификации
        /// карточки (UC-DSH-002), статусных переходах (UC-DSH-005, UC-DSH-006) и других UC,
        /// где Recipe не требуется.
        /// </summary>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Dish"/>, если запись найдена; иначе <see langword="null"/>.
        /// </returns>
        Task<Dish?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Находит блюдо с подгруженным <see cref="Dish.Recipe"/> и его 1:1-связками
        /// (<c>Timing</c>, <c>Yield</c>, <c>Nutrition</c>), но без подколлекций
        /// <c>RecipeStep</c> и <c>RecipeIngredient</c>. Используется в командах модификации
        /// простых полей рецепта (UC-DSH-003) и в операциях обновления Timing/Yield/Nutrition.
        /// </summary>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Dish"/> с подгруженным <see cref="Dish.Recipe"/>, если запись найдена;
        /// иначе <see langword="null"/>.
        /// </returns>
        Task<Dish?> GetByIdWithRecipeAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Находит блюдо с полностью загруженным агрегатом: <see cref="Dish.Recipe"/>,
        /// его 1:1-связки и подколлекции (<c>RecipeStep</c>, <c>RecipeIngredient</c>),
        /// плюс связки тегов и категорий. Используется в UC-DSH-004 (Publish) для проверки
        /// инвариантов и сборки JSON-снепшота, а также в полной карточке для редактирования.
        /// </summary>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Dish"/> с полностью загруженным агрегатом, если запись найдена;
        /// иначе <see langword="null"/>.
        /// </returns>
        Task<Dish?> GetByIdWithFullRecipeAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Находит блюдо с подгруженной коллекцией <see cref="Dish.Categories"/>
        /// (без <c>Recipe</c> и <c>Tags</c>). Используется в командах replace-семантики
        /// набора категорий (UC-DSH-007): без явной подгрузки коллекции EF Core не
        /// отследит удаление существующих записей <c>DishCategory</c> при
        /// <c>_categories.Clear()</c> в Domain.
        /// </summary>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Dish"/> с подгруженной коллекцией <see cref="Dish.Categories"/>,
        /// если запись найдена; иначе <see langword="null"/>.
        /// </returns>
        Task<Dish?> GetByIdWithCategoriesAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Находит блюдо по уникальному <see cref="Dish.Slug"/>.
        /// Используется для публичного доступа к карточке блюда
        /// (UC-DSH-050, UC-DSH-051, UC-DSH-052).
        /// </summary>
        /// <param name="slug">URL-friendly идентификатор блюда.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Dish"/>, если запись с таким slug найдена; иначе <see langword="null"/>.
        /// </returns>
        Task<Dish?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает постраничный список черновиков (<c>Status = Draft</c>) указанного автора,
        /// отсортированный по <see cref="Dish.UpdatedAt"/> по убыванию. Подколлекции
        /// <c>Recipe</c>, <c>Categories</c>, <c>Tags</c> не загружаются. Используется
        /// в UC-DSH-053 (GetMyDrafts).
        /// </summary>
        /// <param name="authorUserId">Идентификатор автора (текущий пользователь).</param>
        /// <param name="page">Номер страницы, начиная с 1.</param>
        /// <param name="pageSize">Количество элементов на странице.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// Кортеж: <c>Items</c> — элементы запрошенной страницы (может быть пустым),
        /// <c>TotalCount</c> — общее количество черновиков автора без учёта пагинации.
        /// </returns>
        Task<(IReadOnlyList<Dish> Items, int TotalCount)> ListDraftsByAuthorAsync(
            Guid authorUserId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверяет, существует ли блюдо с указанным <see cref="Dish.Slug"/>.
        /// Используется при создании черновика (UC-DSH-001) для разрешения коллизий
        /// автоматически сгенерированного slug — Application Handler добавляет суффикс
        /// (<c>-2</c>, <c>-3</c>, …) до тех пор, пока метод не вернёт <see langword="false"/>.
        /// </summary>
        /// <param name="slug">URL-friendly идентификатор для проверки.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see langword="true"/>, если блюдо с таким slug уже существует;
        /// иначе <see langword="false"/>.
        /// </returns>
        Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавляет новое блюдо в хранилище. Вызывается из команды создания черновика
        /// (UC-DSH-001) после генерации уникального slug и валидации входных данных.
        /// </summary>
        /// <param name="dish">Блюдо для сохранения.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task AddAsync(Dish dish, CancellationToken cancellationToken = default);

        /// <summary>
        /// Атомарно увеличивает счётчик просмотров <see cref="Dish.ViewsCount"/> на 1
        /// у блюда со статусом <see cref="Domain.Enums.DishStatus.Published"/>. Используется
        /// в UC-DSH-070 IncrementDishViews. Реализация — один <c>UPDATE</c> без загрузки
        /// агрегата (EF Core 8 <c>ExecuteUpdateAsync</c>). Условие
        /// <c>Status = Published</c> зашито в <c>WHERE</c>: 0 затронутых строк означает
        /// «блюда нет» или «блюдо не опубликовано» — обе ситуации интерпретируются как 404,
        /// чтобы публичный эндпоинт не раскрывал существование черновиков.
        /// </summary>
        /// <param name="dishId">Идентификатор блюда.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// Количество затронутых строк: 1 при успешном инкременте опубликованного блюда,
        /// 0 если блюдо отсутствует или не находится в <c>Published</c>.
        /// </returns>
        Task<int> IncrementViewsAsync(Guid dishId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохраняет изменения в хранилище (Unit of Work).
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
