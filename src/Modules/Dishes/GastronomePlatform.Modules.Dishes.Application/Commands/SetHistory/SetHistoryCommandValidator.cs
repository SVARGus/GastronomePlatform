using FluentValidation;
using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetHistory
{
    /// <summary>
    /// Валидатор команды <see cref="SetHistoryCommand"/>.
    /// </summary>
    /// <remarks>
    /// Лимит длины <see cref="Dish.MAX_HISTORY_TEXT_LENGTH"/> — единый источник в Domain.
    /// Значение <see langword="null"/> допустимо (очистить поле).
    /// </remarks>
    public sealed class SetHistoryCommandValidator : AbstractValidator<SetHistoryCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SetHistoryCommandValidator"/>.
        /// </summary>
        public SetHistoryCommandValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");

            RuleFor(x => x.HistoryText)
                .MaximumLength(Dish.MAX_HISTORY_TEXT_LENGTH)
                    .WithMessage($"Историческое описание не должно превышать {Dish.MAX_HISTORY_TEXT_LENGTH} символов.")
                .When(x => x.HistoryText is not null);
        }
    }
}
