using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.SearchDishes
{
    /// <summary>
    /// Валидатор запроса <see cref="SearchDishesQuery"/>.
    /// </summary>
    public sealed class SearchDishesQueryValidator : AbstractValidator<SearchDishesQuery>
    {
        /// <summary>Лимит длины подстроки поиска — защита от слишком больших payload-ов.</summary>
        private const int MAX_TEXT_LENGTH = 200;

        /// <summary>Максимально допустимый <c>PageSize</c>.</summary>
        private const int MAX_PAGE_SIZE = 25;

        /// <summary>Жёсткий потолок размера фильтра по категориям/тегам/сложности/стоимости.</summary>
        private const int MAX_FILTER_ITEMS = 50;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SearchDishesQueryValidator"/>.
        /// </summary>
        public SearchDishesQueryValidator()
        {
            RuleFor(x => x.Text)
                .MaximumLength(MAX_TEXT_LENGTH)
                    .WithMessage($"Подстрока поиска не должна превышать {MAX_TEXT_LENGTH} символов.")
                .When(x => x.Text is not null);

            RuleFor(x => x.CategoryIds)
                .Must(ids => ids is null || ids.Count <= MAX_FILTER_ITEMS)
                    .WithMessage($"Список категорий не должен превышать {MAX_FILTER_ITEMS} элементов.");

            RuleFor(x => x.TagIds)
                .Must(ids => ids is null || ids.Count <= MAX_FILTER_ITEMS)
                    .WithMessage($"Список тегов не должен превышать {MAX_FILTER_ITEMS} элементов.");

            RuleFor(x => x.Difficulties)
                .Must(d => d is null || d.Count <= MAX_FILTER_ITEMS)
                    .WithMessage($"Список уровней сложности не должен превышать {MAX_FILTER_ITEMS} элементов.");

            RuleFor(x => x.Costs)
                .Must(c => c is null || c.Count <= MAX_FILTER_ITEMS)
                    .WithMessage($"Список оценок стоимости не должен превышать {MAX_FILTER_ITEMS} элементов.");

            RuleFor(x => x.MinRating)
                .InclusiveBetween(0m, 5m)
                    .WithMessage("Минимальный рейтинг должен быть в диапазоне 0..5.")
                .When(x => x.MinRating.HasValue);

            RuleFor(x => x.SortBy).IsInEnum()
                .WithMessage("Способ сортировки должен быть допустимым значением.");

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
