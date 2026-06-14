using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos
{
    /// <summary>
    /// DTO ингредиента для autocomplete-подсказок при добавлении в рецепт
    /// (UC-DSH-062 SearchIngredients) и карточки в энциклопедии ингредиентов
    /// (UC-DSH-063 GetIngredientById).
    /// </summary>
    /// <param name="Id">Идентификатор ингредиента.</param>
    /// <param name="Name">Название (уникальное в справочнике).</param>
    /// <param name="PluralName">Форма родительного падежа. Опционально.</param>
    /// <param name="Description">Развёрнутое описание (markdown). Опционально.</param>
    /// <param name="ImageMediaId">Идентификатор изображения в Media. Опционально.</param>
    /// <param name="IsLiquid">Флаг «продукт жидкий».</param>
    /// <param name="DensityApprox">Приближённая плотность (г/мл), если задана.</param>
    /// <param name="IsAllergen">Флаг «продукт-аллерген».</param>
    /// <param name="AllergenType">Тип аллергена (если задан).</param>
    /// <param name="DietConflictsMask">Маска диетических меток, с которыми конфликтует ингредиент.</param>
    /// <param name="BaseMeasureUnitId">Идентификатор базовой единицы хранения.</param>
    /// <param name="DefaultNutritionId">Идентификатор КБЖУ по умолчанию (опционально).</param>
    /// <param name="IsActive">Признак активности.</param>
    public sealed record IngredientDto(
        Guid Id,
        string Name,
        string? PluralName,
        string? Description,
        Guid? ImageMediaId,
        bool IsLiquid,
        decimal? DensityApprox,
        bool IsAllergen,
        AllergenType? AllergenType,
        DietLabels DietConflictsMask,
        Guid BaseMeasureUnitId,
        Guid? DefaultNutritionId,
        bool IsActive);
}
