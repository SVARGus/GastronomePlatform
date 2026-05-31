namespace GastronomePlatform.Modules.Media.Domain.Enums
{
    /// <summary>
    /// Номинальный размер миниатюры. Хранится как <c>int</c> в БД.
    /// </summary>
    /// <remarks>
    /// На Этапе 2 фактически генерируется только <see cref="Medium"/>.
    /// Остальные размеры зарезервированы — генерация появится на Этапе 8+
    /// при разнообразии контекстов отображения.
    /// </remarks>
    public enum ThumbnailSize
    {
        /// <summary>150×150. Появится на Этапе 8+.</summary>
        Small = 0,

        /// <summary>400×400. Единственный генерируемый размер на Этапе 2.</summary>
        Medium = 1,

        /// <summary>800×800. Появится на Этапе 8+.</summary>
        Large = 2
    }
}
