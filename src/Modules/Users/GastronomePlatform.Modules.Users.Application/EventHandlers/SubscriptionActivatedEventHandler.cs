using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Auth.Domain.Contracts;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using GastronomePlatform.Modules.Subscriptions.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GastronomePlatform.Modules.Users.Application.EventHandlers
{
    /// <summary>
    /// Обработчик события активации подписки. Повышает роль пользователя до
    /// <c>PlatformRoles.PREMIUM</c> при активации подписки рода
    /// <see cref="PlanKind.Base"/>. Подписки рода <see cref="PlanKind.AddOn"/>
    /// на роль не влияют.
    /// </summary>
    /// <remarks>
    /// Событие публикуется <c>IDomainEventDispatcher</c> уже после
    /// <c>SaveChangesAsync</c> транзакции активации подписки. Значит откат
    /// транзакции невозможен — при неудаче назначения роли handler логирует
    /// ошибку и завершается без исключения. Повторная попытка возможна только
    /// через Outbox-паттерн (Этап 8+) либо ручное вмешательство.
    /// </remarks>
    public sealed class SubscriptionActivatedEventHandler : INotificationHandler<SubscriptionActivatedEvent>
    {
        private readonly IAuthUserService _authUserService;
        private readonly ILogger<SubscriptionActivatedEventHandler> _logger;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SubscriptionActivatedEventHandler"/>.
        /// </summary>
        /// <param name="authUserService">Публичный контракт модуля Auth для управления ролями.</param>
        /// <param name="logger">Логгер.</param>
        public SubscriptionActivatedEventHandler(IAuthUserService authUserService,
            ILogger<SubscriptionActivatedEventHandler> logger)
        {
            _authUserService = authUserService ?? throw new ArgumentNullException(nameof(authUserService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task Handle(SubscriptionActivatedEvent notification, CancellationToken cancellationToken)
        {
            // Роль повышается только для базовых подписок; AddOn-подписки не дают Premium.
            if (notification.PlanKind != PlanKind.Base)
            {
                return;
            }

            // Idempotency — защита от повторной обработки события (retry, дубли).
            IReadOnlyCollection<string> currentRoles =
                await _authUserService.GetUserRolesAsync(notification.UserId, cancellationToken);

            if (currentRoles.Contains(PlatformRoles.PREMIUM))
            {
                _logger.LogWarning(
                    "Пользователь {UserId} уже имеет роль {Role}. " +
                    "Повторная обработка SubscriptionActivatedEvent {EventId} пропущена.",
                    notification.UserId, PlatformRoles.PREMIUM, notification.EventId);

                return;
            }

            Result addRoleResult =
                await _authUserService.AddUserToRoleAsync(notification.UserId, PlatformRoles.PREMIUM, cancellationToken);

            if (addRoleResult.IsFailure)
            {
                // Транзакция активации подписки уже закоммичена, откат невозможен.
                // Ошибку эскалирует администратор / фоновая синхронизация (Этап 8+).
                _logger.LogError(
                    "Не удалось назначить роль {Role} пользователю {UserId} " +
                    "при обработке SubscriptionActivatedEvent {EventId}. Код ошибки: {ErrorCode}. Сообщение: {ErrorMessage}.",
                    PlatformRoles.PREMIUM, notification.UserId, notification.EventId,
                    addRoleResult.Error.Code, addRoleResult.Error.Message);

                return;
            }

            _logger.LogInformation(
                "Пользователю {UserId} назначена роль {Role} " +
                "по событию SubscriptionActivatedEvent {EventId} (подписка {SubscriptionId}, план {PlanId}).",
                notification.UserId, PlatformRoles.PREMIUM,
                notification.EventId, notification.SubscriptionId, notification.PlanId);
        }
    }
}
