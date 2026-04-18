using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Auth.Domain.Contracts;
using GastronomePlatform.Modules.Auth.Domain.Errors;
using GastronomePlatform.Modules.Users.Application.Commands.ChangeEmail;
using GastronomePlatform.Modules.Users.Domain.Entities;
using GastronomePlatform.Modules.Users.Domain.Errors;
using GastronomePlatform.Modules.Users.Domain.Repositories;
using Moq;

namespace GastronomePlatform.Users.UnitTests.Application.Commands
{
    /// <summary>
    /// Тесты для <see cref="ChangeEmailCommandHandler"/>.
    /// </summary>
    public sealed class ChangeEmailCommandHandlerTests
    {
        private readonly Mock<IUserProfileRepository> _repositoryMock = new();
        private readonly Mock<IAuthUserService> _authUserServiceMock = new();
        private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
        private readonly ChangeEmailCommandHandler _handler;

        private static readonly Guid _userId = Guid.NewGuid();
        private const string OLD_EMAIL = "old@example.com";
        private const string NEW_EMAIL = "new@example.com";
        private const string USERNAME = "test_user";
        private const string PHONE = "+79001234567";
        private static readonly DateTimeOffset _createdAt =
            new(2026, 4, 1, 10, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset _now =
            new(2026, 4, 12, 12, 0, 0, TimeSpan.Zero);

        public ChangeEmailCommandHandlerTests()
        {
            _handler = new ChangeEmailCommandHandler(
                _repositoryMock.Object,
                _authUserServiceMock.Object,
                _dateTimeProviderMock.Object);
        }

        private static UserProfile CreateProfile() =>
            UserProfile.Create(_userId, OLD_EMAIL, USERNAME, PHONE, _createdAt);

        private static ChangeEmailCommand CreateCommand() => new(_userId, NEW_EMAIL);

        #region Constructor

        [Fact]
        public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new ChangeEmailCommandHandler(
                null!, _authUserServiceMock.Object, _dateTimeProviderMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("userProfileRepository");
        }

        [Fact]
        public void Constructor_WithNullAuthUserService_ShouldThrowArgumentNullException()
        {
            Action action = () => new ChangeEmailCommandHandler(
                _repositoryMock.Object, null!, _dateTimeProviderMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("authUserService");
        }

        [Fact]
        public void Constructor_WithNullDateTimeProvider_ShouldThrowArgumentNullException()
        {
            Action action = () => new ChangeEmailCommandHandler(
                _repositoryMock.Object, _authUserServiceMock.Object, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("dateTimeProvider");
        }

        #endregion

        #region Success

        [Fact]
        public async Task Handle_WhenAuthAcceptsChange_UpdatesEmailInMirrorAndSavesAsync()
        {
            // Arrange
            UserProfile profile = CreateProfile();

            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);
            _authUserServiceMock
                .Setup(s => s.ChangeEmailAsync(_userId, NEW_EMAIL, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());
            _dateTimeProviderMock.SetupGet(d => d.UtcNow).Returns(_now);

            // Act
            Result result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();

                // Email в профиле обновлён
                profile.Email.Should().Be(NEW_EMAIL);

                // Остальные Auth-зеркальные поля нетронуты
                profile.Phone.Should().Be(PHONE);
                profile.UserName.Should().Be(USERNAME);

                profile.UpdatedAt.Should().Be(_now);

                _repositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        #endregion

        #region Failure — профиль не найден

        [Fact]
        public async Task Handle_WhenProfileNotFound_ReturnsErrorAndDoesNotCallAuthOrSaveAsync()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserProfile?)null);

            // Act
            Result result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert — fail-fast: Auth не дёргается, изменений нет
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(UsersErrors.ProfileNotFound);

                _authUserServiceMock.Verify(
                    s => s.ChangeEmailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Never);
                _repositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        #endregion

        #region Failure — Auth отказал

        [Fact]
        public async Task Handle_WhenAuthRejectsChange_PropagatesErrorAndDoesNotUpdateProfileAsync()
        {
            // Arrange — профиль есть, но новый email занят в Auth
            UserProfile profile = CreateProfile();

            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);
            _authUserServiceMock
                .Setup(s => s.ChangeEmailAsync(_userId, NEW_EMAIL, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure(AuthErrors.EmailAlreadyTaken));

            // Act
            Result result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(AuthErrors.EmailAlreadyTaken);

                // Профиль не обновлён — зеркало остаётся синхронным с Auth
                profile.Email.Should().Be(OLD_EMAIL);

                _repositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        #endregion
    }
}
