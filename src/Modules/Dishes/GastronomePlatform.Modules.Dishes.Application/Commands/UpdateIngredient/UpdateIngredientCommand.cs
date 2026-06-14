using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UpdateIngredient
{
    /// <summary>
    /// Команда обновления записи справочника ингредиентов (UC-DSH-111).
    /// Заменяет все редактируемые поля; флаг <c>IsActive</c> через эту команду не меняется
    /// (отдельный UC-DSH-112). Авторизация — роль <c>Admin</c>.
    /// </summary>
    /// <param name="IngredientId">Идентификатор существующего ингредиента.</param>
    /// <param name="Name">Новое название.</param>
    /// <param name="PluralName">Форма родительного падежа. Опционально.</param>
    /// <param name="Description">Описание. Опционально.</param>
    /// <param name="ImageMediaId">Идентификатор изображения в Media. Опционально.</param>
    /// <param name="IsLiquid">Флаг «продукт жидкий».</param>
    /// <param name="DensityApprox">Плотность, г/мл. Обязательна при <paramref name="IsLiquid"/>.</param>
    /// <param name="IsAllergen">Флаг «продукт-аллерген».</param>
    /// <param name="AllergenType">Тип аллергена. Обязателен при <paramref name="IsAllergen"/>.</param>
    /// <param name="DietConflictsMask">Маска конфликтующих диет-меток.</param>
    /// <param name="BaseMeasureUnitId">Базовая единица хранения.</param>
    /// <param name="DefaultNutritionId">Идентификатор КБЖУ по умолчанию. Опционально.</param>
    public sealed record UpdateIngredientCommand(
        Guid IngredientId,
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
        Guid? DefaultNutritionId) : ICommand;
}
