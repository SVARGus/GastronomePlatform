namespace GastronomePlatform.Modules.Dishes.Application.Commands.AddFreeformIngredientToRecipe
{
    /// <summary>
    /// Результат успешного выполнения команды <see cref="AddFreeformIngredientToRecipeCommand"/>.
    /// </summary>
    /// <param name="Id">Идентификатор созданной позиции <c>RecipeIngredient</c>.</param>
    public sealed record AddFreeformIngredientToRecipeResult(Guid Id);
}
