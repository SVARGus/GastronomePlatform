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

        public static readonly Error NotDishOwner =
            Error.Forbidden("DISHES.NOT_DISH_OWNER",
                "Эту операцию может выполнить только автор блюда.");

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

        public static readonly Error StepNotFound =
            Error.NotFound("DISHES.STEP_NOT_FOUND", "Шаг рецепта не найден.");

        public static readonly Error InvalidTemperature =
            Error.Conflict("DISHES.INVALID_TEMPERATURE",
                "Температура приготовления должна быть в диапазоне от −30 до 300 градусов.");

        public static readonly Error InvalidTimerMinutes =
            Error.Conflict("DISHES.INVALID_TIMER_MINUTES",
                "Время таймера должно быть в диапазоне от 1 до 1440 минут.");

        public static readonly Error InvalidStepOrder =
            Error.Conflict("DISHES.INVALID_STEP_ORDER",
                "Список шагов для переупорядочивания должен содержать все шаги рецепта без дубликатов.");

        public static readonly Error RecipeIngredientNotFound =
            Error.NotFound("DISHES.RECIPE_INGREDIENT_NOT_FOUND",
                "Ингредиент рецепта не найден.");

        public static readonly Error InvalidIngredientComposition =
            Error.Conflict("DISHES.INVALID_INGREDIENT_COMPOSITION",
                "Ингредиент рецепта должен ссылаться на справочник ИЛИ задаваться свободным текстом, " +
                "но не оба сразу. Спецификация сорта допустима только при ссылке на справочник.");

        public static readonly Error DietLabelsConflictWithComposition =
            Error.Conflict("DISHES.DIET_LABELS_CONFLICT_WITH_COMPOSITION",
                "Запрошенные диетические метки конфликтуют с составом рецепта. " +
                "Снимите конфликтующие биты или измените состав ингредиентов.");

        public static readonly Error InvalidQuantity =
            Error.Conflict("DISHES.INVALID_QUANTITY",
                "Количество ингредиента должно быть строго положительным.");

        public static readonly Error InvalidIngredientOrder =
            Error.Conflict("DISHES.INVALID_INGREDIENT_ORDER",
                "Список ингредиентов для переупорядочивания должен содержать все позиции рецепта без дубликатов.");

        public static readonly Error CategoryLimitExceeded =
            Error.Conflict("DISHES.CATEGORY_LIMIT_EXCEEDED",
                "У блюда может быть не более 3 категорий.");

        public static readonly Error TagLimitExceeded =
            Error.Conflict("DISHES.TAG_LIMIT_EXCEEDED",
                "У блюда может быть не более 20 тегов.");

        public static readonly Error DuplicateCategoryId =
            Error.Conflict("DISHES.DUPLICATE_CATEGORY_ID",
                "Список категорий не должен содержать дубликатов.");

        public static readonly Error CategoryNotFound =
            Error.NotFound("DISHES.CATEGORY_NOT_FOUND",
                "Одна или несколько категорий не найдены в справочнике или деактивированы.");

        public static readonly Error TagNotFound =
            Error.NotFound("DISHES.TAG_NOT_FOUND",
                "Тег не найден в справочнике.");

        public static readonly Error DuplicateTagId =
            Error.Conflict("DISHES.DUPLICATE_TAG_ID",
                "Список тегов не должен содержать дубликатов.");

        public static readonly Error SlugGenerationExhausted =
            Error.Failure("DISHES.SLUG_GENERATION_EXHAUSTED",
                "Не удалось подобрать уникальный slug — превышен лимит попыток.");

        public static readonly Error IngredientNotFound =
            Error.NotFound("DISHES.INGREDIENT_NOT_FOUND",
                "Ингредиент не найден в справочнике.");

        public static readonly Error IngredientInactive =
            Error.Conflict("DISHES.INGREDIENT_INACTIVE",
                "Ингредиент деактивирован. Выберите активный ингредиент из справочника.");

        public static readonly Error IngredientSpecNotFound =
            Error.NotFound("DISHES.INGREDIENT_SPEC_NOT_FOUND",
                "Спецификация (сорт) ингредиента не найдена.");

        public static readonly Error IngredientSpecMismatch =
            Error.Conflict("DISHES.INGREDIENT_SPEC_MISMATCH",
                "Указанная спецификация принадлежит другому ингредиенту.");

        public static readonly Error MeasureUnitNotFound =
            Error.NotFound("DISHES.MEASURE_UNIT_NOT_FOUND",
                "Единица измерения не найдена в справочнике.");
    }
}
