using FluentValidation.TestHelper;
using GastronomePlatform.Modules.Subscriptions.Application.Commands.SetPlanGrants;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Subscriptions.UnitTests.Application.Validators
{
    /// <summary>
    /// Тесты для <see cref="SetPlanGrantsCommandValidator"/>.
    /// </summary>
    public sealed class SetPlanGrantsCommandValidatorTests
    {
        private readonly SetPlanGrantsCommandValidator _validator = new();

        private static SetPlanGrantsCommand CreateValidCommand(
            Guid? planId = null,
            IReadOnlyList<PlanGrantSpec>? grants = null)
            => new(
                PlanId: planId ?? Guid.NewGuid(),
                Grants: grants ?? new List<PlanGrantSpec>
                {
                    new(FeatureGrant.FullRecipes, null),
                    new(FeatureGrant.PortionCalculator, null),
                });

        #region Happy path

        [Fact]
        public void Validate_ValidCommand_ShouldHaveNoErrors()
        {
            _validator.TestValidate(CreateValidCommand())
                .ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyGrantsList_ShouldHaveNoErrors()
        {
            // Arrange — пустой список = снять все гранты, валидный сценарий (см. UC-SUB-007)
            SetPlanGrantsCommand command = CreateValidCommand(grants: new List<PlanGrantSpec>());

            _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_QuotaGrantWithPositiveQuantity_ShouldHaveNoErrors()
        {
            // Arrange — валидатор проверяет только форму: Quantity > 0. Правило
            // «Quantity применимо только к PromotionAdvanced» — в хендлере.
            SetPlanGrantsCommand command = CreateValidCommand(grants: new List<PlanGrantSpec>
            {
                new(FeatureGrant.PromotionAdvanced, 10),
            });

            _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        #region PlanId

        [Fact]
        public void PlanId_WhenEmpty_ShouldHaveError()
        {
            SetPlanGrantsCommand command = CreateValidCommand(planId: Guid.Empty);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.PlanId);
        }

        #endregion

        #region Grants collection

        [Fact]
        public void Grants_WhenNull_ShouldHaveError()
        {
            // Собираем command напрямую, не через CreateValidCommand:
            // helper использует `?? defaultList` для аргумента grants и заменил бы null дефолтом.
            SetPlanGrantsCommand command = new(PlanId: Guid.NewGuid(), Grants: null!);

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.Grants);
        }

        [Fact]
        public void Grants_WhenDuplicateGrantValues_ShouldHaveError()
        {
            SetPlanGrantsCommand command = CreateValidCommand(grants: new List<PlanGrantSpec>
            {
                new(FeatureGrant.FullRecipes, null),
                new(FeatureGrant.FullRecipes, null),
            });

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor(c => c.Grants);
        }

        #endregion

        #region Grant item — enum validity

        [Fact]
        public void GrantItem_WhenGrantOutOfEnum_ShouldHaveError()
        {
            SetPlanGrantsCommand command = CreateValidCommand(grants: new List<PlanGrantSpec>
            {
                new((FeatureGrant)99, null),
            });

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor("Grants[0].Grant");
        }

        #endregion

        #region Grant item — Quantity

        [Fact]
        public void GrantItem_WhenQuantityZero_ShouldHaveError()
        {
            SetPlanGrantsCommand command = CreateValidCommand(grants: new List<PlanGrantSpec>
            {
                new(FeatureGrant.PromotionAdvanced, 0),
            });

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor("Grants[0].Quantity");
        }

        [Fact]
        public void GrantItem_WhenQuantityNegative_ShouldHaveError()
        {
            SetPlanGrantsCommand command = CreateValidCommand(grants: new List<PlanGrantSpec>
            {
                new(FeatureGrant.PromotionAdvanced, -1),
            });

            _validator.TestValidate(command)
                .ShouldHaveValidationErrorFor("Grants[0].Quantity");
        }

        [Fact]
        public void GrantItem_WhenQuantityNull_ShouldHaveNoError()
        {
            SetPlanGrantsCommand command = CreateValidCommand(grants: new List<PlanGrantSpec>
            {
                new(FeatureGrant.FullRecipes, null),
            });

            _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
        }

        #endregion
    }
}
