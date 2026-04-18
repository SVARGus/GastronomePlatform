using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Auth.Application.Abstractions;
using GastronomePlatform.Modules.Auth.Application.Commands.Login;
using GastronomePlatform.Modules.Auth.Application.DTOs;
using GastronomePlatform.Modules.Auth.Domain.Entities;
using GastronomePlatform.Modules.Auth.Domain.Errors;
using GastronomePlatform.Modules.Auth.Domain.Repositories;
using Moq;

namespace GastronomePlatform.Auth.UnitTests.Application
{
    /// <summary>
    /// Тесты для <see cref="LoginCommandHandler"/>.
    /// </summary>
    public sealed class LoginCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IJwtService> _jwtServiceMock = new();
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
        private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
        private readonly LoginCommandHandler _handler;

        private static readonly Guid _userId = Guid.NewGuid();
        private const string EMAIL = "user@example.com";
        private const string PASSWORD = "SecurePass123!";
        private const string ROLE = "User";
        private const string ACCESS_TOKEN = "access-token-value";
        private const string REFRESH_TOKEN = "refresh-token-value";
        private const int ACCESS_EXPIRY_MINUTES = 15;
        private const int REFRESH_EXPIRY_DAYS = 30;
        private static readonly DateTimeOffset _now =
            new(2026, 4, 12, 12, 0, 0, TimeSpan.Zero);

        public LoginCommandHandlerTests()
        {
            _handler = new LoginCommandHandler(
                _userRepositoryMock.Object,
                _jwtServiceMock.Object,
                _refreshTokenRepositoryMock.Object,
                _dateTimeProviderMock.Object);
        }

        /// <summary>
        /// Настраивает моки на успешный сценарий: пользователь найден, пароль верный,
        /// роль есть, JWT-сервис и DateTimeProvider возвращают фиксированные значения.
        /// </summary>
        private void SetupSuccessfulFlow()
        {
            _userRepositoryMock
                .Setup(r => r.FindByLoginAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthUserInfo(_userId, EMAIL));
            _userRepositoryMock
                .Setup(r => r.CheckPasswordAsync(_userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _userRepositoryMock
                .Setup(r => r.GetUserRoleAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ROLE);
            _jwtServiceMock.SetupGet(j => j.AccessTokenExpiryMinutes).Returns(ACCESS_EXPIRY_MINUTES);
            _jwtServiceMock.SetupGet(j => j.RefreshTokenExpiryDays).Returns(REFRESH_EXPIRY_DAYS);
            _jwtServiceMock
                .Setup(j => j.GenerateAccessToken(_userId, EMAIL, ROLE))
                .Returns(ACCESS_TOKEN);
            _jwtServiceMock
                .Setup(j => j.GenerateRefreshToken())
                .Returns(REFRESH_TOKEN);
            _dateTimeProviderMock.SetupGet(d => d.UtcNow).Returns(_now);
        }

        private static LoginCommand CreateCommand(string login = EMAIL, string password = PASSWORD)
            => new(login, password);

        #region Constructor

        [Fact]
        public void Constructor_WithNullUserRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new LoginCommandHandler(
                null!, _jwtServiceMock.Object, _refreshTokenRepositoryMock.Object, _dateTimeProviderMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("userRepository");
        }

        [Fact]
        public void Constructor_WithNullJwtService_ShouldThrowArgumentNullException()
        {
            Action action = () => new LoginCommandHandler(
                _userRepositoryMock.Object, null!, _refreshTokenRepositoryMock.Object, _dateTimeProviderMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("jwtService");
        }

        [Fact]
        public void Constructor_WithNullRefreshTokenRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new LoginCommandHandler(
                _userRepositoryMock.Object, _jwtServiceMock.Object, null!, _dateTimeProviderMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("refreshTokenRepository");
        }

        [Fact]
        public void Constructor_WithNullDateTimeProvider_ShouldThrowArgumentNullException()
        {
            Action action = () => new LoginCommandHandler(
                _userRepositoryMock.Object, _jwtServiceMock.Object, _refreshTokenRepositoryMock.Object, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("dateTimeProvider");
        }

        #endregion

        #region Success

        [Fact]
        public async Task Handle_WithValidCredentials_ReturnsLoginResponseWithExpectedFieldsAsync()
        {
            // Arrange
            SetupSuccessfulFlow();

            // Act
            Result<LoginResponse> result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert — один прогон, все поля ответа проверяем в AssertionScope
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                result.Value.AccessToken.Should().Be(ACCESS_TOKEN);
                result.Value.RefreshToken.Should().Be(REFRESH_TOKEN);
                result.Value.ExpiresAt.Should().Be(_now.AddMinutes(ACCESS_EXPIRY_MINUTES));
            }
        }

        [Fact]
        public async Task Handle_WithValidCredentials_PerformsAllLoginSideEffectsAsync()
        {
            // Arrange
            SetupSuccessfulFlow();

            // Act
            await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert — побочные эффекты в правильном составе
            using (new AssertionScope())
            {
                _jwtServiceMock.Verify(
                    j => j.GenerateAccessToken(_userId, EMAIL, ROLE),
                    Times.Once);
                _refreshTokenRepositoryMock.Verify(
                    r => r.DeleteInactiveByUserIdAsync(_userId, It.IsAny<CancellationToken>()),
                    Times.Once);
                _refreshTokenRepositoryMock.Verify(
                    r => r.AddAsync(
                        It.Is<RefreshToken>(rt =>
                            rt.Token == REFRESH_TOKEN &&
                            rt.UserId == _userId &&
                            rt.ExpiresAt == _now.AddDays(REFRESH_EXPIRY_DAYS)),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
                _refreshTokenRepositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Theory]
        [InlineData("user@example.com")]   // email
        [InlineData("john_doe")]            // username
        [InlineData("+79001234567")]        // phone
        public async Task Handle_PassesAnyLoginFormatToRepositoryAsIsAsync(string login)
        {
            // Arrange — handler не парсит формат логина, это ответственность IUserRepository
            SetupSuccessfulFlow();

            // Act
            await _handler.Handle(CreateCommand(login: login), CancellationToken.None);

            // Assert — строка из команды уходит в FindByLoginAsync без преобразований
            _userRepositoryMock.Verify(
                r => r.FindByLoginAsync(login, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion

        #region Failure paths

        [Fact]
        public async Task Handle_WhenUserNotFound_ReturnsInvalidCredentialsAndSkipsFurtherStepsAsync()
        {
            // Arrange
            _userRepositoryMock
                .Setup(r => r.FindByLoginAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AuthUserInfo?)null);

            // Act
            Result<LoginResponse> result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert — ошибка + fail-fast (ни пароль, ни токены не запрашиваются)
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(AuthErrors.InvalidCredentials);

                _userRepositoryMock.Verify(
                    r => r.CheckPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
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
        public async Task Handle_WhenPasswordInvalid_ReturnsInvalidCredentialsAndDoesNotIssueTokensAsync()
        {
            // Arrange
            _userRepositoryMock
                .Setup(r => r.FindByLoginAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthUserInfo(_userId, EMAIL));
            _userRepositoryMock
                .Setup(r => r.CheckPasswordAsync(_userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            Result<LoginResponse> result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert — та же ошибка, что при несуществующем пользователе (не раскрываем факт существования)
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
                _refreshTokenRepositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenRoleIsNull_ReturnsInvalidCredentialsAndDoesNotIssueTokensAsync()
        {
            // Arrange — пользователь найден и пароль верный, но роль отсутствует (битое состояние Identity)
            _userRepositoryMock
                .Setup(r => r.FindByLoginAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthUserInfo(_userId, EMAIL));
            _userRepositoryMock
                .Setup(r => r.CheckPasswordAsync(_userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
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

                _refreshTokenRepositoryMock.Verify(
                    r => r.DeleteInactiveByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Never);
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
