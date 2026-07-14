using FluentValidation.TestHelper;
using GastronomePlatform.Modules.Subscriptions.Application.Commands.Subscribe;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;

namespace GastronomePlatform.Subscriptions.UnitTests.Application.Validators
{
    /// <summary>
    /// Тесты для <see cref="SubscribeCommandValidator"/>.
    /// </summary>
    public sealed class SubscribeCommandValidatorTests
    {
        private readonly SubscribeCommandValidator _validator = new();

        private static SubscribeCommand CreateValidCommand(
            Guid? priceId = null,
            string paymentMethodId = "pm_test",
            DateTimeOffset? acceptedTermsAt = null)
            => new(
                PriceId:         priceId ?? Guid.NewGuid(),
                PaymentMethodId: paymentMethodId,
                AcceptedTermsAt: acceptedTermsAt ?? new DateTimeOffset(2026, 7, 8, 12, 0, 0, TimeSpan.Zero));

        #region Happy path

        [Fact]
        public void Validate_ValidCommand_ShouldHaveNoErrors()
        {
            _validator.TestValidate(CreateValidCommand())
                .ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        #region PriceId

        [Fact]
        public void PriceId_WhenEmpty_ShouldHaveError()
        {
            SubscribeCommand command = CreateValidCommand(priceId: Guid.Empty);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.PriceId);
        }

        #endregion

        #region PaymentMethodId

        [Fact]
        public void PaymentMethodId_WhenEmpty_ShouldHaveError()
        {
            SubscribeCommand command = CreateValidCommand(paymentMethodId: string.Empty);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.PaymentMethodId);
        }

        [Fact]
        public void PaymentMethodId_WhenLongerThanMax_ShouldHaveError()
        {
            SubscribeCommand command = CreateValidCommand(
                paymentMethodId: new string('a', UserSubscription.MAX_GATEWAY_PAYMENT_METHOD_ID_LENGTH + 1));

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.PaymentMethodId);
        }

        [Fact]
        public void PaymentMethodId_WhenAtMax_ShouldHaveNoError()
        {
            // Arrange — граничный случай: ровно MAX должен проходить.
            SubscribeCommand command = CreateValidCommand(
                paymentMethodId: new string('a', UserSubscription.MAX_GATEWAY_PAYMENT_METHOD_ID_LENGTH));

            _validator.TestValidate(command)
                .ShouldNotHaveValidationErrorFor(c => c.PaymentMethodId);
        }

        #endregion

        #region AcceptedTermsAt

        [Fact]
        public void AcceptedTermsAt_WhenDefault_ShouldHaveError()
        {
            SubscribeCommand command = CreateValidCommand(acceptedTermsAt: default(DateTimeOffset));

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.AcceptedTermsAt);
        }

        #endregion
    }
}
