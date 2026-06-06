using GastronomePlatform.Common.Domain.Results;

namespace GastronomePlatform.Modules.Media.Application.Abstractions
{
    /// <summary>
    /// Абстракция низкоуровневого хранилища файлов.
    /// Реализации: <c>LocalFileStorage</c> (Этап 2), <c>S3FileStorage</c> (Этап 8+).
    /// </summary>
    public interface IFileStorage
    {
        /// <summary>
        /// Имя провайдера. Сохраняется в поле <c>MediaFile.StorageProvider</c>.
        /// Значения: <c>"local"</c> — Этап 2, <c>"s3"</c> — Этап 8+.
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Сохраняет поток в хранилище по указанному ключу.
        /// </summary>
        /// <param name="content">Поток с содержимым файла.</param>
        /// <param name="storageKey">Ключ хранения (генерируется <c>IStorageKeyGenerator</c>).</param>
        /// <param name="contentType">MIME-тип файла.</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>
        /// <see cref="Result{T}"/> с тем же <paramref name="storageKey"/> при успехе
        /// или ошибкой при сбое хранилища.
        /// </returns>
        Task<Result<string>> SaveAsync(
            Stream content,
            string storageKey,
            string contentType,
            CancellationToken ct = default);

        /// <summary>
        /// Открывает поток для чтения файла. Вызывающий обязан закрыть поток.
        /// </summary>
        /// <param name="storageKey">Ключ файла в хранилище.</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>
        /// <see cref="Result{T}"/> с <see cref="Stream"/> при успехе
        /// или <c>MEDIA.NOT_FOUND</c> если файл не найден.
        /// </returns>
        Task<Result<Stream>> OpenReadAsync(
            string storageKey,
            CancellationToken ct = default);

        /// <summary>
        /// Проверяет существование файла без чтения содержимого.
        /// </summary>
        /// <param name="storageKey">Ключ файла в хранилище.</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns><c>true</c> если файл существует; иначе <c>false</c>.</returns>
        Task<bool> ExistsAsync(
            string storageKey,
            CancellationToken ct = default);

        /// <summary>
        /// Удаляет файл из хранилища. Идемпотентно: отсутствие файла не считается ошибкой.
        /// </summary>
        /// <param name="storageKey">Ключ файла в хранилище.</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>
        /// <see cref="Result.Success"/> при успехе или отсутствии файла;
        /// ошибка при сбое хранилища.
        /// </returns>
        Task<Result> DeleteAsync(
            string storageKey,
            CancellationToken ct = default);
    }
}
