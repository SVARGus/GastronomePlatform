using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Users.Application.DTOs;
using GastronomePlatform.Modules.Users.Application.Queries.GetProfile;
using GastronomePlatform.Modules.Users.Domain.Entities;
using GastronomePlatform.Modules.Users.Domain.Enums;
using GastronomePlatform.Modules.Users.Domain.Errors;
using GastronomePlatform.Modules.Users.Domain.Repositories;
using Moq;

namespace GastronomePlatform.Users.UnitTests.Application.Queries
{
    /// <summary>
    /// Тесты для <see cref="GetProfileQueryHandler"/>.
    /// </summary>
    public sealed class GetProfileQueryHandlerTests
    {
        private readonly Mock<IUserProfileRepository> _repositoryMock = new();
        private readonly GetProfileQueryHandler _handler;

        private static readonly Guid _userId = Guid.NewGuid();
        private static readonly DateTimeOffset _createdAt =
            new(2026, 4, 1, 10, 0, 0, TimeSpan.Zero);

        public GetProfileQueryHandlerTests()
        {
            _handler = new GetProfileQueryHandler(_repositoryMock.Object);
        }

        #region Constructor

        [Fact]
        public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new GetProfileQueryHandler(null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("repository");
        }

        #endregion

        #region Success — маппинг

        [Fact]
        public async Task Handle_WhenProfileExists_ReturnsDtoWithAllFieldsMappedAsync()
        {
            // Arrange — создаём полностью заполненный профиль через публичные Update-методы
            DateOnly dateOfBirth = new(1990, 5, 15);
            Guid avatarId = Guid.NewGuid();

            UserProfile profile = UserProfile.Create(_userId, "user@example.com", "test_user", "+79001234567", _createdAt);
            profile.UpdatePersonalInfo("Иван", "Иванов", "Иванович", "Ваня", _createdAt);
            profile.UpdateBio("Люблю борщ.", _createdAt);
            profile.UpdatePersonalDetails(Gender.Male, dateOfBirth, _createdAt);
            profile.UpdateLocation("Россия", "Краснодарский край", "Краснодар", _createdAt);
            profile.UpdateAvatar(avatarId, _createdAt);
            profile.SetVisibility(false, _createdAt);

            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            // Act
            Result<UserProfileDto> result = await _handler.Handle(new GetProfileQuery(_userId), CancellationToken.None);

            // Assert — полный маппинг всех 17 полей DTO
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();

                UserProfileDto dto = result.Value;
                dto.UserId.Should().Be(_userId);
                dto.Email.Should().Be("user@example.com");
                dto.UserName.Should().Be("test_user");
                dto.IsPublic.Should().BeFalse();
                dto.Phone.Should().Be("+79001234567");
                dto.FirstName.Should().Be("Иван");
                dto.LastName.Should().Be("Иванов");
                dto.MiddleName.Should().Be("Иванович");
                dto.DisplayName.Should().Be("Ваня");
                dto.Bio.Should().Be("Люблю борщ.");
                dto.Gender.Should().Be("Male"); // enum → string через ToString()
                dto.DateOfBirth.Should().Be(dateOfBirth);
                dto.AvatarMediaId.Should().Be(avatarId);
                dto.Country.Should().Be("Россия");
                dto.Region.Should().Be("Краснодарский край");
                dto.City.Should().Be("Краснодар");
                dto.CreatedAt.Should().Be(_createdAt);
            }
        }

        [Fact]
        public async Task Handle_WhenProfileHasNullGender_ShouldReturnDtoWithNullGenderAsync()
        {
            // Arrange — Gender опционален, profile без UpdatePersonalDetails оставит null
            UserProfile profile = UserProfile.Create(_userId, "user@x.com", "u", null, _createdAt);

            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            // Act
            Result<UserProfileDto> result = await _handler.Handle(new GetProfileQuery(_userId), CancellationToken.None);

            // Assert — Gender?.ToString() на null возвращает null, а не "None"
            result.Value.Gender.Should().BeNull();
        }

        #endregion

        #region Failure

        [Fact]
        public async Task Handle_WhenProfileNotFound_ReturnsProfileNotFoundErrorAsync()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserProfile?)null);

            // Act
            Result<UserProfileDto> result = await _handler.Handle(new GetProfileQuery(_userId), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(UsersErrors.ProfileNotFound);
            }
        }

        #endregion
    }
}
