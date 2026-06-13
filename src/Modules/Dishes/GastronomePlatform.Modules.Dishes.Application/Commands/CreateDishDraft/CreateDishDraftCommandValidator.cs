using FluentValidation;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.CreateDishDraft
{
    /// <summary>
    /// Валидатор команды <see cref="CreateDishDraftCommand"/>.
    /// </summary>
    /// <remarks>
    /// Лимиты длины полей — единый источник в <see cref="Dish"/> (<c>MIN_/MAX_</c>-константы).
    /// </remarks>
    public sealed class CreateDishDraftCommandValidator : AbstractValidator<CreateDishDraftCommand>
    {
        // Маска всех валидных битов перечисления DietLabels (для проверки, что
        // в DietLabelsMask не пришли неподдерживаемые биты).
        private const DietLabels VALID_DIET_LABELS =
            DietLabels.Vegetarian | DietLabels.Vegan | DietLabels.GlutenFree |
            DietLabels.LactoseFree | DietLabels.Halal | DietLabels.Kosher |
            DietLabels.KetoFriendly | DietLabels.LowCarb | DietLabels.LowCalorie |
            DietLabels.SugarFree;

        public CreateDishDraftCommandValidator()
        {
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

            RuleFor(x => x.HistoryText)
                .MaximumLength(Dish.MAX_HISTORY_TEXT_LENGTH)
                    .WithMessage($"Историческое описание не должно превышать {Dish.MAX_HISTORY_TEXT_LENGTH} символов.")
                .When(x => x.HistoryText is not null);

            RuleFor(x => x.DietLabelsMask)
                .Must(mask => mask is null || (mask.Value & ~VALID_DIET_LABELS) == 0)
                .WithMessage("Маска диетических меток содержит неподдерживаемые биты.");
        }
    }
}
