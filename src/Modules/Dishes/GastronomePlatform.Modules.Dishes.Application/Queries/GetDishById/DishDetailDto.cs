using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishById
{
    /// <summary>
    /// Публичная карточка блюда (UC-DSH-050). Без вложенного <c>Recipe</c> —
    /// для рецепта используется отдельный <c>UC-DSH-052</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Структура DTO рассчитана сразу под полную двухслойную модель
    /// (working ↔ published snapshot). На Этапе 2 без UC-DSH-004 PublishDish
    /// в реальности активна только ветка «автор/admin читает свой Draft» —
    /// <see cref="IsPublishedVersion"/> = <see langword="false"/>,
    /// <see cref="HasUnsavedChanges"/> = <see langword="false"/>. После реализации
    /// UC-DSH-004 заработают остальные комбинации полей без изменения контракта.
    /// </para>
    /// </remarks>
    /// <param name="Id">Идентификатор блюда.</param>
    /// <param name="AuthorUserId">Идентификатор автора. Постоянный (не обнуляется при удалении аккаунта).</param>
    /// <param name="Name">Отображаемое название блюда.</param>
    /// <param name="Slug">URL-friendly идентификатор блюда.</param>
    /// <param name="ShortDescription">Краткая подводка. <see langword="null"/>, если не задана.</param>
    /// <param name="Description">Полное описание (markdown). <see langword="null"/>, если не задано.</param>
    /// <param name="HistoryText">Историко-культурный контекст. <see langword="null"/>, если не задан.</param>
    /// <param name="MainImageId">Идентификатор главного фото в Media. <see langword="null"/>, если не задано.</param>
    /// <param name="Status">Текущий статус блюда. Возвращается всем (не приватная мета).</param>
    /// <param name="DifficultyLevel">Уровень сложности приготовления.</param>
    /// <param name="CostEstimate">Грубая оценка стоимости блюда.</param>
    /// <param name="OwnerType">Тип владельца (User / Chef / Restaurant).</param>
    /// <param name="DietLabelsMask">Битовая маска диетических меток.</param>
    /// <param name="AllergensMask">Битовая маска аллергенов из состава ингредиентов.</param>
    /// <param name="HasUnverifiedAllergens">
    /// <see langword="true"/>, если в рецепте есть freeform-ингредиенты и
    /// <see cref="AllergensMask"/> может быть неполной.
    /// </param>
    /// <param name="RatingAvg">Средний рейтинг блюда (0–5).</param>
    /// <param name="RatingCount">Количество оценок.</param>
    /// <param name="ViewsCount">Количество просмотров карточки.</param>
    /// <param name="FavoritesCount">Количество добавлений в избранное.</param>
    /// <param name="PublishedAt">
    /// Момент последней публикации. <see langword="null"/>, если блюдо никогда не публиковалось
    /// или снято с публикации.
    /// </param>
    /// <param name="CreatedAt">Момент создания блюда.</param>
    /// <param name="UpdatedAt">Момент последнего изменения автором.</param>
    /// <param name="IsPublishedVersion">
    /// <see langword="true"/>, если данные пришли из <c>PublishedVersionData</c> (публичный снепшот);
    /// <see langword="false"/>, если из основных таблиц (рабочая версия, доступная только автору/admin).
    /// </param>
    /// <param name="HasUnsavedChanges">
    /// Для автора/admin: <see langword="true"/>, если <c>UpdatedAt &gt; PublishedAt</c> —
    /// в рабочей версии есть несохранённые правки. Для остальных пользователей —
    /// <see langword="null"/> (приватная информация о состоянии редактирования).
    /// </param>
    public sealed record DishDetailDto(
        Guid Id,
        Guid AuthorUserId,
        string Name,
        string Slug,
        string? ShortDescription,
        string? Description,
        string? HistoryText,
        Guid? MainImageId,
        DishStatus Status,
        DifficultyLevel DifficultyLevel,
        CostEstimate CostEstimate,
        OwnerType OwnerType,
        DietLabels DietLabelsMask,
        AllergenType AllergensMask,
        bool HasUnverifiedAllergens,
        decimal RatingAvg,
        int RatingCount,
        long ViewsCount,
        int FavoritesCount,
        DateTimeOffset? PublishedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        bool IsPublishedVersion,
        bool? HasUnsavedChanges);
}
