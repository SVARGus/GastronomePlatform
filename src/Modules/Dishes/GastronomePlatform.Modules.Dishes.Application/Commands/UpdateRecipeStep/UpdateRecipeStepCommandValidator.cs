using FluentValidation;
using GastronomePlatform.Common.Application.Validation;
using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UpdateRecipeStep
{
    /// <summary>
    /// Валидатор команды <see cref="UpdateRecipeStepCommand"/>.
    /// </summary>
    /// <remarks>
    /// Структурные проверки. Лимиты — единый источник в <see cref="RecipeStep"/>.
    /// Существование шага проверяет Domain через <c>Recipe.UpdateStep</c>.
    /// Диапазоны температуры и таймера дублируются в Domain
    /// (<c>RecipeStep.Update</c>) как defense-in-depth.
    /// </remarks>
    public sealed class UpdateRecipeStepCommandValidator : AbstractValidator<UpdateRecipeStepCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UpdateRecipeStepCommandValidator"/>.
        /// </summary>
        public UpdateRecipeStepCommandValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");

            RuleFor(x => x.StepId)
                .NotEmpty().WithMessage("Идентификатор шага обязателен.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Описание шага обязательно.")
                .MinimumLength(RecipeStep.MIN_DESCRIPTION_LENGTH)
                    .WithMessage($"Описание шага должно содержать минимум {RecipeStep.MIN_DESCRIPTION_LENGTH} символов.")
                .MaximumLength(RecipeStep.MAX_DESCRIPTION_LENGTH)
                    .WithMessage($"Описание шага не должно превышать {RecipeStep.MAX_DESCRIPTION_LENGTH} символов.");

            RuleFor(x => x.Title)
                .MaximumLength(RecipeStep.MAX_TITLE_LENGTH)
                    .WithMessage($"Заголовок шага не должен превышать {RecipeStep.MAX_TITLE_LENGTH} символов.")
                .When(x => x.Title is not null);

            RuleFor(x => x.ImageMediaId)
                .NotEqual(Guid.Empty)
                    .WithMessage("Идентификатор иллюстрации не может быть пустым GUID.")
                .When(x => x.ImageMediaId.HasValue);

            RuleFor(x => x.VideoUrl)
                .MaximumLength(RecipeStep.MAX_VIDEO_URL_LENGTH)
                    .WithMessage($"URL видео не должен превышать {RecipeStep.MAX_VIDEO_URL_LENGTH} символов.")
                .Must(UrlValidator.IsValidHttpUrl)
                    .WithMessage("URL видео должен быть валидной http(s) ссылкой.")
                .When(x => !string.IsNullOrWhiteSpace(x.VideoUrl));

            RuleFor(x => x.TemperatureCelsius)
                .InclusiveBetween(RecipeStep.MIN_TEMPERATURE_CELSIUS, RecipeStep.MAX_TEMPERATURE_CELSIUS)
                    .WithMessage(
                        $"Температура должна быть в диапазоне {RecipeStep.MIN_TEMPERATURE_CELSIUS}..{RecipeStep.MAX_TEMPERATURE_CELSIUS} °C.")
                .When(x => x.TemperatureCelsius.HasValue);

            RuleFor(x => x.TimerMinutes)
                .InclusiveBetween(RecipeStep.MIN_TIMER_MINUTES, RecipeStep.MAX_TIMER_MINUTES)
                    .WithMessage(
                        $"Время таймера должно быть в диапазоне {RecipeStep.MIN_TIMER_MINUTES}..{RecipeStep.MAX_TIMER_MINUTES} минут.")
                .When(x => x.TimerMinutes.HasValue);
        }
    }
}
