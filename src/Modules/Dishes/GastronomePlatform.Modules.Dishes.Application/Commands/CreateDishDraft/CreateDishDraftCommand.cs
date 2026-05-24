using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.CreateDishDraft
{
    /// <summary>
    /// Команда создания черновика блюда (UC-DSH-001).
    /// </summary>
    /// <remarks>
    /// Возвращает <see cref="CreateDishDraftResult"/> с идентификатором и slug-ом
    /// созданного блюда. Slug генерируется автоматически на стороне Application
    /// из <paramref name="Name"/>; коллизии разрешаются добавлением суффикса <c>-N</c>.
    /// Обёртка <c>Result&lt;&gt;</c> добавляется автоматически интерфейсом
    /// <see cref="ICommand{TResponse}"/>.
    /// </remarks>
    /// <param name="Name">Отображаемое название блюда (3–200 символов).</param>
    /// <param name="DifficultyLevel">Уровень сложности приготовления.</param>
    /// <param name="CostEstimate">Грубая оценка стоимости блюда.</param>
    /// <param name="ShortDescription">Краткая подводка для карточек каталога. Опционально, до 500 символов.</param>
    /// <param name="Description">Полное описание блюда (markdown). Опционально, до 4000 символов.</param>
    /// <param name="DietLabelsMask">Битовая маска диетических меток. Опционально.</param>
    /// <param name="HistoryText">Историко-культурный контекст блюда. Опционально, до 4000 символов.</param>
    public sealed record CreateDishDraftCommand(
        string Name,
        DifficultyLevel DifficultyLevel,
        CostEstimate CostEstimate,
        string? ShortDescription,
        string? Description,
        DietLabels? DietLabelsMask,
        string? HistoryText) : ICommand<CreateDishDraftResult>;
}
