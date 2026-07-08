using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Application.Commands.CreateSubscriptionPlan;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using GastronomePlatform.Modules.Subscriptions.Domain.Errors;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;
using Moq;

namespace GastronomePlatform.Subscriptions.UnitTests.Application.Commands
{
    /// <summary>
    /// Тесты для <see cref="CreateSubscriptionPlanCommandHandler"/>.
    /// </summary>
    public sealed class CreateSubscriptionPlanCommandHandlerTests
    {
        private readonly Mock<ISubscriptionPlanRepository> _planRepositoryMock = new();
        private readonly Mock<IDateTimeProvider> _clockMock = new();
        private readonly CreateSubscriptionPlanCommandHandler _handler;

        private static readonly DateTimeOffset _now =
            new(2026, 7, 8, 12, 0, 0, TimeSpan.Zero);

        public CreateSubscriptionPlanCommandHandlerTests()
        {
            _handler = new CreateSubscriptionPlanCommandHandler(
                _planRepositoryMock.Object,
                _clockMock.Object);

            _clockMock.SetupGet(c => c.UtcNow).Returns(_now);
        }

        private static CreateSubscriptionPlanCommand CreateCommand(
            PlanKind planKind = PlanKind.Base,
            string publicName = "Премиум",
            string? technicalName = "premium",
            string? requiredRole = null)
            => new(
                PlanKind: planKind,
                PublicName: publicName,
                TechnicalName: technicalName,
                Description: "Полный доступ",
                RequiredRole: requiredRole,
                AvailableFrom: null,
                AvailableUntil: null,
                InternalNotes: null);

        #region Constructor

        [Fact]
        public void Constructor_WithNullPlanRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new CreateSubscriptionPlanCommandHandler(null!, _clockMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("planRepository");
        }

        [Fact]
        public void Constructor_WithNullClock_ShouldThrowArgumentNullException()
        {
            Action action = () => new CreateSubscriptionPlanCommandHandler(_planRepositoryMock.Object, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("clock");
        }

        #endregion

        #region Success

        [Fact]
        public async Task Handle_WithUniqueTechnicalName_CreatesPlanAndSavesAsync()
        {
            // Arrange
            _planRepositoryMock
                .Setup(r => r.TechnicalNameExistsAsync("premium", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            Result<CreateSubscriptionPlanResult> result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                result.Value.PlanId.Should().NotBeEmpty();

                _planRepositoryMock.Verify(
                    r => r.AddAsync(It.Is<SubscriptionPlan>(p =>
                        p.PlanKind == PlanKind.Base &&
                        p.PublicName == "Премиум" &&
                        p.TechnicalName == "premium" &&
                        p.CreatedAt == _now &&
                        p.UpdatedAt == _now),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
                _planRepositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Fact]
        public async Task Handle_WithNullTechnicalName_SkipsUniquenessCheckAndCreatesPlanAsync()
        {
            // Arrange — при null TechnicalName pre-check не должен вызываться
            // (partial UNIQUE-индекс в БД покрывает только WHERE IS NOT NULL).
            CreateSubscriptionPlanCommand command = CreateCommand(technicalName: null);

            // Act
            Result<CreateSubscriptionPlanResult> result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();

                _planRepositoryMock.Verify(
                    r => r.TechnicalNameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Never);
                _planRepositoryMock.Verify(
                    r => r.AddAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        #endregion

        #region Failure

        [Fact]
        public async Task Handle_WhenTechnicalNameTaken_ReturnsConflictAndDoesNotAddAsync()
        {
            // Arrange
            _planRepositoryMock
                .Setup(r => r.TechnicalNameExistsAsync("premium", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            Result<CreateSubscriptionPlanResult> result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(SubscriptionsErrors.TechnicalNameTaken);

                _planRepositoryMock.Verify(
                    r => r.AddAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<CancellationToken>()),
                    Times.Never);
                _planRepositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenAddOnWithRequiredRole_ReturnsDomainErrorAndDoesNotAddAsync()
        {
            // Arrange — доменный инвариант: AddOn не может иметь RequiredRole.
            CreateSubscriptionPlanCommand command = CreateCommand(
                planKind: PlanKind.AddOn,
                technicalName: null,
                requiredRole: "Chef");

            // Act
            Result<CreateSubscriptionPlanResult> result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(SubscriptionsErrors.AddOnCannotHaveRequiredRole);

                _planRepositoryMock.Verify(
                    r => r.AddAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<CancellationToken>()),
                    Times.Never);
                _planRepositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        #endregion
    }
}
