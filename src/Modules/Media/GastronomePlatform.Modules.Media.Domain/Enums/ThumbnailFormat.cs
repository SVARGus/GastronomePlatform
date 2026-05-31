namespace GastronomePlatform.Modules.Media.Domain.Enums
{
    /// <summary>
    /// Формат миниатюры. Хранится как <c>int</c> в БД.
    /// </summary>
    /// <remarks>
    /// На Этапе 2 поддерживается только <see cref="Jpeg"/>. <see cref="WebP"/>
    /// и <see cref="Avif"/> зарезервированы для Этапа 8+ как форматы экономии
    /// трафика; включение требует соответствующей реализации в Image-pipeline.
    /// </remarks>
    public enum ThumbnailFormat
    {
        /// <summary>JPEG — максимальная совместимость со всеми клиентами.</summary>
        Jpeg = 0,

        /// <summary>WebP — ~30% экономия трафика. Появится на Этапе 8+.</summary>
        WebP = 1,

        /// <summary>AVIF — лучший компромисс качество/размер, ограниченная поддержка клиентами. Этап 8+.</summary>
        Avif = 2
    }
}
