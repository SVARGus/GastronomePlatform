using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Users.Application.Commands.UpdatePersonalInfo;
using GastronomePlatform.Modules.Users.Domain.Entities;
using GastronomePlatform.Modules.Users.Domain.Enums;
using GastronomePlatform.Modules.Users.Domain.Errors;
using GastronomePlatform.Modules.Users.Domain.Repositories;
using Moq;

namespace GastronomePlatform.Users.UnitTests.Application.Commands
{
    /// <summary>
    /// Тесты для <see cref="UpdatePersonalInfoCommandHandler"/>.
    /// </summary>
    public sealed class UpdatePersonalInfoCommandHandlerTests
    {
        private readonly Mock<IUserProfileRepository> _repositoryMock = new();
        private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
        private readonly UpdatePersonalInfoCommandHandler _handler;

        private static readonly Guid _userId = Guid.NewGuid();
        private static readonly DateTimeOffset _createdAt =
            new(2026, 4, 1, 10, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset _now =
            new(2026, 4, 12, 12, 0, 0, TimeSpan.Zero);

        public UpdatePersonalInfoCommandHandlerTests()
        {
            _handler = new UpdatePersonalInfoCommandHandler(
                _repositoryMock.Object,
                _dateTimeProviderMock.Object);
        }

        private static UserProfile CreateProfile() =>
            UserProfile.Create(_userId, "user@x.com", "user", null, _createdAt);

        private static UpdatePersonalInfoCommand CreateCommand() => new(
            UserId: _userId,
            FirstName: "Иван",
            LastName: "Иванов",
            MiddleName: "Иванович",
            DisplayName: "Ваня",
            Bio: "Био.",
            Gender: Gender.Male,
            DateOfBirth: new DateOnly(1990, 5, 15));

        #region Constructor

        [Fact]
        public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new UpdatePersonalInfoCommandHandler(null!, _dateTimeProviderMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("userProfileRepository");
        }

        [Fact]
        public void Constructor_WithNullDateTimeProvider_ShouldThrowArgumentNullException()
        {
            Action action = () => new UpdatePersonalInfoCommandHandler(_repositoryMock.Object, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("dateTimeProvider");
        }

        #endregion

        #region Success

        [Fact]
        public async Task Handle_WhenProfileExists_UpdatesAllFieldGroupsAndSavesAsync()
        {
            // Arrange — handler вызывает три доменных метода: UpdatePersonalInfo, UpdateBio, UpdatePersonalDetails
            UserProfile profile = CreateProfile();
            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);
            _dateTimeProviderMock.SetupGet(d => d.UtcNow).Returns(_now);

            UpdatePersonalInfoCommand command = CreateCommand();

            // Act
            Result result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();

                // Персональные данные
                profile.FirstName.Should().Be("Иван");
                profile.LastName.Should().Be("Иванов");
                profile.MiddleName.Should().Be("Иванович");
                profile.DisplayName.Should().Be("Ваня");

                // Био
                profile.Bio.Should().Be("Био.");

                // Дополнительные детали
                profile.Gender.Should().Be(Gender.Male);
                profile.DateOfBirth.Should().Be(new DateOnly(1990, 5, 15));

                // Время обновления — от IDateTimeProvider
                profile.UpdatedAt.Should().Be(_now);

                // Сохранение вызвано
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
            Result result = await _handler.Handle(CreateCommand(), CancellationToken.None);

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
