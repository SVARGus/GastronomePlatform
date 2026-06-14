namespace GastronomePlatform.Modules.Dishes.Application.Commands.CreateIngredient
{
    /// <summary>
    /// Результат команды <see cref="CreateIngredientCommand"/> — идентификатор созданного ингредиента.
    /// </summary>
    /// <param name="Id">Идентификатор новой записи в справочнике.</param>
    public sealed record CreateIngredientResult(Guid Id);
}
