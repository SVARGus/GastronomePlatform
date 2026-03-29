using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Auth.Domain.Entities;
using GastronomePlatform.Modules.Auth.Domain.Errors;
using GastronomePlatform.Modules.Auth.Domain.Repositories;

namespace GastronomePlatform.Modules.Auth.Application.Commands.Logout
{
    /// <summary>
    /// Обработчик команды разлогирования.
    /// </summary>
    public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand>
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IDateTimeProvider _dateTimeProvider;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="LogoutCommandHandler"/>.
        /// </summary>
        /// <param name="refreshTokenRepository">Репозиторий refresh-токенов.</param>
        /// <param name="dateTimeProvider">Провайдер текущего времени.</param>
        public LogoutCommandHandler(IRefreshTokenRepository refreshTokenRepository, IDateTimeProvider dateTimeProvider)
        {
            _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            // 1. Находим токен в БД по значению
            RefreshToken? refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

            if (refreshToken is null)
            {
                return AuthErrors.InvalidToken;
            }

            // 2. Отозвать старый токен — доменная логика на объекте
            refreshToken.Revoke(_dateTimeProvider.UtcNow);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
