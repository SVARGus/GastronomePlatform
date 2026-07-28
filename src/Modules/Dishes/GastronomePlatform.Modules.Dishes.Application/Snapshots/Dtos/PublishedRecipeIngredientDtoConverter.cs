using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace GastronomePlatform.Modules.Dishes.Application.Snapshots.Dtos
{
    /// <summary>
    /// Конвертер полиморфной позиции рецепта в jsonb-снепшоте. Заменяет атрибутный
    /// полиморфизм (<c>JsonPolymorphic</c>/<c>JsonDerivedType</c>), потому что PostgreSQL
    /// канонизирует <c>jsonb</c> и пересортировывает ключи объекта — дискриминатор
    /// <c>type</c> перестаёт быть первым свойством, а System.Text.Json в .NET 8 требует
    /// его строго первым и падает с <c>NotSupportedException</c> при чтении снепшота из БД.
    /// Конвертер ищет <c>type</c> в любой позиции объекта.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Чтение: объект буферизуется в <see cref="JsonDocument"/> (сотни байт на позицию),
    /// по значению <c>type</c> выбирается конкретный DTO. Запись: производный DTO
    /// сериализуется штатно, затем <c>type</c> дописывается первым свойством —
    /// формат на диске идентичен прежнему атрибутному.
    /// </para>
    /// <para>
    /// На производные типы конвертер не распространяется
    /// (<see cref="JsonConverter{T}"/> совпадает только с базовым типом),
    /// поэтому их штатная десериализация не зацикливается; лишнее свойство
    /// <c>type</c> при этом игнорируется как неизвестное.
    /// </para>
    /// </remarks>
    public sealed class PublishedRecipeIngredientDtoConverter : JsonConverter<PublishedRecipeIngredientDto>
    {
        private const string DISCRIMINATOR_PROPERTY = "type";
        private const string CATALOG_DISCRIMINATOR = "catalog";
        private const string FREEFORM_DISCRIMINATOR = "freeform";

        /// <summary>
        /// Читает позицию рецепта, определяя конкретный тип по свойству <c>type</c>
        /// в любой позиции объекта.
        /// </summary>
        /// <param name="reader">Читатель JSON, спозиционированный на начале объекта.</param>
        /// <param name="typeToConvert">Целевой тип (базовый <see cref="PublishedRecipeIngredientDto"/>).</param>
        /// <param name="options">Опции сериализации.</param>
        /// <returns>Экземпляр конкретного DTO позиции рецепта.</returns>
        public override PublishedRecipeIngredientDto Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);

            if (!document.RootElement.TryGetProperty(DISCRIMINATOR_PROPERTY, out JsonElement discriminator))
            {
                throw new JsonException(
                    $"Позиция рецепта в снепшоте не содержит дискриминатор «{DISCRIMINATOR_PROPERTY}».");
            }

            string? discriminatorValue = discriminator.GetString();

            return discriminatorValue switch
            {
                CATALOG_DISCRIMINATOR => document.RootElement.Deserialize<PublishedCatalogIngredientDto>(options)
                    ?? throw new JsonException("Catalog-позиция снепшота десериализована как null."),
                FREEFORM_DISCRIMINATOR => document.RootElement.Deserialize<PublishedFreeformIngredientDto>(options)
                    ?? throw new JsonException("Freeform-позиция снепшота десериализована как null."),
                _ => throw new JsonException(
                    $"Неизвестный дискриминатор позиции рецепта в снепшоте: «{discriminatorValue}».")
            };
        }

        /// <summary>
        /// Пишет позицию рецепта: производный DTO сериализуется штатно,
        /// дискриминатор <c>type</c> добавляется первым свойством.
        /// </summary>
        /// <param name="writer">Писатель JSON.</param>
        /// <param name="value">Позиция рецепта (конкретный производный DTO).</param>
        /// <param name="options">Опции сериализации.</param>
        public override void Write(
            Utf8JsonWriter writer,
            PublishedRecipeIngredientDto value,
            JsonSerializerOptions options)
        {
            string discriminatorValue = value switch
            {
                PublishedCatalogIngredientDto => CATALOG_DISCRIMINATOR,
                PublishedFreeformIngredientDto => FREEFORM_DISCRIMINATOR,
                _ => throw new JsonException(
                    $"Неизвестный производный тип позиции рецепта: {value.GetType().Name}.")
            };

            JsonNode node = JsonSerializer.SerializeToNode(value, value.GetType(), options)
                ?? throw new JsonException("Позиция рецепта сериализована как null.");

            writer.WriteStartObject();
            writer.WriteString(DISCRIMINATOR_PROPERTY, discriminatorValue);
            foreach (KeyValuePair<string, JsonNode?> property in node.AsObject())
            {
                writer.WritePropertyName(property.Key);
                if (property.Value is null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    property.Value.WriteTo(writer, options);
                }
            }

            writer.WriteEndObject();
        }
    }
}
