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
        /// Возвращает все категории справочника, включая неактивные. Используется
        /// в admin-сценариях (UC-DSH-104 MoveCategory) для проверки циклов и глубины
        /// иерархии — деактивированные узлы тоже учитываются.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Полный список категорий справочника.</returns>
        Task<IReadOnlyList<Category>> ListAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверяет существование категории с указанным slug. Используется
        /// в UC-DSH-101 CreateCategory и UC-DSH-105 RegenerateSlug для разрешения коллизий
        /// автоматически сгенерированного slug — Application Handler добавляет суффикс
        /// (<c>-2</c>, <c>-3</c>, …) до тех пор, пока метод не вернёт <see langword="false"/>.
        /// </summary>
        /// <param name="slug">URL-friendly идентификатор для проверки.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see langword="true"/>, если категория с таким slug уже существует;
        /// иначе <see langword="false"/>.
        /// </returns>
        Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверяет, есть ли у указанной категории дочерние записи. Используется
        /// в UC-DSH-103 для решения между hard delete и soft delete.
        /// </summary>
        /// <param name="categoryId">Идентификатор категории.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see langword="true"/>, если есть хотя бы один наследник; иначе <see langword="false"/>.
        /// </returns>
        Task<bool> HasChildrenAsync(Guid categoryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверяет, есть ли связки <c>DishCategory</c> или <c>DishCategoryPublished</c>
        /// для указанной категории. Используется в UC-DSH-103 для решения
        /// между hard delete и soft delete: если категория где-то используется, удалять
        /// её физически нельзя.
        /// </summary>
        /// <param name="categoryId">Идентификатор категории.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see langword="true"/>, если есть хотя бы одна связка; иначе <see langword="false"/>.
        /// </returns>
        Task<bool> HasDishLinksAsync(Guid categoryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Физически удаляет категорию из БД (UC-DSH-103). Вызывается Application Handler-ом
        /// только после проверки отсутствия детей и связок. Реализация — прямой
        /// <c>ExecuteDeleteAsync</c> без загрузки агрегата в трекер.
        /// </summary>
        /// <param name="categoryId">Идентификатор категории.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Количество затронутых строк (1 при удалении, 0 если категории нет).</returns>
        Task<int> DeleteAsync(Guid categoryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает категории, идентификаторы которых входят в указанный набор.
        /// Используется в UC-DSH-007 SetCategories для проверки существования всех
        /// присланных <c>CategoryId</c> одним SQL-запросом.
        /// </summary>
        /// <remarks>
        /// Возвращает только активные категории (<see cref="Category.IsActive"/> = <see langword="true"/>) —
        /// присвоение деактивированной категории блюду не должно проходить. Если количество
        /// возвращённых записей не совпадает с количеством запрошенных <c>id</c> — какой-то
        /// идентификатор не существует или неактивен, Application Handler должен вернуть ошибку.
        /// При пустом наборе возвращает пустой список без обращения к БД.
        /// </remarks>
        /// <param name="ids">Набор идентификаторов категорий для проверки.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Найденные активные категории; порядок не гарантируется.</returns>
        Task<IReadOnlyList<Category>> ListByIdsAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken = default);

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
