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

        public static readonly Error DishAlreadyPublished =
            Error.Conflict("DISHES.DISH_ALREADY_PUBLISHED",
                "Блюдо уже опубликовано, и в нём нет несохранённых изменений.");

        public static readonly Error DishAlreadyArchived =
            Error.Conflict("DISHES.DISH_ALREADY_ARCHIVED", "Блюдо уже архивировано.");

        public static readonly Error DishNotPublished =
            Error.Conflict("DISHES.DISH_NOT_PUBLISHED",
                "Снять с публикации можно только опубликованное блюдо.");

        public static readonly Error CannotPublishArchivedDish =
            Error.Conflict("DISHES.CANNOT_PUBLISH_ARCHIVED_DISH",
                "Архивированное блюдо нельзя опубликовать.");

        public static readonly Error MainImageRequiredForPublish =
            Error.Conflict("DISHES.MAIN_IMAGE_REQUIRED_FOR_PUBLISH",
                "Для публикации блюда необходимо загрузить главное фото.");

        public static readonly Error StepsRequiredForPublish =
            Error.Conflict("DISHES.STEPS_REQUIRED_FOR_PUBLISH",
                "Для публикации блюда рецепт должен содержать хотя бы один шаг.");

        public static readonly Error IngredientsRequiredForPublish =
            Error.Conflict("DISHES.INGREDIENTS_REQUIRED_FOR_PUBLISH",
                "Для публикации блюда рецепт должен содержать хотя бы один ингредиент.");

        public static readonly Error TimingRequiredForPublish =
            Error.Conflict("DISHES.TIMING_REQUIRED_FOR_PUBLISH",
                "Для публикации блюда требуется указать общее время приготовления.");

        public static readonly Error InvalidServingsDefault =
            Error.Conflict("DISHES.INVALID_SERVINGS_DEFAULT",
                "Количество порций по умолчанию должно быть не меньше 1.");

        public static readonly Error InvalidTiming =
            Error.Conflict("DISHES.INVALID_TIMING",
                "Время приготовления должно быть неотрицательным.");

        public static readonly Error InvalidYield =
            Error.Conflict("DISHES.INVALID_YIELD",
                "Выход блюда: количество порций должно быть не меньше 1, остальные значения — неотрицательные.");
    }
}
