using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Media.Application.Abstractions;
using SkiaSharp;

namespace GastronomePlatform.Modules.Media.Infrastructure.Processing
{
    /// <summary>
    /// Реализация <see cref="IImageProcessor"/> на основе библиотеки <c>SkiaSharp</c> (Apache 2).
    /// CPU-bound операции выполняются через <c>Task.Run</c> для освобождения потока ASP.NET Core.
    /// </summary>
    public sealed class ImageProcessor : IImageProcessor
    {
        /// <inheritdoc/>
        public Result<ImageInfo> GetImageInfo(byte[] content, string contentType)
        {
            try
            {
                using var bitmap = SKBitmap.Decode(content);

                if (bitmap is null)
                {
                    return Error.Validation(
                        "IMAGE.INVALID_FORMAT",
                        "Файл не является корректным изображением.");
                }

                return new ImageInfo(bitmap.Width, bitmap.Height);
            }
            catch (Exception)
            {
                return Error.Validation(
                    "IMAGE.INVALID_FORMAT",
                    "Файл не является корректным изображением.");
            }
        }

        /// <inheritdoc/>
        public Task<Result<ThumbnailData>> GenerateMediumThumbnailAsync(
            byte[] content,
            string contentType,
            int targetSize,
            int jpegQuality,
            CancellationToken ct = default)
            => Task.Run(() => GenerateThumbnail(content, targetSize, jpegQuality), ct);

        private static Result<ThumbnailData> GenerateThumbnail(
            byte[] content,
            int targetSize,
            int jpegQuality)
        {
            try
            {
                using var bitmap = SKBitmap.Decode(content);

                if (bitmap is null)
                {
                    return Error.Failure(
                        "IMAGE.THUMBNAIL_FAILED",
                        "Не удалось создать миниатюру изображения.");
                }

                var scale = Math.Min((float)targetSize / bitmap.Width, (float)targetSize / bitmap.Height);
                var newWidth  = Math.Max(1, (int)(bitmap.Width  * scale));
                var newHeight = Math.Max(1, (int)(bitmap.Height * scale));

                using var resized = bitmap.Resize(
                    new SKImageInfo(newWidth, newHeight),
                    new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));

                if (resized is null)
                {
                    return Error.Failure(
                        "IMAGE.THUMBNAIL_FAILED",
                        "Не удалось изменить размер изображения.");
                }

                using var skImage = SKImage.FromBitmap(resized);
                using var data    = skImage.Encode(SKEncodedImageFormat.Jpeg, jpegQuality);

                return new ThumbnailData(data.ToArray(), newWidth, newHeight);
            }
            catch (Exception)
            {
                return Error.Failure(
                    "IMAGE.THUMBNAIL_FAILED",
                    "Не удалось создать миниатюру изображения.");
            }
        }
    }
}
