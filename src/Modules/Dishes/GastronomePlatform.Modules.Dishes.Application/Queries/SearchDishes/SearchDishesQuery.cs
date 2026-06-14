using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.SearchDishes
{
    /// <summary>
    /// Запрос каталожного поиска опубликованных блюд (UC-DSH-054). Анонимный публичный
    /// эндпоинт. Все поля фильтра опциональны; обязательный фильтр —
    /// <c>PublishedVersionData IS NOT NULL</c> применяется на стороне репозитория.
    /// </summary>
    /// <param name="Text">Подстрока поиска по <c>Name</c> и <c>ShortDescription</c>. Опционально.</param>
    /// <param name="CategoryIds">Идентификаторы категорий (OR). Опционально.</param>
    /// <param name="TagIds">Идентификаторы тегов (OR). Опционально.</param>
    /// <param name="DietLabelsMask">Битовая маска требуемых меток (AND). <c>None</c> или <see langword="null"/> — без фильтра.</param>
    /// <param name="Difficulties">Уровни сложности (IN). Опционально.</param>
    /// <param name="Costs">Оценки стоимости (IN). Опционально.</param>
    /// <param name="MinRating">Минимальный <c>Dish.RatingAvg</c>. Опционально.</param>
    /// <param name="SortBy">Способ сортировки. По умолчанию <see cref="DishSearchSortBy.Newest"/>.</param>
    /// <param name="Page">Номер страницы, начиная с 1.</param>
    /// <param name="PageSize">Размер страницы (1..25).</param>
    public sealed record SearchDishesQuery(
        string? Text,
        IReadOnlyList<Guid>? CategoryIds,
        IReadOnlyList<Guid>? TagIds,
        DietLabels? DietLabelsMask,
        IReadOnlyList<DifficultyLevel>? Difficulties,
        IReadOnlyList<CostEstimate>? Costs,
        decimal? MinRating,
        DishSearchSortBy SortBy,
        int Page,
        int PageSize) : IQuery<SearchDishesResult>;
}
