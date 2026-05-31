using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.PublishDish
{
    /// <summary>
    /// Валидатор команды <see cref="PublishDishCommand"/>. Единственное входное поле —
    /// <see cref="PublishDishCommand.DishId"/>; защита от <see cref="Guid.Empty"/>.
    /// </summary>
    /// <remarks>
    /// Все содержательные инварианты публикации (наличие главного фото, шагов рецепта,
    /// ингредиентов, ненулевое общее время приготовления, защита от повторной публикации
    /// без правок) проверяются Domain-методом <c>Dish.Publish</c> — там же возвращаются
    /// соответствующие коды ошибок типа <c>Conflict</c> (HTTP 409).
    /// </remarks>
    public sealed class PublishDishCommandValidator : AbstractValidator<PublishDishCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="PublishDishCommandValidator"/>.
        /// </summary>
        public PublishDishCommandValidator()
        {
            RuleFor(x => x.DishId).NotEmpty();
        }
    }
}
