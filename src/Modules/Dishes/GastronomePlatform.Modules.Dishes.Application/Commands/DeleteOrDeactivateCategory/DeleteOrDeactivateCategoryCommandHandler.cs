using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.DeleteOrDeactivateCategory
{
    /// <summary>
    /// Обработчик команды <see cref="DeleteOrDeactivateCategoryCommand"/> (UC-DSH-103).
    /// </summary>
    /// <remarks>
    /// Логика:
    /// <list type="number">
    ///   <item>Проверка существования.</item>
    ///   <item>Если есть дочерние категории <b>или</b> связки <c>DishCategory</c> /
    ///         <c>DishCategoryPublished</c> — soft delete (<c>Deactivate</c>),
    ///         результат <c>WasDeleted = false</c>.</item>
    ///   <item>Иначе — hard delete через <see cref="ICategoryRepository.DeleteAsync"/>,
    ///         результат <c>WasDeleted = true</c>.</item>
    /// </list>
    /// </remarks>
    public sealed class DeleteOrDeactivateCategoryCommandHandler
        : ICommandHandler<DeleteOrDeactivateCategoryCommand, DeleteOrDeactivateCategoryResult>
    {
        private readonly ICategoryRepository _categoryRepository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DeleteOrDeactivateCategoryCommandHandler"/>.
        /// </summary>
        /// <param name="categoryRepository">Репозиторий категорий.</param>
        public DeleteOrDeactivateCategoryCommandHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository
                ?? throw new ArgumentNullException(nameof(categoryRepository));
        }

        /// <inheritdoc/>
        public async Task<Result<DeleteOrDeactivateCategoryResult>> Handle(
            DeleteOrDeactivateCategoryCommand request,
            CancellationToken cancellationToken)
        {
            Category? category = await _categoryRepository.GetByIdAsync(
                request.CategoryId, cancellationToken);
            if (category is null)
            {
                return DishesErrors.CategoryNotFound;
            }

            bool hasChildren = await _categoryRepository.HasChildrenAsync(
                category.Id, cancellationToken);
            bool hasLinks = await _categoryRepository.HasDishLinksAsync(
                category.Id, cancellationToken);

            if (hasChildren || hasLinks)
            {
                category.Deactivate();
                await _categoryRepository.SaveChangesAsync(cancellationToken);
                return new DeleteOrDeactivateCategoryResult(WasDeleted: false);
            }

            int affected = await _categoryRepository.DeleteAsync(category.Id, cancellationToken);
            return affected > 0
                ? new DeleteOrDeactivateCategoryResult(WasDeleted: true)
                : DishesErrors.CategoryNotFound;
        }
    }
}
