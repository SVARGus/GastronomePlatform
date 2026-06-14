using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Enums;

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
        /// Находит блюдо с подгруженной коллекцией <see cref="Dish.Tags"/>
        /// (без <c>Recipe</c> и <c>Categories</c>). Используется в командах replace-семантики
        /// набора тегов (UC-DSH-008): без явной подгрузки коллекции EF Core не
        /// отследит удаление существующих записей <c>DishTag</c> при
        /// <c>_tags.Clear()</c> в Domain.
        /// </summary>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Dish"/> с подгруженной коллекцией <see cref="Dish.Tags"/>,
        /// если запись найдена; иначе <see langword="null"/>.
        /// </returns>
        Task<Dish?> GetByIdWithTagsAsync(Guid id, CancellationToken cancellationToken = default);

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
        /// Возвращает постраничный список <b>опубликованных</b> блюд указанного автора
        /// (<c>PublishedVersionData IS NOT NULL</c>), отсортированный по
        /// <see cref="Dish.PublishedAt"/> по убыванию. Используется в UC-DSH-055
        /// GetDishesByAuthor — анонимный публичный эндпоинт страницы автора.
        /// Подколлекции <c>Recipe</c>, <c>Categories</c>, <c>Tags</c> не загружаются.
        /// </summary>
        /// <param name="authorUserId">Идентификатор автора.</param>
        /// <param name="page">Номер страницы, начиная с 1.</param>
        /// <param name="pageSize">Количество элементов на странице.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// Кортеж: <c>Items</c> — элементы запрошенной страницы (может быть пустым),
        /// <c>TotalCount</c> — общее количество публичных блюд автора без учёта пагинации.
        /// </returns>
        Task<(IReadOnlyList<Dish> Items, int TotalCount)> ListPublishedByAuthorAsync(
            Guid authorUserId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Каталожный поиск опубликованных блюд с фильтрами, сортировкой и пагинацией
        /// (UC-DSH-054 SearchDishes). Обязательный фильтр —
        /// <c>PublishedVersionData IS NOT NULL</c>; всё остальное опционально.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Поиск по тексту проходит через <c>ILIKE</c> по основным полям
        /// <see cref="Dish.Name"/> и <see cref="Dish.ShortDescription"/>. Это компромисс
        /// Этапа 2 (см. UC-DSH-054 §«Известные ограничения»). Поиск по jsonb-snapshot
        /// — задача Этапа 8+ (возможно через ElasticSearch/OpenSearch).
        /// </para>
        /// <para>
        /// Фильтры по категориям и тегам резолвятся через
        /// <see cref="DishCategoryPublished"/> и <see cref="DishTagPublished"/> —
        /// посетители видят опубликованный набор связок, а не рабочую копию.
        /// </para>
        /// <para>
        /// <see cref="Dish.DietLabelsMask"/> фильтруется битовым AND: блюдо должно иметь
        /// <b>все</b> запрошенные метки. Например, фильтр <c>Vegan | GlutenFree</c>
        /// отбирает только блюда, у которых оба бита установлены.
        /// </para>
        /// </remarks>
        /// <param name="text">Подстрока для поиска по имени и краткому описанию. <see langword="null"/> или пусто — без фильтра.</param>
        /// <param name="categoryIds">Идентификаторы категорий (OR). <see langword="null"/> или пусто — без фильтра.</param>
        /// <param name="tagIds">Идентификаторы тегов (OR). <see langword="null"/> или пусто — без фильтра.</param>
        /// <param name="dietLabelsMask">Битовая маска требуемых диетических меток (AND). <see langword="null"/> или <c>None</c> — без фильтра.</param>
        /// <param name="difficulties">Уровни сложности (IN). <see langword="null"/> или пусто — без фильтра.</param>
        /// <param name="costs">Оценки стоимости (IN). <see langword="null"/> или пусто — без фильтра.</param>
        /// <param name="minRating">Минимальный <see cref="Dish.RatingAvg"/>. <see langword="null"/> — без фильтра.</param>
        /// <param name="sortBy">Способ сортировки результатов.</param>
        /// <param name="page">Номер страницы, начиная с 1.</param>
        /// <param name="pageSize">Количество элементов на странице.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// Кортеж: <c>Items</c> — элементы запрошенной страницы (может быть пустым),
        /// <c>TotalCount</c> — общее количество подходящих блюд без учёта пагинации.
        /// </returns>
        Task<(IReadOnlyList<Dish> Items, int TotalCount)> SearchPublishedAsync(
            string? text,
            IReadOnlyCollection<Guid>? categoryIds,
            IReadOnlyCollection<Guid>? tagIds,
            DietLabels? dietLabelsMask,
            IReadOnlyCollection<DifficultyLevel>? difficulties,
            IReadOnlyCollection<CostEstimate>? costs,
            decimal? minRating,
            DishSearchSortBy sortBy,
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
        /// Массово обновляет <see cref="Dish.UpdatedAt"/> у блюд из указанного набора.
        /// Используется в admin-сценариях каскадного изменения связок (например,
        /// UC-DSH-131 DeleteTag) — у каждого затронутого блюда состав связок поменялся,
        /// и индикатор «есть несохранённые правки» должен сработать.
        /// </summary>
        /// <remarks>
        /// Реализуется через EF Core 8 <c>ExecuteUpdateAsync</c> одним <c>UPDATE</c>
        /// без загрузки агрегатов. Доменные события (<c>DishUpdatedEvent</c>) при этом
        /// не поднимаются — это сознательный компромисс admin-операций массового
        /// каскада: подписчиков на Этапе 2 нет, а грузить десятки агрегатов ради
        /// одного поля и события дорого. При появлении подписчиков (Этап 5+)
        /// потребуется явный механизм рассылки событий после batch-update.
        /// </remarks>
        /// <param name="dishIds">Идентификаторы блюд для обновления. Пустой набор — no-op.</param>
        /// <param name="utcNow">Новое значение <see cref="Dish.UpdatedAt"/>.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Количество затронутых строк.</returns>
        Task<int> BulkMarkAsUpdatedAsync(
            IReadOnlyCollection<Guid> dishIds,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохраняет изменения в хранилище (Unit of Work).
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
