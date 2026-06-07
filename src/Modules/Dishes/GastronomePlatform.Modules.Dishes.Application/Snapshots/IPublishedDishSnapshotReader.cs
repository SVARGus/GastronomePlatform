using GastronomePlatform.Modules.Dishes.Application.Snapshots.Dtos;

namespace GastronomePlatform.Modules.Dishes.Application.Snapshots
{
    /// <summary>
    /// Парсер jsonb-снепшота публичной версии блюда. Симметричный к
    /// <see cref="IPublishedDishSnapshotBuilder"/>: Builder сериализует
    /// агрегат в JSON, Reader восстанавливает структурное представление
    /// для последующего маппинга в публичные DTO.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Используется в UC-DSH-052 (GetDishRecipe) для извлечения рецепта
    /// из <c>Dish.PublishedVersionData</c> и в UC-DSH-050 (GetDishById)
    /// для snapshot-ветки чтения карточки. На Этапе 8+ может пригодиться
    /// для admin-операций по пересборке снепшотов и для UC-DSH-056
    /// (пересчёт ингредиентов на N порций).
    /// </para>
    /// <para>
    /// Чистая функция от JSON-строки к <see cref="PublishedDishSnapshot"/>:
    /// никакого I/O, никаких запросов в БД. Stateless — безопасно
    /// регистрировать как Singleton.
    /// </para>
    /// <para>
    /// Использует тот же <see cref="System.Text.Json.JsonSerializerOptions"/>,
    /// что и Builder (<see cref="SnapshotJsonOptions.Default"/>) — формат
    /// сериализации и десериализации согласован.
    /// </para>
    /// </remarks>
    public interface IPublishedDishSnapshotReader
    {
        /// <summary>
        /// Десериализует JSON-строку снепшота в структурное представление.
        /// </summary>
        /// <param name="snapshotJson">JSON-строка из <c>Dish.PublishedVersionData</c>.</param>
        /// <returns>Распарсенный <see cref="PublishedDishSnapshot"/>.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="snapshotJson"/> равен <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.Text.Json.JsonException">
        /// Снепшот повреждён или не соответствует формату — например,
        /// отсутствует поле-дискриминатор <c>type</c> у ингредиента, либо
        /// отсутствует обязательное поле верхнего уровня. Кейс нештатный
        /// (баг в Publish или ручная порча БД); обрабатывается глобальным
        /// middleware как HTTP 500.
        /// </exception>
        PublishedDishSnapshot Read(string snapshotJson);
    }
}
