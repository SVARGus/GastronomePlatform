using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.SearchTagsAutocomplete
{
    /// <summary>
    /// Запрос автокомплита тегов по префиксу (UC-DSH-060). Возвращает теги,
    /// чей нормализованный <c>NormalizedName</c> начинается с присланной подстроки,
    /// в порядке убывания <c>UsageCount</c>.
    /// </summary>
    /// <remarks>
    /// Анонимный публичный эндпоинт. Поиск нормализуется через
    /// <c>TagNameNormalizer</c> — клиент может слать «Без Глютена » или «без глютена».
    /// Пустой результат — допустимый.
    /// </remarks>
    /// <param name="Query">Подстрока для поиска (1..50 символов до нормализации).</param>
    /// <param name="Limit">Максимальное число возвращаемых тегов (1..50, дефолт — 10).</param>
    public sealed record SearchTagsAutocompleteQuery(string Query, int Limit) : IQuery<IReadOnlyList<TagDto>>;
}
