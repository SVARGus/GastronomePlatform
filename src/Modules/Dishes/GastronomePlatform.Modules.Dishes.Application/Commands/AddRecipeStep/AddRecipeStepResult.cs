namespace GastronomePlatform.Modules.Dishes.Application.Commands.AddRecipeStep
{
    /// <summary>
    /// Результат успешного выполнения команды <see cref="AddRecipeStepCommand"/>.
    /// </summary>
    /// <param name="Id">Идентификатор созданного <c>RecipeStep</c>.</param>
    public sealed record AddRecipeStepResult(Guid Id);
}
