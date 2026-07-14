using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Application.Authorization;
using GastronomePlatform.Modules.Subscriptions.Application.Queries.GetSubscriptionById;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using GastronomePlatform.Modules.Subscriptions.Domain.Errors;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;
using Moq;

namespace GastronomePlatform.Subscriptions.UnitTests.Application.Queries
{
    /// <summary>
    /// Тесты для <see cref="GetSubscriptionByIdQueryHandler"/> (UC-SUB-021).
    /// </summary>
    /// <remarks>
    /// <c>ISubscriptionAccessPolicy</c> мокается как чёрный ящик — Owner/Admin-логика
    /// покрывается в <c>SubscriptionAccessPolicyTests</c>. Тесты фокусируются на
    /// маппинге <c>UserSubscription → SubscriptionResponse</c> (19 полей) и на
    /// пробросе ошибок Policy.
    /// </remarks>
    public sealed class GetSubscriptionByIdQueryHandlerTests
    {
        private readonly Mock<ISubscriptionAccessPolicy> _accessPolicyMock = new();
        private readonly Mock<IUserSubscriptionRepository> _userSubRepoMock = new();
        private readonly Mock<ICurrentUserService> _currentUserMock = new();
        private readonly GetSubscriptionByIdQueryHandler _handler;

        private static readonly DateTimeOffset _now =
            new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);
        private static readonly Guid _actorUserId = Guid.NewGuid();
        private static readonly Guid _subscriptionId = Guid.NewGuid();
        private static readonly IReadOnlyCollection<string> _actorRoles = new[] { PlatformRoles.USER };

        public GetSubscriptionByIdQueryHandlerTests()
        {
            _handler = new GetSubscriptionByIdQueryHandler(
                _accessPolicyMock.Object,
                _userSubRepoMock.Object,
                _currentUserMock.Object);

            _currentUserMock.SetupGet(u => u.UserId).Returns(_actorUserId);
            _currentUserMock.SetupGet(u => u.Roles).Returns(_actorRoles);
        }

        #region Helpers

        /// <summary>
        /// Создаёт активную Standard-подписку через <see cref="UserSubscription.Activate"/>
        /// с фиксированными параметрами — позволяет сверить каждое поле DTO.
        /// </summary>
        private static UserSubscription CreateActiveSubscription(
            Guid userId,
            Guid planId,
            Guid priceId)
        {
            var result = UserSubscription.Activate(
                userId:                 userId,
                planId:                 planId,
                planKind:               PlanKind.Base,
                priceId:                priceId,
                priceKind:              OfferKind.Standard,
                amount:                 1499m,
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

        /// <summary>
        /// Создаёт Trial-подписку — <c>SnapshotAmount</c> = 0, <c>TrialEnd</c> != null.
        /// </summary>
        private static UserSubscription CreateTrialSubscription(Guid userId)
        {
            var result = UserSubscription.Activate(
                userId:                 userId,
                planId:                 Guid.NewGuid(),
                planKind:               PlanKind.Base,
                priceId:                Guid.NewGuid(),
                priceKind:              OfferKind.Trial,
                amount:                 0m,
                currency:               "RUB",
                durationDays:           null,
                trialDays:              14,
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

        /// <summary>
        /// Создаёт активную подписку и переводит её в <see cref="SubscriptionStatus.Canceled"/>
        /// для проверки маппинга Canceled-специфичных полей (<c>CanceledAt</c>, <c>AutoRenew=false</c>,
        /// <c>RecurringDisabledReason=UserCanceled</c>).
        /// </summary>
        private static UserSubscription CreateCanceledSubscription(Guid userId)
        {
            var activatedAt = _now.AddDays(-2);
            var canceledAt = _now.AddDays(-1);

            var result = UserSubscription.Activate(
                userId:                 userId,
                planId:                 Guid.NewGuid(),
                planKind:               PlanKind.Base,
                priceId:                Guid.NewGuid(),
                priceKind:              OfferKind.Standard,
                amount:                 990m,
                currency:               "RUB",
                durationDays:           30,
                trialDays:              null,
                gatewayPaymentMethodId: "pm_test",
                gatewayTransactionId:   "tx_test",
                gatewayPayload:         null,
                termsSnapshot:          "{}",
                documentNumber:         null,
                contentHash:            null,
                acceptedAt:             activatedAt,
                utcNow:                 activatedAt);
            var subscription = result.Value;
            subscription.Cancel(canceledAt);
            return subscription;
        }

        #endregion

        #region Constructor

        [Fact]
        public void Constructor_WithNullAccessPolicy_ShouldThrowArgumentNullException()
        {
            Action action = () => new GetSubscriptionByIdQueryHandler(
                null!, _userSubRepoMock.Object, _currentUserMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("accessPolicy");
        }

        [Fact]
        public void Constructor_WithNullUserSubscriptionRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new GetSubscriptionByIdQueryHandler(
                _accessPolicyMock.Object, null!, _currentUserMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("userSubscriptionRepository");
        }

        [Fact]
        public void Constructor_WithNullCurrentUser_ShouldThrowArgumentNullException()
        {
            Action action = () => new GetSubscriptionByIdQueryHandler(
                _accessPolicyMock.Object, _userSubRepoMock.Object, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("currentUser");
        }

        #endregion

        #region Success — маппинг DTO

        [Fact]
        public async Task Handle_HappyPathActive_MapsAllFieldsCorrectlyAsync()
        {
            // Arrange
            var planId = Guid.NewGuid();
            var priceId = Guid.NewGuid();
            UserSubscription subscription = CreateActiveSubscription(_actorUserId, planId, priceId);
            _accessPolicyMock
                .Setup(p => p.AuthorizeOperationAsync(
                    _subscriptionId, _actorUserId, _actorRoles, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());
            _userSubRepoMock
                .Setup(r => r.GetByIdAsync(_subscriptionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscription);

            // Act
            Result<SubscriptionResponse> result = await _handler.Handle(
                new GetSubscriptionByIdQuery(_subscriptionId), CancellationToken.None);

            // Assert — все 19 полей DTO
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                SubscriptionResponse dto = result.Value;

                dto.Id.Should().Be(subscription.Id);
                dto.UserId.Should().Be(_actorUserId);
                dto.PlanId.Should().Be(planId);
                dto.CurrentPriceId.Should().Be(priceId);
                dto.Status.Should().Be(SubscriptionStatus.Active);
                dto.SnapshotAmount.Should().Be(1499m);
                dto.SnapshotCurrency.Should().Be("RUB");
                dto.StartsAt.Should().Be(_now);
                dto.CurrentPeriodStart.Should().Be(_now);
                dto.CurrentPeriodEnd.Should().Be(_now.AddDays(30));
                dto.TrialEnd.Should().BeNull();
                dto.NextBillingAt.Should().Be(_now.AddDays(30));
                dto.AutoRenew.Should().BeTrue();
                dto.CancelAtPeriodEnd.Should().BeFalse();
                dto.RecurringDisabledReason.Should().BeNull();
                dto.CanceledAt.Should().BeNull();
                dto.EndedAt.Should().BeNull();
                dto.CreatedAt.Should().Be(_now);
                dto.UpdatedAt.Should().Be(_now);
            }
        }

        [Fact]
        public async Task Handle_HappyPathTrial_MapsTrialSpecificFieldsAsync()
        {
            // Arrange — Trial: SnapshotAmount = 0, TrialEnd не null.
            UserSubscription subscription = CreateTrialSubscription(_actorUserId);
            _accessPolicyMock
                .Setup(p => p.AuthorizeOperationAsync(
                    _subscriptionId, _actorUserId, _actorRoles, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());
            _userSubRepoMock
                .Setup(r => r.GetByIdAsync(_subscriptionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscription);

            // Act
            Result<SubscriptionResponse> result = await _handler.Handle(
                new GetSubscriptionByIdQuery(_subscriptionId), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                SubscriptionResponse dto = result.Value;

                dto.Status.Should().Be(SubscriptionStatus.Trialing);
                dto.SnapshotAmount.Should().Be(0m);
                dto.TrialEnd.Should().Be(_now.AddDays(14));
                dto.CurrentPeriodEnd.Should().Be(_now.AddDays(14));
                dto.NextBillingAt.Should().Be(_now.AddDays(14));
            }
        }

        [Fact]
        public async Task Handle_HappyPathCanceled_MapsCanceledSpecificFieldsAsync()
        {
            // Arrange — Canceled-подписка: AutoRenew=false, CancelAtPeriodEnd=true,
            // RecurringDisabledReason=UserCanceled, NextBillingAt=null, CanceledAt != null.
            UserSubscription subscription = CreateCanceledSubscription(_actorUserId);
            _accessPolicyMock
                .Setup(p => p.AuthorizeOperationAsync(
                    _subscriptionId, _actorUserId, _actorRoles, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());
            _userSubRepoMock
                .Setup(r => r.GetByIdAsync(_subscriptionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscription);

            // Act
            Result<SubscriptionResponse> result = await _handler.Handle(
                new GetSubscriptionByIdQuery(_subscriptionId), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                SubscriptionResponse dto = result.Value;

                dto.Status.Should().Be(SubscriptionStatus.Canceled);
                dto.AutoRenew.Should().BeFalse();
                dto.CancelAtPeriodEnd.Should().BeTrue();
                dto.RecurringDisabledReason.Should().Be(RecurringDisabledReason.UserCanceled);
                dto.NextBillingAt.Should().BeNull();
                dto.CanceledAt.Should().Be(_now.AddDays(-1));
                dto.EndedAt.Should().BeNull();
            }
        }

        #endregion

        #region Failure

        [Fact]
        public async Task Handle_WhenPolicyReturnsNotFound_PropagatesErrorAsync()
        {
            // Arrange
            _accessPolicyMock
                .Setup(p => p.AuthorizeOperationAsync(
                    _subscriptionId, _actorUserId, _actorRoles, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure(SubscriptionsErrors.NotFound));

            // Act
            Result<SubscriptionResponse> result = await _handler.Handle(
                new GetSubscriptionByIdQuery(_subscriptionId), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(SubscriptionsErrors.NotFound);

                _userSubRepoMock.Verify(
                    r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenPolicyReturnsForbidden_PropagatesErrorAsync()
        {
            // Arrange
            _accessPolicyMock
                .Setup(p => p.AuthorizeOperationAsync(
                    _subscriptionId, _actorUserId, _actorRoles, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure(SubscriptionsErrors.ForbiddenNotOwner));

            // Act
            Result<SubscriptionResponse> result = await _handler.Handle(
                new GetSubscriptionByIdQuery(_subscriptionId), CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(SubscriptionsErrors.ForbiddenNotOwner);
        }

        [Fact]
        public async Task Handle_WhenPolicySucceedsButRepoReturnsNull_ReturnsNotFoundAsync()
        {
            // Arrange — race-window: Policy успешно проверила, но между вызовами
            // подписка удалена. Defensive null-check в handler должен вернуть NotFound.
            _accessPolicyMock
                .Setup(p => p.AuthorizeOperationAsync(
                    _subscriptionId, _actorUserId, _actorRoles, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());
            _userSubRepoMock
                .Setup(r => r.GetByIdAsync(_subscriptionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserSubscription?)null);

            // Act
            Result<SubscriptionResponse> result = await _handler.Handle(
                new GetSubscriptionByIdQuery(_subscriptionId), CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(SubscriptionsErrors.NotFound);
        }

        #endregion
    }
}
