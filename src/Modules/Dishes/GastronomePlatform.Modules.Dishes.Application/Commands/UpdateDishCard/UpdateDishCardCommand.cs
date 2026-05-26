using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UpdateDishCard
{
    /// <summary>
    /// Команда обновления основных полей публичной карточки блюда (UC-DSH-002).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Содержит только поля карточки. <c>DietLabelsMask</c>, <c>MainImageId</c>
    /// и история блюда (<c>HistoryText</c>) меняются отдельными командами:
    /// SetDietLabels (UC-DSH-009), ChangeMainImage (UC-DSH-011),
    /// UpdateHistory (UC-DSH-010).
    /// </para>
    /// <para>
    /// <c>OwnerType</c> не передаётся клиентом — резолвится Handler-ом
    /// из ролей текущего пользователя по тем же правилам, что и при создании
    /// (см. <c>OwnerTypeResolver</c>). При смене роли автора (например,
    /// повышение в Chef) следующий <c>UpdateCard</c> переопределит
    /// <c>OwnerType</c>; старые блюда без обновления остаются с прежним.
    /// </para>
    /// <para>
    /// Возвращает <see cref="Common.Domain.Results.Result"/> без значения —
    /// при успехе эндпоинт отдаёт <c>204 No Content</c>.
    /// </para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда для обновления.</param>
    /// <param name="Name">Новое название (3–200 символов).</param>
    /// <param name="ShortDescription">Краткая подводка. <see langword="null"/> — очистить.</param>
    /// <param name="Description">Полное описание (markdown). <see langword="null"/> — очистить.</param>
    /// <param name="DifficultyLevel">Уровень сложности.</param>
    /// <param name="CostEstimate">Оценка стоимости.</param>
    public sealed record UpdateDishCardCommand(
        Guid DishId,
        string Name,
        string? ShortDescription,
        string? Description,
        DifficultyLevel DifficultyLevel,
        CostEstimate CostEstimate) : ICommand;
}
