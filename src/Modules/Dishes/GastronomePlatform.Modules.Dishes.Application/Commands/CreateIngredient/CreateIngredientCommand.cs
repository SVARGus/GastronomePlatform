using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.CreateIngredient
{
    /// <summary>
    /// Команда создания записи справочника ингредиентов (UC-DSH-110).
    /// </summary>
    /// <remarks>
    /// Авторизация — роль <c>Admin</c>. Проверка на эндпоинте через
    /// <c>[Authorize(Roles = PlatformRoles.ADMIN)]</c>.
    /// </remarks>
    /// <param name="Name">Уникальное название продукта.</param>
    /// <param name="PluralName">Форма родительного падежа. Опционально.</param>
    /// <param name="Description">Описание (markdown). Опционально.</param>
    /// <param name="ImageMediaId">Идентификатор изображения в Media. Опционально.</param>
    /// <param name="IsLiquid">Флаг «продукт жидкий». При <see langword="true"/> обязателен <paramref name="DensityApprox"/>.</param>
    /// <param name="DensityApprox">Плотность, г/мл. Обязательна при <paramref name="IsLiquid"/>.</param>
    /// <param name="IsAllergen">Флаг «продукт-аллерген». При <see langword="true"/> обязателен <paramref name="AllergenType"/>.</param>
    /// <param name="AllergenType">Тип аллергена. Обязателен при <paramref name="IsAllergen"/>.</param>
    /// <param name="DietConflictsMask">Маска конфликтующих диет-меток. <c>None</c> — нейтральный продукт.</param>
    /// <param name="BaseMeasureUnitId">Идентификатор базовой единицы хранения.</param>
    /// <param name="DefaultNutritionId">Идентификатор КБЖУ по умолчанию. Опционально.</param>
    public sealed record CreateIngredientCommand(
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
        Guid? DefaultNutritionId) : ICommand<CreateIngredientResult>;
}
