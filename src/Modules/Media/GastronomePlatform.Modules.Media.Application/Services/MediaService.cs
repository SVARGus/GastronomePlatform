using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Media.Application.Configuration;
using GastronomePlatform.Modules.Media.Application.Contracts;
using GastronomePlatform.Modules.Media.Domain.Enums;
using GastronomePlatform.Modules.Media.Domain.Errors;
using GastronomePlatform.Modules.Media.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace GastronomePlatform.Modules.Media.Application.Services
{
    /// <summary>
    /// Реализация межмодульного контракта <see cref="IMediaService"/>.
    /// Предоставляет операции UC-MED-200–204 для вызовов из других модулей
    /// (Dishes, Users и т.п.) без выхода за пределы модуля Media.
    /// </summary>
    public sealed class MediaService : IMediaService
    {
        private readonly IMediaFileRepository _repository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IOptions<MediaOptions> _options;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="MediaService"/>.
        /// </summary>
        /// <param name="repository">Репозиторий медиафайлов.</param>
        /// <param name="currentUser">Сервис текущего пользователя (используется для проверки роли Admin при операциях с системными файлами).</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="options">Типизированные настройки модуля Media.</param>
        public MediaService(
            IMediaFileRepository repository,
            ICurrentUserService currentUser,
            IDateTimeProvider clock,
            IOptions<MediaOptions> options)
        {
            _repository  = repository  ?? throw new ArgumentNullException(nameof(repository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _clock       = clock       ?? throw new ArgumentNullException(nameof(clock));
            _options     = options     ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc/>
        public async Task<Result<MediaMetadataDto>> GetMetadataAsync(
            Guid mediaId,
            CancellationToken ct = default)
        {
            var file = await _repository.GetByIdAsync(mediaId, ct);
            if (file is null)
            {
                return MediaErrors.NotFound;
            }

            return MapToDto(file);
        }

        /// <inheritdoc/>
        public async Task<Result<IReadOnlyDictionary<Guid, MediaMetadataDto>>> GetMetadataBatchAsync(
            IReadOnlyCollection<Guid> mediaIds,
            CancellationToken ct = default)
        {
            var files = await _repository.GetBatchByIdsAsync(mediaIds, ct);

            IReadOnlyDictionary<Guid, MediaMetadataDto> result = files
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => MapToDto(kvp.Value));

            return Result<IReadOnlyDictionary<Guid, MediaMetadataDto>>.Success(result);
        }

        /// <inheritdoc/>
        public async Task<Result> AttachToEntityAsync(
            Guid mediaId,
            Guid actorUserId,
            string entityType,
            Guid entityId,
            CancellationToken ct = default)
        {
            var file = await _repository.GetByIdAsync(mediaId, ct);
            if (file is null)
            {
                return MediaErrors.NotFound;
            }

            if (file.Status != MediaStatus.Ready)
            {
                return MediaErrors.NotReady;
            }

            if (file.EntityType is not null)
            {
                return MediaErrors.AlreadyAttached;
            }

            // Проверка владения: пользовательский файл → actorUserId должен совпадать с OwnerUserId.
            // Системный файл (OwnerUserId IS NULL) → только Admin может прикрепить.
            if (file.OwnerUserId is not null)
            {
                if (file.OwnerUserId.Value != actorUserId)
                {
                    return MediaErrors.NotOwned;
                }
            }
            else
            {
                if (!_currentUser.IsInRole(PlatformRoles.ADMIN))
                {
                    return MediaErrors.NotOwned;
                }
            }

            var attachResult = file.AttachToEntity(entityType, entityId, _clock.UtcNow);
            if (attachResult.IsFailure)
            {
                return attachResult.Error;
            }

            await _repository.SaveChangesAsync(ct);
            return Result.Success();
        }

        /// <inheritdoc/>
        public async Task<Result> DetachFromEntityAsync(
            Guid mediaId,
            CancellationToken ct = default)
        {
            var file = await _repository.GetByIdAsync(mediaId, ct);
            if (file is null)
            {
                return MediaErrors.NotFound;
            }

            var orphanTimeout = TimeSpan.FromHours(_options.Value.Orphan.ExpirationHours);
            var detachResult = file.DetachFromEntity(orphanTimeout, _clock.UtcNow);
            if (detachResult.IsFailure)
            {
                return detachResult.Error;
            }

            await _repository.SaveChangesAsync(ct);
            return Result.Success();
        }

        /// <inheritdoc/>
        public async Task<Result> DeleteByEntityAsync(
            string entityType,
            Guid entityId,
            CancellationToken ct = default)
        {
            var files = await _repository.ListByEntityAsync(entityType, entityId, ct);
            if (files.Count == 0)
            {
                return Result.Success();
            }

            var utcNow = _clock.UtcNow;
            var orphanTimeout = TimeSpan.FromHours(_options.Value.Orphan.ExpirationHours);

            foreach (var file in files)
            {
                // Отвязываем перед удалением — SoftDelete требует EntityType == null.
                if (file.EntityType is not null)
                {
                    file.DetachFromEntity(orphanTimeout, utcNow);
                }

                file.SoftDelete(utcNow);
            }

            await _repository.SaveChangesAsync(ct);
            return Result.Success();
        }

        private static MediaMetadataDto MapToDto(Domain.Entities.MediaFile file) =>
            new(
                file.Id,
                file.OwnerUserId,
                file.DataCategory,
                file.EntityType,
                file.EntityId,
                file.Width,
                file.Height,
                file.Status,
                file.ContentType);
    }
}
