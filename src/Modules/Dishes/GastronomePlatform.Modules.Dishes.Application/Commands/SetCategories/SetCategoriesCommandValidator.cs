using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetCategories
{
    /// <summary>
    /// Валидатор команды <see cref="SetCategoriesCommand"/>.
    /// </summary>
    /// <remarks>
    /// Структурные проверки: список не <see langword="null"/>, в пределах лимита
    /// (3 категории), без дубликатов и без пустого <see cref="Guid.Empty"/>. Лимит
    /// и проверка дубликатов дублируются Domain-методом <c>Dish.SetCategories</c>
    /// как defense-in-depth; существование <c>CategoryId</c> проверяется Handler-ом.
    /// </remarks>
    public sealed class SetCategoriesCommandValidator : AbstractValidator<SetCategoriesCommand>
    {
        /// <summary>
        /// Максимальное число категорий у блюда — должно совпадать с Domain-константой
        /// <c>Dish.MAX_CATEGORIES</c>. Дублируется на уровне валидатора, чтобы
        /// дать пользователю осмысленное сообщение через FluentValidation
        /// до похода в Domain.
        /// </summary>
        private const int MAX_CATEGORIES = 3;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SetCategoriesCommandValidator"/>.
        /// </summary>
        public SetCategoriesCommandValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");

            RuleFor(x => x.CategoryIds)
                .NotNull().WithMessage("Список категорий обязателен (для очистки передайте пустой массив).");

            RuleFor(x => x.CategoryIds)
                .Must(ids => ids is null || ids.Count <= MAX_CATEGORIES)
                    .WithMessage($"У блюда может быть не более {MAX_CATEGORIES} категорий.")
                .Must(ids => ids is null || ids.All(id => id != Guid.Empty))
                    .WithMessage("Идентификаторы категорий не могут быть пустыми.")
                .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
                    .WithMessage("Список категорий не должен содержать дубликатов.")
                .When(x => x.CategoryIds is not null);
        }
    }
}
