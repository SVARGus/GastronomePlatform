using System.Text.Json;
using GastronomePlatform.Modules.Dishes.Application.Snapshots.Dtos;

namespace GastronomePlatform.Modules.Dishes.Application.Snapshots
{
    /// <summary>
    /// Реализация <see cref="IPublishedDishSnapshotReader"/>. Десериализует
    /// jsonb-снепшот из <c>Dish.PublishedVersionData</c> в полиморфные DTO
    /// через <see cref="SnapshotJsonOptions.Default"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stateless — безопасно регистрировать как Singleton в DI-контейнере.
    /// Симметрично <see cref="PublishedDishSnapshotBuilder"/>.
    /// </para>
    /// <para>
    /// Полиморфизм <see cref="PublishedRecipeIngredientDto"/> по полю-дискриминатору
    /// <c>type</c> обрабатывается атрибутами <c>JsonPolymorphic</c>/<c>JsonDerivedType</c>
    /// автоматически — никакой дополнительной настройки в Reader не требуется.
    /// </para>
    /// </remarks>
    public sealed class PublishedDishSnapshotReader : IPublishedDishSnapshotReader
    {
        /// <inheritdoc/>
        public PublishedDishSnapshot Read(string snapshotJson)
        {
            ArgumentNullException.ThrowIfNull(snapshotJson);

            PublishedDishSnapshot? snapshot = JsonSerializer.Deserialize<PublishedDishSnapshot>(
                snapshotJson,
                SnapshotJsonOptions.Default);

            return snapshot is null
                ? throw new JsonException(
                    "Снепшот десериализован как null — невалидный формат jsonb-снепшота блюда.")
                : snapshot;
        }
    }
}
