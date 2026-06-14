using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Helpers;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.MoveCategory
{
    /// <summary>
    /// Обработчик команды <see cref="MoveCategoryCommand"/> (UC-DSH-104).
    /// </summary>
    /// <remarks>
    /// Поток:
    /// <list type="number">
    ///   <item>Загрузка плоского списка категорий через
    ///         <see cref="ICategoryRepository.ListAllAsync"/> (включая неактивные).</item>
    ///   <item>Проверка существования перемещаемой категории и нового родителя
    ///         (для непустого <c>NewParentId</c>).</item>
    ///   <item>Проверка отсутствия цикла: <c>NewParentId</c> не должен лежать
    ///         в поддереве перемещаемой категории.</item>
    ///   <item>Проверка глубины: глубина нового родителя + глубина перемещаемого
    ///         поддерева ≤ <see cref="Category.MAX_DEPTH"/>.</item>
    ///   <item>Делегирование Domain: <see cref="Category.Move"/>, сохранение.</item>
    /// </list>
    /// </remarks>
    public sealed class MoveCategoryCommandHandler : ICommandHandler<MoveCategoryCommand>
    {
        private readonly ICategoryRepository _categoryRepository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="MoveCategoryCommandHandler"/>.
        /// </summary>
        /// <param name="categoryRepository">Репозиторий категорий.</param>
        public MoveCategoryCommandHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository
                ?? throw new ArgumentNullException(nameof(categoryRepository));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(
            MoveCategoryCommand request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<Category> all = await _categoryRepository.ListAllAsync(cancellationToken);
            Dictionary<Guid, Category> byId = all.ToDictionary(c => c.Id);

            if (!byId.TryGetValue(request.CategoryId, out Category? _))
            {
                return DishesErrors.CategoryNotFound;
            }

            if (request.NewParentId.HasValue)
            {
                if (!byId.TryGetValue(request.NewParentId.Value, out Category? parent)
                    || !parent.IsActive)
                {
                    return DishesErrors.CategoryParentNotFound;
                }

                // Запрет перемещения в собственное поддерево (включая прямых детей).
                HashSet<Guid> descendants = CategoryHierarchyValidator
                    .CollectDescendants(request.CategoryId, all);
                if (descendants.Contains(request.NewParentId.Value))
                {
                    return DishesErrors.CategoryMoveToOwnDescendant;
                }
            }

            // Глубина: уровень нового родителя (для прямого ребёнка) + (глубина поддерева - 1).
            int parentChildLevel = CategoryHierarchyValidator
                .CalculateChildDepth(request.NewParentId, byId);
            int subtreeDepth = CategoryHierarchyValidator
                .CalculateSubtreeDepth(request.CategoryId, all);
            int totalDepth = parentChildLevel + subtreeDepth - 1;

            if (totalDepth > Category.MAX_DEPTH)
            {
                return DishesErrors.CategoryDepthExceeded;
            }

            // Загружаем категорию в трекер для сохранения.
            Category? tracked = await _categoryRepository.GetByIdAsync(
                request.CategoryId, cancellationToken);
            if (tracked is null)
            {
                return DishesErrors.CategoryNotFound;
            }

            tracked.Move(request.NewParentId);
            await _categoryRepository.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
