using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Auth.Application.Abstractions;
using GastronomePlatform.Modules.Auth.Application.DTOs;
using GastronomePlatform.Modules.Auth.Domain.Entities;
using GastronomePlatform.Modules.Auth.Domain.Errors;
using GastronomePlatform.Modules.Auth.Domain.Repositories;

namespace GastronomePlatform.Modules.Auth.Application.Commands.RefreshAccessToken
{
    /// <summary>
    /// Обработчик команды обновления пары токенов.
    /// </summary>
    public sealed class RefreshAccessTokenCommandHandler : ICommandHandler<RefreshAccessTokenCommand, LoginResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IDateTimeProvider _dateTimeProvider;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="RefreshAccessTokenCommandHandler"/>.
        /// </summary>
        /// <param name="userRepository">Репозиторий пользователей.</param>
        /// <param name="refreshTokenRepository">Репозиторий refresh-токенов.</param>
        /// <param name="jwtService">Сервис генерации JWT-токенов.</param>
        /// <param name="dateTimeProvider">Провайдер текущего времени.</param>
        public RefreshAccessTokenCommandHandler(IUserRepository userRepository, IJwtService jwtService,
            IRefreshTokenRepository refreshTokenRepository, IDateTimeProvider dateTimeProvider)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
            _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        /// <inheritdoc/>
        public async Task<Result<LoginResponse>> Handle(RefreshAccessTokenCommand request, CancellationToken cancellationToken)
        {
            // 1. Находим токен в БД по значению
            RefreshToken? refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

            if (refreshToken is null)
            {
                return AuthErrors.InvalidToken;
            }

            // 2. Проверить активность токена (не истёк и не отозван)
            if (!refreshToken.IsActive)
            {
                return AuthErrors.InvalidToken;
            }

            // 3. Получить данные пользователя по UserId из токена
            AuthUserInfo? userInfo = await _userRepository.GetAuthUserInfoByIdAsync(refreshToken.UserId, cancellationToken);

            if (userInfo is null)
            {
                return AuthErrors.UserNotFound;
            }

            // 4. Получить роль пользователя
            string? role = await _userRepository.GetUserRoleAsync(refreshToken.UserId, cancellationToken);

            if (role is null)
            {
                return AuthErrors.InvalidCredentials;
            }

            // 5. Отозвать старый токен — доменная логика на объекте
            // EF Core отследит изменение и обновит запись при SaveChangesAsync
            refreshToken.Revoke(_dateTimeProvider.UtcNow);

            // 6. Гененрируем новую пару токенов
            string newAccessToken = _jwtService.GenerateAccessToken(userInfo.Id, userInfo.Email, role);
            string newRefreshTokenValue = _jwtService.GenerateRefreshToken();

            // 7. Создать и сохранить новый RefreshToken
            DateTimeOffset expiresAt = _dateTimeProvider.UtcNow.AddDays(_jwtService.RefreshTokenExpiryDays);

            RefreshToken newRefreshToken = RefreshToken.Create(newRefreshTokenValue, userInfo.Id, expiresAt);

            await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

            // Один SaveChangesAsync фиксирует и отзыв старого и создание нового
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

            // 8. Вернуть новую пару токенов клиенту
            DateTimeOffset accessTokenExpiresAt = _dateTimeProvider.UtcNow.AddMinutes(_jwtService.AccessTokenExpiryMinutes);

            return new LoginResponse(newAccessToken, newRefreshTokenValue, accessTokenExpiresAt);
        }
    }
}
