using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UpdateRecipe
{
    /// <summary>
    /// Команда обновления простых полей рецепта блюда (UC-DSH-003).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Содержит только поля, размещённые непосредственно на <c>Recipe</c>: вводный текст,
    /// порции по умолчанию, признак алкоголя, советы автора, рекомендации по сервировке
    /// и заметки. Шаги, ингредиенты, тайминг, выход и КБЖУ имеют отдельные UC
    /// и отдельные команды (UC-DSH-020..023, UC-DSH-030..033, UC-DSH-040..042).
    /// </para>
    /// <para>
    /// Правка не трогает <c>Dish.PublishedVersionData</c> — для отражения изменений
    /// в публичной версии требуется отдельный вызов UC-DSH-004 PublishDish.
    /// </para>
    /// <para>
    /// Возвращает <see cref="Common.Domain.Results.Result"/> без значения — при успехе
    /// эндпоинт отдаёт <c>204 No Content</c>.
    /// </para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда, рецепт которого обновляется.</param>
    /// <param name="IntroductionText">Вводный текст рецепта. <see langword="null"/> — очистить.</param>
    /// <param name="ServingsDefault">Количество порций по умолчанию (не меньше 1).</param>
    /// <param name="IsAlcoholic">Признак содержания алкоголя в рецепте.</param>
    /// <param name="AuthorTips">Советы автора по приготовлению. <see langword="null"/> — очистить.</param>
    /// <param name="ServingSuggestions">Рекомендации по сервировке. <see langword="null"/> — очистить.</param>
    /// <param name="Notes">Дополнительные заметки. <see langword="null"/> — очистить.</param>
    public sealed record UpdateRecipeCommand(
        Guid DishId,
        string? IntroductionText,
        int ServingsDefault,
        bool IsAlcoholic,
        string? AuthorTips,
        string? ServingSuggestions,
        string? Notes) : ICommand;
}
