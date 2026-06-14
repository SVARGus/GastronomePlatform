using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.DeactivateIngredient
{
    /// <summary>
    /// Команда деактивации ингредиента (UC-DSH-112). Переключает <c>IsActive = false</c>;
    /// существующие связки <c>RecipeIngredient</c> сохраняются. Авторизация — роль <c>Admin</c>.
    /// </summary>
    /// <remarks>
    /// Идемпотентна: повторный вызов на уже неактивном ингредиенте возвращает <c>204</c>.
    /// Для повторной активации специальный UC на Этапе 2 не выделен — Domain-метод
    /// <c>Ingredient.Activate()</c> готов и может быть подключён по требованию.
    /// </remarks>
    /// <param name="IngredientId">Идентификатор деактивируемого ингредиента.</param>
    public sealed record DeactivateIngredientCommand(Guid IngredientId) : ICommand;
}
