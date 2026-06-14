using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishesByAuthor
{
    /// <summary>
    /// Валидатор запроса <see cref="GetDishesByAuthorQuery"/>.
    /// </summary>
    public sealed class GetDishesByAuthorQueryValidator : AbstractValidator<GetDishesByAuthorQuery>
    {
        // Верхний предел размера страницы для каталожных списков по автору.
        // Совпадает с пределом UC-DSH-053 GetMyDrafts.
        private const int MAX_PAGE_SIZE = 25;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetDishesByAuthorQueryValidator"/>.
        /// </summary>
        public GetDishesByAuthorQueryValidator()
        {
            RuleFor(x => x.AuthorUserId)
                .NotEmpty().WithMessage("Идентификатор автора обязателен.");

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1)
                    .WithMessage("Номер страницы должен быть не меньше 1.");

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1)
                    .WithMessage("Размер страницы должен быть не меньше 1.")
                .LessThanOrEqualTo(MAX_PAGE_SIZE)
                    .WithMessage($"Размер страницы не должен превышать {MAX_PAGE_SIZE}.");
        }
    }
}
