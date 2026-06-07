using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetDietLabels
{
    /// <summary>
    /// Валидатор команды <see cref="SetDietLabelsCommand"/>.
    /// </summary>
    /// <remarks>
    /// Содержательная проверка совместимости маски с составом — задача Domain
    /// (<c>Dish.SetDietLabels</c>), здесь — только структурная валидация входа.
    /// Значение <c>DietLabelsMask = None</c> допустимо (снятие всех меток) —
    /// поэтому on-bit-проверки тут нет.
    /// </remarks>
    public sealed class SetDietLabelsCommandValidator : AbstractValidator<SetDietLabelsCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SetDietLabelsCommandValidator"/>.
        /// </summary>
        public SetDietLabelsCommandValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");
        }
    }
}
