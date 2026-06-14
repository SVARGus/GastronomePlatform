using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.RegenerateCategorySlug
{
    /// <summary>
    /// Валидатор команды <see cref="RegenerateCategorySlugCommand"/>.
    /// </summary>
    public sealed class RegenerateCategorySlugCommandValidator
        : AbstractValidator<RegenerateCategorySlugCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="RegenerateCategorySlugCommandValidator"/>.
        /// </summary>
        public RegenerateCategorySlugCommandValidator()
        {
            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Идентификатор категории обязателен.");
        }
    }
}
