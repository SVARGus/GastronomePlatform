using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Helpers;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.CreateCategory
{
    /// <summary>
    /// Обработчик команды <see cref="CreateCategoryCommand"/> (UC-DSH-101).
    /// </summary>
    /// <remarks>
    /// Поток:
    /// <list type="number">
    ///   <item>Если задан <c>ParentId</c> — проверка существования родителя
    ///         (<see cref="DishesErrors.CategoryParentNotFound"/> для несуществующих/деактивированных).</item>
    ///   <item>Проверка глубины: новый узел не должен превышать <see cref="Category.MAX_DEPTH"/>.</item>
    ///   <item>Генерация уникального slug через <see cref="ISlugGenerator"/>
    ///         + retry с суффиксом <c>-N</c>. Источник — <c>CreateDishDraftCommandHandler</c>.</item>
    ///   <item>Создание через <see cref="Category.Create"/> + сохранение.</item>
    /// </list>
    /// </remarks>
    public sealed class CreateCategoryCommandHandler
        : ICommandHandler<CreateCategoryCommand, CreateCategoryResult>
    {
        // Защитный лимит на коллизии slug (как в CreateDishDraft).
        private const int MAX_SLUG_ATTEMPTS = 30;

        private readonly ICategoryRepository _categoryRepository;
        private readonly ISlugGenerator _slugGenerator;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CreateCategoryCommandHandler"/>.
        /// </summary>
        /// <param name="categoryRepository">Репозиторий категорий.</param>
        /// <param name="slugGenerator">Генератор slug-идентификаторов.</param>
        public CreateCategoryCommandHandler(
            ICategoryRepository categoryRepository,
            ISlugGenerator slugGenerator)
        {
            _categoryRepository = categoryRepository
                ?? throw new ArgumentNullException(nameof(categoryRepository));
            _slugGenerator = slugGenerator
                ?? throw new ArgumentNullException(nameof(slugGenerator));
        }

        /// <inheritdoc/>
        public async Task<Result<CreateCategoryResult>> Handle(
            CreateCategoryCommand request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<Category> all =
                await _categoryRepository.ListAllAsync(cancellationToken);
            Dictionary<Guid, Category> byId = all.ToDictionary(c => c.Id);

            if (request.ParentId.HasValue)
            {
                if (!byId.TryGetValue(request.ParentId.Value, out Category? parent)
                    || !parent.IsActive)
                {
                    return DishesErrors.CategoryParentNotFound;
                }
            }

            Result depthCheck = CategoryHierarchyValidator
                .EnsureChildDepthWithinLimit(request.ParentId, byId);
            if (depthCheck.IsFailure)
            {
                return depthCheck.Error;
            }

            Result<string> slugResult = await ResolveUniqueSlugAsync(request.Name, cancellationToken);
            if (slugResult.IsFailure)
            {
                return slugResult.Error;
            }

            Category category = Category.Create(
                name: request.Name,
                slug: slugResult.Value,
                parentId: request.ParentId,
                order: request.Order);

            if (request.IconMediaId.HasValue)
            {
                category.Update(category.Name, category.Order, request.IconMediaId);
            }

            await _categoryRepository.AddAsync(category, cancellationToken);
            await _categoryRepository.SaveChangesAsync(cancellationToken);

            return new CreateCategoryResult(category.Id, category.Slug);
        }

        private async Task<Result<string>> ResolveUniqueSlugAsync(string name, CancellationToken ct)
        {
            string baseSlug = _slugGenerator.Generate(name);
            if (string.IsNullOrEmpty(baseSlug))
            {
                baseSlug = $"category-{Guid.NewGuid():N}"[..18];
            }

            const int SUFFIX_RESERVE = 5;
            if (baseSlug.Length > Category.MAX_SLUG_LENGTH - SUFFIX_RESERVE)
            {
                baseSlug = baseSlug[..(Category.MAX_SLUG_LENGTH - SUFFIX_RESERVE)];
            }

            string candidate = baseSlug;
            int attempt = 1;

            while (await _categoryRepository.SlugExistsAsync(candidate, ct))
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
