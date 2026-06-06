using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Media.Application.Abstractions;
using GastronomePlatform.Modules.Media.Application.Configuration;
using Microsoft.Extensions.Options;

namespace GastronomePlatform.Modules.Media.Infrastructure.Storage
{
    /// <summary>
    /// Реализация <see cref="IFileStorage"/> на основе локальной файловой системы (Этап 2).
    /// Файлы хранятся в директории, задаваемой через <c>Media:Storage:LocalBasePath</c>.
    /// В Docker Compose монтируется как named volume <c>media-data:/data/media</c>.
    /// На Этапе 8+ заменяется на <c>S3FileStorage</c> без изменения потребителей.
    /// </summary>
    public sealed class LocalFileStorage : IFileStorage
    {
        private readonly string _basePath;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="LocalFileStorage"/>.
        /// </summary>
        /// <param name="options">Типизированные настройки модуля Media.</param>
        public LocalFileStorage(IOptions<MediaOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);
            _basePath = options.Value.Storage.LocalBasePath;
        }

        /// <inheritdoc/>
        public string ProviderName => "local";

        /// <inheritdoc/>
        public async Task<Result<string>> SaveAsync(
            Stream content,
            string storageKey,
            string contentType,
            CancellationToken ct = default)
        {
            try
            {
                var fullPath = BuildFullPath(storageKey);
                var directory = Path.GetDirectoryName(fullPath)!;
                Directory.CreateDirectory(directory);

                await using var fileStream = new FileStream(
                    fullPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true);

                await content.CopyToAsync(fileStream, ct);
                return storageKey;
            }
            catch (Exception)
            {
                return Error.Failure("STORAGE.SAVE_FAILED", "Ошибка сохранения файла в локальное хранилище.");
            }
        }

        /// <inheritdoc/>
        public Task<Result<Stream>> OpenReadAsync(
            string storageKey,
            CancellationToken ct = default)
        {
            try
            {
                var fullPath = BuildFullPath(storageKey);

                if (!File.Exists(fullPath))
                {
                    return Task.FromResult<Result<Stream>>(
                        Error.NotFound("STORAGE.NOT_FOUND", "Файл не найден в локальном хранилище."));
                }

                Stream stream = new FileStream(
                    fullPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 81920,
                    useAsync: true);

                return Task.FromResult<Result<Stream>>(stream);
            }
            catch (Exception)
            {
                return Task.FromResult<Result<Stream>>(
                    Error.Failure("STORAGE.READ_FAILED", "Ошибка чтения файла из локального хранилища."));
            }
        }

        /// <inheritdoc/>
        public Task<bool> ExistsAsync(
            string storageKey,
            CancellationToken ct = default)
            => Task.FromResult(File.Exists(BuildFullPath(storageKey)));

        /// <inheritdoc/>
        public Task<Result> DeleteAsync(
            string storageKey,
            CancellationToken ct = default)
        {
            try
            {
                var fullPath = BuildFullPath(storageKey);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                return Task.FromResult(Result.Success());
            }
            catch (Exception)
            {
                return Task.FromResult(
                    Result.Failure(
                        Error.Failure("STORAGE.DELETE_FAILED", "Ошибка удаления файла из локального хранилища.")));
            }
        }

        private string BuildFullPath(string storageKey)
            => Path.Combine(_basePath, storageKey.Replace('/', Path.DirectorySeparatorChar));
    }
}
