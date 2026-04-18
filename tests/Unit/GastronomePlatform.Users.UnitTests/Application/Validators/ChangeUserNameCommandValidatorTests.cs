using FluentValidation.TestHelper;
using GastronomePlatform.Modules.Users.Application.Commands.ChangeUserName;

namespace GastronomePlatform.Users.UnitTests.Application.Validators
{
    /// <summary>
    /// Тесты для <see cref="ChangeUserNameCommandValidator"/>.
    /// </summary>
    public sealed class ChangeUserNameCommandValidatorTests
    {
        private readonly ChangeUserNameCommandValidator _validator = new();

        private static ChangeUserNameCommand CreateCommand(
            Guid? userId = null,
            string newUserName = "valid_user")
            => new(userId ?? Guid.NewGuid(), newUserName);

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

        #region NewUserName

        [Theory]
        [InlineData("")]                   // пустой
        [InlineData("ab")]                 // короче 3
        [InlineData("user name")]          // пробел недопустим
        [InlineData("user-name")]          // тире недопустимо
        [InlineData("user.name")]          // точка недопустима
        [InlineData("юзер")]               // не-латиница недопустима
        public void NewUserName_WhenInvalid_ShouldHaveError(string newUserName)
        {
            _validator.TestValidate(CreateCommand(newUserName: newUserName))
                .ShouldHaveValidationErrorFor(c => c.NewUserName);
        }

        [Fact]
        public void NewUserName_WhenLongerThan100_ShouldHaveError()
        {
            string tooLong = new string('a', 101);

            _validator.TestValidate(CreateCommand(newUserName: tooLong))
                .ShouldHaveValidationErrorFor(c => c.NewUserName);
        }

        #endregion
    }
}
