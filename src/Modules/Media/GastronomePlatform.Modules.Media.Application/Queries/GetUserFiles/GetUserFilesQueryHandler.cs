using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Media.Domain.Errors;
using GastronomePlatform.Modules.Media.Domain.Repositories;

namespace GastronomePlatform.Modules.Media.Application.Queries.GetUserFiles
{
    /// <summary>
    /// Обработчик запроса <see cref="GetUserFilesQuery"/> (UC-MED-103).
    /// </summary>
    public sealed class GetUserFilesQueryHandler
        : IQueryHandler<GetUserFilesQuery, GetUserFilesResult>
    {
        private readonly IMediaFileRepository _repository;
        private readonly ICurrentUserService _currentUser;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetUserFilesQueryHandler"/>.
        /// </summary>
        /// <param name="repository">Репозиторий медиафайлов.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        public GetUserFilesQueryHandler(
            IMediaFileRepository repository,
            ICurrentUserService currentUser)
        {
            _repository  = repository  ?? throw new ArgumentNullException(nameof(repository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        }

        /// <inheritdoc/>
        public async Task<Result<GetUserFilesResult>> Handle(
            GetUserFilesQuery request,
            CancellationToken cancellationToken)
        {
            // Defense-in-depth: контроллер защищён [Authorize(Roles="Admin")].
            if (!_currentUser.IsInRole(PlatformRoles.ADMIN))
            {
                return MediaErrors.ForbiddenNotOwner;
            }

            var files = await _repository.ListByOwnerAsync(
                request.UserId,
                request.Status,
                request.EntityType,
                request.Page,
                request.PageSize,
                cancellationToken);

            var items = files
                .Select(f => new UserFileItemResult(
                    f.Id,
                    f.ContentType,
                    f.SizeBytes,
                    f.Width,
                    f.Height,
                    f.Status,
                    f.EntityType,
                    f.EntityId,
                    f.CreatedAt,
                    f.AttachedAt,
                    f.DeletedAt))
                .ToList();

            return new GetUserFilesResult(items);
        }
    }
}
