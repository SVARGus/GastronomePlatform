using FluentValidation.TestHelper;
using GastronomePlatform.Modules.Users.Application.Commands.ChangePhone;

namespace GastronomePlatform.Users.UnitTests.Application.Validators
{
    /// <summary>
    /// Тесты для <see cref="ChangePhoneCommandValidator"/>.
    /// </summary>
    public sealed class ChangePhoneCommandValidatorTests
    {
        private readonly ChangePhoneCommandValidator _validator = new();

        private static ChangePhoneCommand CreateCommand(
            Guid? userId = null,
            string newPhone = "+79001234567")
            => new(userId ?? Guid.NewGuid(), newPhone);

        #region Happy path

        [Theory]
        [InlineData("+79001234567")]             // с плюсом
        [InlineData("89001234567")]              // без плюса
        [InlineData("+7 (900) 123-45-67")]       // с пробелами, скобками, тире
        public void Validate_ValidPhoneFormats_ShouldHaveNoErrors(string phone)
        {
            _validator.TestValidate(CreateCommand(newPhone: phone))
                .ShouldNotHaveAnyValidationErrors();
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

        #region NewPhone

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("abc123")]                    // буквы недопустимы
        [InlineData("+7 900 abc")]                // смешанные символы
        [InlineData("7-900-число")]               // кириллица
        public void NewPhone_WhenInvalidFormat_ShouldHaveError(string newPhone)
        {
            _validator.TestValidate(CreateCommand(newPhone: newPhone))
                .ShouldHaveValidationErrorFor(c => c.NewPhone);
        }

        [Fact]
        public void NewPhone_WhenLongerThan50_ShouldHaveError()
        {
            // Arrange — только допустимые символы, но длина 51
            string tooLong = new string('1', 51);

            _validator.TestValidate(CreateCommand(newPhone: tooLong))
                .ShouldHaveValidationErrorFor(c => c.NewPhone);
        }

        #endregion
    }
}
