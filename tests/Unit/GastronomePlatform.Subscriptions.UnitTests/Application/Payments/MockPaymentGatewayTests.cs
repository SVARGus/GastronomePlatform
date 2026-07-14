using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Application.Payments;
using GastronomePlatform.Modules.Subscriptions.Infrastructure.Payments;

namespace GastronomePlatform.Subscriptions.UnitTests.Application.Payments
{
    /// <summary>
    /// Тесты для <see cref="MockPaymentGateway"/> (Phase A stub).
    /// </summary>
    /// <remarks>
    /// Форма <c>mock_tx_{Guid:N}</c> — деталь реализации; тесты проверяют только
    /// префикс <c>StartsWith("mock_tx_")</c>, чтобы не быть хрупкими к смене
    /// именования (например, <c>mock_tx_v2_</c> в Phase B fixture-е).
    /// </remarks>
    public sealed class MockPaymentGatewayTests
    {
        private readonly MockPaymentGateway _gateway = new();

        [Fact]
        public async Task AuthorizeVerificationChargeAsync_ReturnsSuccessWithSyntheticTransactionAsync()
        {
            // Act
            Result<PaymentGatewayResult> result = await _gateway.AuthorizeVerificationChargeAsync(
                "pm_test_verification",
                "RUB",
                CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                result.Value.TransactionId.Should().StartWith("mock_tx_");
                result.Value.RawPayload.Should().NotBeNullOrEmpty();
                result.Value.RawPayload.Should().Contain("pm_test_verification");
            }
        }

        [Fact]
        public async Task AuthorizeInitialChargeAsync_ReturnsSuccessWithSyntheticTransactionAsync()
        {
            // Act
            Result<PaymentGatewayResult> result = await _gateway.AuthorizeInitialChargeAsync(
                "pm_test_initial",
                4990m,
                "RUB",
                CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                result.Value.TransactionId.Should().StartWith("mock_tx_");
                result.Value.RawPayload.Should().NotBeNullOrEmpty();
                result.Value.RawPayload.Should().Contain("pm_test_initial");
            }
        }
    }
}
