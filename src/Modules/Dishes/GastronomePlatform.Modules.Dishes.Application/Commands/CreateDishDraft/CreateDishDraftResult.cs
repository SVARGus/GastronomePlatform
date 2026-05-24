namespace GastronomePlatform.Modules.Dishes.Application.Commands.CreateDishDraft
{
    /// <summary>
    /// Результат успешного выполнения команды <see cref="CreateDishDraftCommand"/>.
    /// </summary>
    /// <param name="Id">Идентификатор созданного блюда.</param>
    /// <param name="Slug">URL-friendly идентификатор блюда (уникальный в рамках платформы).</param>
    public sealed record CreateDishDraftResult(Guid Id, string Slug);
}
