using System.Text.Json;
using System.Text.Json.Serialization;

namespace GastronomePlatform.Modules.Dishes.Application.Snapshots
{
    /// <summary>
    /// Опции <see cref="JsonSerializerOptions"/> для сериализации и десериализации
    /// jsonb-снепшота <c>Dish.PublishedVersionData</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// По ADR-0012 §5 Areas of Caution сериализатор jsonb-снепшота и WebAPI должны
    /// использовать согласованные настройки, иначе формат отдаваемого клиенту JSON
    /// и формат, хранящийся в БД, расходятся. На текущем этапе WebAPI настроен через
    /// <c>AddJsonOptions</c> в <c>Program.cs</c>: <see cref="JsonStringEnumConverter"/>
    /// + дефолтная (PascalCase) политика именования свойств. Этот класс хранит те же
    /// настройки, доступные Application-слою без зависимости от ASP.NET.
    /// </para>
    /// <para>
    /// Свойство <see cref="Default"/> возвращает иммутабельный экземпляр опций
    /// (<see cref="JsonSerializerOptions.MakeReadOnly()"/> применяется при первом
    /// обращении) — безопасно использовать как singleton.
    /// </para>
    /// </remarks>
    public static class SnapshotJsonOptions
    {
        private static readonly JsonSerializerOptions _default = CreateDefault();

        /// <summary>
        /// Опции по умолчанию для сериализации/десериализации снепшота:
        /// PascalCase, enum как строки, без отступов, без специальной обработки null
        /// (отсутствующие поля сериализуются как <c>null</c> для совместимости при
        /// расширении DTO в будущем).
        /// </summary>
        public static JsonSerializerOptions Default => _default;

        private static JsonSerializerOptions CreateDefault()
        {
            JsonSerializerOptions options = new()
            {
                WriteIndented = false
            };

            options.Converters.Add(new JsonStringEnumConverter());
            options.MakeReadOnly();

            return options;
        }
    }
}
