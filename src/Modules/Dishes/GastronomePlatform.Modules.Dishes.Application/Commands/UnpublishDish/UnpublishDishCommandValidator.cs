using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UnpublishDish
{
    /// <summary>
    /// Валидатор команды <see cref="UnpublishDishCommand"/>. Единственное входное поле —
    /// <see cref="UnpublishDishCommand.DishId"/>; защита от <see cref="Guid.Empty"/>.
    /// </summary>
    /// <remarks>
    /// Содержательное правило «снять с публикации можно только опубликованное блюдо»
    /// проверяется Domain-методом <c>Dish.Unpublish</c>: при нарушении возвращается
    /// <c>DISHES.DISH_NOT_PUBLISHED</c> (HTTP 409).
    /// </remarks>
    public sealed class UnpublishDishCommandValidator : AbstractValidator<UnpublishDishCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UnpublishDishCommandValidator"/>.
        /// </summary>
        public UnpublishDishCommandValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");
        }
    }
}
