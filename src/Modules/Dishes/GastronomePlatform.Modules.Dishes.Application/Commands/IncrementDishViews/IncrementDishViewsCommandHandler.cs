using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.IncrementDishViews
{
    /// <summary>
    /// Обработчик команды <see cref="IncrementDishViewsCommand"/> (UC-DSH-070).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Поток выполнения:
    /// </para>
    /// <list type="number">
    ///   <item>Вызов <see cref="IDishRepository.IncrementViewsAsync"/> — один SQL
    ///         <c>UPDATE ... SET "ViewsCount" = "ViewsCount" + 1 WHERE "Id" = @id
    ///         AND "Status" = Published</c>.</item>
    ///   <item>Если затронуто 0 строк — блюдо отсутствует или не опубликовано;
    ///         возвращается <see cref="DishesErrors.DishNotFound"/> (HTTP 404).</item>
    ///   <item>1 строка — успех, <see cref="Result.Success()"/>.</item>
    /// </list>
    /// <para>
    /// Агрегат <c>Dish</c> не загружается; <c>UpdatedAtInterceptor</c> не задействуется
    /// (UPDATE идёт мимо <c>ChangeTracker</c>). Поле <c>UpdatedAt</c> исключено из триггера
    /// интерсептора отдельно — счётчик просмотров не должен влиять на индикатор
    /// «несохранённых правок» автора.
    /// </para>
    /// <para>
    /// TODO (когда появится бизнес-причина): фильтр самопросмотров автора — если
    /// <c>CurrentUserService.UserId == Dish.AuthorUserId</c>, не инкрементировать.
    /// Требует одного дополнительного SELECT или расширения <c>WHERE</c> условием
    /// <c>AuthorUserId &lt;&gt; @currentUserId</c>. См. запись в техдолге будущих этапов.
    /// </para>
    /// </remarks>
    public sealed class IncrementDishViewsCommandHandler : ICommandHandler<IncrementDishViewsCommand>
    {
        private readonly IDishRepository _dishRepository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="IncrementDishViewsCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        public IncrementDishViewsCommandHandler(IDishRepository dishRepository)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(IncrementDishViewsCommand request, CancellationToken cancellationToken)
        {
            int rowsAffected = await _dishRepository.IncrementViewsAsync(request.DishId, cancellationToken);
            if (rowsAffected == 0)
            {
                return DishesErrors.DishNotFound;
            }

            return Result.Success();
        }
    }
}
