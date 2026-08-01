using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishRecipe
{
    /// <summary>
    /// Запрос рецепта блюда по идентификатору (UC-DSH-052).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Эндпоинт защищён политикой <c>VALID_ACTOR</c> — гости получают <c>401</c>.
    /// Просмотр опубликованного рецепта требует гранта <c>FullRecipes</c>
    /// (POL-004); автор блюда и admin проходят без проверки.
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
    ///   <item><paramref name="PreferWorkingVersion"/> = <see langword="true"/> и
    ///         текущий пользователь — автор/admin → отдаётся рабочая версия рецепта
    ///         даже при наличии снепшота (источник данных редактора рецепта).
    ///         Для остальных параметр игнорируется.</item>
    /// </list>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="PreferWorkingVersion">
    /// Запрос рабочей версии вместо снепшота (частичная реализация отложенного
    /// UC-DSH-083). Учитывается только для автора/admin.
    /// </param>
    public sealed record GetDishRecipeQuery(
        Guid DishId,
        bool PreferWorkingVersion = false) : IQuery<DishRecipeDto>;
}
