using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Modules.Auth.Domain.Events;
using GastronomePlatform.Modules.Users.Domain.Entities;
using GastronomePlatform.Modules.Users.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GastronomePlatform.Modules.Users.Application.EventHandlers
{
    /// <summary>
    /// Обработчик события регистрации пользователя.
    /// Создаёт профиль пользователя в модуле Users при регистрации через Auth.
    /// </summary>
    public sealed class UserRegisteredEventHandler : INotificationHandler<UserRegisteredEvent>
    {
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<UserRegisteredEventHandler> _logger;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UserRegisteredEventHandler"/>.
        /// </summary>
        /// <param name="userProfileRepository">Репозиторий профилей пользователей.</param>
        /// <param name="dateTimeProvider">Провайдер текущего времени.</param>
        /// <param name="logger">Логгер.</param>
        public UserRegisteredEventHandler(IUserProfileRepository userProfileRepository, IDateTimeProvider dateTimeProvider,
            ILogger<UserRegisteredEventHandler> logger)
        {
            _userProfileRepository = userProfileRepository ?? throw new ArgumentNullException(nameof(userProfileRepository));
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
        {
            // Idempotency — защита от повторной обработки события
            bool profileExists = await _userProfileRepository.ExistsAsync(notification.UserId, cancellationToken);

            if (profileExists)
            {
                _logger.LogWarning(
                    "Профиль пользователя {UserId} уже существует. " +
                    "Повторная обработка UserRegisteredEvent пропущена.",
                    notification.UserId);

                return;
            }

            UserProfile profile = UserProfile.Create(
                notification.UserId,
                notification.Email,
                notification.UserName,
                notification.PhoneNumber,
                _dateTimeProvider.UtcNow);

            await _userProfileRepository.AddAsync(profile, cancellationToken);
            await _userProfileRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Профиль пользователя {UserId} успешно создан.",
                notification.UserId);
        }
    }
}
