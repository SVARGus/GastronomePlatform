using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Application.Commands.AddPlanPrice;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using GastronomePlatform.Modules.Subscriptions.Domain.Errors;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;
using Moq;

namespace GastronomePlatform.Subscriptions.UnitTests.Application.Commands
{
    /// <summary>
    /// Тесты для <see cref="AddPlanPriceCommandHandler"/>.
    /// </summary>
    public sealed class AddPlanPriceCommandHandlerTests
    {
        private readonly Mock<ISubscriptionPlanRepository> _planRepositoryMock = new();
        private readonly Mock<IPlanPriceRepository> _priceRepositoryMock = new();
        private readonly Mock<IDateTimeProvider> _clockMock = new();
        private readonly AddPlanPriceCommandHandler _handler;

        private static readonly DateTimeOffset _now =
            new(2026, 7, 8, 12, 0, 0, TimeSpan.Zero);
        private static readonly Guid _planId = Guid.NewGuid();
        private static readonly Guid _otherPlanId = Guid.NewGuid();

        public AddPlanPriceCommandHandlerTests()
        {
            _handler = new AddPlanPriceCommandHandler(
                _planRepositoryMock.Object,
                _priceRepositoryMock.Object,
                _clockMock.Object);

            _clockMock.SetupGet(c => c.UtcNow).Returns(_now);
        }

        private static SubscriptionPlan CreatePlan()
        {
            // Handler читает у плана только «is null»; конкретный Id не важен
            // (proof-of-existence-паттерн). Оставляем сгенерированный фабрикой Guid.
            var result = SubscriptionPlan.Create(
                planKind:       PlanKind.Base,
                publicName:     "Премиум",
                technicalName:  null,
                description:    null,
                requiredRole:   null,
                availableFrom:  null,
                availableUntil: null,
                internalNotes:  null,
                utcNow:         _now);

            return result.Value;
        }

        private static PlanPrice CreateStandardPrice(Guid planId, decimal amount = 1000m)
        {
            var result = PlanPrice.Create(
                planId:          planId,
                kind:            OfferKind.Standard,
                publicName:      "Год",
                durationDays:    365,
                currency:        "RUB",
                amount:          amount,
                compareAtAmount: null,
                discountPercent: null,
                trialDays:       null,
                isRecurring:     true,
                isPurchasable:   true,
                renewsAsPriceId: null,
                fallbackPriceId: null,
                availableFrom:   null,
                availableUntil:  null,
                internalNotes:   null,
                utcNow:          _now);

            return result.Value;
        }

        private static AddPlanPriceCommand CreateCommand(
            Guid? planId = null,
            OfferKind kind = OfferKind.Standard,
            decimal amount = 1000m,
            int? trialDays = null,
            bool isRecurring = true,
            Guid? renewsAsPriceId = null,
            Guid? fallbackPriceId = null)
            => new(
                PlanId:           planId ?? _planId,
                Kind:             kind,
                PublicName:       "Год",
                DurationDays:     365,
                Currency:         "RUB",
                Amount:           amount,
                CompareAtAmount:  null,
                DiscountPercent:  null,
                TrialDays:        trialDays,
                IsRecurring:      isRecurring,
                IsPurchasable:    true,
                RenewsAsPriceId:  renewsAsPriceId,
                FallbackPriceId:  fallbackPriceId,
                AvailableFrom:    null,
                AvailableUntil:   null,
                InternalNotes:    null);

        #region Constructor

        [Fact]
        public void Constructor_WithNullPlanRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new AddPlanPriceCommandHandler(null!, _priceRepositoryMock.Object, _clockMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("planRepository");
        }

        [Fact]
        public void Constructor_WithNullPriceRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new AddPlanPriceCommandHandler(_planRepositoryMock.Object, null!, _clockMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("priceRepository");
        }

        [Fact]
        public void Constructor_WithNullClock_ShouldThrowArgumentNullException()
        {
            Action action = () => new AddPlanPriceCommandHandler(_planRepositoryMock.Object, _priceRepositoryMock.Object, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("clock");
        }

        #endregion

        #region Success

        [Fact]
        public async Task Handle_WithoutTransitions_CreatesPriceAndSavesAsync()
        {
            // Arrange
            _planRepositoryMock
                .Setup(r => r.GetByIdAsync(_planId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreatePlan());

            // Act
            Result<AddPlanPriceResult> result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                result.Value.PriceId.Should().NotBeEmpty();

                _priceRepositoryMock.Verify(
                    r => r.AddAsync(It.Is<PlanPrice>(p =>
                        p.PlanId == _planId &&
                        p.Kind == OfferKind.Standard &&
                        p.Amount == 1000m &&
                        p.Currency == "RUB" &&
                        p.CreatedAt == _now),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
                _priceRepositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Fact]
        public async Task Handle_WhenRenewsAsPriceIsSamePlan_CreatesPriceAsync()
        {
            // Arrange
            Guid renewsAsPriceId = Guid.NewGuid();
            PlanPrice target = CreateStandardPrice(_planId);

            _planRepositoryMock
                .Setup(r => r.GetByIdAsync(_planId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreatePlan());
            _priceRepositoryMock
                .Setup(r => r.GetByIdAsync(renewsAsPriceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(target);

            // Act
            Result<AddPlanPriceResult> result = await _handler.Handle(
                CreateCommand(renewsAsPriceId: renewsAsPriceId),
                CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        #endregion

        #region Failure

        [Fact]
        public async Task Handle_WhenPlanNotFound_ReturnsPlanNotFoundAndDoesNotAddAsync()
        {
            // Arrange
            _planRepositoryMock
                .Setup(r => r.GetByIdAsync(_planId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SubscriptionPlan?)null);

            // Act
            Result<AddPlanPriceResult> result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(SubscriptionsErrors.PlanNotFound);

                _priceRepositoryMock.Verify(
                    r => r.AddAsync(It.IsAny<PlanPrice>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenRenewsAsPriceNotFound_ReturnsTransitionPriceNotFoundAsync()
        {
            // Arrange
            Guid renewsAsPriceId = Guid.NewGuid();
            _planRepositoryMock
                .Setup(r => r.GetByIdAsync(_planId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreatePlan());
            _priceRepositoryMock
                .Setup(r => r.GetByIdAsync(renewsAsPriceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((PlanPrice?)null);

            // Act
            Result<AddPlanPriceResult> result = await _handler.Handle(
                CreateCommand(renewsAsPriceId: renewsAsPriceId),
                CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(SubscriptionsErrors.TransitionPriceNotFound);

                _priceRepositoryMock.Verify(
                    r => r.AddAsync(It.IsAny<PlanPrice>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenRenewsAsPriceBelongsToOtherPlan_ReturnsTransitionPriceCrossPlanAsync()
        {
            // Arrange
            Guid renewsAsPriceId = Guid.NewGuid();
            PlanPrice targetOfOtherPlan = CreateStandardPrice(_otherPlanId);

            _planRepositoryMock
                .Setup(r => r.GetByIdAsync(_planId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreatePlan());
            _priceRepositoryMock
                .Setup(r => r.GetByIdAsync(renewsAsPriceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(targetOfOtherPlan);

            // Act
            Result<AddPlanPriceResult> result = await _handler.Handle(
                CreateCommand(renewsAsPriceId: renewsAsPriceId),
                CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(SubscriptionsErrors.TransitionPriceCrossPlan);

                _priceRepositoryMock.Verify(
                    r => r.AddAsync(It.IsAny<PlanPrice>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenFallbackPriceNotFound_ReturnsTransitionPriceNotFoundAsync()
        {
            // Arrange
            Guid fallbackPriceId = Guid.NewGuid();
            _planRepositoryMock
                .Setup(r => r.GetByIdAsync(_planId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreatePlan());
            _priceRepositoryMock
                .Setup(r => r.GetByIdAsync(fallbackPriceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((PlanPrice?)null);

            // Act
            Result<AddPlanPriceResult> result = await _handler.Handle(
                CreateCommand(fallbackPriceId: fallbackPriceId),
                CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(SubscriptionsErrors.TransitionPriceNotFound);
        }

        [Fact]
        public async Task Handle_WhenFallbackPriceBelongsToOtherPlan_ReturnsTransitionPriceCrossPlanAsync()
        {
            // Arrange
            Guid fallbackPriceId = Guid.NewGuid();
            _planRepositoryMock
                .Setup(r => r.GetByIdAsync(_planId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreatePlan());
            _priceRepositoryMock
                .Setup(r => r.GetByIdAsync(fallbackPriceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateStandardPrice(_otherPlanId));

            // Act
            Result<AddPlanPriceResult> result = await _handler.Handle(
                CreateCommand(fallbackPriceId: fallbackPriceId),
                CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(SubscriptionsErrors.TransitionPriceCrossPlan);
        }

        [Fact]
        public async Task Handle_WhenDomainInvariantFails_ReturnsDomainErrorAndDoesNotAddAsync()
        {
            // Arrange — доменный инвариант PlanPrice.Create: Amount ≥ 0.
            _planRepositoryMock
                .Setup(r => r.GetByIdAsync(_planId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreatePlan());

            // Act
            Result<AddPlanPriceResult> result = await _handler.Handle(
                CreateCommand(amount: -100m),
                CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(SubscriptionsErrors.PriceNegativeAmount);

                _priceRepositoryMock.Verify(
                    r => r.AddAsync(It.IsAny<PlanPrice>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        #endregion
    }
}
