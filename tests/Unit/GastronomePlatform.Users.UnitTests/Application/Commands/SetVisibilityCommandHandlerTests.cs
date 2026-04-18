using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Users.Application.Commands.SetVisibility;
using GastronomePlatform.Modules.Users.Domain.Entities;
using GastronomePlatform.Modules.Users.Domain.Errors;
using GastronomePlatform.Modules.Users.Domain.Repositories;
using Moq;

namespace GastronomePlatform.Users.UnitTests.Application.Commands
{
    /// <summary>
    /// Тесты для <see cref="SetVisibilityCommandHandler"/>.
    /// </summary>
    public sealed class SetVisibilityCommandHandlerTests
    {
        private readonly Mock<IUserProfileRepository> _repositoryMock = new();
        private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
        private readonly SetVisibilityCommandHandler _handler;

        private static readonly Guid _userId = Guid.NewGuid();
        private static readonly DateTimeOffset _createdAt =
            new(2026, 4, 1, 10, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset _now =
            new(2026, 4, 12, 12, 0, 0, TimeSpan.Zero);

        public SetVisibilityCommandHandlerTests()
        {
            _handler = new SetVisibilityCommandHandler(
                _repositoryMock.Object,
                _dateTimeProviderMock.Object);
        }

        private static UserProfile CreateProfile() =>
            UserProfile.Create(_userId, "user@x.com", "user", null, _createdAt);

        #region Constructor

        [Fact]
        public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new SetVisibilityCommandHandler(null!, _dateTimeProviderMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("userProfileRepository");
        }

        [Fact]
        public void Constructor_WithNullDateTimeProvider_ShouldThrowArgumentNullException()
        {
            Action action = () => new SetVisibilityCommandHandler(_repositoryMock.Object, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("dateTimeProvider");
        }

        #endregion

        #region Success

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Handle_WhenProfileExists_UpdatesVisibilityAndSavesAsync(bool isPublic)
        {
            // Arrange — начальное состояние противоположное, чтобы проверить реальный переход
            UserProfile profile = CreateProfile();
            profile.SetVisibility(!isPublic, _createdAt);

            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);
            _dateTimeProviderMock.SetupGet(d => d.UtcNow).Returns(_now);

            // Act
            Result result = await _handler.Handle(new SetVisibilityCommand(_userId, isPublic), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                profile.IsPublic.Should().Be(isPublic);
                profile.UpdatedAt.Should().Be(_now);

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
            Result result = await _handler.Handle(new SetVisibilityCommand(_userId, true), CancellationToken.None);

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
