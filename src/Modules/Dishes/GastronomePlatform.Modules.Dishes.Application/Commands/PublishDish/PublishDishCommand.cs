using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.PublishDish
{
    /// <summary>
    /// Команда публикации блюда (UC-DSH-004).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Покрывает три бизнес-ветки публикации (см. <c>use-cases/README.md</c> UC-DSH-004):
    /// первая публикация (<c>Draft → Published</c>), повторная публикация
    /// (<c>Published</c> с несохранёнными правками относительно последнего снепшота)
    /// и возврат с <c>Unpublished → Published</c>. Различение между ветками выполняет
    /// Domain (<c>Dish.Publish</c>) — Application лишь готовит снепшот и делегирует.
    /// </para>
    /// <para>
    /// Тело запроса не содержит входных полей: публикация — это переход состояния
    /// на основе уже сохранённого содержимого блюда. Все данные для снепшота берутся
    /// из текущего агрегата.
    /// </para>
    /// <para>
    /// Возвращает <see cref="Common.Domain.Results.Result"/> без значения — при успехе
    /// эндпоинт отдаёт <c>204 No Content</c>.
    /// </para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда, которое публикуется.</param>
    public sealed record PublishDishCommand(Guid DishId) : ICommand;
}
