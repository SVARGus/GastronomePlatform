using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.MoveCategory
{
    /// <summary>
    /// Валидатор команды <see cref="MoveCategoryCommand"/>.
    /// </summary>
    public sealed class MoveCategoryCommandValidator : AbstractValidator<MoveCategoryCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="MoveCategoryCommandValidator"/>.
        /// </summary>
        public MoveCategoryCommandValidator()
        {
            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Идентификатор категории обязателен.");

            RuleFor(x => x.NewParentId)
                .NotEqual(x => x.CategoryId)
                    .WithMessage("Категория не может быть собственным родителем.")
                .When(x => x.NewParentId.HasValue);
        }
    }
}
