using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Auth.Domain.Contracts;
using GastronomePlatform.Modules.Auth.Domain.Errors;
using GastronomePlatform.Modules.Users.Application.Commands.ChangePhone;
using GastronomePlatform.Modules.Users.Domain.Entities;
using GastronomePlatform.Modules.Users.Domain.Errors;
using GastronomePlatform.Modules.Users.Domain.Repositories;
using Moq;

namespace GastronomePlatform.Users.UnitTests.Application.Commands
{
    /// <summary>
    /// Тесты для <see cref="ChangePhoneCommandHandler"/>.
    /// </summary>
    public sealed class ChangePhoneCommandHandlerTests
    {
        private readonly Mock<IUserProfileRepository> _repositoryMock = new();
        private readonly Mock<IAuthUserService> _authUserServiceMock = new();
        private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
        private readonly ChangePhoneCommandHandler _handler;

        private static readonly Guid _userId = Guid.NewGuid();
        private const string EMAIL = "user@example.com";
        private const string USERNAME = "test_user";
        private const string OLD_PHONE = "+79001111111";
        private const string NEW_PHONE = "+79002222222";
        private static readonly DateTimeOffset _createdAt =
            new(2026, 4, 1, 10, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset _now =
            new(2026, 4, 12, 12, 0, 0, TimeSpan.Zero);

        public ChangePhoneCommandHandlerTests()
        {
            _handler = new ChangePhoneCommandHandler(
                _repositoryMock.Object,
                _authUserServiceMock.Object,
                _dateTimeProviderMock.Object);
        }

        private static UserProfile CreateProfile() =>
            UserProfile.Create(_userId, EMAIL, USERNAME, OLD_PHONE, _createdAt);

        private static ChangePhoneCommand CreateCommand() => new(_userId, NEW_PHONE);

        #region Constructor

        [Fact]
        public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new ChangePhoneCommandHandler(
                null!, _authUserServiceMock.Object, _dateTimeProviderMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("userProfileRepository");
        }

        [Fact]
        public void Constructor_WithNullAuthUserService_ShouldThrowArgumentNullException()
        {
            Action action = () => new ChangePhoneCommandHandler(
                _repositoryMock.Object, null!, _dateTimeProviderMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("authUserService");
        }

        [Fact]
        public void Constructor_WithNullDateTimeProvider_ShouldThrowArgumentNullException()
        {
            Action action = () => new ChangePhoneCommandHandler(
                _repositoryMock.Object, _authUserServiceMock.Object, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("dateTimeProvider");
        }

        #endregion

        #region Success

        [Fact]
        public async Task Handle_WhenAuthAcceptsChange_UpdatesPhoneInMirrorAndSavesAsync()
        {
            // Arrange
            UserProfile profile = CreateProfile();

            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);
            _authUserServiceMock
                .Setup(s => s.ChangePhoneAsync(_userId, NEW_PHONE, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());
            _dateTimeProviderMock.SetupGet(d => d.UtcNow).Returns(_now);

            // Act
            Result result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();

                profile.Phone.Should().Be(NEW_PHONE);

                // Email и UserName нетронуты
                profile.Email.Should().Be(EMAIL);
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

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(UsersErrors.ProfileNotFound);

                _authUserServiceMock.Verify(
                    s => s.ChangePhoneAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
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
            // Arrange
            UserProfile profile = CreateProfile();

            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);
            _authUserServiceMock
                .Setup(s => s.ChangePhoneAsync(_userId, NEW_PHONE, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure(AuthErrors.PhonelAlreadyTaken));

            // Act
            Result result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(AuthErrors.PhonelAlreadyTaken);

                profile.Phone.Should().Be(OLD_PHONE);

                _repositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        #endregion
    }
}
