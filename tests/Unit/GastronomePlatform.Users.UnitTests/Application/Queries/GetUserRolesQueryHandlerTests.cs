using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Auth.Domain.Contracts;
using GastronomePlatform.Modules.Users.Application.Queries.GetUserRoles;
using GastronomePlatform.Modules.Users.Domain.Entities;
using GastronomePlatform.Modules.Users.Domain.Errors;
using GastronomePlatform.Modules.Users.Domain.Repositories;
using Moq;

namespace GastronomePlatform.Users.UnitTests.Application.Queries
{
    /// <summary>
    /// Тесты для <see cref="GetUserRolesQueryHandler"/>.
    /// </summary>
    public sealed class GetUserRolesQueryHandlerTests
    {
        private readonly Mock<IAuthUserService> _authUserServiceMock = new();
        private readonly Mock<IUserProfileRepository> _repositoryMock = new();
        private readonly GetUserRolesQueryHandler _handler;

        private static readonly Guid _userId = Guid.NewGuid();
        private static readonly DateTimeOffset _createdAt =
            new(2026, 4, 1, 10, 0, 0, TimeSpan.Zero);

        public GetUserRolesQueryHandlerTests()
        {
            _handler = new GetUserRolesQueryHandler(
                _authUserServiceMock.Object,
                _repositoryMock.Object);
        }

        private static UserProfile CreateProfile() =>
            UserProfile.Create(_userId, "user@x.com", "user", null, _createdAt);

        #region Constructor

        [Fact]
        public void Constructor_WithNullAuthUserService_ShouldThrowArgumentNullException()
        {
            Action action = () => new GetUserRolesQueryHandler(null!, _repositoryMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("authUserService");
        }

        [Fact]
        public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new GetUserRolesQueryHandler(_authUserServiceMock.Object, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("repository");
        }

        #endregion

        #region Success

        [Fact]
        public async Task Handle_WhenProfileExists_ReturnsRolesFromAuthServiceAsync()
        {
            // Arrange
            IReadOnlyCollection<string> expectedRoles = ["User", "Premium"];

            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateProfile());
            _authUserServiceMock
                .Setup(s => s.GetUserRolesAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedRoles);

            // Act
            Result<IReadOnlyCollection<string>> result = await _handler.Handle(
                new GetUserRolesQuery(_userId), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                result.Value.Should().BeEquivalentTo(expectedRoles);
            }
        }

        [Fact]
        public async Task Handle_WhenUserHasNoRoles_ReturnsEmptyCollectionSuccessAsync()
        {
            // Arrange — пользователь без ролей (edge case, не должно случаться после регистрации,
            // но handler обязан возвращать Success с пустой коллекцией, а не InvalidCredentials)
            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateProfile());
            _authUserServiceMock
                .Setup(s => s.GetUserRolesAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<string>());

            // Act
            Result<IReadOnlyCollection<string>> result = await _handler.Handle(
                new GetUserRolesQuery(_userId), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                result.Value.Should().BeEmpty();
            }
        }

        #endregion

        #region Failure

        [Fact]
        public async Task Handle_WhenProfileNotFound_ReturnsErrorAndDoesNotCallAuthServiceAsync()
        {
            // Arrange — проверка существования профиля в Users обязательна перед запросом ролей в Auth
            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserProfile?)null);

            // Act
            Result<IReadOnlyCollection<string>> result = await _handler.Handle(
                new GetUserRolesQuery(_userId), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(UsersErrors.ProfileNotFound);

                _authUserServiceMock.Verify(
                    s => s.GetUserRolesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        #endregion
    }
}
