using GastronomePlatform.Common.Domain.Primitives;

namespace GastronomePlatform.Modules.Dishes.Domain.Entities
{
    /// <summary>
    /// Категория каталога блюд. Поддерживает иерархию через self-reference <see cref="ParentId"/>.
    /// Создаётся и управляется администратором (UC-DSH-101..105).
    /// </summary>
    /// <remarks>
    /// Доменные инварианты «глубина ≤ 3 уровней» и «нет циклов в <see cref="ParentId"/>»
    /// проверяются на уровне Application — там есть доступ к запросам по таблице.
    /// На уровне Domain эти проверки невозможны (одна сущность не знает структуру всего дерева).
    /// </remarks>
    public sealed class Category : Entity<Guid>
    {
        #region Properties

        /// <summary>
        /// Отображаемое имя категории. Примеры: «Супы», «Грузинская кухня».
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// URL-friendly идентификатор категории. Используется в публичных ссылках
        /// (UC-DSH-059 GetCategoryBySlug). Уникальный в рамках всего справочника.
        /// Примеры: <c>supy</c>, <c>gruzinskaya-kuhnya</c>.
        /// </summary>
        public string Slug { get; private set; } = string.Empty;

        /// <summary>
        /// Идентификатор родительской категории. <see langword="null"/> для корневых категорий.
        /// </summary>
        public Guid? ParentId { get; private set; }

        /// <summary>
        /// Порядок отображения внутри одного уровня иерархии.
        /// Меньшее значение — раньше в списке.
        /// </summary>
        public int Order { get; private set; }

        /// <summary>
        /// Идентификатор файла-иконки в модуле Media. <see langword="null"/>, если иконка не задана.
        /// Логическая ссылка без FK-constraint на уровне БД (кросс-модульно).
        /// </summary>
        public Guid? IconMediaId { get; private set; }

        /// <summary>
        /// Признак активности категории. <see langword="false"/> — категория скрыта в каталоге,
        /// но физически не удалена (мягкое удаление). По умолчанию <see langword="true"/>.
        /// </summary>
        public bool IsActive { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Конструктор без параметров для EF Core.
        /// EF Core использует его при материализации объектов из БД.
        /// </summary>
        private Category() : base() { }

        /// <summary>
        /// Создаёт новый экземпляр <see cref="Category"/>.
        /// Используется только из фабричного метода <see cref="Create"/>.
        /// </summary>
        /// <param name="name">Отображаемое имя категории.</param>
        /// <param name="slug">URL-friendly идентификатор.</param>
        /// <param name="parentId">Идентификатор родителя. <see langword="null"/> для корневых категорий.</param>
        /// <param name="order">Порядок отображения внутри уровня иерархии.</param>
        private Category(
            string name,
            string slug,
            Guid? parentId,
            int order)
            : base(Guid.NewGuid())
        {
            Name = name;
            Slug = slug;
            ParentId = parentId;
            Order = order;
            IsActive = true;
            IconMediaId = null;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Создаёт новую категорию. Slug ожидается уже сгенерированным и проверенным
        /// на уникальность на уровне команды. Глубина иерархии и отсутствие циклов
        /// также проверяются на уровне Application.
        /// </summary>
        /// <param name="name">Отображаемое имя категории.</param>
        /// <param name="slug">URL-friendly идентификатор (lowercase, дефисы вместо пробелов).</param>
        /// <param name="parentId">Идентификатор родителя. <see langword="null"/> — категория верхнего уровня.</param>
        /// <param name="order">Порядок отображения внутри уровня иерархии.</param>
        /// <returns>Новый экземпляр <see cref="Category"/>.</returns>
        public static Category Create(
            string name,
            string slug,
            Guid? parentId,
            int order)
        {
            return new Category(name, slug, parentId, order);
        }

        #endregion
    }
}
