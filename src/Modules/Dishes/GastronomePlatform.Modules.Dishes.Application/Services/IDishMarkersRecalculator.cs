using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Application.Services
{
    /// <summary>
    /// Сервис пересчёта денормализованных маркеров блюда (<see cref="Dish.AllergensMask"/>,
    /// <see cref="Dish.HasUnverifiedAllergens"/>, авто-clear <see cref="Dish.DietLabelsMask"/>).
    /// </summary>
    /// <remarks>
    /// Выносит из командных хендлеров повторяющуюся подготовку аргументов перед вызовом
    /// доменного метода <see cref="Dish.RecalculateDishMarkers"/>: собирает идентификаторы
    /// catalog-ингредиентов из текущего состава рецепта, запрашивает маркеры через
    /// <see cref="GastronomePlatform.Modules.Dishes.Domain.Repositories.IIngredientRepository.GetMarkersByIdsAsync"/>
    /// и передаёт словарь в Domain. Используется в хендлерах UC-DSH-030..032 (Add catalog/freeform,
    /// Update, Remove ингредиента рецепта).
    /// </remarks>
    public interface IDishMarkersRecalculator
    {
        /// <summary>
        /// Пересчитывает маркеры блюда на основе актуального состава рецепта.
        /// </summary>
        /// <param name="dish">Агрегат блюда с уже изменённым составом рецепта.
        /// Загружен через <c>GetByIdWithFullRecipeAsync</c>, что гарантирует
        /// наличие <c>Recipe.Ingredients</c>.</param>
        /// <param name="utcNow">Текущее время в UTC, передаётся в Domain для
        /// <see cref="Dish.UpdatedAt"/> и доменного события auto-correct.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task RecalculateAsync(Dish dish, DateTimeOffset utcNow, CancellationToken cancellationToken = default);
    }
}
