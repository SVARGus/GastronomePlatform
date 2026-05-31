using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Snapshots;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;
using MediatR;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.PublishDish
{
    /// <summary>
    /// Обработчик команды <see cref="PublishDishCommand"/> (UC-DSH-004).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Полная загрузка агрегата через <c>GetByIdWithFullRecipeAsync</c>:
    ///         <c>Recipe</c>, <c>Timing</c>, <c>Yield</c>, <c>Nutrition</c>, <c>Steps</c>,
    ///         <c>Ingredients</c>, <c>Categories</c>, <c>Tags</c>.</item>
    ///   <item>Проверка владения (POL-001 DishOwnership): автор блюда совпадает
    ///         с идентификатором текущего пользователя.</item>
    ///   <item>Сборка jsonb-снепшота через <see cref="IPublishedDishSnapshotBuilder"/>
    ///         (по ADR-0012 — полиморфно для массива ингредиентов).</item>
    ///   <item>Делегирование Domain: <c>dish.Publish(utcNow, snapshot)</c> — там же
    ///         проверки всех инвариантов публикации (Archived, AlreadyPublished, наличие
    ///         главного фото, шагов, ингредиентов, ненулевого общего времени) и заполнение
    ///         <c>PublishedVersionData</c>, <c>PublishedAt</c>, <c>*Published</c>-таблиц.</item>
    ///   <item>Сохранение (один транзакционный коммит) и публикация доменных событий
    ///         через <see cref="IPublisher"/>. На Этапе 2 подписчиков нет; на Этапе 5+
    ///         появятся EventHandler-ы.</item>
    /// </list>
    /// Гарантия валидного <c>UserId</c> — на уровне политики
    /// <c>AuthorizationPolicies.VALID_ACTOR</c>, применённой на эндпоинте,
    /// поэтому <c>_currentUser.UserId!.Value</c> — корректно.
    /// </remarks>
    public sealed class PublishDishCommandHandler : ICommandHandler<PublishDishCommand>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IPublishedDishSnapshotBuilder _snapshotBuilder;
        private readonly IPublisher _publisher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="PublishDishCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="snapshotBuilder">Сборщик jsonb-снепшота публичной версии.</param>
        /// <param name="publisher">Издатель доменных событий MediatR.</param>
        public PublishDishCommandHandler(
            IDishRepository dishRepository,
            ICurrentUserService currentUser,
            IDateTimeProvider clock,
            IPublishedDishSnapshotBuilder snapshotBuilder,
            IPublisher publisher)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _snapshotBuilder = snapshotBuilder ?? throw new ArgumentNullException(nameof(snapshotBuilder));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(PublishDishCommand request, CancellationToken cancellationToken)
        {
            Dish? dish = await _dishRepository.GetByIdWithFullRecipeAsync(request.DishId, cancellationToken);
            if (dish is null)
            {
                return DishesErrors.DishNotFound;
            }

            var actorUserId = _currentUser.UserId!.Value;
            if (dish.AuthorUserId != actorUserId)
            {
                return DishesErrors.NotDishOwner;
            }

            string snapshot = _snapshotBuilder.Build(dish);
            var utcNow = _clock.UtcNow;

            Result publishResult = dish.Publish(utcNow, snapshot);
            if (publishResult.IsFailure)
            {
                return publishResult;
            }

            await _dishRepository.SaveChangesAsync(cancellationToken);
            await PublishDomainEventsAsync(dish, cancellationToken);

            return Result.Success();
        }

        // Доменные события агрегата не публикуются автоматически — публикуем
        // вручную после SaveChangesAsync. На Этапе 2 подписчиков нет, событие
        // DishPublishedEvent «выстреливает вхолостую»; на Этапе 5+ появятся
        // EventHandler-ы (рассылка подписчикам автора, индексация каталога).
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
