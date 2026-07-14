using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Application.Authorization;
using GastronomePlatform.Modules.Subscriptions.Application.Commands.Cancel;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using GastronomePlatform.Modules.Subscriptions.Domain.Errors;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;
using Moq;

namespace GastronomePlatform.Subscriptions.UnitTests.Application.Commands
{
    /// <summary>
    /// Тесты для <see cref="CancelSubscriptionCommandHandler"/> (UC-SUB-022).
    /// </summary>
    /// <remarks>
    /// <c>ISubscriptionAccessPolicy</c> мокается как чёрный ящик — Owner/Admin-логика
    /// покрывается в <c>SubscriptionAccessPolicyTests</c>. Двойная загрузка
    /// (Policy внутри дёргает <c>GetByIdAsync</c>, handler потом ещё раз) в тестах
    /// проявляется как **один** вызов <c>Repo.GetByIdAsync</c> — вызов внутри mock-Policy
    /// не идёт в реальный репозиторий.
    /// </remarks>
    public sealed class CancelSubscriptionCommandHandlerTests
    {
        private readonly Mock<ISubscriptionAccessPolicy> _accessPolicyMock = new();
        private readonly Mock<IUserSubscriptionRepository> _userSubRepoMock = new();
        private readonly Mock<ICurrentUserService> _currentUserMock = new();
        private readonly Mock<IDateTimeProvider> _clockMock = new();
        private readonly CancelSubscriptionCommandHandler _handler;

        private static readonly DateTimeOffset _now =
            new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);
        private static readonly Guid _actorUserId = Guid.NewGuid();
        private static readonly Guid _subscriptionId = Guid.NewGuid();
        private static readonly IReadOnlyCollection<string> _actorRoles = new[] { PlatformRoles.USER };

        public CancelSubscriptionCommandHandlerTests()
        {
            _handler = new CancelSubscriptionCommandHandler(
                _accessPolicyMock.Object,
                _userSubRepoMock.Object,
                _currentUserMock.Object,
                _clockMock.Object);

            _clockMock.SetupGet(c => c.UtcNow).Returns(_now);
            _currentUserMock.SetupGet(u => u.UserId).Returns(_actorUserId);
            _currentUserMock.SetupGet(u => u.Roles).Returns(_actorRoles);
        }

        #region Helpers

        /// <summary>
        /// Создаёт активную Standard-подписку через <see cref="UserSubscription.Activate"/> —
        /// единственный способ собрать агрегат с корректным состоянием.
        /// Момент активации = <c>_now.AddDays(-1)</c>, чтобы <c>_now</c> оставался внутри периода.
        /// </summary>
        private static UserSubscription CreateActiveSubscription()
        {
            var activatedAt = _now.AddDays(-1);
            var result = UserSubscription.Activate(
                userId:                 _actorUserId,
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
                acceptedAt:             activatedAt,
                utcNow:                 activatedAt);
            return result.Value;
        }

        /// <summary>
        /// Создаёт активную подписку и сразу переводит её в <see cref="SubscriptionStatus.Canceled"/> —
        /// для проверки, что повторный Cancel возвращает <c>SUBS.CANNOT_CANCEL_IN_STATUS</c>.
        /// </summary>
        private static UserSubscription CreateCanceledSubscription()
        {
            var subscription = CreateActiveSubscription();
            subscription.Cancel(_now.AddHours(-1));
            return subscription;
        }

        #endregion

        #region Constructor

        [Fact]
        public void Constructor_WithNullAccessPolicy_ShouldThrowArgumentNullException()
        {
            Action action = () => new CancelSubscriptionCommandHandler(
                null!, _userSubRepoMock.Object, _currentUserMock.Object, _clockMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("accessPolicy");
        }

        [Fact]
        public void Constructor_WithNullUserSubscriptionRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new CancelSubscriptionCommandHandler(
                _accessPolicyMock.Object, null!, _currentUserMock.Object, _clockMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("userSubscriptionRepository");
        }

        [Fact]
        public void Constructor_WithNullCurrentUser_ShouldThrowArgumentNullException()
        {
            Action action = () => new CancelSubscriptionCommandHandler(
                _accessPolicyMock.Object, _userSubRepoMock.Object, null!, _clockMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("currentUser");
        }

        [Fact]
        public void Constructor_WithNullClock_ShouldThrowArgumentNullException()
        {
            Action action = () => new CancelSubscriptionCommandHandler(
                _accessPolicyMock.Object, _userSubRepoMock.Object, _currentUserMock.Object, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("clock");
        }

        #endregion

        #region Success

        [Fact]
        public async Task Handle_HappyPath_CancelsSubscriptionAndSavesAsync()
        {
            // Arrange
            UserSubscription subscription = CreateActiveSubscription();
            _accessPolicyMock
                .Setup(p => p.AuthorizeOperationAsync(
                    _subscriptionId, _actorUserId, _actorRoles, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());
            _userSubRepoMock
                .Setup(r => r.GetByIdAsync(_subscriptionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscription);
            _userSubRepoMock
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            Result result = await _handler.Handle(
                new CancelSubscriptionCommand(_subscriptionId), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();

                subscription.Status.Should().Be(SubscriptionStatus.Canceled);
                subscription.AutoRenew.Should().BeFalse();
                subscription.CancelAtPeriodEnd.Should().BeTrue();
                subscription.RecurringDisabledReason.Should().Be(RecurringDisabledReason.UserCanceled);
                subscription.NextBillingAt.Should().BeNull();
                subscription.CanceledAt.Should().Be(_now);
                subscription.UpdatedAt.Should().Be(_now);

                _userSubRepoMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        #endregion

        #region Failure

        [Fact]
        public async Task Handle_WhenPolicyReturnsNotFound_PropagatesErrorAndDoesNotSaveAsync()
        {
            // Arrange
            _accessPolicyMock
                .Setup(p => p.AuthorizeOperationAsync(
                    _subscriptionId, _actorUserId, _actorRoles, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure(SubscriptionsErrors.NotFound));

            // Act
            Result result = await _handler.Handle(
                new CancelSubscriptionCommand(_subscriptionId), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(SubscriptionsErrors.NotFound);

                _userSubRepoMock.Verify(
                    r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Never);
                _userSubRepoMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenPolicyReturnsForbidden_PropagatesErrorAndDoesNotSaveAsync()
        {
            // Arrange
            _accessPolicyMock
                .Setup(p => p.AuthorizeOperationAsync(
                    _subscriptionId, _actorUserId, _actorRoles, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure(SubscriptionsErrors.ForbiddenNotOwner));

            // Act
            Result result = await _handler.Handle(
                new CancelSubscriptionCommand(_subscriptionId), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(SubscriptionsErrors.ForbiddenNotOwner);

                _userSubRepoMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenPolicySucceedsButRepoReturnsNull_ReturnsNotFoundAsync()
        {
            // Arrange — race-window: между Policy.GetByIdAsync и Handler.GetByIdAsync
            // подписка удалена. Defensive null-check в handler должен вернуть NotFound.
            _accessPolicyMock
                .Setup(p => p.AuthorizeOperationAsync(
                    _subscriptionId, _actorUserId, _actorRoles, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());
            _userSubRepoMock
                .Setup(r => r.GetByIdAsync(_subscriptionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserSubscription?)null);

            // Act
            Result result = await _handler.Handle(
                new CancelSubscriptionCommand(_subscriptionId), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(SubscriptionsErrors.NotFound);

                _userSubRepoMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenDomainCancelReturnsInvalidStatus_PropagatesErrorAsync()
        {
            // Arrange — подписка уже в Canceled; повторный Cancel в Domain вернёт
            // SUBS.CANNOT_CANCEL_IN_STATUS. Handler должен пробросить как есть.
            UserSubscription canceled = CreateCanceledSubscription();
            _accessPolicyMock
                .Setup(p => p.AuthorizeOperationAsync(
                    _subscriptionId, _actorUserId, _actorRoles, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());
            _userSubRepoMock
                .Setup(r => r.GetByIdAsync(_subscriptionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(canceled);

            // Act
            Result result = await _handler.Handle(
                new CancelSubscriptionCommand(_subscriptionId), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(SubscriptionsErrors.CannotCancelInStatus);

                _userSubRepoMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        #endregion
    }
}
