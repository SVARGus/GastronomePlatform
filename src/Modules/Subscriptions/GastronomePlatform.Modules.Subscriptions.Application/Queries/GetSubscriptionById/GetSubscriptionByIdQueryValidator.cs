using FluentValidation;

namespace GastronomePlatform.Modules.Subscriptions.Application.Queries.GetSubscriptionById
{
    /// <summary>
    /// Валидатор запроса <see cref="GetSubscriptionByIdQuery"/>.
    /// </summary>
    /// <remarks>
    /// Проверяет только форму запроса: <c>SubscriptionId</c> не должен быть <c>Guid.Empty</c>.
    /// Существование подписки и права актора (POL-004 §4.1) — в хендлере через
    /// <c>ISubscriptionAccessPolicy</c>.
    /// </remarks>
    public sealed class GetSubscriptionByIdQueryValidator : AbstractValidator<GetSubscriptionByIdQuery>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetSubscriptionByIdQueryValidator"/>.
        /// </summary>
        public GetSubscriptionByIdQueryValidator()
        {
            RuleFor(x => x.SubscriptionId)
                .NotEmpty().WithMessage("Идентификатор подписки обязателен.");
        }
    }
}
