using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using GastronomePlatform.Modules.Subscriptions.Domain.Errors;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;

namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.SetPlanGrants
{
    /// <summary>
    /// Обработчик команды <see cref="SetPlanGrantsCommand"/> (UC-SUB-007, admin).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Проверить квотовую применимость: <c>Quantity != null ⇒ Grant = PromotionAdvanced</c>
    ///         (Phase A реестр квотовых грантов — только <c>PromotionAdvanced</c>).</item>
    ///   <item>Загрузить план с составом грантов (<c>GetByIdWithGrantsAsync</c>) —
    ///         нужен для корректной работы EF change tracker при <c>_grants.Clear()</c>.</item>
    ///   <item>Собрать словарь <c>FeatureGrant → int?</c> и вызвать <c>plan.SetGrants(...)</c>.</item>
    ///   <item>Коммит через Unit of Work.</item>
    /// </list>
    /// Реестр квотовых грантов вынесен в <see cref="IsQuotaCarrying"/> — при появлении
    /// новых квотовых грантов на Этапе 4+ дополняется здесь; альтернатива —
    /// реестр в Domain (см. <c>FeatureGrantRoleRequirements</c>), но это отдельное решение
    /// по прецеденту потребителей.
    /// </remarks>
    public sealed class SetPlanGrantsCommandHandler : ICommandHandler<SetPlanGrantsCommand>
    {
        private readonly ISubscriptionPlanRepository _planRepository;
        private readonly IDateTimeProvider _clock;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SetPlanGrantsCommandHandler"/>.
        /// </summary>
        /// <param name="planRepository">Репозиторий планов.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        public SetPlanGrantsCommandHandler(
            ISubscriptionPlanRepository planRepository,
            IDateTimeProvider clock)
        {
            _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
            _clock          = clock          ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(SetPlanGrantsCommand request, CancellationToken cancellationToken)
        {
            foreach (var item in request.Grants)
            {
                if (item.Quantity.HasValue && !IsQuotaCarrying(item.Grant))
                {
                    return SubscriptionsErrors.PlanGrantQuotaNotApplicable;
                }
            }

            var plan = await _planRepository.GetByIdWithGrantsAsync(request.PlanId, cancellationToken);
            if (plan is null)
            {
                return SubscriptionsErrors.PlanNotFound;
            }

            var grantsDict = request.Grants.ToDictionary(item => item.Grant, item => item.Quantity);
            plan.SetGrants(grantsDict, _clock.UtcNow);

            await _planRepository.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        /// <summary>
        /// Реестр квотовых грантов Phase A.
        /// </summary>
        /// <param name="grant">Значение <see cref="FeatureGrant"/>.</param>
        /// <returns>
        /// <see langword="true"/>, если грант несёт квоту через <c>PlanGrant.Quantity</c>;
        /// иначе <see langword="false"/>.
        /// </returns>
        private static bool IsQuotaCarrying(FeatureGrant grant)
        {
            return grant == FeatureGrant.PromotionAdvanced;
        }
    }
}
