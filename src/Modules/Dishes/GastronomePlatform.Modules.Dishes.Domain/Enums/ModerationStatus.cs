namespace GastronomePlatform.Modules.Dishes.Domain.Enums
{
    /// <summary>
    /// Результат модерации блюда. На Этапе 2 — дефолт <c>Approved</c>;
    /// реальная модерация появится на Этапе 8.
    /// Хранится как <c>int</c> в БД. Используется в <c>Dish.ModerationStatus</c>.
    /// </summary>
    public enum ModerationStatus
    {
        /// <summary>Одобрено (дефолт на Этапе 2).</summary>
        Approved = 0,

        /// <summary>На модерации.</summary>
        Pending = 1,

        /// <summary>Отклонено админом.</summary>
        Rejected = 2,

        /// <summary>Жалобы от пользователей, требует пересмотра.</summary>
        Flagged = 3
    }
}
