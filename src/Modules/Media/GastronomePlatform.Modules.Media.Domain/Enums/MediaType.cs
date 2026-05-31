namespace GastronomePlatform.Modules.Media.Domain.Enums
{
    /// <summary>
    /// Тип медиа-контента. Хранится как <c>int</c> в БД.
    /// </summary>
    public enum MediaType
    {
        /// <summary>Изображение. Поддерживается на Этапе 2.</summary>
        Image = 0,

        /// <summary>Видео. Поддержка появится на Этапе 8+.</summary>
        Video = 1
    }
}
