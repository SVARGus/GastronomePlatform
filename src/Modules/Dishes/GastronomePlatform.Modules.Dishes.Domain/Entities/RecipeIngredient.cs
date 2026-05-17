using GastronomePlatform.Common.Domain.Primitives;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Errors;

namespace GastronomePlatform.Modules.Dishes.Domain.Entities
{
    /// <summary>
    /// Позиция в списке ингредиентов рецепта. Часть агрегата <see cref="Dish"/>.
    /// Гибрид: либо ссылка на справочник через <see cref="IngredientId"/>,
    /// либо свободный текст в <see cref="FreeformText"/> — ровно одно из двух.
    /// </summary>
    /// <remarks>
    /// XOR-инвариант <c>(IngredientId IS NOT NULL) &lt;&gt; (FreeformText IS NOT NULL)</c>
    /// обеспечивается структурно: создание возможно только через одну из двух фабрик —
    /// <see cref="CreateFromCatalog"/> или <see cref="CreateFreeform"/>. Тот же инвариант
    /// продублирован в БД CHECK-constraint'ом.
    /// <para>
    /// Все методы изменения состояния — <see langword="internal"/>. Внешний код управляет
    /// ингредиентами через wrapper-методы на <see cref="Dish"/>:
    /// <c>AddRecipeIngredientFromCatalog</c>, <c>AddRecipeIngredientFreeform</c>,
    /// <c>UpdateRecipeIngredient</c>, <c>RemoveRecipeIngredient</c>,
    /// <c>ReorderRecipeIngredients</c>.
    /// </para>
    /// </remarks>
    public sealed class RecipeIngredient : Entity<Guid>
    {
        #region Properties

        /// <summary>
        /// Идентификатор рецепта-владельца. FK на <c>dishes.Recipes</c>.
        /// </summary>
        public Guid RecipeId { get; private set; }

        /// <summary>
        /// Идентификатор ингредиента из справочника. <see langword="null"/>, если
        /// позиция задана через <see cref="FreeformText"/>. FK на <c>dishes.Ingredients</c>.
        /// </summary>
        public Guid? IngredientId { get; private set; }

        /// <summary>
        /// Идентификатор сорта/спецификации ингредиента. Заполняется только если
        /// заполнен <see cref="IngredientId"/>. FK на <c>dishes.IngredientSpecs</c>.
        /// На Этапе 2 — stub-таблица.
        /// </summary>
        public Guid? IngredientSpecId { get; private set; }

        /// <summary>
        /// Свободный текст ингредиента, если в справочнике нет подходящей записи.
        /// <see langword="null"/>, если позиция ссылается через <see cref="IngredientId"/>.
        /// Длина до 200 символов (валидация — на уровне команды).
        /// </summary>
        public string? FreeformText { get; private set; }

        /// <summary>Количество. Строго положительное.</summary>
        public decimal Quantity { get; private set; }

        /// <summary>
        /// Единица измерения количества. FK на <c>dishes.MeasureUnits</c>.
        /// </summary>
        public Guid MeasureUnitId { get; private set; }

        /// <summary>
        /// Порядковый номер позиции в рамках одного <see cref="Recipe"/>. Уникален.
        /// </summary>
        public int Order { get; private set; }

        /// <summary>
        /// <see langword="true"/>, если ингредиент опционален («по желанию»).
        /// </summary>
        public bool IsOptional { get; private set; }

        /// <summary>
        /// Заметка по подготовке: «мелко нарезанный», «комнатной температуры».
        /// До 200 символов (валидация — на уровне команды). Опционально.
        /// </summary>
        public string? PreparationNote { get; private set; }

        #endregion

        #region Constructors

        /// <summary>Конструктор без параметров для EF Core.</summary>
        private RecipeIngredient() : base() { }

        /// <summary>
        /// Приватный конструктор, используется только из фабрик
        /// <see cref="CreateFromCatalog"/> и <see cref="CreateFreeform"/>.
        /// </summary>
        /// <param name="recipeId">Идентификатор рецепта-владельца.</param>
        /// <param name="ingredientId">Идентификатор ингредиента из справочника или <see langword="null"/>.</param>
        /// <param name="ingredientSpecId">Идентификатор спецификации или <see langword="null"/>.</param>
        /// <param name="freeformText">Свободный текст или <see langword="null"/>.</param>
        /// <param name="quantity">Количество.</param>
        /// <param name="measureUnitId">Идентификатор единицы измерения.</param>
        /// <param name="order">Порядковый номер.</param>
        /// <param name="isOptional">Признак опциональности.</param>
        /// <param name="preparationNote">Заметка по подготовке или <see langword="null"/>.</param>
        private RecipeIngredient(
            Guid recipeId,
            Guid? ingredientId,
            Guid? ingredientSpecId,
            string? freeformText,
            decimal quantity,
            Guid measureUnitId,
            int order,
            bool isOptional,
            string? preparationNote)
            : base(Guid.NewGuid())
        {
            RecipeId = recipeId;
            IngredientId = ingredientId;
            IngredientSpecId = ingredientSpecId;
            FreeformText = freeformText;
            Quantity = quantity;
            MeasureUnitId = measureUnitId;
            Order = order;
            IsOptional = isOptional;
            PreparationNote = preparationNote;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Создаёт позицию, ссылающуюся на ингредиент из справочника. Вызывается
        /// только из <c>Recipe.AddIngredientFromCatalog(...)</c>. Валидация значений
        /// (положительность <paramref name="quantity"/> и т.п.) ожидается на уровне команды.
        /// </summary>
        /// <param name="recipeId">Идентификатор рецепта-владельца.</param>
        /// <param name="ingredientId">Идентификатор ингредиента из справочника.</param>
        /// <param name="ingredientSpecId">Идентификатор спецификации. Опционально.</param>
        /// <param name="quantity">Количество.</param>
        /// <param name="measureUnitId">Идентификатор единицы измерения.</param>
        /// <param name="order">Порядковый номер.</param>
        /// <param name="isOptional">Признак опциональности.</param>
        /// <param name="preparationNote">Заметка по подготовке. Опционально.</param>
        /// <returns>Новый <see cref="RecipeIngredient"/>.</returns>
        internal static RecipeIngredient CreateFromCatalog(
            Guid recipeId,
            Guid ingredientId,
            Guid? ingredientSpecId,
            decimal quantity,
            Guid measureUnitId,
            int order,
            bool isOptional,
            string? preparationNote)
        {
            return new RecipeIngredient(
                recipeId,
                ingredientId,
                ingredientSpecId,
                freeformText: null,
                quantity,
                measureUnitId,
                order,
                isOptional,
                preparationNote);
        }

        /// <summary>
        /// Создаёт позицию со свободным текстом — для случаев, когда нужного ингредиента
        /// в справочнике нет. Вызывается только из <c>Recipe.AddIngredientFreeform(...)</c>.
        /// </summary>
        /// <param name="recipeId">Идентификатор рецепта-владельца.</param>
        /// <param name="freeformText">Свободный текст ингредиента.</param>
        /// <param name="quantity">Количество.</param>
        /// <param name="measureUnitId">Идентификатор единицы измерения.</param>
        /// <param name="order">Порядковый номер.</param>
        /// <param name="isOptional">Признак опциональности.</param>
        /// <param name="preparationNote">Заметка по подготовке. Опционально.</param>
        /// <returns>Новый <see cref="RecipeIngredient"/>.</returns>
        internal static RecipeIngredient CreateFreeform(
            Guid recipeId,
            string freeformText,
            decimal quantity,
            Guid measureUnitId,
            int order,
            bool isOptional,
            string? preparationNote)
        {
            return new RecipeIngredient(
                recipeId,
                ingredientId: null,
                ingredientSpecId: null,
                freeformText,
                quantity,
                measureUnitId,
                order,
                isOptional,
                preparationNote);
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Обновляет все поля позиции одним вызовом (соответствует UC-DSH-031).
        /// Допускает смену источника catalog↔freeform. Валидирует XOR
        /// между <paramref name="ingredientId"/> и <paramref name="freeformText"/>,
        /// требование «<paramref name="ingredientSpecId"/> только при заполненном
        /// <paramref name="ingredientId"/>», и положительность <paramref name="quantity"/>.
        /// </summary>
        /// <param name="ingredientId">Новый идентификатор ингредиента или <see langword="null"/>.</param>
        /// <param name="ingredientSpecId">Новый идентификатор спецификации или <see langword="null"/>.</param>
        /// <param name="freeformText">Новый свободный текст или <see langword="null"/>.</param>
        /// <param name="quantity">Новое количество.</param>
        /// <param name="measureUnitId">Новый идентификатор единицы измерения.</param>
        /// <param name="isOptional">Признак опциональности.</param>
        /// <param name="preparationNote">Заметка по подготовке. <see langword="null"/> — очистить.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или <see cref="Result.Failure(Error)"/>
        /// с <see cref="DishesErrors.InvalidIngredientComposition"/> при нарушении XOR
        /// или <see cref="DishesErrors.InvalidQuantity"/> при <paramref name="quantity"/> &lt;= 0.
        /// </returns>
        internal Result Update(
            Guid? ingredientId,
            Guid? ingredientSpecId,
            string? freeformText,
            decimal quantity,
            Guid measureUnitId,
            bool isOptional,
            string? preparationNote)
        {
            var hasIngredient = ingredientId.HasValue;
            var hasFreeform = !string.IsNullOrWhiteSpace(freeformText);

            if (hasIngredient == hasFreeform)
            {
                return Result.Failure(DishesErrors.InvalidIngredientComposition);
            }

            if (ingredientSpecId.HasValue && !hasIngredient)
            {
                return Result.Failure(DishesErrors.InvalidIngredientComposition);
            }

            if (quantity <= 0m)
            {
                return Result.Failure(DishesErrors.InvalidQuantity);
            }

            IngredientId = ingredientId;
            IngredientSpecId = ingredientSpecId;
            FreeformText = hasFreeform ? freeformText : null;
            Quantity = quantity;
            MeasureUnitId = measureUnitId;
            IsOptional = isOptional;
            PreparationNote = preparationNote;

            return Result.Success();
        }

        /// <summary>
        /// Устанавливает порядковый номер позиции. Вызывается только из
        /// <c>Recipe.RemoveIngredient</c> и <c>Recipe.ReorderIngredients</c>
        /// при пересборке порядка.
        /// </summary>
        /// <param name="order">Новый порядковый номер.</param>
        internal void SetOrder(int order)
        {
            Order = order;
        }

        #endregion
    }
}
