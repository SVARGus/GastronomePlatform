using FluentValidation;
using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UpdateCategory
{
    /// <summary>
    /// Валидатор команды <see cref="UpdateCategoryCommand"/>.
    /// </summary>
    public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UpdateCategoryCommandValidator"/>.
        /// </summary>
        public UpdateCategoryCommandValidator()
        {
            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Идентификатор категории обязателен.");

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
