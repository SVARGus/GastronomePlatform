using FluentValidation.TestHelper;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Modules.Subscriptions.Application.Commands.CreateSubscriptionPlan;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Subscriptions.UnitTests.Application.Validators
{
    /// <summary>
    /// Тесты для <see cref="CreateSubscriptionPlanCommandValidator"/>.
    /// </summary>
    public sealed class CreateSubscriptionPlanCommandValidatorTests
    {
        private readonly CreateSubscriptionPlanCommandValidator _validator = new();

        private static CreateSubscriptionPlanCommand CreateValidCommand(
            PlanKind planKind = PlanKind.Base,
            string publicName = "Премиум",
            string? technicalName = "premium",
            string? description = "Полный доступ",
            string? requiredRole = null,
            DateTimeOffset? availableFrom = null,
            DateTimeOffset? availableUntil = null,
            string? internalNotes = null)
            => new(
                PlanKind: planKind,
                PublicName: publicName,
                TechnicalName: technicalName,
                Description: description,
                RequiredRole: requiredRole,
                AvailableFrom: availableFrom,
                AvailableUntil: availableUntil,
                InternalNotes: internalNotes);

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
            // Arrange — все необязательные поля null (When-правила пропускают валидацию)
            CreateSubscriptionPlanCommand command = CreateValidCommand(
                technicalName: null,
                description: null,
                requiredRole: null,
                availableFrom: null,
                availableUntil: null,
                internalNotes: null);

            _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        #region PlanKind

        [Fact]
        public void PlanKind_WhenOutOfEnum_ShouldHaveError()
        {
            CreateSubscriptionPlanCommand command = CreateValidCommand(planKind: (PlanKind)99);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.PlanKind);
        }

        #endregion

        #region PublicName

        [Fact]
        public void PublicName_WhenEmpty_ShouldHaveError()
        {
            CreateSubscriptionPlanCommand command = CreateValidCommand(publicName: string.Empty);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.PublicName);
        }

        [Fact]
        public void PublicName_WhenShorterThanMin_ShouldHaveError()
        {
            CreateSubscriptionPlanCommand command = CreateValidCommand(
                publicName: new string('a', SubscriptionPlan.MIN_PUBLIC_NAME_LENGTH - 1));

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.PublicName);
        }

        [Fact]
        public void PublicName_WhenLongerThanMax_ShouldHaveError()
        {
            CreateSubscriptionPlanCommand command = CreateValidCommand(
                publicName: new string('a', SubscriptionPlan.MAX_PUBLIC_NAME_LENGTH + 1));

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.PublicName);
        }

        #endregion

        #region TechnicalName

        [Fact]
        public void TechnicalName_WhenLongerThanMax_ShouldHaveError()
        {
            CreateSubscriptionPlanCommand command = CreateValidCommand(
                technicalName: new string('a', SubscriptionPlan.MAX_TECHNICAL_NAME_LENGTH + 1));

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.TechnicalName);
        }

        [Fact]
        public void TechnicalName_WhenNull_ShouldHaveNoError()
        {
            CreateSubscriptionPlanCommand command = CreateValidCommand(technicalName: null);

            _validator.TestValidate(command)
                .ShouldNotHaveValidationErrorFor(c => c.TechnicalName);
        }

        #endregion

        #region Description

        [Fact]
        public void Description_WhenLongerThanMax_ShouldHaveError()
        {
            CreateSubscriptionPlanCommand command = CreateValidCommand(
                description: new string('a', SubscriptionPlan.MAX_DESCRIPTION_LENGTH + 1));

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.Description);
        }

        #endregion

        #region InternalNotes

        [Fact]
        public void InternalNotes_WhenLongerThanMax_ShouldHaveError()
        {
            CreateSubscriptionPlanCommand command = CreateValidCommand(
                internalNotes: new string('a', SubscriptionPlan.MAX_INTERNAL_NOTES_LENGTH + 1));

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.InternalNotes);
        }

        #endregion

        #region RequiredRole

        [Theory]
        [InlineData("User")]
        [InlineData("Premium")]
        [InlineData("Chef")]
        [InlineData("Restaurant")]
        [InlineData("Admin")]
        public void RequiredRole_WhenAllowed_ShouldHaveNoError(string role)
        {
            CreateSubscriptionPlanCommand command = CreateValidCommand(requiredRole: role);

            _validator.TestValidate(command)
                .ShouldNotHaveValidationErrorFor(c => c.RequiredRole);
        }

        [Fact]
        public void RequiredRole_WhenNull_ShouldHaveNoError()
        {
            CreateSubscriptionPlanCommand command = CreateValidCommand(requiredRole: null);

            _validator.TestValidate(command)
                .ShouldNotHaveValidationErrorFor(c => c.RequiredRole);
        }

        [Fact]
        public void RequiredRole_WhenOutsideAllowedSet_ShouldHaveError()
        {
            CreateSubscriptionPlanCommand command = CreateValidCommand(requiredRole: "SomeCustomRole");

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.RequiredRole);
        }

        [Fact]
        public void RequiredRole_WhenCaseMismatch_ShouldHaveError()
        {
            // Arrange — сравнение через StringComparer.Ordinal: "admin" ≠ "Admin"
            CreateSubscriptionPlanCommand command = CreateValidCommand(requiredRole: PlatformRoles.ADMIN.ToLowerInvariant());

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.RequiredRole);
        }

        #endregion

        #region Availability window

        [Fact]
        public void AvailabilityWindow_WhenFromEqualUntil_ShouldHaveError()
        {
            DateTimeOffset date = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
            CreateSubscriptionPlanCommand command = CreateValidCommand(
                availableFrom: date,
                availableUntil: date);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c);
        }

        [Fact]
        public void AvailabilityWindow_WhenFromGreaterThanUntil_ShouldHaveError()
        {
            CreateSubscriptionPlanCommand command = CreateValidCommand(
                availableFrom: new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
                availableUntil: new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero));

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c);
        }

        [Fact]
        public void AvailabilityWindow_WhenOnlyFromSet_ShouldHaveNoError()
        {
            CreateSubscriptionPlanCommand command = CreateValidCommand(
                availableFrom: new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
                availableUntil: null);

            _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void AvailabilityWindow_WhenOnlyUntilSet_ShouldHaveNoError()
        {
            CreateSubscriptionPlanCommand command = CreateValidCommand(
                availableFrom: null,
                availableUntil: new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero));

            _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
        }

        #endregion
    }
}
