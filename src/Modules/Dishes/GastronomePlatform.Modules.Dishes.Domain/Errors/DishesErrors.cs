using GastronomePlatform.Common.Domain.Results;

namespace GastronomePlatform.Modules.Dishes.Domain.Errors
{
    /// <summary>
    /// Доменные ошибки модуля Dishes.
    /// </summary>
    public static class DishesErrors
    {
        public static readonly Error DishNotFound =
            Error.NotFound("DISHES.DISH_NOT_FOUND", "Блюдо не найдено.");
    }
}
