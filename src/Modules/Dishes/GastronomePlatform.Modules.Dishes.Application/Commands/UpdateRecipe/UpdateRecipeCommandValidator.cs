using FluentValidation;
using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UpdateRecipe
{
    /// <summary>
    /// Валидатор команды <see cref="UpdateRecipeCommand"/>.
    /// </summary>
    /// <remarks>
    /// Лимиты длины текстовых полей — единый источник в <see cref="Recipe"/>.
    /// </remarks>
    public sealed class UpdateRecipeCommandValidator : AbstractValidator<UpdateRecipeCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UpdateRecipeCommandValidator"/>.
        /// </summary>
        public UpdateRecipeCommandValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");

            RuleFor(x => x.ServingsDefault)
                .GreaterThanOrEqualTo(Recipe.MIN_SERVINGS_DEFAULT)
                    .WithMessage($"Количество порций по умолчанию должно быть не меньше {Recipe.MIN_SERVINGS_DEFAULT}.");

            RuleFor(x => x.IntroductionText)
                .MaximumLength(Recipe.MAX_INTRODUCTION_TEXT_LENGTH)
                    .WithMessage($"Вводный текст рецепта не должен превышать {Recipe.MAX_INTRODUCTION_TEXT_LENGTH} символов.")
                .When(x => x.IntroductionText is not null);

            RuleFor(x => x.AuthorTips)
                .MaximumLength(Recipe.MAX_AUTHOR_TIPS_LENGTH)
                    .WithMessage($"Советы автора не должны превышать {Recipe.MAX_AUTHOR_TIPS_LENGTH} символов.")
                .When(x => x.AuthorTips is not null);

            RuleFor(x => x.ServingSuggestions)
                .MaximumLength(Recipe.MAX_SERVING_SUGGESTIONS_LENGTH)
                    .WithMessage($"Рекомендации по сервировке не должны превышать {Recipe.MAX_SERVING_SUGGESTIONS_LENGTH} символов.")
                .When(x => x.ServingSuggestions is not null);

            RuleFor(x => x.Notes)
                .MaximumLength(Recipe.MAX_NOTES_LENGTH)
                    .WithMessage($"Заметки не должны превышать {Recipe.MAX_NOTES_LENGTH} символов.")
                .When(x => x.Notes is not null);
        }
    }
}
