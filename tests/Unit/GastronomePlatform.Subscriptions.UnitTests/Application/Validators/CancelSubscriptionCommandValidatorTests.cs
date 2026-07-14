using FluentValidation.TestHelper;
using GastronomePlatform.Modules.Subscriptions.Application.Commands.Cancel;

namespace GastronomePlatform.Subscriptions.UnitTests.Application.Validators
{
    /// <summary>
    /// Тесты для <see cref="CancelSubscriptionCommandValidator"/>.
    /// </summary>
    public sealed class CancelSubscriptionCommandValidatorTests
    {
        private readonly CancelSubscriptionCommandValidator _validator = new();

        #region Happy path

        [Fact]
        public void Validate_ValidCommand_ShouldHaveNoErrors()
        {
            CancelSubscriptionCommand command = new(SubscriptionId: Guid.NewGuid());

            _validator.TestValidate(command)
                .ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        #region SubscriptionId

        [Fact]
        public void SubscriptionId_WhenEmpty_ShouldHaveError()
        {
            CancelSubscriptionCommand command = new(SubscriptionId: Guid.Empty);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.SubscriptionId);
        }

        #endregion
    }
}
