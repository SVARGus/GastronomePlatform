using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Dishes.Application.Queries.GetDishById;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishBySlug
{
    /// <summary>
    /// Запрос публичной карточки блюда по slug (UC-DSH-051).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Анонимный запрос — допускает и гостей, и аутентифицированных пользователей.
    /// В отличие от UC-DSH-050 (по Id), здесь возвращается <b>только</b> snapshot-версия:
    /// slug привязан к публичной карточке, рабочая копия по slug не доступна.
    /// </para>
    /// <para>
    /// Семантика отдачи:
    /// </para>
    /// <list type="bullet">
    ///   <item>Блюдо отсутствует <b>или</b> <c>Status = Archived</c> <b>или</b>
    ///         <c>PublishedVersionData IS NULL</c> → <c>404</c> всем (включая автора).</item>
    ///   <item>Есть снепшот → отдаётся публичная версия. Для автора/admin при наличии
    ///         правок в рабочем слое флаг <c>HasUnsavedChanges = true</c>.</item>
    /// </list>
    /// <para>
    /// Возвращает тот же DTO <see cref="DishDetailDto"/>, что UC-DSH-050.
    /// </para>
    /// </remarks>
    /// <param name="Slug">URL-friendly идентификатор блюда.</param>
    public sealed record GetDishBySlugQuery(string Slug) : IQuery<DishDetailDto>;
}
