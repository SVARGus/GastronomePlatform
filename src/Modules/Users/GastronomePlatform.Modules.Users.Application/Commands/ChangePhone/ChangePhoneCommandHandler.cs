using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Auth.Domain.Contracts;
using GastronomePlatform.Modules.Users.Domain.Entities;
using GastronomePlatform.Modules.Users.Domain.Errors;
using GastronomePlatform.Modules.Users.Domain.Repositories;

namespace GastronomePlatform.Modules.Users.Application.Commands.ChangePhone
{
    /// <summary>
    /// Обработчик изменения номера телефона пользователя.
    /// </summary>
    public sealed class ChangePhoneCommandHandler : ICommandHandler<ChangePhoneCommand>
    {
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IAuthUserService _authUserService;
        private readonly IDateTimeProvider _dateTimeProvider;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="ChangePhoneCommandHandler"/>.
        /// </summary>
        /// <param name="userProfileRepository">Репозиторий профилей пользователей.</param>
        /// <param name="authUserService">Сервис учётных данных модуля Auth.</param>
        /// <param name="dateTimeProvider">Провайдер текущего времени.</param>
        public ChangePhoneCommandHandler(IUserProfileRepository userProfileRepository, IAuthUserService authUserService,
            IDateTimeProvider dateTimeProvider)
        {
            _userProfileRepository = userProfileRepository ?? throw new ArgumentNullException(nameof(userProfileRepository));
            _authUserService = authUserService ?? throw new ArgumentNullException(nameof(authUserService));
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(ChangePhoneCommand request, CancellationToken cancellationToken)
        {
            UserProfile? userProfile = await _userProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken);

            if (userProfile is null)
            {
                return UsersErrors.ProfileNotFound;
            }

            Result authResult = await _authUserService.ChangePhoneAsync(request.UserId, request.NewPhone, cancellationToken);

            if (authResult.IsFailure)
            {
                return authResult.Error;
            }

            userProfile.UpdateAuthMirrorData(
                userProfile.Email,
                request.NewPhone,
                userProfile.UserName,
                _dateTimeProvider.UtcNow);

            await _userProfileRepository.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
