using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Errors;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;

namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.CreateSubscriptionPlan
{
    /// <summary>
    /// Обработчик команды <see cref="CreateSubscriptionPlanCommand"/> (UC-SUB-001, admin).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Если <c>TechnicalName</c> задан — pre-check уникальности через репозиторий
    ///         (partial UNIQUE-индекс покрывает <c>WHERE "TechnicalName" IS NOT NULL</c>,
    ///         но проверка на уровне Application даёт корректный
    ///         <c>SUBS.TECHNICAL_NAME_TAKEN</c> вместо гонки с 500 из БД).</item>
    ///   <item>Вызов <see cref="SubscriptionPlan.Create"/> — доменный инвариант
    ///         «AddOn не может иметь RequiredRole».</item>
    ///   <item>Добавление в хранилище и коммит.</item>
    /// </list>
    /// Авторизация — на уровне контроллера (<c>[Authorize(Roles = ADMIN)]</c>). POL-004
    /// здесь не применяется по §2.2 (admin-каталог).
    /// </remarks>
    public sealed class CreateSubscriptionPlanCommandHandler
        : ICommandHandler<CreateSubscriptionPlanCommand, CreateSubscriptionPlanResult>
    {
        private readonly ISubscriptionPlanRepository _planRepository;
        private readonly IDateTimeProvider _clock;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CreateSubscriptionPlanCommandHandler"/>.
        /// </summary>
        /// <param name="planRepository">Репозиторий планов.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        public CreateSubscriptionPlanCommandHandler(
            ISubscriptionPlanRepository planRepository,
            IDateTimeProvider clock)
        {
            _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
            _clock          = clock          ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <inheritdoc/>
        public async Task<Result<CreateSubscriptionPlanResult>> Handle(
            CreateSubscriptionPlanCommand request,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(request.TechnicalName))
            {
                bool taken = await _planRepository.TechnicalNameExistsAsync(request.TechnicalName, cancellationToken);
                if (taken)
                {
                    return SubscriptionsErrors.TechnicalNameTaken;
                }
            }

            var utcNow = _clock.UtcNow;

            Result<SubscriptionPlan> createResult = SubscriptionPlan.Create(
                planKind:       request.PlanKind,
                publicName:     request.PublicName,
                technicalName:  request.TechnicalName,
                description:    request.Description,
                requiredRole:   request.RequiredRole,
                availableFrom:  request.AvailableFrom,
                availableUntil: request.AvailableUntil,
                internalNotes:  request.InternalNotes,
                utcNow:         utcNow);

            if (createResult.IsFailure)
            {
                return createResult.Error;
            }

            var plan = createResult.Value;
            await _planRepository.AddAsync(plan, cancellationToken);
            await _planRepository.SaveChangesAsync(cancellationToken);

            return new CreateSubscriptionPlanResult(plan.Id);
        }
    }
}
