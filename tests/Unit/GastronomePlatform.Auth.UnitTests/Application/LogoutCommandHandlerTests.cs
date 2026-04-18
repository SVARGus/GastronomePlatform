using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Auth.Application.Commands.Logout;
using GastronomePlatform.Modules.Auth.Domain.Entities;
using GastronomePlatform.Modules.Auth.Domain.Errors;
using GastronomePlatform.Modules.Auth.Domain.Repositories;
using Moq;

namespace GastronomePlatform.Auth.UnitTests.Application
{
    /// <summary>
    /// Тесты для <see cref="LogoutCommandHandler"/>.
    /// </summary>
    public sealed class LogoutCommandHandlerTests
    {
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
        private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
        private readonly LogoutCommandHandler _handler;

        private static readonly Guid _userId = Guid.NewGuid();
        private const string REFRESH_TOKEN = "refresh-token-value";
        private static readonly DateTimeOffset _now =
            new(2026, 4, 12, 12, 0, 0, TimeSpan.Zero);

        public LogoutCommandHandlerTests()
        {
            _handler = new LogoutCommandHandler(
                _refreshTokenRepositoryMock.Object,
                _dateTimeProviderMock.Object);
        }

        private static RefreshToken CreateActiveToken() =>
            RefreshToken.Create(REFRESH_TOKEN, _userId, DateTimeOffset.UtcNow.AddDays(30));

        private static LogoutCommand CreateCommand(string token = REFRESH_TOKEN) => new(token);

        #region Constructor

        [Fact]
        public void Constructor_WithNullRefreshTokenRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new LogoutCommandHandler(null!, _dateTimeProviderMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("refreshTokenRepository");
        }

        [Fact]
        public void Constructor_WithNullDateTimeProvider_ShouldThrowArgumentNullException()
        {
            Action action = () => new LogoutCommandHandler(_refreshTokenRepositoryMock.Object, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("dateTimeProvider");
        }

        #endregion

        #region Success

        [Fact]
        public async Task Handle_WithActiveToken_RevokesTokenAndReturnsSuccessAsync()
        {
            // Arrange — сохраняем ссылку, чтобы проверить отзыв после Handle
            RefreshToken activeToken = CreateActiveToken();

            _refreshTokenRepositoryMock
                .Setup(r => r.GetByTokenAsync(REFRESH_TOKEN, It.IsAny<CancellationToken>()))
                .ReturnsAsync(activeToken);
            _dateTimeProviderMock.SetupGet(d => d.UtcNow).Returns(_now);

            // Act
            Result result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                activeToken.RevokedAt.Should().Be(_now);
                activeToken.IsActive.Should().BeFalse();

                _refreshTokenRepositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Fact]
        public async Task Handle_WithAlreadyRevokedToken_ReturnsSuccessAsync()
        {
            // Arrange — идемпотентность: повторный logout не бросает ошибку.
            // RefreshToken.Revoke сам игнорирует вызов если токен неактивен,
            // поэтому handler возвращает Success без фактического изменения состояния.
            RefreshToken revokedToken = CreateActiveToken();
            DateTimeOffset originalRevokedAt = DateTimeOffset.UtcNow;
            revokedToken.Revoke(originalRevokedAt);

            _refreshTokenRepositoryMock
                .Setup(r => r.GetByTokenAsync(REFRESH_TOKEN, It.IsAny<CancellationToken>()))
                .ReturnsAsync(revokedToken);
            _dateTimeProviderMock.SetupGet(d => d.UtcNow).Returns(_now);

            // Act
            Result result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();

                // Исходная дата отзыва не перезаписана — Revoke игнорируется на неактивном токене
                revokedToken.RevokedAt.Should().Be(originalRevokedAt);
            }
        }

        #endregion

        #region Failure

        [Fact]
        public async Task Handle_WhenTokenNotFound_ReturnsInvalidTokenAndDoesNotSaveAsync()
        {
            // Arrange
            _refreshTokenRepositoryMock
                .Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RefreshToken?)null);

            // Act
            Result result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(AuthErrors.InvalidToken);

                _refreshTokenRepositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        #endregion
    }
}
