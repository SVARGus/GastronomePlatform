using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Application.Snapshots
{
    /// <summary>
    /// Сборщик jsonb-снепшота публичной версии блюда. Вызывается из
    /// <c>PublishDishCommandHandler</c> (UC-DSH-004) перед <c>Dish.Publish(...)</c>:
    /// результирующая строка передаётся в Domain-метод как параметр и сохраняется
    /// в <c>Dish.PublishedVersionData</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Реализация — чистая функция от <see cref="Dish"/> к JSON-строке: никакого I/O,
    /// никаких запросов в БД. Это гарантирует детерминированность снепшота (одна и та же
    /// доменная модель → один и тот же JSON) и упрощает unit-тестирование.
    /// </para>
    /// <para>
    /// Предусловие: на вход подаётся <see cref="Dish"/>, у которого через
    /// <c>IDishRepository.GetByIdWithFullRecipeAsync</c> подгружены <c>Recipe</c>,
    /// все 1:1-связки (<c>Timing</c>, <c>Yield</c>, <c>Nutrition</c>), подколлекции
    /// (<c>Steps</c>, <c>Ingredients</c>) и связки тегов и категорий.
    /// Builder не загружает данные дополнительно.
    /// </para>
    /// <para>
    /// Используется тот же <see cref="System.Text.Json.JsonSerializerOptions"/>,
    /// что и для WebAPI-ответов (см. <see cref="SnapshotJsonOptions.Default"/>) —
    /// формат снепшота и формат API-ответа согласованы по ADR-0012 §5.
    /// </para>
    /// </remarks>
    public interface IPublishedDishSnapshotBuilder
    {
        /// <summary>
        /// Собирает JSON-строку снепшота для переданного блюда.
        /// </summary>
        /// <param name="dish">Полностью загруженный агрегат <see cref="Dish"/>.</param>
        /// <returns>JSON-строка снепшота, готовая к сохранению в <c>jsonb</c>-поле.</returns>
        string Build(Dish dish);
    }
}
