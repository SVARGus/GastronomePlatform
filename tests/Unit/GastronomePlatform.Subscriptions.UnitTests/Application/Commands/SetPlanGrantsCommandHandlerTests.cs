using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Application.Commands.SetPlanGrants;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using GastronomePlatform.Modules.Subscriptions.Domain.Errors;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;
using Moq;

namespace GastronomePlatform.Subscriptions.UnitTests.Application.Commands
{
    /// <summary>
    /// Тесты для <see cref="SetPlanGrantsCommandHandler"/>.
    /// </summary>
    public sealed class SetPlanGrantsCommandHandlerTests
    {
        private readonly Mock<ISubscriptionPlanRepository> _planRepositoryMock = new();
        private readonly Mock<IDateTimeProvider> _clockMock = new();
        private readonly SetPlanGrantsCommandHandler _handler;

        private static readonly DateTimeOffset _now =
            new(2026, 7, 8, 12, 0, 0, TimeSpan.Zero);
        private static readonly Guid _planId = Guid.NewGuid();

        public SetPlanGrantsCommandHandlerTests()
        {
            _handler = new SetPlanGrantsCommandHandler(
                _planRepositoryMock.Object,
                _clockMock.Object);

            _clockMock.SetupGet(c => c.UtcNow).Returns(_now);
        }

        private static SubscriptionPlan CreatePlan()
        {
            // Handler читает у плана Grants и вызывает SetGrants — конкретный Id не важен.
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

        private static SetPlanGrantsCommand CreateCommand(IReadOnlyList<PlanGrantSpec> grants) =>
            new(PlanId: _planId, Grants: grants);

        #region Constructor

        [Fact]
        public void Constructor_WithNullPlanRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new SetPlanGrantsCommandHandler(null!, _clockMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("planRepository");
        }

        [Fact]
        public void Constructor_WithNullClock_ShouldThrowArgumentNullException()
        {
            Action action = () => new SetPlanGrantsCommandHandler(_planRepositoryMock.Object, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("clock");
        }

        #endregion

        #region Success

        [Fact]
        public async Task Handle_WithTwoBooleanGrants_SetsGrantsAndSavesAsync()
        {
            // Arrange
            SubscriptionPlan plan = CreatePlan();
            _planRepositoryMock
                .Setup(r => r.GetByIdWithGrantsAsync(_planId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(plan);

            SetPlanGrantsCommand command = CreateCommand(new List<PlanGrantSpec>
            {
                new(FeatureGrant.FullRecipes, null),
                new(FeatureGrant.PortionCalculator, null),
            });

            // Act
            Result result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();

                plan.Grants.Should().HaveCount(2);
                plan.Grants.Should().Contain(g => g.Grant == FeatureGrant.FullRecipes && g.Quantity == null);
                plan.Grants.Should().Contain(g => g.Grant == FeatureGrant.PortionCalculator && g.Quantity == null);
                plan.UpdatedAt.Should().Be(_now);

                _planRepositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Fact]
        public async Task Handle_WithPromotionAdvancedAndQuantity_SetsGrantWithQuotaAsync()
        {
            // Arrange
            SubscriptionPlan plan = CreatePlan();
            _planRepositoryMock
                .Setup(r => r.GetByIdWithGrantsAsync(_planId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(plan);

            SetPlanGrantsCommand command = CreateCommand(new List<PlanGrantSpec>
            {
                new(FeatureGrant.PromotionAdvanced, 10),
            });

            // Act
            Result result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                plan.Grants.Should().ContainSingle();
                plan.Grants[0].Grant.Should().Be(FeatureGrant.PromotionAdvanced);
                plan.Grants[0].Quantity.Should().Be(10);
            }
        }

        [Fact]
        public async Task Handle_WithEmptyGrantsList_ClearsGrantsAndSavesAsync()
        {
            // Arrange — пустой список = снять все гранты (replace-семантика).
            SubscriptionPlan plan = CreatePlan();
            plan.SetGrants(
                new Dictionary<FeatureGrant, int?> { { FeatureGrant.FullRecipes, null } },
                _now);
            plan.Grants.Should().ContainSingle(); // pre-condition

            _planRepositoryMock
                .Setup(r => r.GetByIdWithGrantsAsync(_planId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(plan);

            SetPlanGrantsCommand command = CreateCommand(new List<PlanGrantSpec>());

            // Act
            Result result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                plan.Grants.Should().BeEmpty();

                _planRepositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        #endregion

        #region Failure

        [Fact]
        public async Task Handle_WhenNonQuotaGrantHasQuantity_ReturnsQuotaNotApplicableBeforeLoadingPlanAsync()
        {
            // Arrange — quota-check выполняется ДО загрузки плана (см. handler).
            SetPlanGrantsCommand command = CreateCommand(new List<PlanGrantSpec>
            {
                new(FeatureGrant.FullRecipes, 5),
            });

            // Act
            Result result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(SubscriptionsErrors.PlanGrantQuotaNotApplicable);

                _planRepositoryMock.Verify(
                    r => r.GetByIdWithGrantsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Never);
                _planRepositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenPlanNotFound_ReturnsPlanNotFoundAndDoesNotSaveAsync()
        {
            // Arrange
            _planRepositoryMock
                .Setup(r => r.GetByIdWithGrantsAsync(_planId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SubscriptionPlan?)null);

            SetPlanGrantsCommand command = CreateCommand(new List<PlanGrantSpec>
            {
                new(FeatureGrant.FullRecipes, null),
            });

            // Act
            Result result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(SubscriptionsErrors.PlanNotFound);

                _planRepositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        #endregion
    }
}
