using GastronomePlatform.Modules.Media.Application.Abstractions;
using GastronomePlatform.Modules.Media.Domain.Constants;
using GastronomePlatform.Modules.Media.Domain.Enums;

namespace GastronomePlatform.Modules.Media.Infrastructure.Storage
{
    /// <summary>
    /// Реализация <see cref="IStorageKeyGenerator"/>.
    /// Генерирует детерминированные пути файлов в хранилище на основе категории,
    /// типа сущности и идентификатора файла.
    /// </summary>
    /// <remarks>
    /// Структура путей:
    /// <list type="bullet">
    ///   <item>Оригиналы: <c>public/{folder}/{year}/{month}/{mediaId}.{ext}</c> — для высоконагруженных типов (Dish, RecipeStep)</item>
    ///   <item>Оригиналы: <c>public/{folder}/{mediaId}.{ext}</c> — для системных типов (CategoryIcon, IngredientImage)</item>
    ///   <item>Оригиналы: <c>personal/{folder}/{mediaId}.{ext}</c> — для Personal-категории (UserAvatar)</item>
    ///   <item>Миниатюры: <c>thumbnails/{category}/{folder}/{mediaId}_{size}.{format}</c></item>
    /// </list>
    /// </remarks>
    public sealed class StorageKeyGenerator : IStorageKeyGenerator
    {
        /// <inheritdoc/>
        public string Generate(
            MediaDataCategory category,
            string entityType,
            Guid mediaId,
            string extension)
        {
            var categoryPrefix = category == MediaDataCategory.Personal ? "personal" : "public";
            var folder = GetEntityFolder(entityType);

            if (UsesDateSegment(entityType))
            {
                var now = DateTimeOffset.UtcNow;
                return $"{categoryPrefix}/{folder}/{now.Year}/{now.Month:D2}/{mediaId}.{extension}";
            }

            return $"{categoryPrefix}/{folder}/{mediaId}.{extension}";
        }

        /// <inheritdoc/>
        public string GenerateThumbnail(
            MediaDataCategory category,
            string entityType,
            Guid mediaId,
            ThumbnailSize size,
            ThumbnailFormat format)
        {
            var categoryPrefix = category == MediaDataCategory.Personal ? "personal" : "public";
            var folder = GetEntityFolder(entityType);
            var sizeSuffix = size.ToString().ToLowerInvariant();
            var formatExtension = GetFormatExtension(format);

            return $"thumbnails/{categoryPrefix}/{folder}/{mediaId}_{sizeSuffix}.{formatExtension}";
        }

        private static string GetEntityFolder(string entityType) => entityType switch
        {
            MediaEntityTypes.DISH             => "dishes",
            MediaEntityTypes.RECIPE_STEP      => "recipe-steps",
            MediaEntityTypes.CATEGORY_ICON    => "category-icons",
            MediaEntityTypes.INGREDIENT_IMAGE => "ingredient-images",
            MediaEntityTypes.USER_AVATAR      => "user-avatars",
            _                                 => entityType.ToLowerInvariant()
        };

        private static bool UsesDateSegment(string entityType) =>
            entityType is MediaEntityTypes.DISH or MediaEntityTypes.RECIPE_STEP;

        private static string GetFormatExtension(ThumbnailFormat format) => format switch
        {
            ThumbnailFormat.Jpeg => "jpg",
            ThumbnailFormat.WebP => "webp",
            ThumbnailFormat.Avif => "avif",
            _                   => "jpg"
        };
    }
}
