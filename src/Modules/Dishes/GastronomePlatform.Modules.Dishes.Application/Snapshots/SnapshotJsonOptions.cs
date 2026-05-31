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
    /// Свойство <see cref="Default"/> возвращает иммутабельный экземпляр опций:
    /// при инициализации вызывается <see cref="JsonSerializerOptions.MakeReadOnly(bool)"/>
    /// с <c>populateMissingResolver: true</c>, чтобы System.Text.Json подставил
    /// дефолтный рефлексионный <see cref="System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver"/>
    /// (без него безпараметровая перегрузка <c>MakeReadOnly()</c> бросает
    /// <see cref="InvalidOperationException"/>). Опции stateless — безопасно
    /// использовать как singleton.
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

            // В .NET 8 безпараметровая JsonSerializerOptions.MakeReadOnly() требует
            // уже установленного TypeInfoResolver, иначе бросает InvalidOperationException.
            // Перегрузка с populateMissingResolver: true подставит DefaultJsonTypeInfoResolver
            // (рефлексионный) при отсутствии явного — это ровно то поведение, которое
            // у нас и подразумевалось «по умолчанию» (нет ни Source-Gen JsonContext,
            // ни кастомного резолвера).
            options.MakeReadOnly(populateMissingResolver: true);

            return options;
        }
    }
}
