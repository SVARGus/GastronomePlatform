using FluentValidation.TestHelper;
using GastronomePlatform.Modules.Users.Application.Commands.ChangeEmail;

namespace GastronomePlatform.Users.UnitTests.Application.Validators
{
    /// <summary>
    /// Тесты для <see cref="ChangeEmailCommandValidator"/>.
    /// </summary>
    public sealed class ChangeEmailCommandValidatorTests
    {
        private readonly ChangeEmailCommandValidator _validator = new();

        private static ChangeEmailCommand CreateCommand(
            Guid? userId = null,
            string newEmail = "new@example.com")
            => new(userId ?? Guid.NewGuid(), newEmail);

        #region Happy path

        [Fact]
        public void Validate_ValidCommand_ShouldHaveNoErrors()
        {
            _validator.TestValidate(CreateCommand()).ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        #region UserId

        [Fact]
        public void UserId_WhenEmpty_ShouldHaveError()
        {
            _validator.TestValidate(CreateCommand(userId: Guid.Empty))
                .ShouldHaveValidationErrorFor(c => c.UserId);
        }

        #endregion

        #region NewEmail

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-an-email")]
        [InlineData("@example.com")]
        [InlineData("user@")]
        public void NewEmail_WhenInvalid_ShouldHaveError(string newEmail)
        {
            _validator.TestValidate(CreateCommand(newEmail: newEmail))
                .ShouldHaveValidationErrorFor(c => c.NewEmail);
        }

        [Fact]
        public void NewEmail_WhenLongerThan256_ShouldHaveError()
        {
            // Arrange — валидный формат, но длина 257 символов
            string tooLong = new string('a', 252) + "@b.co";

            _validator.TestValidate(CreateCommand(newEmail: tooLong))
                .ShouldHaveValidationErrorFor(c => c.NewEmail);
        }

        #endregion
    }
}
