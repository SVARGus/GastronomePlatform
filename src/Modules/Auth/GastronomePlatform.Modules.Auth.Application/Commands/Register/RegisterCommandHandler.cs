using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Auth.Domain.Errors;
using GastronomePlatform.Modules.Auth.Domain.Events;
using GastronomePlatform.Modules.Auth.Domain.Repositories;
using MediatR;

namespace GastronomePlatform.Modules.Auth.Application.Commands.Register
{
    /// <summary>
    /// Обработчик команды регистрации нового пользователя.
    /// </summary>
    public sealed class RegisterCommandHandler : ICommandHandler<RegisterCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPublisher _publisher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="RegisterCommandHandler"/>.
        /// </summary>
        /// <param name="userRepository">Репозиторий пользователей.</param>
        /// <param name="publisher">Издатель доменных событий MediatR.</param>
        public RegisterCommandHandler(IUserRepository userRepository, IPublisher publisher)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            // Проверяем уникальность каждого поля отдельно
            if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
            {
                return AuthErrors.EmailAlreadyTaken;
            }

            if (await _userRepository.ExistsByUserNameAsync(request.UserName, cancellationToken))
            {
                return AuthErrors.UserNameAlreadyTaken;
            }

            if (request.Phone is not null && await _userRepository.ExistsByPhoneAsync(request.Phone, cancellationToken))
            {
                return AuthErrors.PhonelAlreadyTaken;
            }

            // Создание пользователя
            Result<Guid> createResult = await _userRepository.CreateAsync(
                request.Email,
                request.UserName,
                request.Password,
                request.Phone,
                cancellationToken);

            // Если Identity отказал (слабый пароль, внутренняя ошибка и т.д.)
            if (createResult.IsFailure)
            {
                return Result.Failure(createResult.Error);
            }

            // Публикация доменного события - модуль Users создаст профиль
            await _publisher.Publish(
                new UserRegisteredEvent
                {
                    UserId = createResult.Value,
                    Email = request.Email,
                    UserName = request.UserName,
                    PhoneNumber = request.Phone
                },
                cancellationToken);

            return Result.Success();
        }
    }
}
