using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Domain.Repositories
{
    /// <summary>
    /// Репозиторий для работы со справочником категорий каталога.
    /// </summary>
    public interface ICategoryRepository
    {
        /// <summary>
        /// Находит категорию по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор категории.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Category"/>, если запись найдена; иначе <see langword="null"/>.
        /// </returns>
        Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Находит категорию по slug-у. Используется в UC-DSH-059 GetCategoryBySlug
        /// для разрешения публичных URL вида <c>/catalog/supy</c>.
        /// </summary>
        /// <param name="slug">URL-friendly идентификатор (например, <c>supy</c>).</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Category"/>, если запись с таким slug найдена; иначе <see langword="null"/>.
        /// </returns>
        Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает все активные категории (<see cref="Category.IsActive"/> = <see langword="true"/>).
        /// Используется для построения дерева категорий (UC-DSH-057): иерархия собирается
        /// на стороне приложения по <see cref="Category.ParentId"/>.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Список активных категорий, доступный только для чтения.</returns>
        Task<IReadOnlyList<Category>> ListActiveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавляет новую категорию в хранилище. Вызывается из admin-команды UC-DSH-101.
        /// </summary>
        /// <param name="category">Категория для сохранения.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task AddAsync(Category category, CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохраняет изменения в хранилище (Unit of Work).
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
