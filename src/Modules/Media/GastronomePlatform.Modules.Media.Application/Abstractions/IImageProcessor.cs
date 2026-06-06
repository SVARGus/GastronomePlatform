using GastronomePlatform.Common.Domain.Results;

namespace GastronomePlatform.Modules.Media.Application.Abstractions
{
    /// <summary>
    /// Абстракция обработки изображений.
    /// Реализация в Infrastructure использует <c>SixLabors.ImageSharp v2.1.x</c> (Apache 2.0).
    /// </summary>
    public interface IImageProcessor
    {
        /// <summary>
        /// Читает размеры изображения без полной декодировки.
        /// </summary>
        /// <param name="content">Байты файла изображения.</param>
        /// <param name="contentType">MIME-тип файла.</param>
        /// <returns>
        /// <see cref="ImageInfo"/> с шириной и высотой при успехе;
        /// ошибка если файл повреждён или не является изображением.
        /// </returns>
        Result<ImageInfo> GetImageInfo(byte[] content, string contentType);

        /// <summary>
        /// Генерирует Medium-миниатюру: изображение масштабируется до <paramref name="targetSize"/>
        /// пикселей по большей стороне с сохранением пропорций, затем конвертируется в JPEG.
        /// </summary>
        /// <param name="content">Байты исходного изображения.</param>
        /// <param name="contentType">MIME-тип исходного файла.</param>
        /// <param name="targetSize">Максимальная сторона миниатюры в пикселях (например, 400).</param>
        /// <param name="jpegQuality">Качество JPEG-кодирования (0–100).</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>
        /// <see cref="ThumbnailData"/> с байтами JPEG, фактическими размерами при успехе;
        /// ошибка если обработка изображения не удалась.
        /// </returns>
        Task<Result<ThumbnailData>> GenerateMediumThumbnailAsync(
            byte[] content,
            string contentType,
            int targetSize,
            int jpegQuality,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Метаданные изображения (размеры), прочитанные из заголовка файла.
    /// </summary>
    /// <param name="Width">Ширина изображения в пикселях.</param>
    /// <param name="Height">Высота изображения в пикселях.</param>
    public sealed record ImageInfo(int Width, int Height);

    /// <summary>
    /// Результат генерации миниатюры.
    /// </summary>
    /// <param name="Content">Байты JPEG-миниатюры.</param>
    /// <param name="Width">Фактическая ширина миниатюры в пикселях.</param>
    /// <param name="Height">Фактическая высота миниатюры в пикселях.</param>
    public sealed record ThumbnailData(byte[] Content, int Width, int Height);
}
