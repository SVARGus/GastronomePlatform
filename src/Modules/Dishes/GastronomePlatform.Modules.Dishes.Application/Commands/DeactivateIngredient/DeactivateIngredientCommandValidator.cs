using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.DeactivateIngredient
{
    /// <summary>
    /// Валидатор команды <see cref="DeactivateIngredientCommand"/>.
    /// </summary>
    public sealed class DeactivateIngredientCommandValidator
        : AbstractValidator<DeactivateIngredientCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DeactivateIngredientCommandValidator"/>.
        /// </summary>
        public DeactivateIngredientCommandValidator()
        {
            RuleFor(x => x.IngredientId)
                .NotEmpty().WithMessage("Идентификатор ингредиента обязателен.");
        }
    }
}
