using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Media.Domain.Enums;
using GastronomePlatform.Modules.Media.Domain.Errors;
using GastronomePlatform.Modules.Media.Domain.Repositories;

namespace GastronomePlatform.Modules.Media.Application.Queries.GetFileMetadata
{
    /// <summary>
    /// Обработчик запроса <see cref="GetFileMetadataQuery"/> (UC-MED-004).
    /// </summary>
    public sealed class GetFileMetadataQueryHandler : IQueryHandler<GetFileMetadataQuery, FileMetadataResult>
    {
        private readonly IMediaFileRepository _repository;
        private readonly ICurrentUserService _currentUser;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetFileMetadataQueryHandler"/>.
        /// </summary>
        /// <param name="repository">Репозиторий медиафайлов.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        public GetFileMetadataQueryHandler(
            IMediaFileRepository repository,
            ICurrentUserService currentUser)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        }

        /// <inheritdoc/>
        public async Task<Result<FileMetadataResult>> Handle(GetFileMetadataQuery request, CancellationToken cancellationToken)
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

            // POL-002: Personal-метаданные — только аутентифицированным
            if (mediaFile.DataCategory == MediaDataCategory.Personal && !_currentUser.IsAuthenticated)
            {
                return MediaErrors.Unauthorized;
            }

            var thumbnails = mediaFile.Thumbnails
                .Select(t => new ThumbnailInfoResult(t.Size, t.Format, t.Width, t.Height, t.SizeBytes))
                .ToList();

            return new FileMetadataResult(
                mediaFile.Id,
                mediaFile.ContentType,
                mediaFile.SizeBytes,
                mediaFile.Width,
                mediaFile.Height,
                mediaFile.Status,
                mediaFile.EntityType,
                mediaFile.EntityId,
                mediaFile.CreatedAt,
                thumbnails);
        }
    }
}
