using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;
using MediatR;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UpdateRecipe
{
    /// <summary>
    /// Обработчик команды <see cref="UpdateRecipeCommand"/> (UC-DSH-003).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Загрузка блюда с подгруженным <see cref="Dish.Recipe"/> через
    ///         <see cref="IDishRepository.GetByIdWithRecipeAsync"/>.</item>
    ///   <item>Проверка владения (POL-001 DishOwnership): автор блюда совпадает
    ///         с идентификатором текущего пользователя.</item>
    ///   <item>Атомарное обновление полей рецепта через <see cref="Dish.UpdateRecipe"/>.</item>
    ///   <item>Сохранение и публикация доменных событий.</item>
    /// </list>
    /// Гарантия валидного <c>UserId</c> — на уровне политики
    /// <c>AuthorizationPolicies.VALID_ACTOR</c>, применённой на эндпоинте.
    /// </remarks>
    public sealed class UpdateRecipeCommandHandler : ICommandHandler<UpdateRecipeCommand>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IPublisher _publisher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UpdateRecipeCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="publisher">Издатель доменных событий MediatR.</param>
        public UpdateRecipeCommandHandler(
            IDishRepository dishRepository,
            ICurrentUserService currentUser,
            IDateTimeProvider clock,
            IPublisher publisher)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(UpdateRecipeCommand request, CancellationToken cancellationToken)
        {
            Dish? dish = await _dishRepository.GetByIdWithRecipeAsync(request.DishId, cancellationToken);
            if (dish is null)
            {
                return DishesErrors.DishNotFound;
            }

            var actorUserId = _currentUser.UserId!.Value;
            if (dish.AuthorUserId != actorUserId)
            {
                return DishesErrors.NotDishOwner;
            }

            var utcNow = _clock.UtcNow;

            Result updateResult = dish.UpdateRecipe(
                introductionText: request.IntroductionText,
                servingsDefault: request.ServingsDefault,
                isAlcoholic: request.IsAlcoholic,
                authorTips: request.AuthorTips,
                servingSuggestions: request.ServingSuggestions,
                notes: request.Notes,
                utcNow: utcNow);

            if (updateResult.IsFailure)
            {
                return updateResult;
            }

            await _dishRepository.SaveChangesAsync(cancellationToken);
            await PublishDomainEventsAsync(dish, cancellationToken);

            return Result.Success();
        }

        // Доменные события агрегата не публикуются автоматически — публикуем
        // вручную после SaveChangesAsync. На Этапе 2 подписчиков нет, события
        // «выстреливают вхолостую»; на Этапе 5+ появятся EventHandler-ы.
        private async Task PublishDomainEventsAsync(Dish dish, CancellationToken ct)
        {
            var events = dish.DomainEvents.ToList();
            dish.ClearDomainEvents();

            foreach (var domainEvent in events)
            {
                await _publisher.Publish(domainEvent, ct);
            }
        }
    }
}
