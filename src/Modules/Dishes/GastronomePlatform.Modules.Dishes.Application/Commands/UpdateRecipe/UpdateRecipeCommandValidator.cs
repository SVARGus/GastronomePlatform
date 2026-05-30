using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UpdateRecipe
{
    /// <summary>
    /// Валидатор команды <see cref="UpdateRecipeCommand"/>.
    /// </summary>
    /// <remarks>
    /// Лимиты длины текстовых полей выбраны как разумный верхний предел для пользовательских
    /// блоков рецепта: 4000 символов соответствует <c>Description</c> блюда (UC-DSH-002).
    /// На уровне БД ограничения нет (text без длины) — это вторая линия защиты от случайного
    /// чрезмерного ввода через UI или клиентскую интеграцию.
    /// </remarks>
    public sealed class UpdateRecipeCommandValidator : AbstractValidator<UpdateRecipeCommand>
    {
        private const int MAX_TEXT_LENGTH = 4000;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UpdateRecipeCommandValidator"/>.
        /// </summary>
        public UpdateRecipeCommandValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");

            RuleFor(x => x.ServingsDefault)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Количество порций по умолчанию должно быть не меньше 1.");

            RuleFor(x => x.IntroductionText)
                .MaximumLength(MAX_TEXT_LENGTH)
                .WithMessage($"Вводный текст рецепта не должен превышать {MAX_TEXT_LENGTH} символов.")
                .When(x => x.IntroductionText is not null);

            RuleFor(x => x.AuthorTips)
                .MaximumLength(MAX_TEXT_LENGTH)
                .WithMessage($"Советы автора не должны превышать {MAX_TEXT_LENGTH} символов.")
                .When(x => x.AuthorTips is not null);

            RuleFor(x => x.ServingSuggestions)
                .MaximumLength(MAX_TEXT_LENGTH)
                .WithMessage($"Рекомендации по сервировке не должны превышать {MAX_TEXT_LENGTH} символов.")
                .When(x => x.ServingSuggestions is not null);

            RuleFor(x => x.Notes)
                .MaximumLength(MAX_TEXT_LENGTH)
                .WithMessage($"Заметки не должны превышать {MAX_TEXT_LENGTH} символов.")
                .When(x => x.Notes is not null);
        }
    }
}
