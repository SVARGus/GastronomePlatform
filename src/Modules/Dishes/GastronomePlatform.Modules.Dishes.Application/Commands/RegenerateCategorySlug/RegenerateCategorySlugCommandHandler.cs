using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.RegenerateCategorySlug
{
    /// <summary>
    /// Обработчик команды <see cref="RegenerateCategorySlugCommand"/> (UC-DSH-105).
    /// </summary>
    public sealed class RegenerateCategorySlugCommandHandler
        : ICommandHandler<RegenerateCategorySlugCommand, RegenerateCategorySlugResult>
    {
        // Защитный лимит на коллизии slug.
        private const int MAX_SLUG_ATTEMPTS = 30;

        private readonly ICategoryRepository _categoryRepository;
        private readonly ISlugGenerator _slugGenerator;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="RegenerateCategorySlugCommandHandler"/>.
        /// </summary>
        /// <param name="categoryRepository">Репозиторий категорий.</param>
        /// <param name="slugGenerator">Генератор slug.</param>
        public RegenerateCategorySlugCommandHandler(
            ICategoryRepository categoryRepository,
            ISlugGenerator slugGenerator)
        {
            _categoryRepository = categoryRepository
                ?? throw new ArgumentNullException(nameof(categoryRepository));
            _slugGenerator = slugGenerator
                ?? throw new ArgumentNullException(nameof(slugGenerator));
        }

        /// <inheritdoc/>
        public async Task<Result<RegenerateCategorySlugResult>> Handle(
            RegenerateCategorySlugCommand request,
            CancellationToken cancellationToken)
        {
            Category? category = await _categoryRepository.GetByIdAsync(
                request.CategoryId, cancellationToken);
            if (category is null)
            {
                return DishesErrors.CategoryNotFound;
            }

            Result<string> slugResult = await ResolveUniqueSlugAsync(
                category.Name, category.Slug, cancellationToken);
            if (slugResult.IsFailure)
            {
                return slugResult.Error;
            }

            category.RegenerateSlug(slugResult.Value);
            await _categoryRepository.SaveChangesAsync(cancellationToken);

            return new RegenerateCategorySlugResult(category.Slug);
        }

        // Slug регенерируется из текущего Name; при коллизии — суффикс -N.
        // Текущий slug категории не считается конфликтом — это «сама себя» переименовываем.
        private async Task<Result<string>> ResolveUniqueSlugAsync(
            string name,
            string currentSlug,
            CancellationToken ct)
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

            while (candidate != currentSlug
                && await _categoryRepository.SlugExistsAsync(candidate, ct))
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
