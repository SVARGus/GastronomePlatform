using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Media.Application.Abstractions;
using GastronomePlatform.Modules.Media.Domain.Enums;
using GastronomePlatform.Modules.Media.Domain.Errors;
using GastronomePlatform.Modules.Media.Domain.Repositories;

namespace GastronomePlatform.Modules.Media.Application.Queries.GetThumbnail
{
    /// <summary>
    /// Обработчик запроса <see cref="GetThumbnailQuery"/> (UC-MED-003).
    /// </summary>
    /// <remarks>
    /// Миниатюры доступны всем пользователям, включая гостей, независимо от
    /// <c>DataCategory</c> (POL-002, Этап 2).
    /// </remarks>
    public sealed class GetThumbnailQueryHandler : IQueryHandler<GetThumbnailQuery, GetThumbnailResult>
    {
        private readonly IMediaFileRepository _repository;
        private readonly IFileStorage _fileStorage;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetThumbnailQueryHandler"/>.
        /// </summary>
        /// <param name="repository">Репозиторий медиафайлов.</param>
        /// <param name="fileStorage">Хранилище файлов.</param>
        public GetThumbnailQueryHandler(
            IMediaFileRepository repository,
            IFileStorage fileStorage)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        }

        /// <inheritdoc/>
        public async Task<Result<GetThumbnailResult>> Handle(GetThumbnailQuery request, CancellationToken cancellationToken)
        {
            var mediaFile = await _repository.GetByIdWithThumbnailsAsync(request.MediaId, cancellationToken);
            if (mediaFile is null)
            {
                return MediaErrors.NotFound;
                }

            if (mediaFile.Status is MediaStatus.Deleted or MediaStatus.Failed)
            {
                return MediaErrors.NotFound;
            }

            if (mediaFile.Status is MediaStatus.Uploaded or MediaStatus.Processing)
            {
                return MediaErrors.NotReady;
            }

            var thumbnail = mediaFile.Thumbnails.FirstOrDefault(
                t => t.Size == request.Size && t.Format == request.Format);

            if (thumbnail is null)
            {
                return MediaErrors.NotFound;
            }

            var streamResult = await _fileStorage.OpenReadAsync(thumbnail.StorageKey, cancellationToken);
            if (streamResult.IsFailure)
            {
                return MediaErrors.NotFound;
            }

            var contentType = request.Format switch
            {
                ThumbnailFormat.WebP => "image/webp",
                ThumbnailFormat.Avif => "image/avif",
                _                   => "image/jpeg"
            };

            return new GetThumbnailResult(streamResult.Value, contentType);
        }
    }
}
