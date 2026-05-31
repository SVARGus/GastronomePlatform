namespace GastronomePlatform.Modules.Media.Domain.Enums
{
    /// <summary>
    /// Категория данных медиафайла для compliance с 152-ФЗ.
    /// Хранится как <c>int</c> в БД.
    /// </summary>
    /// <remarks>
    /// Маршрутизация по регионам хранения по этому полю — задача Этапа 8+.
    /// Сейчас поле используется политикой <c>POL-002 Media Access Policy</c>
    /// для разграничения доступа: <see cref="Public"/> — отдаём всем,
    /// <see cref="Personal"/> — оригиналы только авторизованным.
    /// </remarks>
    public enum MediaDataCategory
    {
        /// <summary>Публичный контент: фотографии блюд, шагов рецепта, иконки категорий.</summary>
        Public = 0,

        /// <summary>Персональные данные: аватары пользователей, документы.</summary>
        Personal = 1
    }
}
