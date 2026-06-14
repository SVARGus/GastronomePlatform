using GastronomePlatform.Common.Domain.Primitives;

namespace GastronomePlatform.Modules.Dishes.Domain.Entities
{
    /// <summary>
    /// Пользовательский тег для блюд. Создаётся при первом упоминании
    /// и переиспользуется через дедупликацию по <see cref="NormalizedName"/>.
    /// </summary>
    /// <remarks>
    /// На Этапе 2 теги создаются через <c>UC-DSH-008 SetTags</c>: для каждого имени
    /// проверяется наличие записи с таким же <see cref="NormalizedName"/>;
    /// если есть — переиспользуется, иначе создаётся новая.
    /// Нормализация (lowercase + trim + collapse внутренних пробелов) выполняется
    /// на уровне Application; в Domain поступает уже готовое <see cref="NormalizedName"/>.
    /// </remarks>
    public sealed class Tag : Entity<Guid>
    {
        #region Limits

        /// <summary>Минимальная длина <see cref="Name"/> после trim.</summary>
        public const int MIN_NAME_LENGTH = 1;

        /// <summary>Максимальная длина <see cref="Name"/>.</summary>
        public const int MAX_NAME_LENGTH = 50;

        /// <summary>
        /// Максимальная длина <see cref="Slug"/>. Больше, чем у <see cref="Name"/>,
        /// потому что транслитерация кириллицы расширяет строку
        /// (например, «щ» → «sch»).
        /// </summary>
        public const int MAX_SLUG_LENGTH = 80;

        #endregion

        #region Properties

        /// <summary>
        /// Оригинальное написание тега, как ввёл пользователь: «Без глютена», «Веганское».
        /// Отображается в UI.
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Нормализованная форма имени: lowercase + trim + collapse внутренних пробелов.
        /// Используется для дедупликации и поиска: «Без глютена» и «без глютена»
        /// должны мапиться на одну запись. Уникальна в рамках всего справочника.
        /// </summary>
        public string NormalizedName { get; private set; } = string.Empty;

        /// <summary>
        /// URL-friendly идентификатор тега. Используется в публичных ссылках
        /// (страницы <c>/tags/vegan</c>, <c>/tags/bez-glyutena</c>). Уникален в рамках
        /// справочника. Генерируется на уровне Application через <c>ISlugGenerator</c>
        /// (ASCII-транслит кириллицы); коллизии разрешаются суффиксом <c>-N</c>.
        /// </summary>
        public string Slug { get; private set; } = string.Empty;

        /// <summary>
        /// Количество блюд, к которым прикреплён этот тег.
        /// Обновляется атомарно через <see cref="IncrementUsage"/> и
        /// <see cref="DecrementUsage"/> при изменении набора тегов блюда (UC-DSH-008).
        /// </summary>
        public int UsageCount { get; private set; }

        /// <summary>
        /// Признак «одобрен администратором для общего автокомплита».
        /// Если <see langword="true"/> — тег появляется в автокомплите даже при низком <see cref="UsageCount"/>.
        /// </summary>
        public bool IsVerified { get; private set; }

        /// <summary>
        /// Идентификатор пользователя, который первым создал тег.
        /// <see langword="null"/> для системных тегов из seed-данных.
        /// Логическая ссылка без FK на уровне БД (изоляция модулей).
        /// </summary>
        public Guid? CreatedByUserId { get; private set; }

        /// <summary>
        /// Дата и время создания тега (UTC).
        /// </summary>
        public DateTimeOffset CreatedAt { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Конструктор без параметров для EF Core.
        /// EF Core использует его при материализации объектов из БД.
        /// </summary>
        private Tag() : base() { }

        /// <summary>
        /// Создаёт новый экземпляр <see cref="Tag"/>.
        /// Используется только из фабричного метода <see cref="Create"/>.
        /// </summary>
        /// <param name="name">Оригинальное написание тега.</param>
        /// <param name="normalizedName">Нормализованная форма имени для дедупликации.</param>
        /// <param name="slug">URL-friendly идентификатор тега.</param>
        /// <param name="createdByUserId">Идентификатор автора. <see langword="null"/> для системных тегов.</param>
        /// <param name="createdAt">Дата и время создания (UTC).</param>
        private Tag(
            string name,
            string normalizedName,
            string slug,
            Guid? createdByUserId,
            DateTimeOffset createdAt)
            : base(Guid.NewGuid())
        {
            Name = name;
            NormalizedName = normalizedName;
            Slug = slug;
            CreatedByUserId = createdByUserId;
            CreatedAt = createdAt;
            UsageCount = 0;
            IsVerified = false;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Создаёт новый тег. Валидация имени, нормализация и генерация slug ожидаются
        /// на уровне команды (FluentValidation + <c>ISlugGenerator</c> в Application).
        /// </summary>
        /// <param name="name">Оригинальное написание тега, как ввёл пользователь.</param>
        /// <param name="normalizedName">Готовая нормализованная форма имени.</param>
        /// <param name="slug">Готовый URL-friendly идентификатор (уникальный в справочнике).</param>
        /// <param name="createdByUserId">Идентификатор автора. <see langword="null"/> для системных тегов.</param>
        /// <param name="createdAt">Дата и время создания (UTC).</param>
        /// <returns>Новый экземпляр <see cref="Tag"/>.</returns>
        public static Tag Create(
            string name,
            string normalizedName,
            string slug,
            Guid? createdByUserId,
            DateTimeOffset createdAt)
        {
            return new Tag(name, normalizedName, slug, createdByUserId, createdAt);
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Увеличивает <see cref="UsageCount"/> на 1. Вызывается из Application Handler-а
        /// при добавлении тега к блюду (UC-DSH-008 SetTags).
        /// </summary>
        public void IncrementUsage()
        {
            UsageCount++;
        }

        /// <summary>
        /// Уменьшает <see cref="UsageCount"/> на 1, но не ниже 0. Вызывается из Application
        /// Handler-а при удалении тега у блюда (UC-DSH-008 SetTags).
        /// </summary>
        /// <remarks>
        /// Защита от ухода в отрицательное значение — на случай рассинхронизации
        /// денормализованного счётчика с фактическим количеством связок <c>DishTag</c>.
        /// </remarks>
        public void DecrementUsage()
        {
            if (UsageCount > 0)
            {
                UsageCount--;
            }
        }

        #endregion
    }
}
