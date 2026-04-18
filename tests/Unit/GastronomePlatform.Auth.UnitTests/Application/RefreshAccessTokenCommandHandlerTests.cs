using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Auth.Application.Abstractions;
using GastronomePlatform.Modules.Auth.Application.Commands.RefreshAccessToken;
using GastronomePlatform.Modules.Auth.Application.DTOs;
using GastronomePlatform.Modules.Auth.Domain.Entities;
using GastronomePlatform.Modules.Auth.Domain.Errors;
using GastronomePlatform.Modules.Auth.Domain.Repositories;
using Moq;

namespace GastronomePlatform.Auth.UnitTests.Application
{
    /// <summary>
    /// Тесты для <see cref="RefreshAccessTokenCommandHandler"/>.
    /// </summary>
    public sealed class RefreshAccessTokenCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IJwtService> _jwtServiceMock = new();
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
        private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
        private readonly RefreshAccessTokenCommandHandler _handler;

        private static readonly Guid _userId = Guid.NewGuid();
        private const string EMAIL = "user@example.com";
        private const string ROLE = "User";
        private const string OLD_REFRESH_TOKEN = "old-refresh-token";
        private const string NEW_ACCESS_TOKEN = "new-access-token";
        private const string NEW_REFRESH_TOKEN = "new-refresh-token";
        private const int ACCESS_EXPIRY_MINUTES = 15;
        private const int REFRESH_EXPIRY_DAYS = 30;
        private static readonly DateTimeOffset _now =
            new(2026, 4, 12, 12, 0, 0, TimeSpan.Zero);

        public RefreshAccessTokenCommandHandlerTests()
        {
            _handler = new RefreshAccessTokenCommandHandler(
                _userRepositoryMock.Object,
                _jwtServiceMock.Object,
                _refreshTokenRepositoryMock.Object,
                _dateTimeProviderMock.Object);
        }

        /// <summary>
        /// Создаёт активный refresh-токен (ExpiresAt в будущем относительно реального UtcNow —
        /// IsActive у RefreshToken зависит от системных часов, не от IDateTimeProvider).
        /// </summary>
        private static RefreshToken CreateActiveToken(string value = OLD_REFRESH_TOKEN) =>
            RefreshToken.Create(value, _userId, DateTimeOffset.UtcNow.AddDays(30));

        /// <summary>
        /// Настраивает моки на успешный сценарий.
        /// </summary>
        private void SetupSuccessfulFlow(RefreshToken activeToken)
        {
            _refreshTokenRepositoryMock
                .Setup(r => r.GetByTokenAsync(OLD_REFRESH_TOKEN, It.IsAny<CancellationToken>()))
                .ReturnsAsync(activeToken);
            _userRepositoryMock
                .Setup(r => r.GetAuthUserInfoByIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthUserInfo(_userId, EMAIL));
            _userRepositoryMock
                .Setup(r => r.GetUserRoleAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ROLE);
            _jwtServiceMock.SetupGet(j => j.AccessTokenExpiryMinutes).Returns(ACCESS_EXPIRY_MINUTES);
            _jwtServiceMock.SetupGet(j => j.RefreshTokenExpiryDays).Returns(REFRESH_EXPIRY_DAYS);
            _jwtServiceMock
                .Setup(j => j.GenerateAccessToken(_userId, EMAIL, ROLE))
                .Returns(NEW_ACCESS_TOKEN);
            _jwtServiceMock
                .Setup(j => j.GenerateRefreshToken())
                .Returns(NEW_REFRESH_TOKEN);
            _dateTimeProviderMock.SetupGet(d => d.UtcNow).Returns(_now);
        }

        private static RefreshAccessTokenCommand CreateCommand(string token = OLD_REFRESH_TOKEN)
            => new(token);

        #region Constructor

        [Fact]
        public void Constructor_WithNullUserRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new RefreshAccessTokenCommandHandler(
                null!, _jwtServiceMock.Object, _refreshTokenRepositoryMock.Object, _dateTimeProviderMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("userRepository");
        }

        [Fact]
        public void Constructor_WithNullJwtService_ShouldThrowArgumentNullException()
        {
            Action action = () => new RefreshAccessTokenCommandHandler(
                _userRepositoryMock.Object, null!, _refreshTokenRepositoryMock.Object, _dateTimeProviderMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("jwtService");
        }

        [Fact]
        public void Constructor_WithNullRefreshTokenRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new RefreshAccessTokenCommandHandler(
                _userRepositoryMock.Object, _jwtServiceMock.Object, null!, _dateTimeProviderMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("refreshTokenRepository");
        }

        [Fact]
        public void Constructor_WithNullDateTimeProvider_ShouldThrowArgumentNullException()
        {
            Action action = () => new RefreshAccessTokenCommandHandler(
                _userRepositoryMock.Object, _jwtServiceMock.Object, _refreshTokenRepositoryMock.Object, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("dateTimeProvider");
        }

        #endregion

        #region Success

        [Fact]
        public async Task Handle_WithValidToken_ReturnsLoginResponseWithExpectedFieldsAsync()
        {
            // Arrange
            SetupSuccessfulFlow(CreateActiveToken());

            // Act
            Result<LoginResponse> result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                result.Value.AccessToken.Should().Be(NEW_ACCESS_TOKEN);
                result.Value.RefreshToken.Should().Be(NEW_REFRESH_TOKEN);
                result.Value.ExpiresAt.Should().Be(_now.AddMinutes(ACCESS_EXPIRY_MINUTES));
            }
        }

        [Fact]
        public async Task Handle_WithValidToken_PerformsTokenRotationAsync()
        {
            // Arrange — сохраняем ссылку на старый токен, чтобы потом проверить его отзыв
            RefreshToken oldToken = CreateActiveToken();
            SetupSuccessfulFlow(oldToken);

            // Act
            await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert — Token Rotation: старый отозван, новый создан, один SaveChanges
            using (new AssertionScope())
            {
                oldToken.RevokedAt.Should().Be(_now);
                oldToken.IsActive.Should().BeFalse();

                _jwtServiceMock.Verify(
                    j => j.GenerateAccessToken(_userId, EMAIL, ROLE),
                    Times.Once);

                _refreshTokenRepositoryMock.Verify(
                    r => r.AddAsync(
                        It.Is<RefreshToken>(rt =>
                            rt.Token == NEW_REFRESH_TOKEN &&
                            rt.UserId == _userId &&
                            rt.ExpiresAt == _now.AddDays(REFRESH_EXPIRY_DAYS)),
                        It.IsAny<CancellationToken>()),
                    Times.Once);

                // Один SaveChanges фиксирует и отзыв старого, и создание нового
                _refreshTokenRepositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        #endregion

        #region Failure — невалидный токен

        [Fact]
        public async Task Handle_WhenTokenNotFound_ReturnsInvalidTokenAndDoesNotProceedAsync()
        {
            // Arrange
            _refreshTokenRepositoryMock
                .Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RefreshToken?)null);

            // Act
            Result<LoginResponse> result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(AuthErrors.InvalidToken);

                _userRepositoryMock.Verify(
                    r => r.GetAuthUserInfoByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Never);
                _refreshTokenRepositoryMock.Verify(
                    r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
                    Times.Never);
                _refreshTokenRepositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenTokenExpired_ReturnsInvalidTokenAndDoesNotProceedAsync()
        {
            // Arrange — токен в БД есть, но истёк
            RefreshToken expired = RefreshToken.Create(
                OLD_REFRESH_TOKEN, _userId, DateTimeOffset.UtcNow.AddSeconds(-10));

            _refreshTokenRepositoryMock
                .Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expired);

            // Act
            Result<LoginResponse> result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(AuthErrors.InvalidToken);

                _userRepositoryMock.Verify(
                    r => r.GetAuthUserInfoByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Never);
                _refreshTokenRepositoryMock.Verify(
                    r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenTokenRevoked_ReturnsInvalidTokenAndDoesNotProceedAsync()
        {
            // Arrange — токен в БД есть, но уже отозван
            RefreshToken revoked = CreateActiveToken();
            revoked.Revoke(DateTimeOffset.UtcNow);

            _refreshTokenRepositoryMock
                .Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(revoked);

            // Act
            Result<LoginResponse> result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(AuthErrors.InvalidToken);

                _refreshTokenRepositoryMock.Verify(
                    r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        #endregion

        #region Failure — пользователь / роль

        [Fact]
        public async Task Handle_WhenUserNotFound_ReturnsUserNotFoundErrorAndDoesNotIssueTokensAsync()
        {
            // Arrange — токен активен, но пользователь по UserId из токена не найден
            _refreshTokenRepositoryMock
                .Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateActiveToken());
            _userRepositoryMock
                .Setup(r => r.GetAuthUserInfoByIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((AuthUserInfo?)null);

            // Act
            Result<LoginResponse> result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(AuthErrors.UserNotFound);

                _userRepositoryMock.Verify(
                    r => r.GetUserRoleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Never);
                _jwtServiceMock.Verify(
                    j => j.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()),
                    Times.Never);
                _refreshTokenRepositoryMock.Verify(
                    r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenRoleIsNull_ReturnsInvalidCredentialsAndDoesNotIssueTokensAsync()
        {
            // Arrange — пользователь найден, но роль отсутствует (битое состояние Identity)
            _refreshTokenRepositoryMock
                .Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateActiveToken());
            _userRepositoryMock
                .Setup(r => r.GetAuthUserInfoByIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthUserInfo(_userId, EMAIL));
            _userRepositoryMock
                .Setup(r => r.GetUserRoleAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);

            // Act
            Result<LoginResponse> result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(AuthErrors.InvalidCredentials);

                _jwtServiceMock.Verify(
                    j => j.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()),
                    Times.Never);
                _refreshTokenRepositoryMock.Verify(
                    r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        #endregion
    }
}
