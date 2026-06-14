using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos
{
    /// <summary>
    /// Превью-карточка опубликованного блюда в списках каталога
    /// (UC-DSH-055 GetDishesByAuthor, UC-DSH-054 SearchDishes).
    /// Не содержит вложенный <c>Recipe</c> и полные тексты карточки.
    /// </summary>
    /// <param name="Id">Идентификатор блюда.</param>
    /// <param name="AuthorUserId">Идентификатор автора (для построения ссылок и атрибуции).</param>
    /// <param name="Slug">URL-friendly идентификатор.</param>
    /// <param name="Name">Отображаемое название блюда.</param>
    /// <param name="ShortDescription">Краткая подводка. <see langword="null"/>, если не задана.</param>
    /// <param name="MainImageId">Идентификатор главного фото в Media. <see langword="null"/>, если не задано.</param>
    /// <param name="DifficultyLevel">Уровень сложности.</param>
    /// <param name="CostEstimate">Грубая оценка стоимости.</param>
    /// <param name="DietLabelsMask">Битовая маска диетических меток автора.</param>
    /// <param name="AllergensMask">Битовая маска аллергенов из состава ингредиентов.</param>
    /// <param name="HasUnverifiedAllergens">Флаг неполной маски аллергенов (есть freeform-ингредиенты).</param>
    /// <param name="RatingAvg">Средний рейтинг (0..5).</param>
    /// <param name="RatingCount">Количество оценок.</param>
    /// <param name="ViewsCount">Количество просмотров карточки.</param>
    /// <param name="FavoritesCount">Количество добавлений в избранное.</param>
    /// <param name="PublishedAt">Момент последней публикации (для сортировки и индикаторов «новинка»).</param>
    /// <param name="CreatedAt">Момент создания блюда.</param>
    public sealed record DishCardListItemDto(
        Guid Id,
        Guid AuthorUserId,
        string Slug,
        string Name,
        string? ShortDescription,
        Guid? MainImageId,
        DifficultyLevel DifficultyLevel,
        CostEstimate CostEstimate,
        DietLabels DietLabelsMask,
        AllergenType AllergensMask,
        bool HasUnverifiedAllergens,
        decimal RatingAvg,
        int RatingCount,
        long ViewsCount,
        int FavoritesCount,
        DateTimeOffset? PublishedAt,
        DateTimeOffset CreatedAt);
}
