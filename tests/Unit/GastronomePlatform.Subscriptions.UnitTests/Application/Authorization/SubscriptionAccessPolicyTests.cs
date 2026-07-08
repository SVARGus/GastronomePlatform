using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Application.Authorization;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using GastronomePlatform.Modules.Subscriptions.Domain.Errors;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;
using Moq;

namespace GastronomePlatform.Subscriptions.UnitTests.Application.Authorization
{
    /// <summary>
    /// Тесты для <see cref="SubscriptionAccessPolicy"/> (POL-004 §4.1, §4.3).
    /// </summary>
    public sealed class SubscriptionAccessPolicyTests
    {
        private readonly Mock<IUserSubscriptionRepository> _repositoryMock = new();
        private readonly SubscriptionAccessPolicy _policy;

        private static readonly DateTimeOffset _now =
            new(2026, 7, 8, 12, 0, 0, TimeSpan.Zero);
        private static readonly Guid _subscriptionId = Guid.NewGuid();
        private static readonly Guid _ownerUserId = Guid.NewGuid();
        private static readonly Guid _otherUserId = Guid.NewGuid();

        public SubscriptionAccessPolicyTests()
        {
            _policy = new SubscriptionAccessPolicy(_repositoryMock.Object);
        }

        /// <summary>
        /// Создаёт стандартную платную подписку с указанным <paramref name="userId"/>-владельцем.
        /// Использует фабрику <see cref="UserSubscription.Activate"/> — единственный способ
        /// собрать агрегат c правильно установленным <c>UserId</c>.
        /// </summary>
        private static UserSubscription CreateSubscription(Guid userId)
        {
            var result = UserSubscription.Activate(
                userId:                 userId,
                planId:                 Guid.NewGuid(),
                planKind:               PlanKind.Base,
                priceId:                Guid.NewGuid(),
                priceKind:              OfferKind.Standard,
                amount:                 1000m,
                currency:               "RUB",
                durationDays:           30,
                trialDays:              null,
                gatewayPaymentMethodId: "pm_test",
                gatewayTransactionId:   "tx_test",
                gatewayPayload:         null,
                termsSnapshot:          "{}",
                documentNumber:         null,
                contentHash:            null,
                acceptedAt:             _now,
                utcNow:                 _now);

            return result.Value;
        }

        #region Constructor

        [Fact]
        public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new SubscriptionAccessPolicy(null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("repository");
        }

        #endregion

        #region AuthorizeOperationAsync

        [Fact]
        public async Task Authorize_WhenSubscriptionNotFound_ReturnsNotFoundAsync()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetByIdAsync(_subscriptionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserSubscription?)null);

            // Act
            Result result = await _policy.AuthorizeOperationAsync(
                _subscriptionId,
                _ownerUserId,
                new[] { PlatformRoles.USER },
                CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(SubscriptionsErrors.NotFound);
            }
        }

        [Fact]
        public async Task Authorize_WhenActorIsAdminAndNotOwner_ReturnsSuccessAsync()
        {
            // Arrange — POL-004 §4.3: Admin имеет доступ к любой подписке.
            _repositoryMock
                .Setup(r => r.GetByIdAsync(_subscriptionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateSubscription(_ownerUserId));

            // Act — actor = не владелец, но с ролью Admin.
            Result result = await _policy.AuthorizeOperationAsync(
                _subscriptionId,
                _otherUserId,
                new[] { PlatformRoles.ADMIN },
                CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Authorize_WhenActorIsOwnerAndNotAdmin_ReturnsSuccessAsync()
        {
            // Arrange — POL-004 §4.1: владелец имеет доступ.
            _repositoryMock
                .Setup(r => r.GetByIdAsync(_subscriptionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateSubscription(_ownerUserId));

            // Act
            Result result = await _policy.AuthorizeOperationAsync(
                _subscriptionId,
                _ownerUserId,
                new[] { PlatformRoles.USER },
                CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Authorize_WhenActorIsNotOwnerAndNotAdmin_ReturnsForbiddenNotOwnerAsync()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetByIdAsync(_subscriptionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateSubscription(_ownerUserId));

            // Act
            Result result = await _policy.AuthorizeOperationAsync(
                _subscriptionId,
                _otherUserId,
                new[] { PlatformRoles.USER },
                CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(SubscriptionsErrors.ForbiddenNotOwner);
            }
        }

        [Fact]
        public async Task Authorize_WhenActorHasEmptyRoles_ChecksOwnershipAsync()
        {
            // Arrange — sanity-check ветки без ролей: полагаемся на UserId-сравнение.
            _repositoryMock
                .Setup(r => r.GetByIdAsync(_subscriptionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateSubscription(_ownerUserId));

            // Act
            Result result = await _policy.AuthorizeOperationAsync(
                _subscriptionId,
                _ownerUserId,
                Array.Empty<string>(),
                CancellationToken.None);

            // Assert — actorUserId == subscription.UserId → Success.
            result.IsSuccess.Should().BeTrue();
        }

        #endregion
    }
}
