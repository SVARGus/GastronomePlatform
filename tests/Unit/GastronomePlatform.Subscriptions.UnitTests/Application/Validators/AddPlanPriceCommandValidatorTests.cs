using FluentValidation.TestHelper;
using GastronomePlatform.Modules.Subscriptions.Application.Commands.AddPlanPrice;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Subscriptions.UnitTests.Application.Validators
{
    /// <summary>
    /// Тесты для <see cref="AddPlanPriceCommandValidator"/>.
    /// </summary>
    public sealed class AddPlanPriceCommandValidatorTests
    {
        private readonly AddPlanPriceCommandValidator _validator = new();

        private static AddPlanPriceCommand CreateValidCommand(
            Guid? planId = null,
            OfferKind kind = OfferKind.Standard,
            string? publicName = "Год",
            int? durationDays = 365,
            string currency = "RUB",
            decimal amount = 4990m,
            decimal? compareAtAmount = null,
            int? discountPercent = null,
            int? trialDays = null,
            bool isRecurring = true,
            bool isPurchasable = true,
            Guid? renewsAsPriceId = null,
            Guid? fallbackPriceId = null,
            DateTimeOffset? availableFrom = null,
            DateTimeOffset? availableUntil = null,
            string? internalNotes = null)
            => new(
                PlanId:           planId ?? Guid.NewGuid(),
                Kind:             kind,
                PublicName:       publicName,
                DurationDays:     durationDays,
                Currency:         currency,
                Amount:           amount,
                CompareAtAmount:  compareAtAmount,
                DiscountPercent:  discountPercent,
                TrialDays:        trialDays,
                IsRecurring:      isRecurring,
                IsPurchasable:    isPurchasable,
                RenewsAsPriceId:  renewsAsPriceId,
                FallbackPriceId:  fallbackPriceId,
                AvailableFrom:    availableFrom,
                AvailableUntil:   availableUntil,
                InternalNotes:    internalNotes);

        #region Happy path

        [Fact]
        public void Validate_ValidCommand_ShouldHaveNoErrors()
        {
            _validator.TestValidate(CreateValidCommand())
                .ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_CommandWithAllOptionalFieldsNull_ShouldHaveNoErrors()
        {
            // Arrange — все When-опциональные поля null
            AddPlanPriceCommand command = CreateValidCommand(
                publicName: null,
                durationDays: null,
                compareAtAmount: null,
                discountPercent: null,
                trialDays: null,
                internalNotes: null);

            _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        #region PlanId

        [Fact]
        public void PlanId_WhenEmpty_ShouldHaveError()
        {
            AddPlanPriceCommand command = CreateValidCommand(planId: Guid.Empty);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.PlanId);
        }

        #endregion

        #region Kind

        [Fact]
        public void Kind_WhenOutOfEnum_ShouldHaveError()
        {
            AddPlanPriceCommand command = CreateValidCommand(kind: (OfferKind)99);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.Kind);
        }

        #endregion

        #region PublicName

        [Fact]
        public void PublicName_WhenLongerThanMax_ShouldHaveError()
        {
            AddPlanPriceCommand command = CreateValidCommand(
                publicName: new string('a', PlanPrice.MAX_PUBLIC_NAME_LENGTH + 1));

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.PublicName);
        }

        #endregion

        #region DurationDays

        [Fact]
        public void DurationDays_WhenZero_ShouldHaveError()
        {
            AddPlanPriceCommand command = CreateValidCommand(durationDays: 0);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.DurationDays);
        }

        [Fact]
        public void DurationDays_WhenNegative_ShouldHaveError()
        {
            AddPlanPriceCommand command = CreateValidCommand(durationDays: -1);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.DurationDays);
        }

        #endregion

        #region Currency

        [Fact]
        public void Currency_WhenEmpty_ShouldHaveError()
        {
            AddPlanPriceCommand command = CreateValidCommand(currency: string.Empty);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.Currency);
        }

        [Fact]
        public void Currency_WhenWrongLength_ShouldHaveError()
        {
            AddPlanPriceCommand command = CreateValidCommand(currency: "RU");

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.Currency);
        }

        [Fact]
        public void Currency_WhenLowercase_ShouldHaveError()
        {
            AddPlanPriceCommand command = CreateValidCommand(currency: "rub");

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.Currency);
        }

        [Fact]
        public void Currency_WhenContainsDigits_ShouldHaveError()
        {
            AddPlanPriceCommand command = CreateValidCommand(currency: "RU1");

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.Currency);
        }

        #endregion

        #region CompareAtAmount

        [Fact]
        public void CompareAtAmount_WhenEqualAmount_ShouldHaveError()
        {
            AddPlanPriceCommand command = CreateValidCommand(amount: 1000m, compareAtAmount: 1000m);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.CompareAtAmount);
        }

        [Fact]
        public void CompareAtAmount_WhenLessThanAmount_ShouldHaveError()
        {
            AddPlanPriceCommand command = CreateValidCommand(amount: 1000m, compareAtAmount: 500m);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.CompareAtAmount);
        }

        [Fact]
        public void CompareAtAmount_WhenGreaterThanAmount_ShouldHaveNoError()
        {
            AddPlanPriceCommand command = CreateValidCommand(amount: 1000m, compareAtAmount: 1500m);

            _validator.TestValidate(command)
                .ShouldNotHaveValidationErrorFor(c => c.CompareAtAmount);
        }

        #endregion

        #region DiscountPercent

        [Fact]
        public void DiscountPercent_WhenNegative_ShouldHaveError()
        {
            AddPlanPriceCommand command = CreateValidCommand(discountPercent: -1);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.DiscountPercent);
        }

        [Fact]
        public void DiscountPercent_WhenGreaterThan100_ShouldHaveError()
        {
            AddPlanPriceCommand command = CreateValidCommand(discountPercent: 101);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.DiscountPercent);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(50)]
        [InlineData(100)]
        public void DiscountPercent_WhenInRange_ShouldHaveNoError(int percent)
        {
            AddPlanPriceCommand command = CreateValidCommand(discountPercent: percent);

            _validator.TestValidate(command)
                .ShouldNotHaveValidationErrorFor(c => c.DiscountPercent);
        }

        #endregion

        #region TrialDays

        [Fact]
        public void TrialDays_WhenZero_ShouldHaveError()
        {
            AddPlanPriceCommand command = CreateValidCommand(trialDays: 0);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.TrialDays);
        }

        [Fact]
        public void TrialDays_WhenNegative_ShouldHaveError()
        {
            AddPlanPriceCommand command = CreateValidCommand(trialDays: -3);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.TrialDays);
        }

        #endregion

        #region InternalNotes

        [Fact]
        public void InternalNotes_WhenLongerThanMax_ShouldHaveError()
        {
            AddPlanPriceCommand command = CreateValidCommand(
                internalNotes: new string('a', PlanPrice.MAX_INTERNAL_NOTES_LENGTH + 1));

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.InternalNotes);
        }

        #endregion

        #region Availability window

        [Fact]
        public void AvailabilityWindow_WhenFromEqualUntil_ShouldHaveError()
        {
            DateTimeOffset date = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
            AddPlanPriceCommand command = CreateValidCommand(availableFrom: date, availableUntil: date);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c);
        }

        [Fact]
        public void AvailabilityWindow_WhenFromGreaterThanUntil_ShouldHaveError()
        {
            AddPlanPriceCommand command = CreateValidCommand(
                availableFrom: new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
                availableUntil: new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero));

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c);
        }

        #endregion
    }
}
