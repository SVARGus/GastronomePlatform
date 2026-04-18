using FluentValidation.TestHelper;
using GastronomePlatform.Modules.Auth.Application.Commands.Login;

namespace GastronomePlatform.Auth.UnitTests.Application
{
    /// <summary>
    /// Тесты для <see cref="LoginCommandValidator"/>.
    /// Валидатор не проверяет формат логина (email/username/phone) — это делает репозиторий.
    /// </summary>
    public sealed class LoginCommandValidatorTests
    {
        private readonly LoginCommandValidator _validator = new();

        private static LoginCommand CreateValidCommand(
            string login = "user@example.com",
            string password = "SecurePass123!")
            => new(login, password);

        #region Happy path

        [Fact]
        public void Validate_ValidCommand_ShouldHaveNoErrors()
        {
            // Arrange
            LoginCommand command = CreateValidCommand();

            // Act & Assert
            _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        #region Login

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Login_WhenEmpty_ShouldHaveError(string login)
        {
            // Arrange
            LoginCommand command = CreateValidCommand(login: login);

            // Act & Assert
            _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.Login);
        }

        [Fact]
        public void Login_WhenLongerThan256_ShouldHaveError()
        {
            // Arrange
            string tooLong = new string('a', 257);
            LoginCommand command = CreateValidCommand(login: tooLong);

            // Act & Assert
            _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.Login);
        }

        #endregion

        #region Password

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Password_WhenEmpty_ShouldHaveError(string password)
        {
            // Arrange
            LoginCommand command = CreateValidCommand(password: password);

            // Act & Assert
            _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.Password);
        }

        [Fact]
        public void Password_WhenLongerThan100_ShouldHaveError()
        {
            // Arrange
            string tooLong = new string('a', 101);
            LoginCommand command = CreateValidCommand(password: tooLong);

            // Act & Assert
            _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.Password);
        }

        #endregion
    }
}
