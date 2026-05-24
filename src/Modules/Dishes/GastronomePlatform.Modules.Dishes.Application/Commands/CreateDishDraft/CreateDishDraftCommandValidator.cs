using FluentValidation;
using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.CreateDishDraft
{
    /// <summary>
    /// Валидатор команды <see cref="CreateDishDraftCommand"/>.
    /// </summary>
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

            RuleFor(x => x.HistoryText)
                .MaximumLength(4000).WithMessage("Историческое описание не должно превышать 4000 символов.")
                .When(x => x.HistoryText is not null);

            RuleFor(x => x.DietLabelsMask)
                .Must(mask => mask is null || (mask.Value & ~VALID_DIET_LABELS) == 0)
                .WithMessage("Маска диетических меток содержит неподдерживаемые биты.");
        }
    }
}
