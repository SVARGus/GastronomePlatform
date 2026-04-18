using FluentValidation.TestHelper;
using GastronomePlatform.Modules.Auth.Application.Commands.Register;

namespace GastronomePlatform.Auth.UnitTests.Application
{
    /// <summary>
    /// Тесты для <see cref="RegisterCommandValidator"/>.
    /// Проверяем наличие/отсутствие ошибки на конкретном поле, без привязки к тексту сообщения.
    /// </summary>
    public sealed class RegisterCommandValidatorTests
    {
        private readonly RegisterCommandValidator _validator = new();

        /// <summary>
        /// Валидная команда — эталон для happy path.
        /// </summary>
        private static RegisterCommand CreateValidCommand(
            string email = "user@example.com",
            string userName = "valid_user",
            string password = "Strong1@Pass",
            string? phone = null)
            => new(email, userName, password, phone);

        #region Happy path

        [Fact]
        public void Validate_ValidCommand_ShouldHaveNoErrors()
        {
            // Arrange
            RegisterCommand command = CreateValidCommand();

            // Act
            TestValidationResult<RegisterCommand> result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ValidCommandWithPhone_ShouldHaveNoErrors()
        {
            // Arrange
            RegisterCommand command = CreateValidCommand(phone: "+79001234567");

            // Act
            TestValidationResult<RegisterCommand> result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        #region Email

        [Theory]
        [InlineData("")]                   // пустой
        [InlineData("   ")]                // пробелы
        [InlineData("not-an-email")]       // без @
        [InlineData("@example.com")]       // без локальной части
        [InlineData("user@")]              // без домена
        public void Email_WhenInvalid_ShouldHaveError(string email)
        {
            // Arrange
            RegisterCommand command = CreateValidCommand(email: email);

            // Act & Assert
            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.Email);
        }

        [Fact]
        public void Email_WhenLongerThan256_ShouldHaveError()
        {
            // Arrange — 252 + "@b.co" = 257 символов, остальные правила проходят
            string tooLong = new string('a', 252) + "@b.co";
            RegisterCommand command = CreateValidCommand(email: tooLong);

            // Act & Assert
            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.Email);
        }

        #endregion

        #region UserName

        [Theory]
        [InlineData("")]                   // пустой
        [InlineData("ab")]                 // короче 3
        [InlineData("user name")]          // пробел недопустим
        [InlineData("user-name")]          // тире недопустимо
        [InlineData("user.name")]          // точка недопустима
        [InlineData("юзер")]               // не-латиница недопустима
        public void UserName_WhenInvalid_ShouldHaveError(string userName)
        {
            // Arrange
            RegisterCommand command = CreateValidCommand(userName: userName);

            // Act & Assert
            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.UserName);
        }

        [Fact]
        public void UserName_WhenLongerThan100_ShouldHaveError()
        {
            // Arrange — только допустимые символы, но длина > 100
            string tooLong = new string('a', 101);
            RegisterCommand command = CreateValidCommand(userName: tooLong);

            // Act & Assert
            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.UserName);
        }

        #endregion

        #region Password

        [Theory]
        [InlineData("")]                // пустой
        [InlineData("Short1!")]         // < 8 символов
        [InlineData("nouppercase1!")]   // без заглавной
        [InlineData("NOLOWERCASE1!")]   // без строчной
        [InlineData("NoDigitPass!")]    // без цифры
        [InlineData("NoSpecial123A")]   // без спецсимвола
        public void Password_WhenInvalid_ShouldHaveError(string password)
        {
            // Arrange
            RegisterCommand command = CreateValidCommand(password: password);

            // Act & Assert
            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.Password);
        }

        [Fact]
        public void Password_WhenLongerThan100_ShouldHaveError()
        {
            // Arrange — все классы символов есть, но длина > 100
            string tooLong = new string('A', 101) + "1a!";
            RegisterCommand command = CreateValidCommand(password: tooLong);

            // Act & Assert
            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.Password);
        }

        #endregion

        #region Phone (условное правило: When Phone is not null)

        [Fact]
        public void Phone_WhenNull_ShouldNotBeValidated()
        {
            // Arrange — телефон опционален, правило активируется только при наличии значения
            RegisterCommand command = CreateValidCommand(phone: null);

            // Act & Assert
            _validator.TestValidate(command)
                .ShouldNotHaveValidationErrorFor(c => c.Phone);
        }

        [Fact]
        public void Phone_WhenLongerThan50_ShouldHaveError()
        {
            // Arrange
            string tooLong = new string('1', 51);
            RegisterCommand command = CreateValidCommand(phone: tooLong);

            // Act & Assert
            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.Phone);
        }

        #endregion
    }
}
