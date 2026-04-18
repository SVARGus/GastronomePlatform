using FluentValidation.TestHelper;
using GastronomePlatform.Modules.Users.Application.Commands.UpdatePersonalInfo;
using GastronomePlatform.Modules.Users.Domain.Enums;

namespace GastronomePlatform.Users.UnitTests.Application.Validators
{
    /// <summary>
    /// Тесты для <see cref="UpdatePersonalInfoCommandValidator"/>.
    /// </summary>
    public sealed class UpdatePersonalInfoCommandValidatorTests
    {
        private readonly UpdatePersonalInfoCommandValidator _validator = new();

        private static UpdatePersonalInfoCommand CreateValidCommand(
            Guid? userId = null,
            string? firstName = "Иван",
            string? lastName = "Иванов",
            string? middleName = "Иванович",
            string? displayName = "Ваня",
            string? bio = "Био.",
            Gender? gender = Gender.Male,
            DateOnly? dateOfBirth = null)
            => new(
                UserId: userId ?? Guid.NewGuid(),
                FirstName: firstName,
                LastName: lastName,
                MiddleName: middleName,
                DisplayName: displayName,
                Bio: bio,
                Gender: gender,
                DateOfBirth: dateOfBirth); // null намеренно — проверяется ветка When()

        #region Happy path

        [Fact]
        public void Validate_ValidCommand_ShouldHaveNoErrors()
        {
            _validator.TestValidate(CreateValidCommand())
                .ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_CommandWithAllNullableFieldsNull_ShouldHaveNoErrors()
        {
            // Arrange — все необязательные поля null (When-правила пропускают валидацию)
            UpdatePersonalInfoCommand command = new(
                UserId: Guid.NewGuid(),
                FirstName: null,
                LastName: null,
                MiddleName: null,
                DisplayName: null,
                Bio: null,
                Gender: null,
                DateOfBirth: null);

            _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        #region UserId

        [Fact]
        public void UserId_WhenEmpty_ShouldHaveError()
        {
            UpdatePersonalInfoCommand command = CreateValidCommand(userId: Guid.Empty);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.UserId);
        }

        #endregion

        #region String length

        [Fact]
        public void FirstName_WhenLongerThan100_ShouldHaveError()
        {
            UpdatePersonalInfoCommand command = CreateValidCommand(firstName: new string('a', 101));

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.FirstName);
        }

        [Fact]
        public void LastName_WhenLongerThan100_ShouldHaveError()
        {
            UpdatePersonalInfoCommand command = CreateValidCommand(lastName: new string('a', 101));

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.LastName);
        }

        [Fact]
        public void MiddleName_WhenLongerThan100_ShouldHaveError()
        {
            UpdatePersonalInfoCommand command = CreateValidCommand(middleName: new string('a', 101));

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.MiddleName);
        }

        [Fact]
        public void DisplayName_WhenLongerThan100_ShouldHaveError()
        {
            UpdatePersonalInfoCommand command = CreateValidCommand(displayName: new string('a', 101));

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.DisplayName);
        }

        [Fact]
        public void Bio_WhenLongerThan2000_ShouldHaveError()
        {
            UpdatePersonalInfoCommand command = CreateValidCommand(bio: new string('a', 2001));

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.Bio);
        }

        #endregion

        #region DateOfBirth

        [Fact]
        public void DateOfBirth_WhenInFuture_ShouldHaveError()
        {
            // Arrange — дата в будущем запрещена (бизнес-инвариант)
            DateOnly future = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            UpdatePersonalInfoCommand command = CreateValidCommand(dateOfBirth: future);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.DateOfBirth);
        }

        [Fact]
        public void DateOfBirth_WhenInPast_ShouldHaveNoError()
        {
            // Arrange — типичная дата рождения в прошлом
            UpdatePersonalInfoCommand command = CreateValidCommand(dateOfBirth: new DateOnly(1990, 5, 15));

            _validator.TestValidate(command)
                .ShouldNotHaveValidationErrorFor(c => c.DateOfBirth);
        }

        [Fact]
        public void DateOfBirth_WhenNull_ShouldHaveNoError()
        {
            // Arrange — null обходит правило через .When()
            UpdatePersonalInfoCommand command = CreateValidCommand(dateOfBirth: null);

            _validator.TestValidate(command)
                .ShouldNotHaveValidationErrorFor(c => c.DateOfBirth);
        }

        #endregion
    }
}
