using FluentValidation;
using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.CreateCategory
{
    /// <summary>
    /// Валидатор команды <see cref="CreateCategoryCommand"/>.
    /// </summary>
    public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CreateCategoryCommandValidator"/>.
        /// </summary>
        public CreateCategoryCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Имя категории обязательно.")
                .MinimumLength(Category.MIN_NAME_LENGTH)
                    .WithMessage($"Имя категории должно содержать не менее {Category.MIN_NAME_LENGTH} символов.")
                .MaximumLength(Category.MAX_NAME_LENGTH)
                    .WithMessage($"Имя категории не должно превышать {Category.MAX_NAME_LENGTH} символов.");

            RuleFor(x => x.Order)
                .GreaterThanOrEqualTo(0)
                    .WithMessage("Порядок отображения не может быть отрицательным.");
        }
    }
}
