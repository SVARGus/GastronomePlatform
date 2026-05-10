using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Domain.Repositories
{
    /// <summary>
    /// Репозиторий для работы со справочником единиц измерения.
    /// </summary>
    public interface IMeasureUnitRepository
    {
        /// <summary>
        /// Находит единицу измерения по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор единицы измерения.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="MeasureUnit"/>, если запись найдена; иначе <see langword="null"/>.
        /// </returns>
        Task<MeasureUnit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Находит единицу измерения по уникальному коду
        /// (например, <c>"g"</c>, <c>"ml"</c>, <c>"kg"</c>).
        /// </summary>
        /// <param name="code">Уникальное кодовое обозначение единицы измерения.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="MeasureUnit"/>, если запись найдена; иначе <see langword="null"/>.
        /// </returns>
        Task<MeasureUnit?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает полный список единиц измерения.
        /// Используется для заполнения dropdown-ов в UI
        /// (например, при добавлении ингредиента в рецепт).
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Список всех единиц измерения, доступный только для чтения.</returns>
        Task<IReadOnlyList<MeasureUnit>> ListAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохраняет изменения в хранилище (Unit of Work).
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
