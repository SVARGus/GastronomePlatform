using FluentValidation.TestHelper;
using GastronomePlatform.Modules.Subscriptions.Application.Queries.GetSubscriptionById;

namespace GastronomePlatform.Subscriptions.UnitTests.Application.Validators
{
    /// <summary>
    /// Тесты для <see cref="GetSubscriptionByIdQueryValidator"/>.
    /// </summary>
    public sealed class GetSubscriptionByIdQueryValidatorTests
    {
        private readonly GetSubscriptionByIdQueryValidator _validator = new();

        #region Happy path

        [Fact]
        public void Validate_ValidQuery_ShouldHaveNoErrors()
        {
            GetSubscriptionByIdQuery query = new(SubscriptionId: Guid.NewGuid());

            _validator.TestValidate(query)
                .ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        #region SubscriptionId

        [Fact]
        public void SubscriptionId_WhenEmpty_ShouldHaveError()
        {
            GetSubscriptionByIdQuery query = new(SubscriptionId: Guid.Empty);

            _validator.TestValidate(query)
                .ShouldHaveValidationErrorFor(q => q.SubscriptionId);
        }

        #endregion
    }
}
