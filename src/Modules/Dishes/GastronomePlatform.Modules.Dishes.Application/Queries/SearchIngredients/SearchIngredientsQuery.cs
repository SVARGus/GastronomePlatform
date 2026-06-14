using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.SearchIngredients
{
    /// <summary>
    /// Запрос поиска активных ингредиентов по префиксу имени (UC-DSH-062).
    /// Case-insensitive префиксный поиск (PostgreSQL <c>ILIKE</c>); ранжирование
    /// по алфавиту. Анонимный публичный эндпоинт.
    /// </summary>
    /// <param name="Query">Префикс имени для поиска.</param>
    /// <param name="Limit">Максимальное число возвращаемых ингредиентов (1..50, дефолт — 20).</param>
    public sealed record SearchIngredientsQuery(string Query, int Limit) : IQuery<IReadOnlyList<IngredientDto>>;
}
