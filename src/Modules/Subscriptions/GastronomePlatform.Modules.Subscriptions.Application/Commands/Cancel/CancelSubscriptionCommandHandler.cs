using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Application.Authorization;
using GastronomePlatform.Modules.Subscriptions.Domain.Errors;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;

namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.Cancel
{
    /// <summary>
    /// Обработчик команды <see cref="CancelSubscriptionCommand"/> (UC-SUB-022).
    /// </summary>
    /// <remarks>
    /// <para>Поток выполнения:</para>
    /// <list type="number">
    ///   <item>Авторизация актора через <see cref="ISubscriptionAccessPolicy"/>
    ///         (POL-004 §4.3 — Owner ‖ Admin). Policy сама загружает подписку и
    ///         возвращает <c>SUBS.NOT_FOUND</c> либо <c>SUBS.FORBIDDEN_NOT_OWNER</c>.</item>
    ///   <item>Повторная загрузка агрегата через <see cref="IUserSubscriptionRepository.GetByIdAsync"/>
    ///         для последующей мутации. Двойная загрузка (Policy + Handler) —
    ///         сознательное следование эталону POL-004 §6.2; N+1 в Phase A приемлем,
    ///         альтернативы (overload Policy с уже загруженной подпиской либо ручная
    ///         проверка прав) отклонены — не отступаем от политики на первом же UC,
    ///         который её использует. Второй null-check — защита от race-window
    ///         с удалением подписки между двумя <c>GetByIdAsync</c>.</item>
    ///   <item>Доменный переход <c>UserSubscription.Cancel(utcNow)</c> — из статусов
    ///         <c>Trialing</c>/<c>Active</c> в <c>Canceled</c>; иначе
    ///         <c>SUBS.CANNOT_CANCEL_IN_STATUS</c> (409). Доступ сохраняется до
    ///         <c>CurrentPeriodEnd</c>, роль-привязанные гранты продолжают действовать —
    ///         отзыв роли эмитится только через <c>SubscriptionExpiredEvent</c>
    ///         в UC-SUB-203.</item>
    ///   <item>Коммит через Unit of Work.</item>
    /// </list>
    /// <para>
    /// <see cref="IDomainEventDispatcher"/> в конструктор не подключается —
    /// <c>UserSubscription.Cancel</c> не поднимает доменных событий. Триггеры пересмотра
    /// и чек-лист расширения зафиксированы в <c>private_TODO-будущие-этапы.md §6.9</c>.
    /// </para>
    /// </remarks>
    public sealed class CancelSubscriptionCommandHandler : ICommandHandler<CancelSubscriptionCommand>
    {
        private readonly ISubscriptionAccessPolicy _accessPolicy;
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CancelSubscriptionCommandHandler"/>.
        /// </summary>
        /// <param name="accessPolicy">Политика авторизации операций над подпиской (POL-004).</param>
        /// <param name="userSubscriptionRepository">Репозиторий подписок пользователей.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        public CancelSubscriptionCommandHandler(
            ISubscriptionAccessPolicy accessPolicy,
            IUserSubscriptionRepository userSubscriptionRepository,
            ICurrentUserService currentUser,
            IDateTimeProvider clock)
        {
            _accessPolicy               = accessPolicy               ?? throw new ArgumentNullException(nameof(accessPolicy));
            _userSubscriptionRepository = userSubscriptionRepository ?? throw new ArgumentNullException(nameof(userSubscriptionRepository));
            _currentUser                = currentUser                ?? throw new ArgumentNullException(nameof(currentUser));
            _clock                      = clock                      ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
        {
            var actorUserId = _currentUser.UserId!.Value;
            var actorRoles = _currentUser.Roles;

            Result authResult = await _accessPolicy.AuthorizeOperationAsync(
                request.SubscriptionId,
                actorUserId,
                actorRoles,
                cancellationToken);
            if (authResult.IsFailure)
            {
                return authResult;
            }

            var subscription = await _userSubscriptionRepository.GetByIdAsync(
                request.SubscriptionId,
                cancellationToken);
            if (subscription is null)
            {
                return SubscriptionsErrors.NotFound;
            }

            Result cancelResult = subscription.Cancel(_clock.UtcNow);
            if (cancelResult.IsFailure)
            {
                return cancelResult;
            }

            await _userSubscriptionRepository.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
