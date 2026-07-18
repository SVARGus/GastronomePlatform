using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Application.Queries.GetSubscriptionCatalog;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;
using Moq;

namespace GastronomePlatform.Subscriptions.UnitTests.Application.Queries
{
    /// <summary>
    /// Тесты для <see cref="GetSubscriptionCatalogQueryHandler"/> (UC-SUB-040).
    /// </summary>
    /// <remarks>
    /// Фокус — правила видимости витрины: какие планы и офферы попадают в выдачу
    /// и какие поля отдаются наружу. Сами предикаты доступности живут в Domain
    /// (<c>SubscriptionPlan.IsAvailableAt</c>, <c>PlanPrice.IsPurchasableAt</c>),
    /// поэтому здесь проверяется, что хендлер их действительно применяет,
    /// а не дублирует условие.
    /// </remarks>
    public sealed class GetSubscriptionCatalogQueryHandlerTests
    {
        private readonly Mock<ISubscriptionPlanRepository> _planRepoMock = new();
        private readonly Mock<IPlanPriceRepository> _priceRepoMock = new();
        private readonly Mock<IDateTimeProvider> _clockMock = new();
        private readonly GetSubscriptionCatalogQueryHandler _handler;

        private static readonly DateTimeOffset _now =
            new(2026, 7, 18, 12, 0, 0, TimeSpan.Zero);

        public GetSubscriptionCatalogQueryHandlerTests()
        {
            _handler = new GetSubscriptionCatalogQueryHandler(
                _planRepoMock.Object,
                _priceRepoMock.Object,
                _clockMock.Object);

            _clockMock.SetupGet(c => c.UtcNow).Returns(_now);
        }

        #region Helpers

        private static SubscriptionPlan CreatePlan(
            string publicName = "Премиум",
            PlanKind planKind = PlanKind.Base,
            string? requiredRole = null,
            DateTimeOffset? availableFrom = null,
            DateTimeOffset? availableUntil = null,
            bool isActive = true)
        {
            var plan = SubscriptionPlan.Create(
                planKind:       planKind,
                publicName:     publicName,
                technicalName:  null,
                description:    "Описание тарифа",
                requiredRole:   requiredRole,
                availableFrom:  availableFrom,
                availableUntil: availableUntil,
                internalNotes:  "Служебная заметка",
                utcNow:         _now).Value;

            if (!isActive)
            {
                plan.Deactivate(_now);
            }

            return plan;
        }

        private static PlanPrice CreateOffer(
            Guid planId,
            decimal amount = 1499m,
            bool isPurchasable = true,
            bool isActive = true,
            DateTimeOffset? availableFrom = null,
            DateTimeOffset? availableUntil = null)
        {
            var offer = PlanPrice.Create(
                planId:           planId,
                kind:             OfferKind.Standard,
                publicName:       "Месяц",
                durationDays:     30,
                currency:         "RUB",
                amount:           amount,
                compareAtAmount:  1999m,
                discountPercent:  25,
                trialDays:        null,
                isRecurring:      true,
                isPurchasable:    isPurchasable,
                renewsAsPriceId:  null,
                fallbackPriceId:  null,
                availableFrom:    availableFrom,
                availableUntil:   availableUntil,
                internalNotes:    "Служебная заметка оффера",
                utcNow:           _now).Value;

            if (!isActive)
            {
                offer.Deactivate(_now);
            }

            return offer;
        }

        private void SetupCatalog(IReadOnlyList<SubscriptionPlan> plans, IReadOnlyList<PlanPrice> offers)
        {
            _planRepoMock
                .Setup(r => r.ListWithGrantsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(plans);

            _priceRepoMock
                .Setup(r => r.ListByPlanIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(offers);
        }

        private Task<Result<IReadOnlyList<SubscriptionCatalogPlanResponse>>> ActAsync()
            => _handler.Handle(new GetSubscriptionCatalogQuery(), CancellationToken.None);

        #endregion

        [Fact]
        public async Task Handle_ВозвращаетПланСОфферамиИГрантами_КогдаВсёДоступно()
        {
            var plan = CreatePlan();
            plan.SetGrants(
                new Dictionary<FeatureGrant, int?>
                {
                    [FeatureGrant.FullRecipes] = null,
                    [FeatureGrant.PromotionAdvanced] = 10
                },
                _now);

            var offer = CreateOffer(plan.Id);
            SetupCatalog(new[] { plan }, new[] { offer });

            var result = await ActAsync();

            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                result.Value.Should().HaveCount(1);

                var card = result.Value[0];
                card.Id.Should().Be(plan.Id);
                card.PublicName.Should().Be("Премиум");
                card.PlanKind.Should().Be(PlanKind.Base);
                card.RequiredRole.Should().BeNull();

                card.Grants.Should().HaveCount(2);
                card.Grants.Should().ContainSingle(g => g.Grant == FeatureGrant.FullRecipes && g.Quantity == null);
                card.Grants.Should().ContainSingle(g => g.Grant == FeatureGrant.PromotionAdvanced && g.Quantity == 10);

                card.Offers.Should().HaveCount(1);
                card.Offers[0].Id.Should().Be(offer.Id);
                card.Offers[0].Amount.Should().Be(1499m);
                card.Offers[0].Currency.Should().Be("RUB");
                card.Offers[0].CompareAtAmount.Should().Be(1999m);
                card.Offers[0].DiscountPercent.Should().Be(25);
                card.Offers[0].DurationDays.Should().Be(30);
                card.Offers[0].IsRecurring.Should().BeTrue();
            }
        }

        [Fact]
        public async Task Handle_ПоказываетРольГейтованныйПлан_СУказаниемТребуемойРоли()
        {
            // Витрина информирует, но не авторизует: план с RequiredRole виден всем,
            // а право на покупку проверяется при оформлении.
            var plan = CreatePlan(requiredRole: PlatformRoles.CHEF);
            SetupCatalog(new[] { plan }, new[] { CreateOffer(plan.Id) });

            var result = await ActAsync();

            using (new AssertionScope())
            {
                result.Value.Should().HaveCount(1);
                result.Value[0].RequiredRole.Should().Be(PlatformRoles.CHEF);
            }
        }

        [Fact]
        public async Task Handle_СкрываетПлан_БезЕдиногоПокупаемогоОффера()
        {
            var plan = CreatePlan();
            var notPurchasable = CreateOffer(plan.Id, isPurchasable: false);
            var deactivated = CreateOffer(plan.Id, isActive: false);

            SetupCatalog(new[] { plan }, new[] { notPurchasable, deactivated });

            var result = await ActAsync();

            result.Value.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_СкрываетНедоступныйПлан_ДажеСПокупаемымОффером()
        {
            var deactivated = CreatePlan(isActive: false);
            var notStarted = CreatePlan(availableFrom: _now.AddDays(1));
            var ended = CreatePlan(availableUntil: _now.AddDays(-1));

            SetupCatalog(
                new[] { deactivated, notStarted, ended },
                new[] { CreateOffer(deactivated.Id), CreateOffer(notStarted.Id), CreateOffer(ended.Id) });

            var result = await ActAsync();

            result.Value.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_ОтдаётТолькоПокупаемыеОфферы_ВнутриВидимогоПлана()
        {
            var plan = CreatePlan();
            var purchasable = CreateOffer(plan.Id, amount: 1499m);
            var hidden = CreateOffer(plan.Id, amount: 999m, isPurchasable: false);
            var expired = CreateOffer(plan.Id, amount: 799m, availableUntil: _now.AddDays(-1));

            SetupCatalog(new[] { plan }, new[] { purchasable, hidden, expired });

            var result = await ActAsync();

            using (new AssertionScope())
            {
                result.Value.Should().HaveCount(1);
                result.Value[0].Offers.Should().HaveCount(1);
                result.Value[0].Offers[0].Id.Should().Be(purchasable.Id);
            }
        }

        [Fact]
        public async Task Handle_ВозвращаетПустойСписок_КогдаКаталогПуст()
        {
            SetupCatalog(Array.Empty<SubscriptionPlan>(), Array.Empty<PlanPrice>());

            var result = await ActAsync();

            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                result.Value.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task Handle_НеЗапрашиваетОфферы_КогдаВидимыхПлановНет()
        {
            // Экономия запроса: если ни один план не проходит фильтр видимости,
            // обращаться за офферами не за чем.
            SetupCatalog(new[] { CreatePlan(isActive: false) }, Array.Empty<PlanPrice>());

            await ActAsync();

            _priceRepoMock.Verify(
                r => r.ListByPlanIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_РаскладываетОфферыПоСвоимПланам()
        {
            var first = CreatePlan("Базовый");
            var second = CreatePlan("Премиум");

            var firstOffer = CreateOffer(first.Id, amount: 499m);
            var secondOffer = CreateOffer(second.Id, amount: 1499m);

            SetupCatalog(new[] { first, second }, new[] { secondOffer, firstOffer });

            var result = await ActAsync();

            using (new AssertionScope())
            {
                result.Value.Should().HaveCount(2);

                var firstCard = result.Value.Single(p => p.Id == first.Id);
                firstCard.Offers.Should().ContainSingle(o => o.Id == firstOffer.Id);

                var secondCard = result.Value.Single(p => p.Id == second.Id);
                secondCard.Offers.Should().ContainSingle(o => o.Id == secondOffer.Id);
            }
        }

        [Fact]
        public async Task Handle_ИспользуетОдинМоментВремени_ДляПлановИОфферов()
        {
            // Время должно браться из IDateTimeProvider ровно один раз: иначе план
            // и его офферы проверялись бы на разные моменты, и на границе окна
            // доступности выдача оказалась бы несогласованной.
            var plan = CreatePlan();
            SetupCatalog(new[] { plan }, new[] { CreateOffer(plan.Id) });

            await ActAsync();

            _clockMock.VerifyGet(c => c.UtcNow, Times.Once);
        }
    }
}
