namespace GastronomePlatform.Modules.Dishes.Application.Commands.RegenerateCategorySlug
{
    /// <summary>
    /// Результат команды <see cref="RegenerateCategorySlugCommand"/>.
    /// </summary>
    /// <param name="NewSlug">Новый уникальный slug категории.</param>
    public sealed record RegenerateCategorySlugResult(string NewSlug);
}
