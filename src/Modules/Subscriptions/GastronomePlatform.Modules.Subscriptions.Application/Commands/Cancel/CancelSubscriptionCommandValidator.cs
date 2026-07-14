using FluentValidation;

namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.Cancel
{
    /// <summary>
    /// Валидатор команды <see cref="CancelSubscriptionCommand"/>.
    /// </summary>
    /// <remarks>
    /// Проверяет только форму запроса: <c>SubscriptionId</c> не должен быть <c>Guid.Empty</c>.
    /// Существование подписки, права актора (POL-004 §4.3) и допустимость перехода
    /// статуса (<c>Trialing</c>/<c>Active</c> → <c>Canceled</c>) — в хендлере.
    /// </remarks>
    public sealed class CancelSubscriptionCommandValidator : AbstractValidator<CancelSubscriptionCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CancelSubscriptionCommandValidator"/>.
        /// </summary>
        public CancelSubscriptionCommandValidator()
        {
            RuleFor(x => x.SubscriptionId)
                .NotEmpty().WithMessage("Идентификатор подписки обязателен.");
        }
    }
}
