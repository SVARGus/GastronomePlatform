namespace GastronomePlatform.Modules.Dishes.Application.Commands.CreateCategory
{
    /// <summary>
    /// Результат команды <see cref="CreateCategoryCommand"/>.
    /// </summary>
    /// <param name="Id">Идентификатор созданной категории.</param>
    /// <param name="Slug">Сгенерированный уникальный slug.</param>
    public sealed record CreateCategoryResult(Guid Id, string Slug);
}
