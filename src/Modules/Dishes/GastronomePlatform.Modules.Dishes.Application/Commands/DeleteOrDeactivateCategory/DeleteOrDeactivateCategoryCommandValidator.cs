using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.DeleteOrDeactivateCategory
{
    /// <summary>
    /// Валидатор команды <see cref="DeleteOrDeactivateCategoryCommand"/>.
    /// </summary>
    public sealed class DeleteOrDeactivateCategoryCommandValidator
        : AbstractValidator<DeleteOrDeactivateCategoryCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DeleteOrDeactivateCategoryCommandValidator"/>.
        /// </summary>
        public DeleteOrDeactivateCategoryCommandValidator()
        {
            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Идентификатор категории обязателен.");
        }
    }
}
