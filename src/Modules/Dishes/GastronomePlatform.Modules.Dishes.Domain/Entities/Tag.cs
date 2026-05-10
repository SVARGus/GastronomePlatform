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
    /// Нормализация (lowercase + trim + транслитерация) выполняется на уровне
    /// Application; в Domain поступает уже готовое <see cref="NormalizedName"/>.
    /// </remarks>
    public sealed class Tag : Entity<Guid>
    {
        #region Properties

        /// <summary>
        /// Оригинальное написание тега, как ввёл пользователь: «Без глютена», «Веганское».
        /// Отображается в UI.
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Нормализованная форма имени: lowercase, trim, транслитерация.
        /// Используется для дедупликации и поиска: «Без глютена» и «без глютена»
        /// должны мапиться на одну запись.
        /// </summary>
        public string NormalizedName { get; private set; } = string.Empty;

        /// <summary>
        /// Количество блюд, к которым прикреплён этот тег.
        /// Обновляется атомарно при добавлении/удалении тега у блюда (UC-DSH-008).
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
        /// <param name="createdByUserId">Идентификатор автора. <see langword="null"/> для системных тегов.</param>
        /// <param name="createdAt">Дата и время создания (UTC).</param>
        private Tag(
            string name,
            string normalizedName,
            Guid? createdByUserId,
            DateTimeOffset createdAt)
            : base(Guid.NewGuid())
        {
            Name = name;
            NormalizedName = normalizedName;
            CreatedByUserId = createdByUserId;
            CreatedAt = createdAt;
            UsageCount = 0;
            IsVerified = false;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Создаёт новый тег. Валидация имени и нормализация ожидаются на уровне команды
        /// (FluentValidation + сервис нормализации в Application).
        /// </summary>
        /// <param name="name">Оригинальное написание тега, как ввёл пользователь.</param>
        /// <param name="normalizedName">Готовая нормализованная форма имени.</param>
        /// <param name="createdByUserId">Идентификатор автора. <see langword="null"/> для системных тегов.</param>
        /// <param name="createdAt">Дата и время создания (UTC).</param>
        /// <returns>Новый экземпляр <see cref="Tag"/>.</returns>
        public static Tag Create(
            string name,
            string normalizedName,
            Guid? createdByUserId,
            DateTimeOffset createdAt)
        {
            return new Tag(name, normalizedName, createdByUserId, createdAt);
        }

        #endregion
    }
}
