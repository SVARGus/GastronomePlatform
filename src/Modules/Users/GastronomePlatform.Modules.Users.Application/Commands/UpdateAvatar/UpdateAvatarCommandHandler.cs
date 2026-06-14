using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Media.Application.Contracts;
using GastronomePlatform.Modules.Media.Domain.Constants;
using GastronomePlatform.Modules.Users.Domain.Entities;
using GastronomePlatform.Modules.Users.Domain.Errors;
using GastronomePlatform.Modules.Users.Domain.Repositories;

namespace GastronomePlatform.Modules.Users.Application.Commands.UpdateAvatar
{
    /// <summary>
    /// Обработчик обновления аватара пользователя.
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Owner-check: пользователь редактирует только свой профиль
    ///         (<see cref="UsersErrors.NotAuthorized"/> при несовпадении).</item>
    ///   <item>Загрузка профиля. <see cref="UsersErrors.ProfileNotFound"/> при отсутствии.</item>
    ///   <item>Сохранение текущего <c>AvatarMediaId</c> для последующего detach.</item>
    ///   <item>Вызов <see cref="UserProfile.UpdateAvatar"/> — Domain обновляет поле и
    ///         фиксирует <c>UpdatedAt</c>.</item>
    ///   <item>Синхронизация Media через <see cref="IMediaService"/> по разнице old/new
    ///         (4 ветки: без изменений / стало null / добавлено / заменено).</item>
    ///   <item>Сохранение.</item>
    /// </list>
    /// <para>
    /// <b>Consistency на Этапе 2:</b> Users и Media — две разные БД-транзакции
    /// (Outbox не используется). Возможна частичная согласованность при сбое SaveChanges
    /// Users после успешного attach Media — orphan-cleanup закроет UC-MED-210 (Этап 8+).
    /// </para>
    /// </remarks>
    public sealed class UpdateAvatarCommandHandler : ICommandHandler<UpdateAvatarCommand>
    {
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ICurrentUserService _currentUser;
        private readonly IMediaService _mediaService;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UpdateAvatarCommandHandler"/>.
        /// </summary>
        /// <param name="userProfileRepository">Репозиторий профилей пользователей.</param>
        /// <param name="dateTimeProvider">Провайдер текущего времени.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="mediaService">Межмодульный сервис модуля Media.</param>
        public UpdateAvatarCommandHandler(
            IUserProfileRepository userProfileRepository,
            IDateTimeProvider dateTimeProvider,
            ICurrentUserService currentUser,
            IMediaService mediaService)
        {
            _userProfileRepository = userProfileRepository ?? throw new ArgumentNullException(nameof(userProfileRepository));
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(UpdateAvatarCommand request, CancellationToken cancellationToken)
        {
            Guid actorUserId = _currentUser.UserId!.Value;
            if (request.UserId != actorUserId)
            {
                return UsersErrors.NotAuthorized;
            }

            UserProfile? userProfile = await _userProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
            if (userProfile is null)
            {
                return UsersErrors.ProfileNotFound;
            }

            Guid? oldAvatarMediaId = userProfile.AvatarMediaId;
            Guid? newAvatarMediaId = request.AvatarMediaId;

            userProfile.UpdateAvatar(newAvatarMediaId, _dateTimeProvider.UtcNow);

            if (oldAvatarMediaId != newAvatarMediaId)
            {
                if (oldAvatarMediaId.HasValue)
                {
                    Result detachResult = await _mediaService.DetachFromEntityAsync(
                        oldAvatarMediaId.Value, cancellationToken);
                    if (detachResult.IsFailure)
                    {
                        return detachResult.Error;
                    }
                }

                if (newAvatarMediaId.HasValue)
                {
                    Result attachResult = await _mediaService.AttachToEntityAsync(
                        mediaId: newAvatarMediaId.Value,
                        actorUserId: actorUserId,
                        entityType: MediaEntityTypes.USER_AVATAR,
                        entityId: request.UserId,
                        ct: cancellationToken);
                    if (attachResult.IsFailure)
                    {
                        return attachResult.Error;
                    }
                }
            }

            await _userProfileRepository.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
