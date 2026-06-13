using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Helpers;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Enums;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.CreateDishDraft
{
    /// <summary>
    /// Обработчик команды <see cref="CreateDishDraftCommand"/> (UC-DSH-001).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Получение идентификатора автора из <see cref="ICurrentUserService"/>.</item>
    ///   <item>Определение <see cref="OwnerType"/> из ролей JWT.</item>
    ///   <item>Генерация slug из <c>Name</c> + разрешение коллизий через суффикс <c>-N</c>
    ///         (лимит 30 попыток).</item>
    ///   <item>Создание агрегата через <see cref="Dish.Create"/>.</item>
    ///   <item>Применение опциональных полей через <see cref="Dish.UpdateCard"/> и
    ///         <see cref="Dish.UpdateHistory"/>.</item>
    ///   <item>Сохранение (один транзакционный коммит).</item>
    ///   <item>Публикация доменных событий из агрегата через <see cref="IDomainEventDispatcher"/>.</item>
    /// </list>
    /// </remarks>
    public sealed class CreateDishDraftCommandHandler
        : ICommandHandler<CreateDishDraftCommand, CreateDishDraftResult>
    {
        // Защита от теоретического зацикливания при коллизиях slug; 30 — компромисс
        // между реалистичными сценариями популярных названий и быстрым отказом при
        // баге в генераторе. См. AF-1 в UC-DSH-001.
        private const int MAX_SLUG_ATTEMPTS = 30;

        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly ISlugGenerator _slugGenerator;
        private readonly IDomainEventDispatcher _eventDispatcher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CreateDishDraftCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="slugGenerator">Генератор slug-идентификаторов.</param>
        /// <param name="eventDispatcher">Диспетчер доменных событий.</param>
        public CreateDishDraftCommandHandler(
            IDishRepository dishRepository,
            ICurrentUserService currentUser,
            IDateTimeProvider clock,
            ISlugGenerator slugGenerator,
            IDomainEventDispatcher eventDispatcher)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _slugGenerator = slugGenerator ?? throw new ArgumentNullException(nameof(slugGenerator));
            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
        }

        /// <inheritdoc/>
        public async Task<Result<CreateDishDraftResult>> Handle(
            CreateDishDraftCommand request,
            CancellationToken cancellationToken)
        {
            // Гарантия валидного UserId — на уровне политики ValidActor
            // (AuthorizationPolicies.VALID_ACTOR), применённой на эндпоинте.
            // Защита от теоретического сценария «middleware пропустил, sub отсутствует»
            // отнесена в инфраструктуру, в Handler-е её больше нет.
            var actorUserId = _currentUser.UserId!.Value;
            var ownerType = OwnerTypeResolver.ResolveFromRoles(_currentUser.Roles);

            var slugResult = await ResolveUniqueSlugAsync(request.Name, cancellationToken);
            if (slugResult.IsFailure)
            {
                return slugResult.Error;
            }

            var utcNow = _clock.UtcNow;

            var dish = Dish.Create(
                authorUserId: actorUserId,
                name: request.Name,
                slug: slugResult.Value,
                difficultyLevel: request.DifficultyLevel,
                costEstimate: request.CostEstimate,
                ownerType: ownerType,
                utcNow: utcNow);

            ApplyOptionalCardFields(dish, request, ownerType, utcNow);
            ApplyOptionalDietLabels(dish, request, utcNow);
            ApplyOptionalHistory(dish, request, utcNow);

            await _dishRepository.AddAsync(dish, cancellationToken);
            await _dishRepository.SaveChangesAsync(cancellationToken);

            await _eventDispatcher.DispatchAsync(dish, cancellationToken);

            return new CreateDishDraftResult(dish.Id, dish.Slug);
        }

        // AF-1 + AF-2: генерация уникального slug. При пустом результате генератора —
        // fallback на короткий Guid-суффикс. Лимит 30 попыток на коллизии — см. UC.
        private async Task<Result<string>> ResolveUniqueSlugAsync(string name, CancellationToken ct)
        {
            var baseSlug = _slugGenerator.Generate(name);
            if (string.IsNullOrEmpty(baseSlug))
            {
                baseSlug = $"dish-{Guid.NewGuid():N}"[..13];
            }

            var candidate = baseSlug;
            var attempt = 1;

            while (await _dishRepository.SlugExistsAsync(candidate, ct))
            {
                attempt++;
                if (attempt > MAX_SLUG_ATTEMPTS)
                {
                    return DishesErrors.SlugGenerationExhausted;
                }

                candidate = $"{baseSlug}-{attempt}";
            }

            return candidate;
        }

        // Применяем UpdateCard только если задано хотя бы одно опц. поле карточки.
        // Внутри UpdateCard поднимается DishUpdatedEvent — не вызываем зря,
        // если опц. полей нет, чтобы не плодить событий.
        private static void ApplyOptionalCardFields(
            Dish dish,
            CreateDishDraftCommand request,
            OwnerType ownerType,
            DateTimeOffset utcNow)
        {
            var hasOptionalCardFields =
                request.ShortDescription is not null
                || request.Description is not null;

            if (!hasOptionalCardFields)
            {
                return;
            }

            dish.UpdateCard(
                name: request.Name,
                shortDescription: request.ShortDescription,
                description: request.Description,
                difficultyLevel: request.DifficultyLevel,
                costEstimate: request.CostEstimate,
                ownerType: ownerType,
                utcNow: utcNow);
        }

        // DietLabelsMask редактируется отдельным Domain-методом Dish.SetDietLabels:
        // декларация автора с валидацией по составу ингредиентов имеет другую семантику,
        // чем прочие поля карточки, и намеренно вынесена из UpdateCard (ADR-0016).
        // На draft-этапе рецепт пустой — словарь конфликтов пуст, Reject-проверка
        // SetDietLabels гарантированно проходит, Result.Failure невозможен по структурному инварианту.
        private static void ApplyOptionalDietLabels(Dish dish, CreateDishDraftCommand request, DateTimeOffset utcNow)
        {
            if (request.DietLabelsMask is null)
            {
                return;
            }

            IReadOnlyDictionary<Guid, DietLabels> noConflicts =
                new Dictionary<Guid, DietLabels>(capacity: 0);
            _ = dish.SetDietLabels(request.DietLabelsMask.Value, noConflicts, utcNow);
        }

        private static void ApplyOptionalHistory(Dish dish, CreateDishDraftCommand request, DateTimeOffset utcNow)
        {
            if (request.HistoryText is null)
            {
                return;
            }

            dish.UpdateHistory(request.HistoryText, utcNow);
        }
    }
}
