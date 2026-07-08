using FluentValidation;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;

namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.Subscribe
{
    /// <summary>
    /// Валидатор команды <see cref="SubscribeCommand"/>.
    /// </summary>
    /// <remarks>
    /// Валидатор проверяет только форму запроса. Существование оффера, покупаемость,
    /// покупочный роль-гейт (POL-004 §4.2), инвариант «≤1 активной Base», авторизация
    /// у платёжного шлюза и доменные инварианты <c>UserSubscription.Activate</c> —
    /// в хендлере.
    /// </remarks>
    public sealed class SubscribeCommandValidator : AbstractValidator<SubscribeCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SubscribeCommandValidator"/>.
        /// </summary>
        public SubscribeCommandValidator()
        {
            RuleFor(x => x.PriceId)
                .NotEmpty().WithMessage("Идентификатор оффера обязателен.");

            RuleFor(x => x.PaymentMethodId)
                .NotEmpty().WithMessage("Токен способа оплаты обязателен.")
                .MaximumLength(UserSubscription.MAX_GATEWAY_PAYMENT_METHOD_ID_LENGTH)
                    .WithMessage($"Токен способа оплаты не должен превышать {UserSubscription.MAX_GATEWAY_PAYMENT_METHOD_ID_LENGTH} символов.");

            RuleFor(x => x.AcceptedTermsAt)
                .NotEqual(default(DateTimeOffset))
                    .WithMessage("Момент согласия с офертой обязателен.");
        }
    }
}
