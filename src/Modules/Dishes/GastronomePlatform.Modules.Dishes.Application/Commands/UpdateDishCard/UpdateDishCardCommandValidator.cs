using FluentValidation;
using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UpdateDishCard
{
    /// <summary>
    /// Валидатор команды <see cref="UpdateDishCardCommand"/>.
    /// </summary>
    /// <remarks>
    /// Лимиты длины полей синхронизированы с <c>CreateDishDraftCommandValidator</c>
    /// через единый источник в <see cref="Dish"/> (<c>MIN_/MAX_</c>-константы) —
    /// одни и те же поля карточки проходят одинаковые проверки независимо от того,
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
                .MinimumLength(Dish.MIN_NAME_LENGTH)
                    .WithMessage($"Название блюда должно содержать минимум {Dish.MIN_NAME_LENGTH} символа.")
                .MaximumLength(Dish.MAX_NAME_LENGTH)
                    .WithMessage($"Название блюда не должно превышать {Dish.MAX_NAME_LENGTH} символов.");

            RuleFor(x => x.DifficultyLevel)
                .IsInEnum().WithMessage("Указан недопустимый уровень сложности.");

            RuleFor(x => x.CostEstimate)
                .IsInEnum().WithMessage("Указана недопустимая оценка стоимости.");

            RuleFor(x => x.ShortDescription)
                .MaximumLength(Dish.MAX_SHORT_DESCRIPTION_LENGTH)
                    .WithMessage($"Краткое описание не должно превышать {Dish.MAX_SHORT_DESCRIPTION_LENGTH} символов.")
                .When(x => x.ShortDescription is not null);

            RuleFor(x => x.Description)
                .MaximumLength(Dish.MAX_DESCRIPTION_LENGTH)
                    .WithMessage($"Полное описание не должно превышать {Dish.MAX_DESCRIPTION_LENGTH} символов.")
                .When(x => x.Description is not null);
        }
    }
}
