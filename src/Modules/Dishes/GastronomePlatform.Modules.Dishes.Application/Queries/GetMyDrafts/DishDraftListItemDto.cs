using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetMyDrafts
{
    /// <summary>
    /// Превью-карточка черновика блюда в списке «мои черновики» (UC-DSH-053).
    /// Не содержит вложенный <c>Recipe</c>, полные тексты и денормализованную
    /// публичную статистику — для детального просмотра используется отдельный UC.
    /// </summary>
    /// <param name="Id">Идентификатор блюда.</param>
    /// <param name="Slug">URL-friendly идентификатор блюда для построения ссылки в UI.</param>
    /// <param name="Name">Отображаемое название блюда.</param>
    /// <param name="ShortDescription">Краткая подводка для карточки. <see langword="null"/>, если не задана.</param>
    /// <param name="MainImageId">Идентификатор главного фото в Media. <see langword="null"/>, если не задано.</param>
    /// <param name="DifficultyLevel">Уровень сложности приготовления.</param>
    /// <param name="CostEstimate">Грубая оценка стоимости блюда.</param>
    /// <param name="DietLabelsMask">Битовая маска диетических меток автора.</param>
    /// <param name="AllergensMask">Битовая маска аллергенов, выведенная из состава ингредиентов.</param>
    /// <param name="HasUnverifiedAllergens">
    /// <see langword="true"/>, если рецепт содержит ингредиенты со свободным текстом
    /// и маска аллергенов может быть неполной.
    /// </param>
    /// <param name="CreatedAt">Момент создания черновика.</param>
    /// <param name="UpdatedAt">Момент последнего изменения автором.</param>
    public sealed record DishDraftListItemDto(
        Guid Id,
        string Slug,
        string Name,
        string? ShortDescription,
        Guid? MainImageId,
        DifficultyLevel DifficultyLevel,
        CostEstimate CostEstimate,
        DietLabels DietLabelsMask,
        AllergenType AllergensMask,
        bool HasUnverifiedAllergens,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
