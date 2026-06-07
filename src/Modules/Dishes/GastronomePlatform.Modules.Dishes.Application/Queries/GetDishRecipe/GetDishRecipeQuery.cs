using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishRecipe
{
    /// <summary>
    /// Запрос рецепта блюда по идентификатору (UC-DSH-052).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Эндпоинт защищён политикой <c>VALID_ACTOR</c> — гости получают <c>401</c>.
    /// На Этапе 3+ доступ будет дополнительно ограничен по подписке через
    /// <c>ISubscriptionService</c> (Premium+ для отдельных рецептов).
    /// </para>
    /// <para>
    /// Видимость определяется так же, как в UC-DSH-050:
    /// </para>
    /// <list type="bullet">
    ///   <item>Есть снепшот → отдаётся публичная версия; для автора/admin
    ///         добавляется флаг <c>HasUnsavedChanges</c>.</item>
    ///   <item>Снепшота нет, текущий пользователь — автор/admin → отдаётся
    ///         рабочая версия рецепта.</item>
    ///   <item>Снепшота нет, текущий пользователь — другой пользователь →
    ///         <c>404</c>.</item>
    ///   <item><c>Status = Archived</c> → <c>404</c> всем.</item>
    /// </list>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    public sealed record GetDishRecipeQuery(Guid DishId) : IQuery<DishRecipeDto>;
}
