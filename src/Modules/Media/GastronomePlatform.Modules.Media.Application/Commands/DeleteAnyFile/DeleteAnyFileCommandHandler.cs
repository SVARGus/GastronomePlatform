using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Media.Application.Configuration;
using GastronomePlatform.Modules.Media.Domain.Entities;
using GastronomePlatform.Modules.Media.Domain.Enums;
using GastronomePlatform.Modules.Media.Domain.Errors;
using GastronomePlatform.Modules.Media.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Options;

namespace GastronomePlatform.Modules.Media.Application.Commands.DeleteAnyFile
{
    /// <summary>
    /// Обработчик команды <see cref="DeleteAnyFileCommand"/> (UC-MED-102).
    /// </summary>
    /// <remarks>
    /// Выполняет принудительный soft delete без проверки владельца.
    /// Если файл привязан к сущности (<see cref="MediaFile.EntityType"/> не null),
    /// выполняет отвязку через <see cref="MediaFile.DetachFromEntity"/> перед удалением,
    /// чтобы удовлетворить инварианту <see cref="MediaFile.SoftDelete"/>
    /// (<c>MEDIA.STILL_ATTACHED</c>).
    /// </remarks>
    public sealed class DeleteAnyFileCommandHandler : ICommandHandler<DeleteAnyFileCommand>
    {
        private readonly IMediaFileRepository _repository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IOptions<MediaOptions> _options;
        private readonly IPublisher _publisher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DeleteAnyFileCommandHandler"/>.
        /// </summary>
        /// <param name="repository">Репозиторий медиафайлов.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="options">Типизированные настройки модуля Media.</param>
        /// <param name="publisher">Издатель доменных событий MediatR.</param>
        public DeleteAnyFileCommandHandler(
            IMediaFileRepository repository,
            ICurrentUserService currentUser,
            IDateTimeProvider clock,
            IOptions<MediaOptions> options,
            IPublisher publisher)
        {
            _repository  = repository  ?? throw new ArgumentNullException(nameof(repository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _clock       = clock       ?? throw new ArgumentNullException(nameof(clock));
            _options     = options     ?? throw new ArgumentNullException(nameof(options));
            _publisher   = publisher   ?? throw new ArgumentNullException(nameof(publisher));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(DeleteAnyFileCommand request, CancellationToken cancellationToken)
        {
            // Defense-in-depth: контроллер защищён [Authorize(Roles="Admin")].
            if (!_currentUser.IsInRole(PlatformRoles.ADMIN))
            {
                return MediaErrors.ForbiddenNotOwner;
            }

            var utcNow = _clock.UtcNow;

            var mediaFile = await _repository.GetByIdAsync(request.MediaId, cancellationToken);
            if (mediaFile is null)
            {
                return MediaErrors.NotFound;
            }

            if (mediaFile.Status == MediaStatus.Deleted)
            {
                return MediaErrors.AlreadyDeleted;
            }

            // Если файл привязан к сущности — сначала отвязываем,
            // чтобы удовлетворить инварианту SoftDelete (MEDIA.STILL_ATTACHED).
            if (mediaFile.EntityType is not null)
            {
                var orphanTimeout = TimeSpan.FromHours(_options.Value.Orphan.ExpirationHours);
                var detachResult = mediaFile.DetachFromEntity(orphanTimeout, utcNow);
                if (detachResult.IsFailure)
                {
                    return detachResult.Error;
                }
            }

            var deleteResult = mediaFile.SoftDelete(utcNow);
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
