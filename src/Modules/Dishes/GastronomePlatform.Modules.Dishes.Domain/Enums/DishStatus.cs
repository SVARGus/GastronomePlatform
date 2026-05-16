namespace GastronomePlatform.Modules.Dishes.Domain.Enums
{
    /// <summary>
    /// Жизненный цикл блюда с точки зрения автора.
    /// Хранится как <c>int</c> в БД. Используется в <c>Dish.Status</c>.
    /// </summary>
    public enum DishStatus
    {
        /// <summary>Черновик. Виден только автору.</summary>
        Draft = 0,

        /// <summary>Опубликовано, видно всем (с учётом ModerationStatus).</summary>
        Published = 1,

        /// <summary>Снято с публикации автором. Может быть возвращено в <c>Published</c>.</summary>
        Unpublished = 2,

        /// <summary>Мягкое удаление. По прямой ссылке — 404, но данные сохранены в БД.</summary>
        Archived = 3
    }
}
