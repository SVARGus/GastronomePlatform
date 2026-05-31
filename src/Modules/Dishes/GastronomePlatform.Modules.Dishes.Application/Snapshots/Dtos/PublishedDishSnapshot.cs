using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Snapshots.Dtos
{
    /// <summary>
    /// Корневой объект jsonb-снепшота, сохраняемого в <c>Dish.PublishedVersionData</c>
    /// при публикации блюда (UC-DSH-004). Содержит публичную карточку и полный рецепт
    /// на момент публикации.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MVP-формат (см. UC-DSH-004): справочные сущности (категории, теги, ингредиенты,
    /// единицы измерения) представлены только идентификаторами. Имена/slug-и
    /// в снепшот не денормализуются — резолвятся потребителем при чтении.
    /// Миграция на денормализованный формат — задача UC-DSH-052 GetDishRecipe
    /// или Этапа 8+ rebuild snapshot.
    /// </para>
    /// <para>
    /// В снепшот <b>не</b> включаются:
    /// </para>
    /// <list type="bullet">
    ///   <item>Идентификатор блюда (<c>Dish.Id</c>) — известен по самой записи в БД.</item>
    ///   <item>Lifecycle-метаданные (<c>Status</c>, <c>CreatedAt</c>, <c>UpdatedAt</c>,
    ///         <c>PublishedAt</c>, <c>PublishedVersionUpdatedAt</c>) — хранятся
    ///         в полях самой записи <c>Dish</c>.</item>
    ///   <item>Runtime-счётчики (<c>RatingAvg</c>, <c>RatingCount</c>, <c>ViewsCount</c>,
    ///         <c>FavoritesCount</c>) — это «живые» значения, обновляемые событиями,
    ///         им нечего делать в иммутабельном слепке.</item>
    ///   <item>Модерация (<c>ModerationStatus</c>) — поле жизненного цикла, не публичный контент.</item>
    /// </list>
    /// </remarks>
    /// <param name="Name">Отображаемое название блюда.</param>
    /// <param name="Slug">URL-friendly идентификатор блюда.</param>
    /// <param name="ShortDescription">Краткая подводка для каталога. Опционально.</param>
    /// <param name="Description">Полное описание (markdown). Опционально.</param>
    /// <param name="HistoryText">Историко-культурный контекст. Опционально.</param>
    /// <param name="MainImageId">Идентификатор главного фото в Media. Обязательно
    /// для публикации (инвариант проверяется <c>Dish.Publish</c>).</param>
    /// <param name="DifficultyLevel">Уровень сложности приготовления.</param>
    /// <param name="CostEstimate">Грубая оценка стоимости блюда.</param>
    /// <param name="OwnerType">Тип владельца, денормализованный из ролей автора
    /// на момент последнего обновления карточки.</param>
    /// <param name="DietLabelsMask">Битовая маска диетических меток (например, <c>Vegan | GlutenFree</c>).</param>
    /// <param name="AllergensMask">Битовая маска аллергенов, рассчитанная из состава ингредиентов.</param>
    /// <param name="HasUnverifiedAllergens"><see langword="true"/>, если в рецепте есть freeform-позиции
    /// и <see cref="AllergensMask"/> может быть неполной.</param>
    /// <param name="Recipe">Снепшот рецепта со всеми вложенными сущностями.</param>
    /// <param name="Categories">Категории блюда на момент публикации (0..3).</param>
    /// <param name="Tags">Теги блюда на момент публикации (0..20).</param>
    public sealed record PublishedDishSnapshot(
        string Name,
        string Slug,
        string? ShortDescription,
        string? Description,
        string? HistoryText,
        Guid? MainImageId,
        DifficultyLevel DifficultyLevel,
        CostEstimate CostEstimate,
        OwnerType OwnerType,
        DietLabels DietLabelsMask,
        AllergenType AllergensMask,
        bool HasUnverifiedAllergens,
        PublishedRecipeDto Recipe,
        IReadOnlyList<PublishedCategorySnapshotDto> Categories,
        IReadOnlyList<PublishedTagSnapshotDto> Tags);
}
