using GastronomePlatform.Modules.Media.Domain.Constants;
using GastronomePlatform.Modules.Media.Domain.Enums;

namespace GastronomePlatform.Modules.Media.Application.Helpers
{
    /// <summary>
    /// Определяет категорию данных медиафайла на основе типа целевой сущности.
    /// Единственное место маппинга <c>EntityType → DataCategory</c> в Application-слое.
    /// </summary>
    public static class DataCategoryResolver
    {
        /// <summary>
        /// Возвращает <see cref="MediaDataCategory"/> для указанного типа сущности.
        /// </summary>
        /// <param name="entityType">
        /// Тип сущности-владельца (константа из <see cref="MediaEntityTypes"/>).
        /// </param>
        /// <returns>
        /// <see cref="MediaDataCategory.Personal"/> для <c>UserAvatar</c> и других персональных типов;
        /// <see cref="MediaDataCategory.Public"/> для всех прочих типов.
        /// </returns>
        public static MediaDataCategory Resolve(string entityType) => entityType switch
        {
            MediaEntityTypes.USER_AVATAR => MediaDataCategory.Personal,
            _ => MediaDataCategory.Public
        };
    }
}
