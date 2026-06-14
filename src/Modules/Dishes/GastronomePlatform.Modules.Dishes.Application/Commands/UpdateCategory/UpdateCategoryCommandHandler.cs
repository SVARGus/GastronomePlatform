using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UpdateCategory
{
    /// <summary>
    /// Обработчик команды <see cref="UpdateCategoryCommand"/> (UC-DSH-102).
    /// </summary>
    public sealed class UpdateCategoryCommandHandler : ICommandHandler<UpdateCategoryCommand>
    {
        private readonly ICategoryRepository _categoryRepository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UpdateCategoryCommandHandler"/>.
        /// </summary>
        /// <param name="categoryRepository">Репозиторий категорий.</param>
        public UpdateCategoryCommandHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository
                ?? throw new ArgumentNullException(nameof(categoryRepository));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(
            UpdateCategoryCommand request,
            CancellationToken cancellationToken)
        {
            Category? category = await _categoryRepository.GetByIdAsync(
                request.CategoryId, cancellationToken);
            if (category is null)
            {
                return DishesErrors.CategoryNotFound;
            }

            category.Update(
                name: request.Name,
                order: request.Order,
                iconMediaId: request.IconMediaId);

            if (request.IsActive)
            {
                category.Activate();
            }
            else
            {
                category.Deactivate();
            }

            await _categoryRepository.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
