using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Domain.Primitives;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Application.Authorization;
using GastronomePlatform.Modules.Subscriptions.Application.Commands.Subscribe;
using GastronomePlatform.Modules.Subscriptions.Application.Payments;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using GastronomePlatform.Modules.Subscriptions.Domain.Errors;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;
using Moq;

namespace GastronomePlatform.Subscriptions.UnitTests.Application.Commands
{
    /// <summary>
    /// Тесты для <see cref="SubscribeCommandHandler"/> (UC-SUB-020).
    /// </summary>
    /// <remarks>
    /// Порядок вызовов <c>SaveChangesAsync</c> → <c>DispatchAsync</c> проверяется в
    /// <c>Handle_HappyPathTrial</c> через <c>Callback</c>-хук Moq с общим списком.
    /// Альтернатива <c>MockSequence</c> хуже читается на async-цепочках,
    /// а функционально эквивалентна.
    /// </remarks>
    public sealed class SubscribeCommandHandlerTests
    {
        private readonly Mock<IPlanPriceRepository> _priceRepoMock = new();
        private readonly Mock<ISubscriptionPlanRepository> _planRepoMock = new();
        private readonly Mock<IUserSubscriptionRepository> _userSubRepoMock = new();
        private readonly Mock<IRoleEligibilityService> _roleEligibilityMock = new();
        private readonly Mock<IPaymentGateway> _paymentGatewayMock = new();
        private readonly Mock<ICurrentUserService> _currentUserMock = new();
        private readonly Mock<IDateTimeProvider> _clockMock = new();
        private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
        private readonly SubscribeCommandHandler _handler;

        private static readonly DateTimeOffset _now =
            new(2026, 7, 8, 12, 0, 0, TimeSpan.Zero);
        private static readonly Guid _userId = Guid.NewGuid();

        public SubscribeCommandHandlerTests()
        {
            _handler = new SubscribeCommandHandler(
                _priceRepoMock.Object,
                _planRepoMock.Object,
                _userSubRepoMock.Object,
                _roleEligibilityMock.Object,
                _paymentGatewayMock.Object,
                _currentUserMock.Object,
                _clockMock.Object,
                _dispatcherMock.Object);

            _clockMock.SetupGet(c => c.UtcNow).Returns(_now);
            _currentUserMock.SetupGet(u => u.UserId).Returns(_userId);
        }

        #region Helpers

        private static SubscriptionPlan CreatePlan(
            PlanKind kind = PlanKind.Base,
            string? requiredRole = null)
        {
            var result = SubscriptionPlan.Create(
                planKind:       kind,
                publicName:     "Премиум",
                technicalName:  null,
                description:    null,
                requiredRole:   requiredRole,
                availableFrom:  null,
                availableUntil: null,
                internalNotes:  null,
                utcNow:         _now);
            return result.Value;
        }

        private static PlanPrice CreateStandardPrice(
            Guid planId,
            bool isPurchasable = true,
            bool isActive = true,
            DateTimeOffset? availableFrom = null,
            DateTimeOffset? availableUntil = null,
            int? durationDays = 30)
        {
            var result = PlanPrice.Create(
                planId:           planId,
                kind:             OfferKind.Standard,
                publicName:       "Месяц",
                durationDays:     durationDays,
                currency:         "RUB",
                amount:           1000m,
                compareAtAmount:  null,
                discountPercent:  null,
                trialDays:        null,
                isRecurring:      true,
                isPurchasable:    isPurchasable,
                renewsAsPriceId:  null,
                fallbackPriceId:  null,
                availableFrom:    availableFrom,
                availableUntil:   availableUntil,
                internalNotes:    null,
                utcNow:           _now);

            var price = result.Value;
            if (!isActive)
            {
                price.Deactivate(_now);
            }
            return price;
        }

        private static PlanPrice CreateTrialPrice(Guid planId, int trialDays = 14)
        {
            var result = PlanPrice.Create(
                planId:           planId,
                kind:             OfferKind.Trial,
                publicName:       "Триал",
                durationDays:     null,
                currency:         "RUB",
                amount:           0m,
                compareAtAmount:  null,
                discountPercent:  null,
                trialDays:        trialDays,
                isRecurring:      false,
                isPurchasable:    true,
                renewsAsPriceId:  null,
                fallbackPriceId:  null,
                availableFrom:    null,
                availableUntil:   null,
                internalNotes:    null,
                utcNow:           _now);
            return result.Value;
        }

        private static SubscribeCommand CreateCommand(Guid priceId) =>
            new(
                PriceId:         priceId,
                PaymentMethodId: "pm_test",
                AcceptedTermsAt: _now);

        private void SetupHappyPathTrial(SubscriptionPlan plan, PlanPrice price)
        {
            _priceRepoMock
                .Setup(r => r.GetByIdAsync(price.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(price);
            _planRepoMock
                .Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(plan);
            _userSubRepoMock
                .Setup(r => r.HasActiveBaseAsync(_userId, _now, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _paymentGatewayMock
                .Setup(g => g.AuthorizeVerificationChargeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymentGatewayResult("mock_tx_1", "{}"));
            _userSubRepoMock
                .Setup(r => r.AddAsync(It.IsAny<UserSubscription>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _userSubRepoMock
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _dispatcherMock
                .Setup(d => d.DispatchAsync(It.IsAny<AggregateRoot<Guid>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        private void SetupHappyPathStandard(SubscriptionPlan plan, PlanPrice price)
        {
            _priceRepoMock
                .Setup(r => r.GetByIdAsync(price.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(price);
            _planRepoMock
                .Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(plan);
            _userSubRepoMock
                .Setup(r => r.HasActiveBaseAsync(_userId, _now, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _paymentGatewayMock
                .Setup(g => g.AuthorizeInitialChargeAsync(
                    It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymentGatewayResult("mock_tx_1", "{}"));
            _userSubRepoMock
                .Setup(r => r.AddAsync(It.IsAny<UserSubscription>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _userSubRepoMock
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _dispatcherMock
                .Setup(d => d.DispatchAsync(It.IsAny<AggregateRoot<Guid>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        #endregion

        #region Constructor

        [Fact]
        public void Constructor_WithNullPriceRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new SubscribeCommandHandler(
                null!, _planRepoMock.Object, _userSubRepoMock.Object, _roleEligibilityMock.Object,
                _paymentGatewayMock.Object, _currentUserMock.Object, _clockMock.Object, _dispatcherMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("priceRepository");
        }

        [Fact]
        public void Constructor_WithNullPlanRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new SubscribeCommandHandler(
                _priceRepoMock.Object, null!, _userSubRepoMock.Object, _roleEligibilityMock.Object,
                _paymentGatewayMock.Object, _currentUserMock.Object, _clockMock.Object, _dispatcherMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("planRepository");
        }

        [Fact]
        public void Constructor_WithNullUserSubscriptionRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new SubscribeCommandHandler(
                _priceRepoMock.Object, _planRepoMock.Object, null!, _roleEligibilityMock.Object,
                _paymentGatewayMock.Object, _currentUserMock.Object, _clockMock.Object, _dispatcherMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("userSubscriptionRepository");
        }

        [Fact]
        public void Constructor_WithNullRoleEligibility_ShouldThrowArgumentNullException()
        {
            Action action = () => new SubscribeCommandHandler(
                _priceRepoMock.Object, _planRepoMock.Object, _userSubRepoMock.Object, null!,
                _paymentGatewayMock.Object, _currentUserMock.Object, _clockMock.Object, _dispatcherMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("roleEligibility");
        }

        [Fact]
        public void Constructor_WithNullPaymentGateway_ShouldThrowArgumentNullException()
        {
            Action action = () => new SubscribeCommandHandler(
                _priceRepoMock.Object, _planRepoMock.Object, _userSubRepoMock.Object, _roleEligibilityMock.Object,
                null!, _currentUserMock.Object, _clockMock.Object, _dispatcherMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("paymentGateway");
        }

        [Fact]
        public void Constructor_WithNullCurrentUser_ShouldThrowArgumentNullException()
        {
            Action action = () => new SubscribeCommandHandler(
                _priceRepoMock.Object, _planRepoMock.Object, _userSubRepoMock.Object, _roleEligibilityMock.Object,
                _paymentGatewayMock.Object, null!, _clockMock.Object, _dispatcherMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("currentUser");
        }

        [Fact]
        public void Constructor_WithNullClock_ShouldThrowArgumentNullException()
        {
            Action action = () => new SubscribeCommandHandler(
                _priceRepoMock.Object, _planRepoMock.Object, _userSubRepoMock.Object, _roleEligibilityMock.Object,
                _paymentGatewayMock.Object, _currentUserMock.Object, null!, _dispatcherMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("clock");
        }

        [Fact]
        public void Constructor_WithNullEventDispatcher_ShouldThrowArgumentNullException()
        {
            Action action = () => new SubscribeCommandHandler(
                _priceRepoMock.Object, _planRepoMock.Object, _userSubRepoMock.Object, _roleEligibilityMock.Object,
                _paymentGatewayMock.Object, _currentUserMock.Object, _clockMock.Object, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("eventDispatcher");
        }

        #endregion

        #region Success

        [Fact]
        public async Task Handle_HappyPathTrial_ActivatesTrialAndDispatchesEventAfterSaveAsync()
        {
            // Arrange
            SubscriptionPlan plan = CreatePlan(PlanKind.Base);
            PlanPrice price = CreateTrialPrice(plan.Id);
            SetupHappyPathTrial(plan, price);

            // Callback-список порядка вызовов: SaveChangesAsync должен идти строго до DispatchAsync.
            var callLog = new List<string>();
            _userSubRepoMock
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Callback(() => callLog.Add("save"))
                .Returns(Task.CompletedTask);
            _dispatcherMock
                .Setup(d => d.DispatchAsync(It.IsAny<AggregateRoot<Guid>>(), It.IsAny<CancellationToken>()))
                .Callback(() => callLog.Add("dispatch"))
                .Returns(Task.CompletedTask);

            // Act
            Result<SubscribeResult> result = await _handler.Handle(CreateCommand(price.Id), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                result.Value.SubscriptionId.Should().NotBeEmpty();

                callLog.Should().Equal("save", "dispatch");

                _paymentGatewayMock.Verify(
                    g => g.AuthorizeVerificationChargeAsync("pm_test", "RUB", It.IsAny<CancellationToken>()),
                    Times.Once);
                _paymentGatewayMock.Verify(
                    g => g.AuthorizeInitialChargeAsync(
                        It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Never);

                _userSubRepoMock.Verify(
                    r => r.AddAsync(It.Is<UserSubscription>(s =>
                        s.UserId == _userId &&
                        s.Status == SubscriptionStatus.Trialing &&
                        s.SnapshotAmount == 0m),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Fact]
        public async Task Handle_HappyPathStandard_ActivatesActiveAndChargesInitialAsync()
        {
            // Arrange
            SubscriptionPlan plan = CreatePlan(PlanKind.Base);
            PlanPrice price = CreateStandardPrice(plan.Id);
            SetupHappyPathStandard(plan, price);

            // Act
            Result<SubscribeResult> result = await _handler.Handle(CreateCommand(price.Id), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();

                _paymentGatewayMock.Verify(
                    g => g.AuthorizeInitialChargeAsync("pm_test", 1000m, "RUB", It.IsAny<CancellationToken>()),
                    Times.Once);
                _paymentGatewayMock.Verify(
                    g => g.AuthorizeVerificationChargeAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Never);

                _userSubRepoMock.Verify(
                    r => r.AddAsync(It.Is<UserSubscription>(s =>
                        s.Status == SubscriptionStatus.Active &&
                        s.SnapshotAmount == 1000m),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        #endregion

        #region Branching (skip checks)

        [Fact]
        public async Task Handle_WhenPlanHasNoRequiredRole_DoesNotCallEligibilityAsync()
        {
            // Arrange
            SubscriptionPlan plan = CreatePlan(PlanKind.Base, requiredRole: null);
            PlanPrice price = CreateStandardPrice(plan.Id);
            SetupHappyPathStandard(plan, price);

            // Act
            await _handler.Handle(CreateCommand(price.Id), CancellationToken.None);

            // Assert — гейт не задан, сервис eligibility НЕ должен вызываться.
            _roleEligibilityMock.Verify(
                s => s.IsEligibleForRoleAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WhenPlanIsAddOn_DoesNotCheckActiveBaseAsync()
        {
            // Arrange — инвариант «≤1 активной Base» не касается AddOn.
            SubscriptionPlan plan = CreatePlan(PlanKind.AddOn);
            PlanPrice price = CreateStandardPrice(plan.Id);
            SetupHappyPathStandard(plan, price);

            // Act
            await _handler.Handle(CreateCommand(price.Id), CancellationToken.None);

            // Assert
            _userSubRepoMock.Verify(
                r => r.HasActiveBaseAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WhenPlanHasRequiredRoleAndEligible_ProceedsToSuccessAsync()
        {
            // Arrange
            SubscriptionPlan plan = CreatePlan(PlanKind.Base, requiredRole: "Chef");
            PlanPrice price = CreateStandardPrice(plan.Id);
            SetupHappyPathStandard(plan, price);
            _roleEligibilityMock
                .Setup(s => s.IsEligibleForRoleAsync(_userId, "Chef", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            Result<SubscribeResult> result = await _handler.Handle(CreateCommand(price.Id), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                _roleEligibilityMock.Verify(
                    s => s.IsEligibleForRoleAsync(_userId, "Chef", It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        #endregion

        #region Failure

        [Fact]
        public async Task Handle_WhenPriceNotFound_ReturnsPriceNotFoundAsync()
        {
            // Arrange
            var priceId = Guid.NewGuid();
            _priceRepoMock
                .Setup(r => r.GetByIdAsync(priceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((PlanPrice?)null);

            // Act
            Result<SubscribeResult> result = await _handler.Handle(CreateCommand(priceId), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(SubscriptionsErrors.PriceNotFound);

                _userSubRepoMock.Verify(
                    r => r.AddAsync(It.IsAny<UserSubscription>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenPriceIsPurchasableFalse_ReturnsOfferNotPurchasableAsync()
        {
            // Arrange
            SubscriptionPlan plan = CreatePlan();
            PlanPrice price = CreateStandardPrice(plan.Id, isPurchasable: false);
            _priceRepoMock
                .Setup(r => r.GetByIdAsync(price.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(price);

            // Act
            Result<SubscribeResult> result = await _handler.Handle(CreateCommand(price.Id), CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(SubscriptionsErrors.OfferNotPurchasable);
        }

        [Fact]
        public async Task Handle_WhenPriceIsActiveFalse_ReturnsOfferNotPurchasableAsync()
        {
            // Arrange
            SubscriptionPlan plan = CreatePlan();
            PlanPrice price = CreateStandardPrice(plan.Id, isActive: false);
            _priceRepoMock
                .Setup(r => r.GetByIdAsync(price.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(price);

            // Act
            Result<SubscribeResult> result = await _handler.Handle(CreateCommand(price.Id), CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(SubscriptionsErrors.OfferNotPurchasable);
        }

        [Fact]
        public async Task Handle_WhenAvailableFromInFuture_ReturnsOfferNotPurchasableAsync()
        {
            // Arrange
            SubscriptionPlan plan = CreatePlan();
            PlanPrice price = CreateStandardPrice(plan.Id, availableFrom: _now.AddDays(7));
            _priceRepoMock
                .Setup(r => r.GetByIdAsync(price.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(price);

            // Act
            Result<SubscribeResult> result = await _handler.Handle(CreateCommand(price.Id), CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(SubscriptionsErrors.OfferNotPurchasable);
        }

        [Fact]
        public async Task Handle_WhenAvailableUntilInPast_ReturnsOfferNotPurchasableAsync()
        {
            // Arrange — AvailableUntil = utcNow-1s (уже прошло, guard <=).
            SubscriptionPlan plan = CreatePlan();
            PlanPrice price = CreateStandardPrice(
                plan.Id,
                availableFrom: _now.AddDays(-30),
                availableUntil: _now.AddSeconds(-1));
            _priceRepoMock
                .Setup(r => r.GetByIdAsync(price.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(price);

            // Act
            Result<SubscribeResult> result = await _handler.Handle(CreateCommand(price.Id), CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(SubscriptionsErrors.OfferNotPurchasable);
        }

        [Fact]
        public async Task Handle_WhenPlanNotFound_ReturnsPlanNotFoundAsync()
        {
            // Arrange
            SubscriptionPlan plan = CreatePlan();
            PlanPrice price = CreateStandardPrice(plan.Id);
            _priceRepoMock
                .Setup(r => r.GetByIdAsync(price.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(price);
            _planRepoMock
                .Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SubscriptionPlan?)null);

            // Act
            Result<SubscribeResult> result = await _handler.Handle(CreateCommand(price.Id), CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(SubscriptionsErrors.PlanNotFound);
        }

        [Fact]
        public async Task Handle_WhenRoleGateNotEligible_ReturnsForbiddenRoleRequiredAsync()
        {
            // Arrange
            SubscriptionPlan plan = CreatePlan(PlanKind.Base, requiredRole: "Chef");
            PlanPrice price = CreateStandardPrice(plan.Id);
            _priceRepoMock
                .Setup(r => r.GetByIdAsync(price.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(price);
            _planRepoMock
                .Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(plan);
            _roleEligibilityMock
                .Setup(s => s.IsEligibleForRoleAsync(_userId, "Chef", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            Result<SubscribeResult> result = await _handler.Handle(CreateCommand(price.Id), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(SubscriptionsErrors.ForbiddenRoleRequired);

                _userSubRepoMock.Verify(
                    r => r.AddAsync(It.IsAny<UserSubscription>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenHasActiveBase_ReturnsAlreadyHasBaseAsync()
        {
            // Arrange
            SubscriptionPlan plan = CreatePlan(PlanKind.Base);
            PlanPrice price = CreateStandardPrice(plan.Id);
            _priceRepoMock
                .Setup(r => r.GetByIdAsync(price.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(price);
            _planRepoMock
                .Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(plan);
            _userSubRepoMock
                .Setup(r => r.HasActiveBaseAsync(_userId, _now, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            Result<SubscribeResult> result = await _handler.Handle(CreateCommand(price.Id), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(SubscriptionsErrors.AlreadyHasBase);

                _paymentGatewayMock.Verify(
                    g => g.AuthorizeInitialChargeAsync(
                        It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Never);
                _userSubRepoMock.Verify(
                    r => r.AddAsync(It.IsAny<UserSubscription>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenPaymentGatewayFails_ReturnsGatewayErrorAndDoesNotAddAsync()
        {
            // Arrange
            var gatewayError = Error.Validation("PAY.DECLINED", "Списание отклонено.");

            SubscriptionPlan plan = CreatePlan(PlanKind.Base);
            PlanPrice price = CreateStandardPrice(plan.Id);
            _priceRepoMock
                .Setup(r => r.GetByIdAsync(price.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(price);
            _planRepoMock
                .Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(plan);
            _userSubRepoMock
                .Setup(r => r.HasActiveBaseAsync(_userId, _now, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _paymentGatewayMock
                .Setup(g => g.AuthorizeInitialChargeAsync(
                    It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<PaymentGatewayResult>.Failure(gatewayError));

            // Act
            Result<SubscribeResult> result = await _handler.Handle(CreateCommand(price.Id), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(gatewayError);

                _userSubRepoMock.Verify(
                    r => r.AddAsync(It.IsAny<UserSubscription>(), It.IsAny<CancellationToken>()),
                    Times.Never);
                _dispatcherMock.Verify(
                    d => d.DispatchAsync(It.IsAny<AggregateRoot<Guid>>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenActivateFailsForPaidWithoutDurationDays_ReturnsDomainErrorAsync()
        {
            // Arrange — доменный инвариант UserSubscription.Activate:
            // paid оффер обязан иметь DurationDays.
            SubscriptionPlan plan = CreatePlan(PlanKind.Base);
            PlanPrice price = CreateStandardPrice(plan.Id, durationDays: null);
            SetupHappyPathStandard(plan, price);

            // Act
            Result<SubscribeResult> result = await _handler.Handle(CreateCommand(price.Id), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(SubscriptionsErrors.ActivatePaidRequiresDurationDays);

                _userSubRepoMock.Verify(
                    r => r.AddAsync(It.IsAny<UserSubscription>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        #endregion
    }
}
