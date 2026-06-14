using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetIngredientById
{
    /// <summary>
    /// Запрос карточки ингредиента по идентификатору (UC-DSH-063).
    /// Анонимный публичный эндпоинт. Возвращает в том числе и неактивные ингредиенты —
    /// флаг <c>IsActive</c> приходит в DTO, UI решает, как отображать.
    /// </summary>
    /// <param name="IngredientId">Идентификатор ингредиента.</param>
    public sealed record GetIngredientByIdQuery(Guid IngredientId) : IQuery<IngredientDto>;
}
