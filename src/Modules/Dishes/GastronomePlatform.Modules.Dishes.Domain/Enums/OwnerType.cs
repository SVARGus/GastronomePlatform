namespace GastronomePlatform.Modules.Dishes.Domain.Enums
{
    /// <summary>
    /// Тип автора-владельца блюда. Денормализуется из ролей пользователя
    /// на момент публикации — при последующей смене роли автора старые блюда
    /// сохраняют тот тип, который был зафиксирован в момент публикации.
    /// Хранится как <c>int</c> в БД. Используется в <c>Dish.OwnerType</c>.
    /// </summary>
    public enum OwnerType
    {
        /// <summary>Обычный пользователь / блогер.</summary>
        User = 0,

        /// <summary>Самозанятый повар.</summary>
        Chef = 1,

        /// <summary>Ресторан.</summary>
        Restaurant = 2,

        /// <summary>Бренд / производитель (зарезервировано на будущее).</summary>
        Brand = 3
    }
}
