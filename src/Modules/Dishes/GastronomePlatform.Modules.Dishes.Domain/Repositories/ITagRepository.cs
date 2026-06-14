using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Domain.Repositories
{
    /// <summary>
    /// Репозиторий для работы со справочником пользовательских тегов.
    /// </summary>
    public interface ITagRepository
    {
        /// <summary>
        /// Находит тег по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор тега.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Tag"/>, если запись найдена; иначе <see langword="null"/>.
        /// </returns>
        Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Находит тег по нормализованному имени.
        /// Используется для дедупликации перед созданием нового тега
        /// (UC-DSH-008 SetTags): «Без глютена» и «без глютена» должны мапиться на одну запись.
        /// </summary>
        /// <param name="normalizedName">Нормализованное имя тега (lowercase + trim + collapse пробелов).</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Tag"/>, если запись с таким нормализованным именем найдена;
        /// иначе <see langword="null"/>.
        /// </returns>
        Task<Tag?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает теги, нормализованные имена которых входят в указанный набор.
        /// Используется в UC-DSH-008 SetTags для batch-резолва входного набора имён:
        /// найденные теги переиспользуются, отсутствующие создаются Application Handler-ом.
        /// </summary>
        /// <param name="normalizedNames">Набор нормализованных имён для поиска.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Найденные теги; порядок не гарантируется. Пустой список при пустом входе.</returns>
        Task<IReadOnlyList<Tag>> ListByNormalizedNamesAsync(
            IReadOnlyCollection<string> normalizedNames,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает теги, идентификаторы которых входят в указанный набор.
        /// Используется в UC-DSH-008 SetTags для загрузки <see cref="Tag"/>-объектов,
        /// у которых нужно изменить <see cref="Tag.UsageCount"/> (дельта старых/новых тегов).
        /// </summary>
        /// <param name="ids">Набор идентификаторов тегов для загрузки.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Найденные теги; порядок не гарантируется. Пустой список при пустом входе.</returns>
        Task<IReadOnlyList<Tag>> ListByIdsAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает теги, у которых <see cref="Tag.NormalizedName"/> начинается
        /// с указанного префикса. Используется для автокомплита (UC-DSH-060).
        /// Ранжирование по <see cref="Tag.UsageCount"/> по убыванию;
        /// при равенстве — по имени.
        /// </summary>
        /// <param name="normalizedPrefix">Нормализованный префикс поиска
        /// (lowercase + trim, формируется <c>TagNameNormalizer</c>).</param>
        /// <param name="limit">Максимальное количество возвращаемых записей.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Список тегов, отсортированный по популярности и имени.</returns>
        Task<IReadOnlyList<Tag>> SearchByNormalizedNamePrefixAsync(
            string normalizedPrefix,
            int limit,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает топ-N верифицированных тегов по <see cref="Tag.UsageCount"/>
        /// (UC-DSH-061). Только <see cref="Tag.IsVerified"/> = <see langword="true"/> —
        /// для облака тегов на главной странице нужны admin-одобренные теги.
        /// </summary>
        /// <param name="limit">Максимальное количество возвращаемых записей.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Список тегов, отсортированный по <see cref="Tag.UsageCount"/> убыванию.</returns>
        Task<IReadOnlyList<Tag>> ListTopVerifiedByUsageAsync(
            int limit,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверяет, существует ли тег с указанным <see cref="Tag.Slug"/>.
        /// Используется при создании нового тега для разрешения коллизий
        /// автоматически сгенерированного slug — Application Handler добавляет суффикс
        /// (<c>-2</c>, <c>-3</c>, …) до тех пор, пока метод не вернёт <see langword="false"/>.
        /// </summary>
        /// <param name="slug">URL-friendly идентификатор для проверки.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see langword="true"/>, если тег с таким slug уже существует;
        /// иначе <see langword="false"/>.
        /// </returns>
        Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавляет новый тег в хранилище.
        /// </summary>
        /// <param name="tag">Тег для сохранения.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task AddAsync(Tag tag, CancellationToken cancellationToken = default);

        /// <summary>
        /// Удаляет тег вместе со всеми связками <c>DishTag</c> и <c>DishTagPublished</c>
        /// (UC-DSH-131 DeleteTag). Возвращает идентификаторы блюд, которые имели этот тег
        /// в рабочей копии — Application Handler использует их для обновления
        /// <c>Dish.UpdatedAt</c>.
        /// </summary>
        /// <remarks>
        /// Каскад удаления связок выполняется через EF Core 8 <c>ExecuteDeleteAsync</c>
        /// без загрузки блюд в трекер. Это безопасно: для пересчёта <c>UsageCount</c> нечего делать —
        /// тег уходит целиком. <c>Dish.UpdatedAt</c> и <c>DishUpdatedEvent</c> поднимаются
        /// в Handler-е после получения списка затронутых блюд.
        /// </remarks>
        /// <param name="tagId">Идентификатор удаляемого тега.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// Список идентификаторов блюд, у которых был этот тег в рабочей копии
        /// (по таблице <c>DishTag</c>, до удаления). Пустой список — тег не использовался.
        /// </returns>
        Task<IReadOnlyList<Guid>> RemoveWithLinksAsync(
            Guid tagId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохраняет изменения в хранилище (Unit of Work).
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
