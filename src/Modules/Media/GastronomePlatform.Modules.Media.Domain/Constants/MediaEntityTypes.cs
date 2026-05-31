namespace GastronomePlatform.Modules.Media.Domain.Constants
{
    /// <summary>
    /// Константы типов сущностей-владельцев медиафайлов.
    /// Хранятся в поле <c>MediaFile.EntityType</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Модули, работающие с медиа (Dishes, Users, …), <b>никогда не передают
    /// магические строки</b> — только эти константы.
    /// </para>
    /// <para>
    /// При выделении Media в микросервис константы уходят вместе с модулем,
    /// потребители получают их через NuGet-пакет контрактов Media.
    /// </para>
    /// </remarks>
    public static class MediaEntityTypes
    {
        /// <summary>Главное фото блюда (<c>Dish.MainImageId</c>).</summary>
        public const string DISH = "Dish";

        /// <summary>Иллюстрация шага рецепта (<c>RecipeStep.ImageMediaId</c>).</summary>
        public const string RECIPE_STEP = "RecipeStep";

        /// <summary>Иконка категории каталога. Системный файл, OwnerUserId = NULL.</summary>
        public const string CATEGORY_ICON = "CategoryIcon";

        /// <summary>Фотография ингредиента справочника. Системный файл, OwnerUserId = NULL.</summary>
        public const string INGREDIENT_IMAGE = "IngredientImage";

        /// <summary>Аватар пользователя (<c>UserProfile.AvatarMediaId</c>). Personal-категория.</summary>
        public const string USER_AVATAR = "UserAvatar";

        // TODO: Этап 6+
        //   public const string BUSINESS_LOGO = "BusinessLogo";
        //   public const string USER_DOCUMENT = "UserDocument";

        /// <summary>
        /// Полный список известных типов сущностей. Используется для проверки
        /// <c>AttachToEntity</c> — отклоняет неизвестные значения с ошибкой
        /// <c>MediaErrors.UnknownEntityType</c>.
        /// </summary>
        public static readonly IReadOnlySet<string> KNOWN_TYPES = new HashSet<string>(StringComparer.Ordinal)
        {
            DISH,
            RECIPE_STEP,
            CATEGORY_ICON,
            INGREDIENT_IMAGE,
            USER_AVATAR
        };
    }
}
