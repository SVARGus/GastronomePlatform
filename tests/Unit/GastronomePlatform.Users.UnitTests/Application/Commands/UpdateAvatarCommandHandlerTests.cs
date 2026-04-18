using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Users.Application.Commands.UpdateAvatar;
using GastronomePlatform.Modules.Users.Domain.Entities;
using GastronomePlatform.Modules.Users.Domain.Errors;
using GastronomePlatform.Modules.Users.Domain.Repositories;
using Moq;

namespace GastronomePlatform.Users.UnitTests.Application.Commands
{
    /// <summary>
    /// Тесты для <see cref="UpdateAvatarCommandHandler"/>.
    /// </summary>
    public sealed class UpdateAvatarCommandHandlerTests
    {
        private readonly Mock<IUserProfileRepository> _repositoryMock = new();
        private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
        private readonly UpdateAvatarCommandHandler _handler;

        private static readonly Guid _userId = Guid.NewGuid();
        private static readonly DateTimeOffset _createdAt =
            new(2026, 4, 1, 10, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset _now =
            new(2026, 4, 12, 12, 0, 0, TimeSpan.Zero);

        public UpdateAvatarCommandHandlerTests()
        {
            _handler = new UpdateAvatarCommandHandler(
                _repositoryMock.Object,
                _dateTimeProviderMock.Object);
        }

        private static UserProfile CreateProfile() =>
            UserProfile.Create(_userId, "user@x.com", "user", null, _createdAt);

        #region Constructor

        [Fact]
        public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new UpdateAvatarCommandHandler(null!, _dateTimeProviderMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("userProfileRepository");
        }

        [Fact]
        public void Constructor_WithNullDateTimeProvider_ShouldThrowArgumentNullException()
        {
            Action action = () => new UpdateAvatarCommandHandler(_repositoryMock.Object, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("dateTimeProvider");
        }

        #endregion

        #region Success

        [Fact]
        public async Task Handle_WhenProfileExists_UpdatesAvatarAndSavesAsync()
        {
            // Arrange
            UserProfile profile = CreateProfile();
            Guid avatarId = Guid.NewGuid();

            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);
            _dateTimeProviderMock.SetupGet(d => d.UtcNow).Returns(_now);

            // Act
            Result result = await _handler.Handle(new UpdateAvatarCommand(_userId, avatarId), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                profile.AvatarMediaId.Should().Be(avatarId);
                profile.UpdatedAt.Should().Be(_now);

                _repositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Fact]
        public async Task Handle_WithNullAvatarId_ShouldClearAvatarAndSaveAsync()
        {
            // Arrange — null трактуется доменом как "удалить аватар"
            UserProfile profile = CreateProfile();
            profile.UpdateAvatar(Guid.NewGuid(), _createdAt); // сначала ставим аватар

            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);
            _dateTimeProviderMock.SetupGet(d => d.UtcNow).Returns(_now);

            // Act — передаём null для удаления
            Result result = await _handler.Handle(new UpdateAvatarCommand(_userId, AvatarMediaId: null), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                profile.AvatarMediaId.Should().BeNull();

                _repositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        #endregion

        #region Failure

        [Fact]
        public async Task Handle_WhenProfileNotFound_ReturnsErrorAndDoesNotSaveAsync()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserProfile?)null);

            // Act
            Result result = await _handler.Handle(new UpdateAvatarCommand(_userId, Guid.NewGuid()), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(UsersErrors.ProfileNotFound);

                _repositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        #endregion
    }
}
