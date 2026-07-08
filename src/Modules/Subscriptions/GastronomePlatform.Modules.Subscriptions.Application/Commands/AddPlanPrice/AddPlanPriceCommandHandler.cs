using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Errors;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;

namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.AddPlanPrice
{
    /// <summary>
    /// Обработчик команды <see cref="AddPlanPriceCommand"/> (UC-SUB-004, admin).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Проверить существование плана (<c>SUBS.PLAN_NOT_FOUND</c>).</item>
    ///   <item>Если задан <c>RenewsAsPriceId</c> — загрузить целевой оффер:
    ///         отсутствует → <c>SUBS.TRANSITION_PRICE_NOT_FOUND</c>, чужой план →
    ///         <c>SUBS.TRANSITION_PRICE_CROSS_PLAN</c>. Аналогично для <c>FallbackPriceId</c>.</item>
    ///   <item>Вызвать <c>PlanPrice.Create</c> — доменные инварианты
    ///         (Amount ≥ 0, Trial-правила, IsRecurring ⇒ переходы null).</item>
    ///   <item>Добавить оффер в хранилище и коммит.</item>
    /// </list>
    /// Cycle-detection на цепочках <c>RenewsAs</c>/<c>Fallback</c> в этом UC не требуется:
    /// новый оффер ещё не существует в БД, поэтому существующая (валидная по инварианту)
    /// цепочка не может закольцеваться через него. Cycle-detection становится актуальной
    /// в UC-SUB-005 UpdateOffer, где меняется <c>RenewsAsPriceId</c> у существующего оффера.
    /// </remarks>
    public sealed class AddPlanPriceCommandHandler : ICommandHandler<AddPlanPriceCommand, AddPlanPriceResult>
    {
        private readonly ISubscriptionPlanRepository _planRepository;
        private readonly IPlanPriceRepository _priceRepository;
        private readonly IDateTimeProvider _clock;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="AddPlanPriceCommandHandler"/>.
        /// </summary>
        /// <param name="planRepository">Репозиторий планов.</param>
        /// <param name="priceRepository">Репозиторий офферов.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        public AddPlanPriceCommandHandler(
            ISubscriptionPlanRepository planRepository,
            IPlanPriceRepository priceRepository,
            IDateTimeProvider clock)
        {
            _planRepository  = planRepository  ?? throw new ArgumentNullException(nameof(planRepository));
            _priceRepository = priceRepository ?? throw new ArgumentNullException(nameof(priceRepository));
            _clock           = clock           ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <inheritdoc/>
        public async Task<Result<AddPlanPriceResult>> Handle(
            AddPlanPriceCommand request,
            CancellationToken cancellationToken)
        {
            var plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken);
            if (plan is null)
            {
                return SubscriptionsErrors.PlanNotFound;
            }

            var renewsCheck = await CheckTransitionTargetAsync(request.RenewsAsPriceId, request.PlanId, cancellationToken);
            if (renewsCheck.IsFailure)
            {
                return renewsCheck.Error;
            }

            var fallbackCheck = await CheckTransitionTargetAsync(request.FallbackPriceId, request.PlanId, cancellationToken);
            if (fallbackCheck.IsFailure)
            {
                return fallbackCheck.Error;
            }

            var utcNow = _clock.UtcNow;

            Result<PlanPrice> createResult = PlanPrice.Create(
                planId:           request.PlanId,
                kind:             request.Kind,
                publicName:       request.PublicName,
                durationDays:     request.DurationDays,
                currency:         request.Currency,
                amount:           request.Amount,
                compareAtAmount:  request.CompareAtAmount,
                discountPercent:  request.DiscountPercent,
                trialDays:        request.TrialDays,
                isRecurring:      request.IsRecurring,
                isPurchasable:    request.IsPurchasable,
                renewsAsPriceId:  request.RenewsAsPriceId,
                fallbackPriceId:  request.FallbackPriceId,
                availableFrom:    request.AvailableFrom,
                availableUntil:   request.AvailableUntil,
                internalNotes:    request.InternalNotes,
                utcNow:           utcNow);

            if (createResult.IsFailure)
            {
                return createResult.Error;
            }

            var price = createResult.Value;
            await _priceRepository.AddAsync(price, cancellationToken);
            await _priceRepository.SaveChangesAsync(cancellationToken);

            return new AddPlanPriceResult(price.Id);
        }

        /// <summary>
        /// Проверяет, что целевой оффер перехода существует и принадлежит указанному плану.
        /// </summary>
        /// <param name="targetPriceId">Идентификатор целевого оффера (или <see langword="null"/>).</param>
        /// <param name="expectedPlanId">Ожидаемый идентификатор плана.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Result.Success()"/>, если <paramref name="targetPriceId"/> = <see langword="null"/>
        /// либо оффер найден и принадлежит нужному плану. Иначе — соответствующая ошибка.
        /// </returns>
        private async Task<Result> CheckTransitionTargetAsync(
            Guid? targetPriceId,
            Guid expectedPlanId,
            CancellationToken cancellationToken)
        {
            if (!targetPriceId.HasValue)
            {
                return Result.Success();
            }

            var target = await _priceRepository.GetByIdAsync(targetPriceId.Value, cancellationToken);
            if (target is null)
            {
                return SubscriptionsErrors.TransitionPriceNotFound;
            }

            if (target.PlanId != expectedPlanId)
            {
                return SubscriptionsErrors.TransitionPriceCrossPlan;
            }

            return Result.Success();
        }
    }
}
