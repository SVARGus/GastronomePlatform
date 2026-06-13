using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.ReorderRecipeSteps
{
    /// <summary>
    /// Валидатор команды <see cref="ReorderRecipeStepsCommand"/>.
    /// </summary>
    /// <remarks>
    /// Структурные проверки: непустой список, отсутствие <c>Guid.Empty</c>.
    /// Полноту покрытия и отсутствие дубликатов проверяет Domain через
    /// <c>Recipe.ReorderSteps</c> — возвращает <c>DISHES.INVALID_STEP_ORDER</c>
    /// или <c>DISHES.STEP_NOT_FOUND</c>.
    /// </remarks>
    public sealed class ReorderRecipeStepsCommandValidator : AbstractValidator<ReorderRecipeStepsCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="ReorderRecipeStepsCommandValidator"/>.
        /// </summary>
        public ReorderRecipeStepsCommandValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");

            RuleFor(x => x.OrderedStepIds)
                .NotNull().WithMessage("Список идентификаторов шагов обязателен.")
                .Must(list => list is not null && list.Count > 0)
                    .WithMessage("Список идентификаторов шагов не должен быть пустым.")
                .Must(list => list is null || list.All(id => id != Guid.Empty))
                    .WithMessage("Список не должен содержать пустых GUID.");
        }
    }
}
