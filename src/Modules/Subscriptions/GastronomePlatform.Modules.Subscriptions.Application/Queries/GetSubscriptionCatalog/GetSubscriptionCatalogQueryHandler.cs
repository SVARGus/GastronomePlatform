using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;

namespace GastronomePlatform.Modules.Subscriptions.Application.Queries.GetSubscriptionCatalog
{
    /// <summary>
    /// Обработчик запроса <see cref="GetSubscriptionCatalogQuery"/> (UC-SUB-040).
    /// </summary>
    /// <remarks>
    /// <para>Поток выполнения:</para>
    /// <list type="number">
    ///   <item>Загрузка планов с составом грантов.</item>
    ///   <item>Отбор предлагаемых к покупке через <c>SubscriptionPlan.IsAvailableAt</c>.</item>
    ///   <item>Загрузка офферов отобранных планов одним запросом.</item>
    ///   <item>Отбор покупаемых офферов через <c>PlanPrice.IsPurchasableAt</c>.</item>
    ///   <item>Сборка карточек; планы без единого покупаемого оффера отбрасываются.</item>
    /// </list>
    /// <para>
    /// Оба предиката — доменные методы, а не условия запроса. Это сознательный
    /// размен: фильтрация идёт в памяти после загрузки, зато правила видимости
    /// существуют в единственном экземпляре и совпадают с теми, по которым
    /// оформление принимает или отклоняет оффер. Каталог измеряется единицами
    /// планов, поэтому цена размена близка к нулю.
    /// </para>
    /// <para>
    /// Время берётся один раз в начале обработки и применяется к обоим предикатам:
    /// иначе план и его офферы проверялись бы на разные моменты, и на границе окна
    /// доступности выдача оказалась бы несогласованной.
    /// </para>
    /// <para>
    /// Запрос не может завершиться ошибкой: пустой каталог — это пустой список,
    /// а не отказ. <c>Result</c> возвращается ради единообразия с остальными
    /// запросами модуля.
    /// </para>
    /// </remarks>
    public sealed class GetSubscriptionCatalogQueryHandler
        : IQueryHandler<GetSubscriptionCatalogQuery, IReadOnlyList<SubscriptionCatalogPlanResponse>>
    {
        private readonly ISubscriptionPlanRepository _planRepository;
        private readonly IPlanPriceRepository _priceRepository;
        private readonly IDateTimeProvider _clock;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetSubscriptionCatalogQueryHandler"/>.
        /// </summary>
        /// <param name="planRepository">Репозиторий каталога планов.</param>
        /// <param name="priceRepository">Репозиторий офферов каталога.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        public GetSubscriptionCatalogQueryHandler(
            ISubscriptionPlanRepository planRepository,
            IPlanPriceRepository priceRepository,
            IDateTimeProvider clock)
        {
            _planRepository  = planRepository  ?? throw new ArgumentNullException(nameof(planRepository));
            _priceRepository = priceRepository ?? throw new ArgumentNullException(nameof(priceRepository));
            _clock           = clock           ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <inheritdoc/>
        public async Task<Result<IReadOnlyList<SubscriptionCatalogPlanResponse>>> Handle(
            GetSubscriptionCatalogQuery request,
            CancellationToken cancellationToken)
        {
            var utcNow = _clock.UtcNow;

            IReadOnlyList<SubscriptionPlan> allPlans =
                await _planRepository.ListWithGrantsAsync(cancellationToken);

            List<SubscriptionPlan> availablePlans = allPlans
                .Where(plan => plan.IsAvailableAt(utcNow))
                .ToList();

            if (availablePlans.Count == 0)
            {
                return Array.Empty<SubscriptionCatalogPlanResponse>();
            }

            IReadOnlyList<PlanPrice> prices = await _priceRepository.ListByPlanIdsAsync(
                availablePlans.Select(plan => plan.Id).ToList(),
                cancellationToken);

            Dictionary<Guid, List<PlanPrice>> purchasableByPlanId = prices
                .Where(price => price.IsPurchasableAt(utcNow))
                .GroupBy(price => price.PlanId)
                .ToDictionary(group => group.Key, group => group.ToList());

            var catalog = new List<SubscriptionCatalogPlanResponse>(availablePlans.Count);

            foreach (var plan in availablePlans)
            {
                if (!purchasableByPlanId.TryGetValue(plan.Id, out var planOffers))
                {
                    // План есть в каталоге, но купить его сейчас не по чему —
                    // на витрине он был бы тупиком.
                    continue;
                }

                catalog.Add(new SubscriptionCatalogPlanResponse(
                    plan.Id,
                    plan.PlanKind,
                    plan.PublicName,
                    plan.Description,
                    plan.RequiredRole,
                    plan.Grants
                        .Select(grant => new SubscriptionCatalogGrantResponse(grant.Grant, grant.Quantity))
                        .ToList(),
                    planOffers
                        .Select(offer => new SubscriptionCatalogOfferResponse(
                            offer.Id,
                            offer.Kind,
                            offer.PublicName,
                            offer.Amount,
                            offer.Currency,
                            offer.CompareAtAmount,
                            offer.DiscountPercent,
                            offer.DurationDays,
                            offer.TrialDays,
                            offer.IsRecurring))
                        .ToList()));
            }

            return catalog;
        }
    }
}
