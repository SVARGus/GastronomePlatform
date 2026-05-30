using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishById
{
    /// <summary>
    /// Запрос публичной карточки блюда по идентификатору (UC-DSH-050).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Анонимный запрос — допускает и гостей, и аутентифицированных пользователей.
    /// Семантика отдачи зависит от наличия <c>Dish.PublishedVersionData</c> и
    /// от того, является ли текущий пользователь автором или администратором:
    /// </para>
    /// <list type="bullet">
    ///   <item>Есть снепшот → отдаётся публичная версия; флаг
    ///         <c>HasUnsavedChanges</c> для автора/admin сигнализирует
    ///         о наличии правок в рабочем слое.</item>
    ///   <item>Снепшота нет, текущий пользователь — автор/admin → отдаётся
    ///         рабочая версия с <c>IsPublishedVersion = false</c>.</item>
    ///   <item>Снепшота нет, текущий пользователь — гость или другой
    ///         пользователь → <c>404</c>.</item>
    ///   <item><c>Status = Archived</c> → <c>404</c> всем (admin-доступ
    ///         к архиву появится на Этапе 8+).</item>
    /// </list>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    public sealed record GetDishByIdQuery(Guid DishId) : IQuery<DishDetailDto>;
}
