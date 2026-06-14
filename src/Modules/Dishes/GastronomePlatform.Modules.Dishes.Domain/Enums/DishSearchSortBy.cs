namespace GastronomePlatform.Modules.Dishes.Domain.Enums
{
    /// <summary>
    /// Способ сортировки результатов каталожного поиска (UC-DSH-054 SearchDishes).
    /// </summary>
    public enum DishSearchSortBy
    {
        /// <summary>Сортировка по <c>Dish.PublishedAt</c> убыванию — свежие сверху. По умолчанию.</summary>
        Newest = 0,

        /// <summary>Сортировка по <c>Dish.RatingAvg</c> убыванию — высокий рейтинг сверху.</summary>
        RatingDesc = 1,

        /// <summary>Сортировка по <c>Dish.ViewsCount</c> убыванию — самые просматриваемые сверху.</summary>
        ViewsDesc = 2,
    }
}
