using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.ArchiveDish
{
    /// <summary>
    /// Валидатор команды <see cref="ArchiveDishCommand"/>. Единственное входное поле —
    /// <see cref="ArchiveDishCommand.DishId"/>; защита от <see cref="Guid.Empty"/>.
    /// </summary>
    /// <remarks>
    /// Содержательное правило «блюдо ещё не архивировано» проверяется Domain-методом
    /// <c>Dish.Archive</c>: при повторном вызове возвращается
    /// <c>DISHES.DISH_ALREADY_ARCHIVED</c> (HTTP 409).
    /// </remarks>
    public sealed class ArchiveDishCommandValidator : AbstractValidator<ArchiveDishCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="ArchiveDishCommandValidator"/>.
        /// </summary>
        public ArchiveDishCommandValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");
        }
    }
}
