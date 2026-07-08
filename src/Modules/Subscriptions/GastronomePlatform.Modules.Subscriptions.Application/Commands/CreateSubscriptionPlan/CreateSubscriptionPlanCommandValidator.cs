using FluentValidation;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;

namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.CreateSubscriptionPlan
{
    /// <summary>
    /// Валидатор команды <see cref="CreateSubscriptionPlanCommand"/>.
    /// </summary>
    /// <remarks>
    /// Лимиты длин — единый источник в <see cref="SubscriptionPlan"/> (<c>MIN_/MAX_</c>-константы).
    /// Доменный инвариант «AddOn не может иметь RequiredRole» проверяется в
    /// <see cref="SubscriptionPlan.Create"/>, а не здесь — валидатор пропускает
    /// сочетание вниз, доменная фабрика вернёт
    /// <see cref="Domain.Errors.SubscriptionsErrors.AddOnCannotHaveRequiredRole"/>.
    /// </remarks>
    public sealed class CreateSubscriptionPlanCommandValidator : AbstractValidator<CreateSubscriptionPlanCommand>
    {
        // Множество допустимых значений RequiredRole. Домен принимает произвольную строку,
        // но по бизнес-смыслу гейт — это одна из ролей платформы; иные строки — опечатка/баг клиента.
        private static readonly HashSet<string> _allowedRequiredRoles = new(StringComparer.Ordinal)
        {
            PlatformRoles.USER,
            PlatformRoles.PREMIUM,
            PlatformRoles.CHEF,
            PlatformRoles.RESTAURANT,
            PlatformRoles.ADMIN,
        };

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CreateSubscriptionPlanCommandValidator"/>.
        /// </summary>
        public CreateSubscriptionPlanCommandValidator()
        {
            RuleFor(x => x.PlanKind)
                .IsInEnum().WithMessage("Указан недопустимый род плана (PlanKind).");

            RuleFor(x => x.PublicName)
                .NotEmpty().WithMessage("Публичное название плана обязательно.")
                .MinimumLength(SubscriptionPlan.MIN_PUBLIC_NAME_LENGTH)
                    .WithMessage($"Публичное название должно содержать минимум {SubscriptionPlan.MIN_PUBLIC_NAME_LENGTH} символа.")
                .MaximumLength(SubscriptionPlan.MAX_PUBLIC_NAME_LENGTH)
                    .WithMessage($"Публичное название не должно превышать {SubscriptionPlan.MAX_PUBLIC_NAME_LENGTH} символов.");

            RuleFor(x => x.TechnicalName)
                .MaximumLength(SubscriptionPlan.MAX_TECHNICAL_NAME_LENGTH)
                    .WithMessage($"Системное имя не должно превышать {SubscriptionPlan.MAX_TECHNICAL_NAME_LENGTH} символов.")
                .When(x => x.TechnicalName is not null);

            RuleFor(x => x.Description)
                .MaximumLength(SubscriptionPlan.MAX_DESCRIPTION_LENGTH)
                    .WithMessage($"Описание не должно превышать {SubscriptionPlan.MAX_DESCRIPTION_LENGTH} символов.")
                .When(x => x.Description is not null);

            RuleFor(x => x.InternalNotes)
                .MaximumLength(SubscriptionPlan.MAX_INTERNAL_NOTES_LENGTH)
                    .WithMessage($"Служебные заметки не должны превышать {SubscriptionPlan.MAX_INTERNAL_NOTES_LENGTH} символов.")
                .When(x => x.InternalNotes is not null);

            RuleFor(x => x.RequiredRole)
                .Must(role => role is null || _allowedRequiredRoles.Contains(role))
                .WithMessage("Указана недопустимая роль в качестве покупочного гейта.");

            RuleFor(x => x)
                .Must(cmd => cmd.AvailableFrom < cmd.AvailableUntil)
                .When(cmd => cmd.AvailableFrom.HasValue && cmd.AvailableUntil.HasValue)
                .WithMessage("Начало окна доступности должно быть строго раньше конца окна.");
        }
    }
}
