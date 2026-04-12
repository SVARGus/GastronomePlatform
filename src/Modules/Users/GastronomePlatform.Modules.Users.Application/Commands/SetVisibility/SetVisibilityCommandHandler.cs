using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Users.Domain.Entities;
using GastronomePlatform.Modules.Users.Domain.Errors;
using GastronomePlatform.Modules.Users.Domain.Repositories;

namespace GastronomePlatform.Modules.Users.Application.Commands.SetVisibility
{
    /// <summary>
    /// Обработчик изменения видимости профиля пользователя.
    /// </summary>
    public sealed class SetVisibilityCommandHandler : ICommandHandler<SetVisibilityCommand>
    {
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IDateTimeProvider _dateTimeProvider;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SetVisibilityCommandHandler"/>.
        /// </summary>
        /// <param name="userProfileRepository">Репозиторий профилей пользователей.</param>
        /// <param name="dateTimeProvider">Провайдер текущего времени.</param>
        public SetVisibilityCommandHandler(IUserProfileRepository userProfileRepository, IDateTimeProvider dateTimeProvider)
        {
            _userProfileRepository = userProfileRepository ?? throw new ArgumentNullException(nameof(userProfileRepository));
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(SetVisibilityCommand request, CancellationToken cancellationToken)
        {
            UserProfile? userProfile = await _userProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken);

            if (userProfile is null)
            {
                return UsersErrors.ProfileNotFound;
            }

            userProfile.SetVisibility(request.IsPublic, _dateTimeProvider.UtcNow);

            await _userProfileRepository.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
