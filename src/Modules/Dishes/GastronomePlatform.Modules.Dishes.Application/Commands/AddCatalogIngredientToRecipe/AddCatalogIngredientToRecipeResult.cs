namespace GastronomePlatform.Modules.Dishes.Application.Commands.AddCatalogIngredientToRecipe
{
    /// <summary>
    /// Результат успешного выполнения команды <see cref="AddCatalogIngredientToRecipeCommand"/>.
    /// </summary>
    /// <param name="Id">Идентификатор созданной позиции <c>RecipeIngredient</c>.</param>
    public sealed record AddCatalogIngredientToRecipeResult(Guid Id);
}
