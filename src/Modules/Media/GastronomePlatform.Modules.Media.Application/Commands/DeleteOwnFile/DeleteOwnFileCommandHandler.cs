using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Media.Domain.Entities;
using GastronomePlatform.Modules.Media.Domain.Enums;
using GastronomePlatform.Modules.Media.Domain.Errors;
using GastronomePlatform.Modules.Media.Domain.Repositories;
using MediatR;

namespace GastronomePlatform.Modules.Media.Application.Commands.DeleteOwnFile
{
    /// <summary>
    /// Обработчик команды <see cref="DeleteOwnFileCommand"/> (UC-MED-005).
    /// </summary>
    /// <remarks>
    /// Применяет политику POL-003:
    /// <list type="bullet">
    ///   <item>Системные файлы (<c>OwnerUserId IS NULL</c>) — нельзя удалить через этот UC.</item>
    ///   <item>Только владелец (actorUserId == OwnerUserId) может удалить файл.</item>
    ///   <item>Если файл привязан к сущности — домен возвращает <c>MEDIA.STILL_ATTACHED</c>.</item>
    /// </list>
    /// </remarks>
    public sealed class DeleteOwnFileCommandHandler : ICommandHandler<DeleteOwnFileCommand>
    {
        private readonly IMediaFileRepository _repository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IPublisher _publisher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DeleteOwnFileCommandHandler"/>.
        /// </summary>
        /// <param name="repository">Репозиторий медиафайлов.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="publisher">Издатель доменных событий MediatR.</param>
        public DeleteOwnFileCommandHandler(
            IMediaFileRepository repository,
            ICurrentUserService currentUser,
            IDateTimeProvider clock,
            IPublisher publisher)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(DeleteOwnFileCommand request, CancellationToken cancellationToken)
        {
            var actorUserId = _currentUser.UserId!.Value;

            var mediaFile = await _repository.GetByIdAsync(request.MediaId, cancellationToken);
            if (mediaFile is null)
            {
                return MediaErrors.NotFound;
            }

            if (mediaFile.Status == MediaStatus.Deleted)
            {
                return MediaErrors.AlreadyDeleted;
            }

            // POL-003: системные файлы запрещено удалять через пользовательский UC
            if (mediaFile.OwnerUserId is null)
            {
                return MediaErrors.ForbiddenSystemFile;
            }

            // POL-003: только владелец
            if (mediaFile.OwnerUserId != actorUserId)
            {
                return MediaErrors.ForbiddenNotOwner;
            }

            var deleteResult = mediaFile.SoftDelete(_clock.UtcNow);
            if (deleteResult.IsFailure)
            {
                return deleteResult.Error;
            }

            await _repository.SaveChangesAsync(cancellationToken);

            await PublishDomainEventsAsync(mediaFile, cancellationToken);

            return Result.Success();
        }

        private async Task PublishDomainEventsAsync(MediaFile mediaFile, CancellationToken ct)
        {
            var events = mediaFile.DomainEvents.ToList();
            mediaFile.ClearDomainEvents();
            foreach (var domainEvent in events)
            {
                await _publisher.Publish(domainEvent, ct);
            }
        }
    }
}
