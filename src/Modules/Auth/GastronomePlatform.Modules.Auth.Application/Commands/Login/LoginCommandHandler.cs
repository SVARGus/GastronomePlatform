using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Auth.Application.Abstractions;
using GastronomePlatform.Modules.Auth.Application.DTOs;
using GastronomePlatform.Modules.Auth.Domain.Entities;
using GastronomePlatform.Modules.Auth.Domain.Errors;
using GastronomePlatform.Modules.Auth.Domain.Repositories;

namespace GastronomePlatform.Modules.Auth.Application.Commands.Login
{
    public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IDateTimeProvider _dateTimeProvider;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="LoginCommandHandler"/>.
        /// </summary>
        /// <param name="userRepository">Репозиторий пользователей.</param>
        /// <param name="refreshTokenRepository">Репозиторий refresh-токенов.</param>
        /// <param name="jwtService">Сервис генерации JWT-токенов.</param>
        /// <param name="dateTimeProvider">Провайдер текущего времени.</param>
        public LoginCommandHandler(IUserRepository userRepository, IJwtService jwtService,
            IRefreshTokenRepository refreshTokenRepository, IDateTimeProvider dateTimeProvider)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
            _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // 1. Найти пользователя по логину (email, username или телефон)
            AuthUserInfo? userInfo = await _userRepository.FindByLoginAsync(request.Login, cancellationToken);

            if (userInfo is null)
            {
                return AuthErrors.InvalidCredentials;
            }

            // 2. Проверить пароль
            // Намеренно возвращаем InvalidCredentials — не раскрываем
            // пользователю существует ли аккаунт с таким логином
            bool passwordValid = await _userRepository.CheckPasswordAsync(userInfo.Id, request.Password, cancellationToken);
            if (!passwordValid)
            {
                return AuthErrors.InvalidCredentials;
            }

            // 3. Получить роль пользователя
            string? role = await _userRepository.GetUserRoleAsync(userInfo.Id, cancellationToken);

            if (role is null)
            {
                return AuthErrors.InvalidCredentials;
            }

            // 4. Удалить неактивные токены — предотвращаем накопление мусора
            await _refreshTokenRepository.DeleteInactiveByUserIdAsync(userInfo.Id, cancellationToken);

            // 5. Сгенерировать access token
            string accessToken = _jwtService.GenerateAccessToken(userInfo.Id, userInfo.Email, role);

            // 6. Сгенерировать refresh token
            string refreshTokenValue = _jwtService.GenerateRefreshToken();

            // 7. Создать доменный объект RefreshToken и сохранить в БД
            DateTimeOffset expiresAt = _dateTimeProvider.UtcNow.AddDays(_jwtService.RefreshTokenExpiryDays);

            RefreshToken refreshToken = RefreshToken.Create(refreshTokenValue, userInfo.Id, expiresAt);

            await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

            // 8. Вернуть токены клиенту
            DateTimeOffset accessTokenExpiresAt = _dateTimeProvider.UtcNow.AddMinutes(_jwtService.AccessTokenExpiryMinutes);

            return new LoginResponse(accessToken, refreshTokenValue, accessTokenExpiresAt);
        }
    }
}
