using GastronomePlatform.Modules.Media.Domain.Enums;

namespace GastronomePlatform.Modules.Media.Application.Abstractions
{
    /// <summary>
    /// Генератор ключей хранения файлов.
    /// Отвечает за формирование пути в хранилище на основе категории данных,
    /// типа сущности и идентификатора файла.
    /// </summary>
    /// <remarks>
    /// Примеры результатов:
    /// <list type="bullet">
    ///   <item><c>public/dishes/2026/05/&lt;guid&gt;.jpg</c> — оригинал;</item>
    ///   <item><c>thumbnails/public/dishes/&lt;guid&gt;_medium.jpg</c> — миниатюра.</item>
    /// </list>
    /// </remarks>
    public interface IStorageKeyGenerator
    {
        /// <summary>
        /// Генерирует ключ хранения для оригинального медиафайла.
        /// </summary>
        /// <param name="category">Категория данных (<c>Public</c> или <c>Personal</c>).</param>
        /// <param name="entityType">Тип сущности-владельца (константа из <c>MediaEntityTypes</c>).</param>
        /// <param name="mediaId">Уникальный идентификатор медиафайла (станет частью пути).</param>
        /// <param name="extension">Расширение файла без точки (например, <c>"jpg"</c>).</param>
        /// <returns>
        /// Ключ хранения вида <c>public/dishes/2026/05/&lt;guid&gt;.jpg</c>.
        /// </returns>
        string Generate(
            MediaDataCategory category,
            string entityType,
            Guid mediaId,
            string extension);

        /// <summary>
        /// Генерирует ключ хранения для миниатюры медиафайла.
        /// </summary>
        /// <param name="category">Категория данных (<c>Public</c> или <c>Personal</c>).</param>
        /// <param name="entityType">Тип сущности-владельца (константа из <c>MediaEntityTypes</c>).</param>
        /// <param name="mediaId">Идентификатор родительского медиафайла.</param>
        /// <param name="size">Размер миниатюры.</param>
        /// <param name="format">Формат миниатюры.</param>
        /// <returns>
        /// Ключ хранения вида <c>thumbnails/public/dishes/&lt;guid&gt;_medium.jpg</c>.
        /// </returns>
        string GenerateThumbnail(
            MediaDataCategory category,
            string entityType,
            Guid mediaId,
            ThumbnailSize size,
            ThumbnailFormat format);
    }
}
