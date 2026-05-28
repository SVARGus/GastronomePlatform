using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetMyDrafts
{
    /// <summary>
    /// Валидатор запроса <see cref="GetMyDraftsQuery"/>.
    /// </summary>
    public sealed class GetMyDraftsQueryValidator : AbstractValidator<GetMyDraftsQuery>
    {
        // Верхний предел размера страницы. Выбран как компромисс между удобством
        // постраничной выборки в UI личного кабинета и нагрузкой на БД при одном запросе.
        private const int MAX_PAGE_SIZE = 25;

        public GetMyDraftsQueryValidator()
        {
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
