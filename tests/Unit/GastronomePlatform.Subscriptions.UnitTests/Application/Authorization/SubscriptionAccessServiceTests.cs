using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Modules.Subscriptions.Application.Authorization;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;
using Moq;

namespace GastronomePlatform.Subscriptions.UnitTests.Application.Authorization
{
    /// <summary>
    /// Тесты для <see cref="SubscriptionAccessService"/>.
    /// </summary>
    /// <remarks>
    /// Сервис — тонкий адаптер поверх <see cref="IUserSubscriptionRepository.ListActiveGrantsByUserAsync"/>.
    /// Логика фильтра «4 статуса + <c>CurrentPeriodEnd &gt; utcNow</c>» и union по подпискам
    /// живёт в репозитории и покрывается integration-тестом (нужна БД либо in-memory EF).
    /// </remarks>
    public sealed class SubscriptionAccessServiceTests
    {
        private readonly Mock<IUserSubscriptionRepository> _repositoryMock = new();
        private readonly Mock<IDateTimeProvider> _clockMock = new();
        private readonly SubscriptionAccessService _service;

        private static readonly DateTimeOffset _now =
            new(2026, 7, 8, 12, 0, 0, TimeSpan.Zero);
        private static readonly Guid _userId = Guid.NewGuid();

        public SubscriptionAccessServiceTests()
        {
            _service = new SubscriptionAccessService(
                _repositoryMock.Object,
                _clockMock.Object);

            _clockMock.SetupGet(c => c.UtcNow).Returns(_now);
        }

        #region Constructor

        [Fact]
        public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new SubscriptionAccessService(null!, _clockMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("repository");
        }

        [Fact]
        public void Constructor_WithNullClock_ShouldThrowArgumentNullException()
        {
            Action action = () => new SubscriptionAccessService(_repositoryMock.Object, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("clock");
        }

        #endregion

        #region GetEffectiveGrantsAsync

        [Fact]
        public async Task GetEffectiveGrantsAsync_WhenRepoReturnsEmpty_ReturnsEmptyAsync()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.ListActiveGrantsByUserAsync(_userId, _now, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FeatureGrant>());

            // Act
            IReadOnlyCollection<FeatureGrant> grants =
                await _service.GetEffectiveGrantsAsync(_userId, CancellationToken.None);

            // Assert
            grants.Should().BeEmpty();
        }

        [Fact]
        public async Task GetEffectiveGrantsAsync_WhenRepoReturnsGrants_ReturnsThemAsIsAsync()
        {
            // Arrange
            var expected = new List<FeatureGrant>
            {
                FeatureGrant.FullRecipes,
                FeatureGrant.PortionCalculator,
                FeatureGrant.PromotionAdvanced,
            };
            _repositoryMock
                .Setup(r => r.ListActiveGrantsByUserAsync(_userId, _now, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            // Act
            IReadOnlyCollection<FeatureGrant> grants =
                await _service.GetEffectiveGrantsAsync(_userId, CancellationToken.None);

            // Assert
            grants.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task GetEffectiveGrantsAsync_PassesClockUtcNowToRepositoryAsync()
        {
            // Arrange — sanity-check: сервис прокидывает _clock.UtcNow как guard-параметр
            // для фильтра CurrentPeriodEnd > utcNow в репозитории.
            _repositoryMock
                .Setup(r => r.ListActiveGrantsByUserAsync(_userId, _now, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FeatureGrant>());

            // Act
            await _service.GetEffectiveGrantsAsync(_userId, CancellationToken.None);

            // Assert
            _repositoryMock.Verify(
                r => r.ListActiveGrantsByUserAsync(_userId, _now, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion

        #region HasFeatureAsync

        [Fact]
        public async Task HasFeatureAsync_WhenGrantPresent_ReturnsTrueAsync()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.ListActiveGrantsByUserAsync(_userId, _now, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FeatureGrant> { FeatureGrant.FullRecipes });

            // Act
            bool hasFeature = await _service.HasFeatureAsync(
                _userId, FeatureGrant.FullRecipes, CancellationToken.None);

            // Assert
            hasFeature.Should().BeTrue();
        }

        [Fact]
        public async Task HasFeatureAsync_WhenGrantAbsent_ReturnsFalseAsync()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.ListActiveGrantsByUserAsync(_userId, _now, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FeatureGrant> { FeatureGrant.PortionCalculator });

            // Act
            bool hasFeature = await _service.HasFeatureAsync(
                _userId, FeatureGrant.FullRecipes, CancellationToken.None);

            // Assert
            hasFeature.Should().BeFalse();
        }

        [Fact]
        public async Task HasFeatureAsync_WhenNoActiveSubscriptions_ReturnsFalseAsync()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.ListActiveGrantsByUserAsync(_userId, _now, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FeatureGrant>());

            // Act
            bool hasFeature = await _service.HasFeatureAsync(
                _userId, FeatureGrant.FullRecipes, CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                hasFeature.Should().BeFalse();
                _repositoryMock.Verify(
                    r => r.ListActiveGrantsByUserAsync(_userId, _now, It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        #endregion
    }
}
