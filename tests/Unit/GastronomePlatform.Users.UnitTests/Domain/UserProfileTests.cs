using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Modules.Users.Domain.Entities;
using GastronomePlatform.Modules.Users.Domain.Enums;

namespace GastronomePlatform.Users.UnitTests.Domain
{
    /// <summary>
    /// Тесты для <see cref="UserProfile"/>.
    /// </summary>
    public sealed class UserProfileTests
    {
        private static readonly Guid _userId = Guid.NewGuid();
        private const string DEFAULT_EMAIL = "user@example.com";
        private const string DEFAULT_USERNAME = "test_user";
        private const string DEFAULT_PHONE = "+79001234567";
        private static readonly DateTimeOffset _createdAt =
            new(2026, 4, 1, 10, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset _updatedAt =
            new(2026, 4, 12, 12, 0, 0, TimeSpan.Zero);

        /// <summary>
        /// Создаёт профиль со значениями по умолчанию — используется в тестах обновления.
        /// </summary>
        private static UserProfile CreateDefaultProfile(string? phone = DEFAULT_PHONE) =>
            UserProfile.Create(_userId, DEFAULT_EMAIL, DEFAULT_USERNAME, phone, _createdAt);

        #region Create

        [Fact]
        public void Create_WithValidData_InitializesAllFields()
        {
            // Act
            UserProfile profile = UserProfile.Create(
                _userId, DEFAULT_EMAIL, DEFAULT_USERNAME, DEFAULT_PHONE, _createdAt);

            // Assert
            using (new AssertionScope())
            {
                // Идентификатор — UserId и Id совпадают (физический FK отсутствует, но логически они равны)
                profile.Id.Should().Be(_userId);
                profile.UserId.Should().Be(_userId);

                // Auth-зеркало
                profile.Email.Should().Be(DEFAULT_EMAIL);
                profile.UserName.Should().Be(DEFAULT_USERNAME);
                profile.Phone.Should().Be(DEFAULT_PHONE);

                // Временные метки
                profile.CreatedAt.Should().Be(_createdAt);
                profile.UpdatedAt.Should().Be(_createdAt);

                // По умолчанию профиль публичный
                profile.IsPublic.Should().BeTrue();

                // Остальные поля — null/default
                profile.FirstName.Should().BeNull();
                profile.LastName.Should().BeNull();
                profile.MiddleName.Should().BeNull();
                profile.DisplayName.Should().BeNull();
                profile.Bio.Should().BeNull();
                profile.Gender.Should().BeNull();
                profile.DateOfBirth.Should().BeNull();
                profile.AvatarMediaId.Should().BeNull();
                profile.Country.Should().BeNull();
                profile.Region.Should().BeNull();
                profile.City.Should().BeNull();
            }
        }

        [Fact]
        public void Create_WithNullPhone_ShouldSetPhoneToNull()
        {
            // Act — phone опционален при регистрации
            UserProfile profile = UserProfile.Create(
                _userId, DEFAULT_EMAIL, DEFAULT_USERNAME, phone: null, _createdAt);

            // Assert
            profile.Phone.Should().BeNull();
        }

        #endregion

        #region UpdatePersonalInfo

        [Fact]
        public void UpdatePersonalInfo_ShouldSetFieldsAndRefreshUpdatedAt()
        {
            // Arrange
            UserProfile profile = CreateDefaultProfile();

            // Act
            profile.UpdatePersonalInfo(
                firstName: "Иван",
                lastName: "Иванов",
                middleName: "Иванович",
                displayName: "Ваня",
                updatedAt: _updatedAt);

            // Assert
            using (new AssertionScope())
            {
                profile.FirstName.Should().Be("Иван");
                profile.LastName.Should().Be("Иванов");
                profile.MiddleName.Should().Be("Иванович");
                profile.DisplayName.Should().Be("Ваня");
                profile.UpdatedAt.Should().Be(_updatedAt);

                // Auth-зеркало не должно изменяться этим методом
                profile.Email.Should().Be(DEFAULT_EMAIL);
                profile.UserName.Should().Be(DEFAULT_USERNAME);
                profile.Phone.Should().Be(DEFAULT_PHONE);
            }
        }

        [Fact]
        public void UpdatePersonalInfo_WithNullValues_ShouldResetFields()
        {
            // Arrange — профиль уже содержит значения
            UserProfile profile = CreateDefaultProfile();
            profile.UpdatePersonalInfo("Иван", "Иванов", "Иванович", "Ваня", _updatedAt);

            // Act — повторный вызов с null сбрасывает поля
            profile.UpdatePersonalInfo(null, null, null, null, _updatedAt);

            // Assert
            using (new AssertionScope())
            {
                profile.FirstName.Should().BeNull();
                profile.LastName.Should().BeNull();
                profile.MiddleName.Should().BeNull();
                profile.DisplayName.Should().BeNull();
            }
        }

        #endregion

        #region UpdateBio

        [Fact]
        public void UpdateBio_ShouldSetBioAndRefreshUpdatedAt()
        {
            // Arrange
            UserProfile profile = CreateDefaultProfile();
            const string BIO = "Люблю готовить борщ.";

            // Act
            profile.UpdateBio(BIO, _updatedAt);

            // Assert
            using (new AssertionScope())
            {
                profile.Bio.Should().Be(BIO);
                profile.UpdatedAt.Should().Be(_updatedAt);
            }
        }

        [Fact]
        public void UpdateBio_WithNull_ShouldClearBio()
        {
            // Arrange
            UserProfile profile = CreateDefaultProfile();
            profile.UpdateBio("Что-то.", _updatedAt);

            // Act
            profile.UpdateBio(null, _updatedAt);

            // Assert
            profile.Bio.Should().BeNull();
        }

        #endregion

        #region UpdatePersonalDetails

        [Fact]
        public void UpdatePersonalDetails_ShouldSetGenderAndDateOfBirthAndRefreshUpdatedAt()
        {
            // Arrange
            UserProfile profile = CreateDefaultProfile();
            DateOnly dateOfBirth = new(1990, 5, 15);

            // Act
            profile.UpdatePersonalDetails(Gender.Male, dateOfBirth, _updatedAt);

            // Assert
            using (new AssertionScope())
            {
                profile.Gender.Should().Be(Gender.Male);
                profile.DateOfBirth.Should().Be(dateOfBirth);
                profile.UpdatedAt.Should().Be(_updatedAt);
            }
        }

        [Fact]
        public void UpdatePersonalDetails_WithNulls_ShouldResetFields()
        {
            // Arrange
            UserProfile profile = CreateDefaultProfile();
            profile.UpdatePersonalDetails(Gender.Female, new DateOnly(1995, 1, 1), _updatedAt);

            // Act
            profile.UpdatePersonalDetails(null, null, _updatedAt);

            // Assert
            using (new AssertionScope())
            {
                profile.Gender.Should().BeNull();
                profile.DateOfBirth.Should().BeNull();
            }
        }

        #endregion

        #region UpdateLocation

        [Fact]
        public void UpdateLocation_ShouldSetFieldsAndRefreshUpdatedAt()
        {
            // Arrange
            UserProfile profile = CreateDefaultProfile();

            // Act
            profile.UpdateLocation("Россия", "Краснодарский край", "Краснодар", _updatedAt);

            // Assert
            using (new AssertionScope())
            {
                profile.Country.Should().Be("Россия");
                profile.Region.Should().Be("Краснодарский край");
                profile.City.Should().Be("Краснодар");
                profile.UpdatedAt.Should().Be(_updatedAt);
            }
        }

        #endregion

        #region UpdateAvatar

        [Fact]
        public void UpdateAvatar_ShouldSetMediaIdAndRefreshUpdatedAt()
        {
            // Arrange
            UserProfile profile = CreateDefaultProfile();
            Guid mediaId = Guid.NewGuid();

            // Act
            profile.UpdateAvatar(mediaId, _updatedAt);

            // Assert
            using (new AssertionScope())
            {
                profile.AvatarMediaId.Should().Be(mediaId);
                profile.UpdatedAt.Should().Be(_updatedAt);
            }
        }

        [Fact]
        public void UpdateAvatar_WithNull_ShouldClearAvatar()
        {
            // Arrange
            UserProfile profile = CreateDefaultProfile();
            profile.UpdateAvatar(Guid.NewGuid(), _updatedAt);

            // Act — null трактуется как удаление аватара
            profile.UpdateAvatar(null, _updatedAt);

            // Assert
            profile.AvatarMediaId.Should().BeNull();
        }

        #endregion

        #region SetVisibility

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SetVisibility_ShouldSetFlagAndRefreshUpdatedAt(bool isPublic)
        {
            // Arrange
            UserProfile profile = CreateDefaultProfile();

            // Act
            profile.SetVisibility(isPublic, _updatedAt);

            // Assert
            using (new AssertionScope())
            {
                profile.IsPublic.Should().Be(isPublic);
                profile.UpdatedAt.Should().Be(_updatedAt);
            }
        }

        #endregion

        #region UpdateAuthMirrorData

        [Fact]
        public void UpdateAuthMirrorData_ShouldSyncAuthFieldsAndRefreshUpdatedAt()
        {
            // Arrange
            UserProfile profile = CreateDefaultProfile();

            // Act — симулируем успешную смену данных в модуле Auth
            profile.UpdateAuthMirrorData(
                email: "new@example.com",
                phone: "+79009876543",
                userName: "new_user",
                updatedAt: _updatedAt);

            // Assert
            using (new AssertionScope())
            {
                profile.Email.Should().Be("new@example.com");
                profile.Phone.Should().Be("+79009876543");
                profile.UserName.Should().Be("new_user");
                profile.UpdatedAt.Should().Be(_updatedAt);
            }
        }

        [Fact]
        public void UpdateAuthMirrorData_ShouldNotAffectProfileSpecificFields()
        {
            // Arrange — в профиле заполнены данные, не связанные с Auth
            UserProfile profile = CreateDefaultProfile();
            profile.UpdatePersonalInfo("Иван", "Иванов", null, "Ваня", _createdAt);
            profile.UpdateBio("Био.", _createdAt);
            profile.UpdateLocation("Россия", "КК", "Краснодар", _createdAt);

            // Act
            profile.UpdateAuthMirrorData("new@x.com", null, "new_user", _updatedAt);

            // Assert — персональные поля остались нетронутыми
            using (new AssertionScope())
            {
                profile.FirstName.Should().Be("Иван");
                profile.LastName.Should().Be("Иванов");
                profile.DisplayName.Should().Be("Ваня");
                profile.Bio.Should().Be("Био.");
                profile.Country.Should().Be("Россия");
                profile.City.Should().Be("Краснодар");
            }
        }

        [Fact]
        public void UpdateAuthMirrorData_WithNullPhone_ShouldSetPhoneToNull()
        {
            // Arrange
            UserProfile profile = CreateDefaultProfile();

            // Act — пользователь удалил телефон в Auth
            profile.UpdateAuthMirrorData("new@x.com", null, "new_user", _updatedAt);

            // Assert
            profile.Phone.Should().BeNull();
        }

        #endregion

        #region Инварианты

        [Fact]
        public void UpdateMethods_ShouldPreserveCreatedAt()
        {
            // Arrange — проверяем что CreatedAt неизменна после любых обновлений
            UserProfile profile = CreateDefaultProfile();
            DateTimeOffset later = _createdAt.AddDays(30);

            // Act — прогон всех Update-методов через профиль
            profile.UpdatePersonalInfo("F", "L", "M", "D", later);
            profile.UpdateBio("bio", later);
            profile.UpdatePersonalDetails(Gender.Other, new DateOnly(1990, 1, 1), later);
            profile.UpdateLocation("C", "R", "Ci", later);
            profile.UpdateAvatar(Guid.NewGuid(), later);
            profile.SetVisibility(false, later);
            profile.UpdateAuthMirrorData("new@x.com", "+79", "new_user", later);

            // Assert — CreatedAt не трогается ни одним из методов обновления
            profile.CreatedAt.Should().Be(_createdAt);
        }

        #endregion
    }
}
