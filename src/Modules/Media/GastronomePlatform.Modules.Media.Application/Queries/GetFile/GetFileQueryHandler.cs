using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Media.Application.Abstractions;
using GastronomePlatform.Modules.Media.Domain.Enums;
using GastronomePlatform.Modules.Media.Domain.Errors;
using GastronomePlatform.Modules.Media.Domain.Repositories;

namespace GastronomePlatform.Modules.Media.Application.Queries.GetFile
{
    /// <summary>
    /// Обработчик запроса <see cref="GetFileQuery"/> (UC-MED-002).
    /// </summary>
    /// <remarks>
    /// Применяет правила POL-002 (Media Access Policy, Этап 2):
    /// <list type="bullet">
    ///   <item>Deleted / Failed → 404.</item>
    ///   <item>Uploaded / Processing → NotReady (425).</item>
    ///   <item>Personal + неаутентифицирован → Unauthorized (403).</item>
    ///   <item>Public → доступен всем, включая гостей.</item>
    /// </list>
    /// </remarks>
    public sealed class GetFileQueryHandler : IQueryHandler<GetFileQuery, GetFileResult>
    {
        private readonly IMediaFileRepository _repository;
        private readonly IFileStorage _fileStorage;
        private readonly ICurrentUserService _currentUser;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetFileQueryHandler"/>.
        /// </summary>
        /// <param name="repository">Репозиторий медиафайлов.</param>
        /// <param name="fileStorage">Хранилище файлов.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        public GetFileQueryHandler(
            IMediaFileRepository repository,
            IFileStorage fileStorage,
            ICurrentUserService currentUser)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        }

        /// <inheritdoc/>
        public async Task<Result<GetFileResult>> Handle(GetFileQuery request, CancellationToken cancellationToken)
        {
            var mediaFile = await _repository.GetByIdAsync(request.MediaId, cancellationToken);
            if (mediaFile is null)
            {
                return MediaErrors.NotFound;
            }

            // POL-002: статусы Deleted / Failed — 404
            if (mediaFile.Status is MediaStatus.Deleted or MediaStatus.Failed)
            {
                return MediaErrors.NotFound;
            }

            // POL-002: файл ещё не готов
            if (mediaFile.Status is MediaStatus.Uploaded or MediaStatus.Processing)
            {
                return MediaErrors.NotReady;
            }

            // POL-002 (Этап 2): Personal-оригинал — только аутентифицированным
            if (mediaFile.DataCategory == MediaDataCategory.Personal && !_currentUser.IsAuthenticated)
            {
                return MediaErrors.Unauthorized;
            }

            // Поток открывается последним — после всех проверок, чтобы не утечь при ошибке.
            var streamResult = await _fileStorage.OpenReadAsync(mediaFile.StorageKey, cancellationToken);
            if (streamResult.IsFailure)
            {
                return MediaErrors.NotFound;
            }

            return new GetFileResult(
                streamResult.Value,
                mediaFile.ContentType,
                mediaFile.OriginalFileName,
                mediaFile.SizeBytes);
        }
    }
}
