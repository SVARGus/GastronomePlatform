using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.DeleteTag
{
    /// <summary>
    /// Обработчик команды <see cref="DeleteTagCommand"/> (UC-DSH-131).
    /// </summary>
    /// <remarks>
    /// Поток:
    /// <list type="number">
    ///   <item>Проверка существования тега.</item>
    ///   <item>Каскадное удаление через
    ///         <see cref="ITagRepository.RemoveWithLinksAsync"/> (тег + DishTag +
    ///         DishTagPublished) с возвратом списка затронутых блюд.</item>
    ///   <item>Массовое обновление <c>Dish.UpdatedAt</c> у затронутых блюд через
    ///         <see cref="IDishRepository.BulkMarkAsUpdatedAsync"/>.</item>
    /// </list>
    /// <para>
    /// Доменное событие <c>DishUpdatedEvent</c> в этой операции не поднимается — это
    /// сознательный компромисс admin-каскада (см. XML-doc <c>BulkMarkAsUpdatedAsync</c>).
    /// </para>
    /// </remarks>
    public sealed class DeleteTagCommandHandler : ICommandHandler<DeleteTagCommand>
    {
        private readonly ITagRepository _tagRepository;
        private readonly IDishRepository _dishRepository;
        private readonly IDateTimeProvider _clock;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DeleteTagCommandHandler"/>.
        /// </summary>
        /// <param name="tagRepository">Репозиторий тегов.</param>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        public DeleteTagCommandHandler(
            ITagRepository tagRepository,
            IDishRepository dishRepository,
            IDateTimeProvider clock)
        {
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(DeleteTagCommand request, CancellationToken cancellationToken)
        {
            Tag? tag = await _tagRepository.GetByIdAsync(request.TagId, cancellationToken);
            if (tag is null)
            {
                return DishesErrors.TagNotFound;
            }

            IReadOnlyList<Guid> affectedDishIds =
                await _tagRepository.RemoveWithLinksAsync(request.TagId, cancellationToken);

            if (affectedDishIds.Count > 0)
            {
                await _dishRepository.BulkMarkAsUpdatedAsync(
                    affectedDishIds, _clock.UtcNow, cancellationToken);
            }

            return Result.Success();
        }
    }
}
