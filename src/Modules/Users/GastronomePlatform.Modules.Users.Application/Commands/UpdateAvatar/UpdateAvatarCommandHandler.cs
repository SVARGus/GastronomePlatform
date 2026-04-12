using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Users.Domain.Entities;
using GastronomePlatform.Modules.Users.Domain.Errors;
using GastronomePlatform.Modules.Users.Domain.Repositories;

namespace GastronomePlatform.Modules.Users.Application.Commands.UpdateAvatar
{
    /// <summary>
    /// Обработчик обновления аватара пользователя.
    /// </summary>
    public sealed class UpdateAvatarCommandHandler : ICommandHandler<UpdateAvatarCommand>
    {
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IDateTimeProvider _dateTimeProvider;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UpdateAvatarCommandHandler"/>.
        /// </summary>
        /// <param name="userProfileRepository">Репозиторий профилей пользователей.</param>
        /// <param name="dateTimeProvider">Провайдер текущего времени.</param>
        public UpdateAvatarCommandHandler(IUserProfileRepository userProfileRepository, IDateTimeProvider dateTimeProvider)
        {
            _userProfileRepository = userProfileRepository ?? throw new ArgumentNullException(nameof(userProfileRepository));
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(UpdateAvatarCommand request, CancellationToken cancellationToken)
        {
            UserProfile? userProfile = await _userProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken);

            if (userProfile is null)
            {
                return UsersErrors.ProfileNotFound;
            }

            userProfile.UpdateAvatar(request.AvatarMediaId, _dateTimeProvider.UtcNow);

            await _userProfileRepository.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
