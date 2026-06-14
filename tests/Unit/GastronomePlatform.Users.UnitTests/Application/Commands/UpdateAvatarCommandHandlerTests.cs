using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Media.Application.Contracts;
using GastronomePlatform.Modules.Media.Domain.Constants;
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
        private readonly Mock<ICurrentUserService> _currentUserMock = new();
        private readonly Mock<IMediaService> _mediaServiceMock = new();
        private readonly UpdateAvatarCommandHandler _handler;

        private static readonly Guid _userId = Guid.NewGuid();
        private static readonly DateTimeOffset _createdAt =
            new(2026, 4, 1, 10, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset _now =
            new(2026, 4, 12, 12, 0, 0, TimeSpan.Zero);

        public UpdateAvatarCommandHandlerTests()
        {
            _currentUserMock.SetupGet(c => c.UserId).Returns(_userId);

            _mediaServiceMock
                .Setup(m => m.AttachToEntityAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());

            _mediaServiceMock
                .Setup(m => m.DetachFromEntityAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());

            _handler = new UpdateAvatarCommandHandler(
                _repositoryMock.Object,
                _dateTimeProviderMock.Object,
                _currentUserMock.Object,
                _mediaServiceMock.Object);
        }

        private static UserProfile CreateProfile() =>
            UserProfile.Create(_userId, "user@x.com", "user", null, _createdAt);

        #region Constructor

        [Fact]
        public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new UpdateAvatarCommandHandler(
                null!, _dateTimeProviderMock.Object, _currentUserMock.Object, _mediaServiceMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("userProfileRepository");
        }

        [Fact]
        public void Constructor_WithNullDateTimeProvider_ShouldThrowArgumentNullException()
        {
            Action action = () => new UpdateAvatarCommandHandler(
                _repositoryMock.Object, null!, _currentUserMock.Object, _mediaServiceMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("dateTimeProvider");
        }

        [Fact]
        public void Constructor_WithNullCurrentUserService_ShouldThrowArgumentNullException()
        {
            Action action = () => new UpdateAvatarCommandHandler(
                _repositoryMock.Object, _dateTimeProviderMock.Object, null!, _mediaServiceMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("currentUser");
        }

        [Fact]
        public void Constructor_WithNullMediaService_ShouldThrowArgumentNullException()
        {
            Action action = () => new UpdateAvatarCommandHandler(
                _repositoryMock.Object, _dateTimeProviderMock.Object, _currentUserMock.Object, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("mediaService");
        }

        #endregion

        #region Success

        [Fact]
        public async Task Handle_WhenProfileExistsAndAvatarAdded_AttachesMediaAndSavesAsync()
        {
            // Arrange — старт без аватара, ставим новый
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

                _mediaServiceMock.Verify(
                    m => m.AttachToEntityAsync(
                        avatarId, _userId, MediaEntityTypes.USER_AVATAR, _userId,
                        It.IsAny<CancellationToken>()),
                    Times.Once);

                _mediaServiceMock.Verify(
                    m => m.DetachFromEntityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Never);

                _repositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Fact]
        public async Task Handle_WithNullAvatarId_DetachesMediaAndSavesAsync()
        {
            // Arrange — сначала ставим аватар, затем передаём null
            UserProfile profile = CreateProfile();
            Guid existingAvatarId = Guid.NewGuid();
            profile.UpdateAvatar(existingAvatarId, _createdAt);

            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);
            _dateTimeProviderMock.SetupGet(d => d.UtcNow).Returns(_now);

            // Act
            Result result = await _handler.Handle(
                new UpdateAvatarCommand(_userId, AvatarMediaId: null), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                profile.AvatarMediaId.Should().BeNull();

                _mediaServiceMock.Verify(
                    m => m.DetachFromEntityAsync(existingAvatarId, It.IsAny<CancellationToken>()),
                    Times.Once);

                _mediaServiceMock.Verify(
                    m => m.AttachToEntityAsync(
                        It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
                        It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Never);

                _repositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Fact]
        public async Task Handle_WhenAvatarReplaced_DetachesOldAndAttachesNewAsync()
        {
            // Arrange — был аватар, ставим другой
            UserProfile profile = CreateProfile();
            Guid oldAvatarId = Guid.NewGuid();
            Guid newAvatarId = Guid.NewGuid();
            profile.UpdateAvatar(oldAvatarId, _createdAt);

            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);
            _dateTimeProviderMock.SetupGet(d => d.UtcNow).Returns(_now);

            // Act
            Result result = await _handler.Handle(
                new UpdateAvatarCommand(_userId, newAvatarId), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                profile.AvatarMediaId.Should().Be(newAvatarId);

                _mediaServiceMock.Verify(
                    m => m.DetachFromEntityAsync(oldAvatarId, It.IsAny<CancellationToken>()),
                    Times.Once);

                _mediaServiceMock.Verify(
                    m => m.AttachToEntityAsync(
                        newAvatarId, _userId, MediaEntityTypes.USER_AVATAR, _userId,
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Fact]
        public async Task Handle_WhenAvatarUnchanged_SkipsMediaOperationsAsync()
        {
            // Arrange — старое и новое значение совпадают
            UserProfile profile = CreateProfile();
            Guid avatarId = Guid.NewGuid();
            profile.UpdateAvatar(avatarId, _createdAt);

            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);
            _dateTimeProviderMock.SetupGet(d => d.UtcNow).Returns(_now);

            // Act
            Result result = await _handler.Handle(
                new UpdateAvatarCommand(_userId, avatarId), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();

                _mediaServiceMock.Verify(
                    m => m.AttachToEntityAsync(
                        It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
                        It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Never);

                _mediaServiceMock.Verify(
                    m => m.DetachFromEntityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        #endregion

        #region Failure

        [Fact]
        public async Task Handle_WhenActorIsNotOwner_ReturnsNotAuthorizedAndDoesNotSaveAsync()
        {
            // Arrange — текущий пользователь не совпадает с UserId из команды
            Guid otherUserId = Guid.NewGuid();
            _currentUserMock.Reset();
            _currentUserMock.SetupGet(c => c.UserId).Returns(otherUserId);

            // Act
            Result result = await _handler.Handle(
                new UpdateAvatarCommand(_userId, Guid.NewGuid()), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(UsersErrors.NotAuthorized);

                _repositoryMock.Verify(
                    r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Never);
                _repositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
                _mediaServiceMock.Verify(
                    m => m.AttachToEntityAsync(
                        It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
                        It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenProfileNotFound_ReturnsErrorAndDoesNotSaveAsync()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserProfile?)null);

            // Act
            Result result = await _handler.Handle(
                new UpdateAvatarCommand(_userId, Guid.NewGuid()), CancellationToken.None);

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

        [Fact]
        public async Task Handle_WhenMediaAttachFails_ReturnsErrorAndDoesNotSaveAsync()
        {
            // Arrange — Media падает на attach, Users SaveChanges не вызывается
            UserProfile profile = CreateProfile();
            Guid avatarId = Guid.NewGuid();

            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);
            _dateTimeProviderMock.SetupGet(d => d.UtcNow).Returns(_now);

            Error mediaError = Error.NotFound("MEDIA.NOT_FOUND", "fake");
            _mediaServiceMock.Reset();
            _mediaServiceMock
                .Setup(m => m.AttachToEntityAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure(mediaError));

            // Act
            Result result = await _handler.Handle(
                new UpdateAvatarCommand(_userId, avatarId), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(mediaError);

                _repositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        #endregion
    }
}
