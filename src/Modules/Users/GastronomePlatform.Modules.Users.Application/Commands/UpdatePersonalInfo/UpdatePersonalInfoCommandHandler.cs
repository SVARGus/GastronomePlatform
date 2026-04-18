using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Users.Domain.Entities;
using GastronomePlatform.Modules.Users.Domain.Errors;
using GastronomePlatform.Modules.Users.Domain.Repositories;

namespace GastronomePlatform.Modules.Users.Application.Commands.UpdatePersonalInfo
{
    /// <summary>
    /// Обработчик обновления персональных данных профиля пользователя.
    /// </summary>
    public sealed class UpdatePersonalInfoCommandHandler : ICommandHandler<UpdatePersonalInfoCommand>
    {
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IDateTimeProvider _dateTimeProvider;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UpdatePersonalInfoCommandHandler"/>.
        /// </summary>
        /// <param name="userProfileRepository">Репозиторий профиля пользователей</param>
        /// <param name="dateTimeProvider">Провайдер текущего времени.</param>
        public UpdatePersonalInfoCommandHandler(IUserProfileRepository userProfileRepository, IDateTimeProvider dateTimeProvider)
        {
            _userProfileRepository = userProfileRepository ?? throw new ArgumentNullException(nameof(userProfileRepository));
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(UpdatePersonalInfoCommand request, CancellationToken cancellationToken)
        {
            UserProfile? userProfile = await _userProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken);

            if (userProfile is null)
            {
                return UsersErrors.ProfileNotFound;
            }

            DateTimeOffset now = _dateTimeProvider.UtcNow;

            userProfile.UpdatePersonalInfo(
                request.FirstName,
                request.LastName,
                request.MiddleName,
                request.DisplayName,
                now);

            userProfile.UpdateBio(
                request.Bio,
                now);

            userProfile.UpdatePersonalDetails(
                request.Gender,
                request.DateOfBirth,
                now);

            await _userProfileRepository.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
