using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UpdateDishCard
{
    /// <summary>
    /// Валидатор команды <see cref="UpdateDishCardCommand"/>.
    /// </summary>
    /// <remarks>
    /// Правила длины и enum-инвариантов синхронизированы с
    /// <c>CreateDishDraftCommandValidator</c> — это намеренно: одни и те же
    /// поля карточки должны проходить одинаковые проверки независимо от того,
    /// создаётся блюдо или обновляется.
    /// </remarks>
    public sealed class UpdateDishCardCommandValidator : AbstractValidator<UpdateDishCardCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UpdateDishCardCommandValidator"/>.
        /// </summary>
        public UpdateDishCardCommandValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Название блюда обязательно.")
                .MinimumLength(3).WithMessage("Название блюда должно содержать минимум 3 символа.")
                .MaximumLength(200).WithMessage("Название блюда не должно превышать 200 символов.");

            RuleFor(x => x.DifficultyLevel)
                .IsInEnum().WithMessage("Указан недопустимый уровень сложности.");

            RuleFor(x => x.CostEstimate)
                .IsInEnum().WithMessage("Указана недопустимая оценка стоимости.");

            RuleFor(x => x.ShortDescription)
                .MaximumLength(500).WithMessage("Краткое описание не должно превышать 500 символов.")
                .When(x => x.ShortDescription is not null);

            RuleFor(x => x.Description)
                .MaximumLength(4000).WithMessage("Полное описание не должно превышать 4000 символов.")
                .When(x => x.Description is not null);
        }
    }
}
