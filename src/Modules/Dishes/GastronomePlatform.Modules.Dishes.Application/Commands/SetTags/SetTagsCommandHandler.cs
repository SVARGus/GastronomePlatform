using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Helpers;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetTags
{
    /// <summary>
    /// Обработчик команды <see cref="SetTagsCommand"/> (UC-DSH-008).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Загрузка блюда с подгруженной коллекцией <see cref="Dish.Tags"/>
    ///         через <see cref="IDishRepository.GetByIdWithTagsAsync"/>.</item>
    ///   <item>POL-001: автор блюда или Admin.</item>
    ///   <item>Нормализация входных имён через <see cref="TagNameNormalizer"/>
    ///         + dedup по нормализованной форме.</item>
    ///   <item>Batch-загрузка существующих тегов через
    ///         <see cref="ITagRepository.ListByNormalizedNamesAsync"/>; для отсутствующих —
    ///         создание <see cref="Tag.Create"/> с уникальным slug
    ///         (<see cref="ISlugGenerator"/> + retry с суффиксом).</item>
    ///   <item>Вычисление дельты: новые vs старые <c>TagId</c>; <see cref="Tag.IncrementUsage"/>
    ///         на добавленные, <see cref="Tag.DecrementUsage"/> на удалённые (загрузка
    ///         удалённых через <see cref="ITagRepository.ListByIdsAsync"/>).</item>
    ///   <item>Делегирование Domain: <see cref="Dish.SetTags"/>. Domain проверяет лимит
    ///         и отсутствие дубликатов как defense-in-depth.</item>
    ///   <item>Сохранение и публикация доменных событий (<c>DishUpdatedEvent</c>).</item>
    /// </list>
    /// </remarks>
    public sealed class SetTagsCommandHandler : ICommandHandler<SetTagsCommand>
    {
        // Защита от теоретического зацикливания при коллизиях slug; та же константа,
        // что в CreateDishDraftCommandHandler — компромисс между реалистичными
        // повторениями и быстрым отказом при баге генератора.
        private const int MAX_SLUG_ATTEMPTS = 30;

        private readonly IDishRepository _dishRepository;
        private readonly ITagRepository _tagRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly ISlugGenerator _slugGenerator;
        private readonly IDomainEventDispatcher _eventDispatcher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SetTagsCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="tagRepository">Репозиторий тегов.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="slugGenerator">Генератор slug-идентификаторов.</param>
        /// <param name="eventDispatcher">Диспетчер доменных событий.</param>
        public SetTagsCommandHandler(
            IDishRepository dishRepository,
            ITagRepository tagRepository,
            ICurrentUserService currentUser,
            IDateTimeProvider clock,
            ISlugGenerator slugGenerator,
            IDomainEventDispatcher eventDispatcher)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _slugGenerator = slugGenerator ?? throw new ArgumentNullException(nameof(slugGenerator));
            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(SetTagsCommand request, CancellationToken cancellationToken)
        {
            Dish? dish = await _dishRepository.GetByIdWithTagsAsync(request.DishId, cancellationToken);
            if (dish is null)
            {
                return DishesErrors.DishNotFound;
            }

            Guid actorUserId = _currentUser.UserId!.Value;
            bool isAdmin = _currentUser.IsInRole(PlatformRoles.ADMIN);
            if (dish.AuthorUserId != actorUserId && !isAdmin)
            {
                return DishesErrors.NotDishOwner;
            }

            // Dedup по нормализованной форме. Запоминаем первое (приоритетное)
            // оригинальное написание для каждого нормализованного варианта —
            // оно станет Tag.Name при создании нового тега.
            Dictionary<string, string> firstOriginalByNormalized = new(StringComparer.Ordinal);
            foreach (string raw in request.TagNames)
            {
                string normalized = TagNameNormalizer.Normalize(raw);
                if (normalized.Length == 0)
                {
                    continue;
                }
                if (!firstOriginalByNormalized.ContainsKey(normalized))
                {
                    firstOriginalByNormalized[normalized] = raw.Trim();
                }
            }

            // Batch-загрузка существующих тегов по нормализованным именам.
            IReadOnlyCollection<string> distinctNormalized =
                firstOriginalByNormalized.Keys.ToArray();
            IReadOnlyList<Tag> existing = distinctNormalized.Count == 0
                ? Array.Empty<Tag>()
                : await _tagRepository.ListByNormalizedNamesAsync(distinctNormalized, cancellationToken);

            Dictionary<string, Tag> existingByNormalized =
                existing.ToDictionary(t => t.NormalizedName, StringComparer.Ordinal);

            // Найденные + созданные = итоговый набор тегов блюда.
            List<Tag> finalTags = new(distinctNormalized.Count);
            foreach (string normalized in distinctNormalized)
            {
                if (existingByNormalized.TryGetValue(normalized, out Tag? existingTag))
                {
                    finalTags.Add(existingTag);
                    continue;
                }

                string originalName = firstOriginalByNormalized[normalized];
                Result<string> slugResult =
                    await ResolveUniqueSlugAsync(originalName, cancellationToken);
                if (slugResult.IsFailure)
                {
                    return slugResult.Error;
                }

                Tag newTag = Tag.Create(
                    name: originalName,
                    normalizedName: normalized,
                    slug: slugResult.Value,
                    createdByUserId: actorUserId,
                    createdAt: _clock.UtcNow);

                await _tagRepository.AddAsync(newTag, cancellationToken);
                finalTags.Add(newTag);
            }

            // Дельта по идентификаторам: что добавилось, что удалилось.
            HashSet<Guid> newTagIds = finalTags.Select(t => t.Id).ToHashSet();
            HashSet<Guid> oldTagIds = dish.Tags.Select(dt => dt.TagId).ToHashSet();
            HashSet<Guid> addedIds = newTagIds.Except(oldTagIds).ToHashSet();
            HashSet<Guid> removedIds = oldTagIds.Except(newTagIds).ToHashSet();

            // Инкремент UsageCount у добавленных. Для них Tag-объекты уже в трекере EF
            // (из existing или из только что созданных).
            foreach (Tag tag in finalTags.Where(t => addedIds.Contains(t.Id)))
            {
                tag.IncrementUsage();
            }

            // Декремент UsageCount у удалённых — нужна явная загрузка, в dish.Tags хранятся
            // только связки DishTag без подгруженного объекта Tag.
            if (removedIds.Count > 0)
            {
                IReadOnlyList<Tag> tagsToDecrement =
                    await _tagRepository.ListByIdsAsync(removedIds, cancellationToken);
                foreach (Tag tag in tagsToDecrement)
                {
                    tag.DecrementUsage();
                }
            }

            Result setResult = dish.SetTags(newTagIds.ToArray(), _clock.UtcNow);
            if (setResult.IsFailure)
            {
                return setResult;
            }

            await _dishRepository.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchAsync(dish, cancellationToken);

            return Result.Success();
        }

        // Генерация уникального slug по тому же шаблону, что в CreateDishDraftCommandHandler:
        // базовый slug → проверка коллизии → суффикс -2, -3, … При пустом результате генератора
        // (нелатинский ввод вроде эмодзи) — fallback на короткий Guid-суффикс.
        private async Task<Result<string>> ResolveUniqueSlugAsync(string name, CancellationToken ct)
        {
            string baseSlug = _slugGenerator.Generate(name);
            if (string.IsNullOrEmpty(baseSlug))
            {
                baseSlug = $"tag-{Guid.NewGuid():N}"[..12];
            }

            // Усечение, чтобы оставить запас под суффикс -N (5 символов).
            const int SUFFIX_RESERVE = 5;
            if (baseSlug.Length > Tag.MAX_SLUG_LENGTH - SUFFIX_RESERVE)
            {
                baseSlug = baseSlug[..(Tag.MAX_SLUG_LENGTH - SUFFIX_RESERVE)];
            }

            string candidate = baseSlug;
            int attempt = 1;

            while (await _tagRepository.SlugExistsAsync(candidate, ct))
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
    }
}
