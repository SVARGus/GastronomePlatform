using GastronomePlatform.Common.Domain.Primitives;
using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Domain.Entities
{
    /// <summary>
    /// Глобальный справочник продуктов. Одно название — одна запись.
    /// Сорта и виды (3.2%, высший сорт) выносятся в отдельную сущность <see cref="IngredientSpec"/>.
    /// </summary>
    /// <remarks>
    /// Условные инварианты «<see cref="IsLiquid"/> ⇒ <see cref="DensityApprox"/> заполнен»
    /// и «<see cref="IsAllergen"/> ⇒ <see cref="AllergenType"/> заполнен» проверяются:
    /// <list type="bullet">
    ///   <item>На уровне Application — через FluentValidation.</item>
    ///   <item>На уровне БД — через CHECK-constraints (defense in depth).</item>
    /// </list>
    /// На Этапе 2 запись создаётся seed-данными или admin-командой.
    /// </remarks>
    public sealed class Ingredient : Entity<Guid>
    {
        #region Limits

        /// <summary>Минимальная длина <see cref="Name"/> после trim.</summary>
        public const int MIN_NAME_LENGTH = 2;

        /// <summary>Максимальная длина <see cref="Name"/>.</summary>
        public const int MAX_NAME_LENGTH = 200;

        /// <summary>Максимальная длина <see cref="PluralName"/>.</summary>
        public const int MAX_PLURAL_NAME_LENGTH = 200;

        /// <summary>Максимальная длина <see cref="Description"/>.</summary>
        public const int MAX_DESCRIPTION_LENGTH = 4000;

        #endregion

        #region Properties

        /// <summary>
        /// Название ингредиента. Уникально в рамках всей таблицы.
        /// Пример: «Мука пшеничная», «Молоко цельное».
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Форма для родительного падежа. Используется в текстовой генерации
        /// строк рецепта вида «200 г муки», «1 л молока». Опционально.
        /// </summary>
        public string? PluralName { get; private set; }

        /// <summary>
        /// Развёрнутое описание продукта для карточки в справочнике (markdown).
        /// До 4000 символов (валидация).
        /// На Этапе 8+ может быть вынесено в отдельную сущность <c>IngredientDetails</c>.
        /// </summary>
        public string? Description { get; private set; }

        /// <summary>
        /// Идентификатор файла-изображения в модуле Media. Опционально.
        /// Логическая ссылка без FK-constraint на уровне БД (кросс-модульно).
        /// </summary>
        public Guid? ImageMediaId { get; private set; }

        /// <summary>
        /// Флаг «продукт жидкий». Используется при конвертации объём ↔ масса.
        /// Если <see langword="true"/>, то <see cref="DensityApprox"/> обязательно заполнен.
        /// </summary>
        public bool IsLiquid { get; private set; }

        /// <summary>
        /// Приближённая плотность продукта, г/мл. Используется для конвертации
        /// объёма в массу (например, молоко ≈ 1.030).
        /// Заполнено только если <see cref="IsLiquid"/> = <see langword="true"/>.
        /// </summary>
        public decimal? DensityApprox { get; private set; }

        /// <summary>
        /// Флаг «продукт является аллергеном». Используется в каталожном фильтре.
        /// Если <see langword="true"/>, то <see cref="AllergenType"/> обязательно заполнен.
        /// </summary>
        public bool IsAllergen { get; private set; }

        /// <summary>
        /// Тип аллергена. Хотя <see cref="Enums.AllergenType"/> — flags-enum,
        /// здесь используется как одиночное значение (продукт может относиться
        /// только к одному типу аллергена).
        /// Заполнено только если <see cref="IsAllergen"/> = <see langword="true"/>.
        /// </summary>
        public AllergenType? AllergenType { get; private set; }

        /// <summary>
        /// Битовая маска диетических меток, с которыми продукт конфликтует.
        /// Если бит установлен, блюдо, содержащее этот ингредиент, не может нести
        /// соответствующую метку <see cref="DietLabels"/>. По умолчанию —
        /// <see cref="DietLabels.None"/> (ингредиент ни с какой меткой не конфликтует).
        /// </summary>
        /// <remarks>
        /// Заполняется модератором/админом через UC-DSH-110/111.
        /// Используется агрегатом <see cref="Dish"/> при автокоррекции
        /// <c>DietLabelsMask</c> и при валидации UC-DSH-009 SetDietLabels.
        /// Источник правды для конфликтов, не выводимых из <see cref="AllergenType"/>
        /// (например, мясо ↔ <see cref="DietLabels.Vegan"/>, алкоголь ↔
        /// <see cref="DietLabels.Halal"/>).
        /// </remarks>
        public DietLabels DietConflictsMask { get; private set; }

        /// <summary>
        /// Идентификатор базовой единицы хранения для этого продукта.
        /// Обычно «г» для твёрдых и «мл» для жидких.
        /// Используется для нормализации количества при расчёте КБЖУ рецепта.
        /// </summary>
        public Guid BaseMeasureUnitId { get; private set; }

        /// <summary>
        /// Идентификатор записи КБЖУ по умолчанию (на 100 г). Опционально.
        /// Используется, если в рецепте не указан конкретный <see cref="IngredientSpec"/>.
        /// Один <c>Nutrition</c> может разделяться несколькими <see cref="Ingredient"/>
        /// (усреднённые значения для близких продуктов).
        /// </summary>
        public Guid? DefaultNutritionId { get; private set; }

        /// <summary>
        /// Признак активности. <see langword="false"/> — продукт скрыт из автокомплита,
        /// но физически не удалён. По умолчанию <see langword="true"/>.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Дата и время создания записи (UTC).
        /// </summary>
        public DateTimeOffset CreatedAt { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Конструктор без параметров для EF Core.
        /// EF Core использует его при материализации объектов из БД.
        /// </summary>
        private Ingredient() : base() { }

        /// <summary>
        /// Создаёт новый экземпляр <see cref="Ingredient"/>.
        /// Используется только из фабричного метода <see cref="Create"/>.
        /// </summary>
        private Ingredient(
            string name,
            string? pluralName,
            string? description,
            Guid? imageMediaId,
            bool isLiquid,
            decimal? densityApprox,
            bool isAllergen,
            AllergenType? allergenType,
            DietLabels dietConflictsMask,
            Guid baseMeasureUnitId,
            Guid? defaultNutritionId,
            DateTimeOffset createdAt)
            : base(Guid.NewGuid())
        {
            Name = name;
            PluralName = pluralName;
            Description = description;
            ImageMediaId = imageMediaId;
            IsLiquid = isLiquid;
            DensityApprox = densityApprox;
            IsAllergen = isAllergen;
            AllergenType = allergenType;
            DietConflictsMask = dietConflictsMask;
            BaseMeasureUnitId = baseMeasureUnitId;
            DefaultNutritionId = defaultNutritionId;
            CreatedAt = createdAt;
            IsActive = true;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Создаёт новую запись справочника ингредиентов. Все условные инварианты
        /// (<see cref="IsLiquid"/>/<see cref="DensityApprox"/>, <see cref="IsAllergen"/>/<see cref="AllergenType"/>)
        /// проверяются на уровне команды (FluentValidation) и продублированы CHECK-constraints в БД.
        /// </summary>
        /// <param name="name">Название продукта (уникальное в рамках справочника).</param>
        /// <param name="pluralName">Форма родительного падежа для текстовой генерации. Опционально.</param>
        /// <param name="description">Развёрнутое описание (markdown). Опционально.</param>
        /// <param name="imageMediaId">Идентификатор файла-изображения в Media. Опционально.</param>
        /// <param name="isLiquid">Флаг «продукт жидкий». Требует заполнения <paramref name="densityApprox"/>.</param>
        /// <param name="densityApprox">Плотность, г/мл. Обязательно при <paramref name="isLiquid"/> = <see langword="true"/>.</param>
        /// <param name="isAllergen">Флаг «продукт-аллерген». Требует заполнения <paramref name="allergenType"/>.</param>
        /// <param name="allergenType">Тип аллергена. Обязательно при <paramref name="isAllergen"/> = <see langword="true"/>.</param>
        /// <param name="dietConflictsMask">Маска диетических меток, с которыми продукт конфликтует.
        /// <see cref="DietLabels.None"/> для нейтральных по составу ингредиентов.</param>
        /// <param name="baseMeasureUnitId">Идентификатор базовой единицы хранения.</param>
        /// <param name="defaultNutritionId">Идентификатор КБЖУ по умолчанию. Опционально.</param>
        /// <param name="createdAt">Дата и время создания (UTC).</param>
        /// <returns>Новый экземпляр <see cref="Ingredient"/>.</returns>
        public static Ingredient Create(
            string name,
            string? pluralName,
            string? description,
            Guid? imageMediaId,
            bool isLiquid,
            decimal? densityApprox,
            bool isAllergen,
            AllergenType? allergenType,
            DietLabels dietConflictsMask,
            Guid baseMeasureUnitId,
            Guid? defaultNutritionId,
            DateTimeOffset createdAt)
        {
            return new Ingredient(
                name,
                pluralName,
                description,
                imageMediaId,
                isLiquid,
                densityApprox,
                isAllergen,
                allergenType,
                dietConflictsMask,
                baseMeasureUnitId,
                defaultNutritionId,
                createdAt);
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Обновляет все редактируемые поля ингредиента (UC-DSH-111 UpdateIngredient).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Условные инварианты (<see cref="IsLiquid"/> ⇒ <see cref="DensityApprox"/>;
        /// <see cref="IsAllergen"/> ⇒ <see cref="AllergenType"/>) ожидаются проверенными
        /// на уровне команды (FluentValidation). Domain их не дублирует —
        /// defense-in-depth обеспечивается CHECK-constraints в БД.
        /// </para>
        /// <para>
        /// Поле <see cref="IsActive"/> через этот метод не меняется — для активации/деактивации
        /// есть отдельные <see cref="Activate"/> / <see cref="Deactivate"/> (UC-DSH-112).
        /// </para>
        /// </remarks>
        /// <param name="name">Новое название (уникальное в справочнике).</param>
        /// <param name="pluralName">Форма родительного падежа. Опционально.</param>
        /// <param name="description">Описание. Опционально.</param>
        /// <param name="imageMediaId">Идентификатор изображения в Media. Опционально.</param>
        /// <param name="isLiquid">Флаг «продукт жидкий».</param>
        /// <param name="densityApprox">Плотность, г/мл. Обязательно при <paramref name="isLiquid"/>.</param>
        /// <param name="isAllergen">Флаг «продукт-аллерген».</param>
        /// <param name="allergenType">Тип аллергена. Обязательно при <paramref name="isAllergen"/>.</param>
        /// <param name="dietConflictsMask">Маска конфликтующих диет-меток.</param>
        /// <param name="baseMeasureUnitId">Базовая единица хранения.</param>
        /// <param name="defaultNutritionId">Идентификатор КБЖУ по умолчанию. Опционально.</param>
        public void Update(
            string name,
            string? pluralName,
            string? description,
            Guid? imageMediaId,
            bool isLiquid,
            decimal? densityApprox,
            bool isAllergen,
            AllergenType? allergenType,
            DietLabels dietConflictsMask,
            Guid baseMeasureUnitId,
            Guid? defaultNutritionId)
        {
            Name = name;
            PluralName = pluralName;
            Description = description;
            ImageMediaId = imageMediaId;
            IsLiquid = isLiquid;
            DensityApprox = densityApprox;
            IsAllergen = isAllergen;
            AllergenType = allergenType;
            DietConflictsMask = dietConflictsMask;
            BaseMeasureUnitId = baseMeasureUnitId;
            DefaultNutritionId = defaultNutritionId;
        }

        /// <summary>
        /// Деактивирует ингредиент (UC-DSH-112). Существующие <c>RecipeIngredient</c>,
        /// ссылающиеся на этот ингредиент, остаются работоспособными — при добавлении
        /// в новые рецепты ингредиент не предлагается (см. <c>SearchActiveByNamePrefixAsync</c>).
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
        }

        /// <summary>
        /// Активирует ранее деактивированный ингредиент. Парный метод для
        /// <see cref="Deactivate"/>.
        /// </summary>
        public void Activate()
        {
            IsActive = true;
        }

        #endregion
    }
}
