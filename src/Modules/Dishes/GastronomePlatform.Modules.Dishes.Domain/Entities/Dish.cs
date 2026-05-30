using GastronomePlatform.Common.Domain.Primitives;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Enums;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Events;

namespace GastronomePlatform.Modules.Dishes.Domain.Entities
{
    /// <summary>
    /// Блюдо — корень агрегата каталога. Публичная карточка блюда, которую видят все
    /// пользователи (включая гостей). Содержит ссылку на <see cref="Recipe"/> и
    /// внутреннее состояние: статус, модерацию, рейтинг, опубликованный снепшот.
    /// </summary>
    /// <remarks>
    /// Двухслойная модель: основные поля карточки хранятся плоско, плюс jsonb-снепшот
    /// <see cref="PublishedVersionData"/> с публичной версией для быстрой отдачи посетителям.
    /// Снепшот заполняется при <see cref="Publish"/> и обнуляется при <see cref="Unpublish"/>
    /// и <see cref="Archive"/>.
    /// </remarks>
    public sealed class Dish : AggregateRoot<Guid>
    {
        // Domain-инварианты ограничения на размер M:M-коллекций (см. дизайн-документ).
        private const int MAX_CATEGORIES = 3;
        private const int MAX_TAGS = 20;

        // Backing fields для M:M-навигаций. Настраиваются в DishConfiguration
        // через HasField("_categories") + PropertyAccessMode.Field (аналогично _tags
        // и обеим *Published-коллекциям).
        private readonly List<DishCategory> _categories = new();
        private readonly List<DishTag> _tags = new();
        private readonly List<DishCategoryPublished> _categoriesPublished = new();
        private readonly List<DishTagPublished> _tagsPublished = new();

        #region Properties

        /// <summary>
        /// Идентификатор автора блюда. Логическая ссылка на <c>users.UserProfiles.UserId</c>
        /// без FK-constraint (кросс-модульно).
        /// </summary>
        public Guid AuthorUserId { get; private set; }

        /// <summary>
        /// Отображаемое название блюда. Длина 3–200 символов
        /// (валидация — на уровне команды через FluentValidation).
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// URL-friendly идентификатор блюда. Генерируется автоматически из <see cref="Name"/>
        /// на уровне Application при создании блюда (UC-DSH-001). После создания изменение
        /// возможно только через отдельный admin-метод. Уникален в рамках всей платформы.
        /// </summary>
        public string Slug { get; private set; } = string.Empty;

        /// <summary>
        /// Краткая подводка для карточек каталога. Опционально.
        /// </summary>
        public string? ShortDescription { get; private set; }

        /// <summary>
        /// Полное «аппетитное» описание блюда (markdown). Не содержит рецепта.
        /// </summary>
        public string? Description { get; private set; }

        /// <summary>
        /// Историко-культурный контекст блюда. Опционально.
        /// </summary>
        public string? HistoryText { get; private set; }

        /// <summary>
        /// Идентификатор главного фото в модуле Media. Без FK-constraint (кросс-модульно).
        /// Обязателен для публикации.
        /// </summary>
        public Guid? MainImageId { get; private set; }

        /// <summary>
        /// Текущий статус жизненного цикла блюда:
        /// <see cref="DishStatus.Draft"/> → <see cref="DishStatus.Published"/> →
        /// <see cref="DishStatus.Unpublished"/> / <see cref="DishStatus.Archived"/>.
        /// </summary>
        public DishStatus Status { get; private set; }

        /// <summary>
        /// Результат модерации блюда. По умолчанию — <see cref="ModerationStatus.Approved"/>.
        /// </summary>
        public ModerationStatus ModerationStatus { get; private set; }

        /// <summary>
        /// Уровень сложности приготовления.
        /// </summary>
        public DifficultyLevel DifficultyLevel { get; private set; }

        /// <summary>
        /// Грубая оценка стоимости блюда.
        /// </summary>
        public CostEstimate CostEstimate { get; private set; }

        /// <summary>
        /// Тип владельца. Денормализуется из ролей автора на момент создания/обновления —
        /// при последующей смене роли автора старые блюда сохраняют тот тип, который был
        /// зафиксирован.
        /// </summary>
        public OwnerType OwnerType { get; private set; }

        /// <summary>
        /// Битовая маска диетических меток (например, <c>Vegan | GlutenFree</c>).
        /// По умолчанию — <see cref="DietLabels.None"/>. Устанавливается автором через
        /// <see cref="UpdateCard"/>.
        /// </summary>
        public DietLabels DietLabelsMask { get; private set; }

        // AllergensMask и HasUnverifiedAllergens — денормализованные публичные маркеры.
        // Источник правды — состав Recipe.RecipeIngredients (Ingredient.AllergenType для
        // ссылочных позиций, флаг unverified для freeform). Хранятся в корне агрегата
        // для быстрого чтения в каталожных запросах.
        //
        // TODO: метод RecalculateAllergens(...) реализуется после добавления Recipe
        // и RecipeIngredient. Вызов из Application-handler'ов модификации состава
        // (UC-DSH-030..033), шаблон вызова:
        //
        //   var ingredientAllergens = await _ingredientRepository
        //       .GetAllergensByIdsAsync(recipeIngredientIds, ct);
        //   dish.RecalculateAllergens(ingredientAllergens, utcNow);

        /// <summary>
        /// Битовая маска аллергенов блюда. Денормализованное поле — отражает суммарный
        /// набор аллергенов всех ингредиентов рецепта. Не задаётся автором напрямую,
        /// пересчитывается при изменении состава ингредиентов. Используется в каталожном
        /// фильтре <c>UC-DSH-054 SearchDishes</c>.
        /// </summary>
        public AllergenType AllergensMask { get; private set; }

        /// <summary>
        /// <see langword="true"/>, если в рецепте есть хотя бы один <c>RecipeIngredient</c>
        /// с <c>FreeformText</c> (без ссылки на справочник <c>Ingredient</c>). Сигнализирует,
        /// что <see cref="AllergensMask"/> может быть неполной. UI на основе этого флага
        /// показывает дисклеймер «Может содержать и другие аллергены — уточняйте у автора».
        /// </summary>
        public bool HasUnverifiedAllergens { get; private set; }

        /// <summary>
        /// Средний рейтинг блюда (0–5). Денормализованное поле; обновляется внешними
        /// обработчиками при изменении оценок, не задаётся напрямую.
        /// </summary>
        public decimal RatingAvg { get; private set; }

        /// <summary>
        /// Количество оценок блюда. Денормализованное поле.
        /// </summary>
        public int RatingCount { get; private set; }

        /// <summary>
        /// Количество просмотров карточки блюда. Денормализованное поле.
        /// </summary>
        public long ViewsCount { get; private set; }

        /// <summary>
        /// Количество добавлений в избранное. Денормализованное поле.
        /// </summary>
        public int FavoritesCount { get; private set; }

        /// <summary>
        /// JSON-снепшот опубликованной версии блюда (карточка + рецепт + шаги + ингредиенты +
        /// теги + категории + Timing + Yield + Nutrition). Заполняется при <see cref="Publish"/>,
        /// обнуляется при <see cref="Unpublish"/>/<see cref="Archive"/>. <see langword="null"/> —
        /// у блюда нет публичной версии. Маппится в БД как <c>jsonb</c>. Структура JSON
        /// формируется Application-слоем при сборке снепшота.
        /// </summary>
        public string? PublishedVersionData { get; private set; }

        /// <summary>
        /// Момент последнего обновления снепшота — при публикации автором или при каскадных
        /// обновлениях. Используется для HTTP-заголовка <c>Last-Modified</c>.
        /// </summary>
        public DateTimeOffset? PublishedVersionUpdatedAt { get; private set; }

        /// <summary>
        /// Момент последней публикации блюда автором. <see langword="null"/>, если блюдо
        /// никогда не публиковалось или снято с публикации.
        /// </summary>
        public DateTimeOffset? PublishedAt { get; private set; }

        /// <summary>
        /// Момент создания блюда. Иммутабелен после создания.
        /// </summary>
        public DateTimeOffset CreatedAt { get; private set; }

        /// <summary>
        /// Момент последнего изменения данных блюда автором. Обновляется через явный
        /// <see cref="MarkAsUpdated"/> или автоматически инфраструктурным
        /// <c>SaveChangesInterceptor</c> при изменении связанных сущностей агрегата.
        /// </summary>
        public DateTimeOffset UpdatedAt { get; private set; }

        /// <summary>
        /// Рецепт блюда — часть агрегата. Создаётся вместе с <see cref="Dish"/> в фабрике;
        /// модификация рецепта выполняется только через wrapper-методы на <see cref="Dish"/>
        /// (например, <see cref="UpdateRecipeIntroduction"/>, <see cref="UpdateTiming"/>),
        /// внешний код не вызывает методы <see cref="Recipe"/> напрямую.
        /// </summary>
        public Recipe Recipe { get; private set; } = null!;

        /// <summary>
        /// Категории блюда (рабочая версия, M:M). Read-only коллекция; полная замена
        /// через <see cref="SetCategories"/>. Изменения этой коллекции синхронизируются
        /// с <see cref="CategoriesPublished"/> только при <see cref="Publish"/>.
        /// </summary>
        public IReadOnlyList<DishCategory> Categories => _categories;

        /// <summary>
        /// Теги блюда (рабочая версия, M:M). Read-only коллекция; полная замена
        /// через <see cref="SetTags"/>.
        /// </summary>
        public IReadOnlyList<DishTag> Tags => _tags;

        /// <summary>
        /// Категории блюда (опубликованная версия). Заполняется при <see cref="Publish"/>
        /// из текущего набора <see cref="Categories"/>, очищается при
        /// <see cref="Unpublish"/> / <see cref="Archive"/>. Используется каталожным
        /// фильтром (UC-DSH-054).
        /// </summary>
        public IReadOnlyList<DishCategoryPublished> CategoriesPublished => _categoriesPublished;

        /// <summary>
        /// Теги блюда (опубликованная версия). Семантика аналогична <see cref="CategoriesPublished"/>.
        /// </summary>
        public IReadOnlyList<DishTagPublished> TagsPublished => _tagsPublished;

        #endregion

        #region Constructors

        /// <summary>
        /// Конструктор без параметров для EF Core.
        /// EF Core использует его при материализации объектов из БД.
        /// </summary>
        private Dish() : base() { }

        /// <summary>
        /// Приватный конструктор, используется только из <see cref="Create"/>.
        /// </summary>
        private Dish(
            Guid authorUserId,
            string name,
            string slug,
            DifficultyLevel difficultyLevel,
            CostEstimate costEstimate,
            OwnerType ownerType,
            DateTimeOffset utcNow)
            : base(Guid.NewGuid())
        {
            AuthorUserId = authorUserId;
            Name = name;
            Slug = slug;
            DifficultyLevel = difficultyLevel;
            CostEstimate = costEstimate;
            OwnerType = ownerType;
            Status = DishStatus.Draft;
            ModerationStatus = ModerationStatus.Approved;
            DietLabelsMask = DietLabels.None;
            AllergensMask = AllergenType.None;
            HasUnverifiedAllergens = false;
            CreatedAt = utcNow;
            UpdatedAt = utcNow;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Создаёт новое блюдо в статусе <see cref="DishStatus.Draft"/>. Одновременно
        /// создаётся вложенный <see cref="Recipe"/> с пустыми <see cref="Entities.Timing"/>
        /// и <see cref="Entities.Yield"/>. Поднимает событие <see cref="DishCreatedEvent"/>.
        /// Валидация параметров (длина строк, формат slug) ожидается на уровне команды
        /// через FluentValidation.
        /// </summary>
        /// <param name="authorUserId">Идентификатор автора (пользователя из модуля Users).</param>
        /// <param name="name">Отображаемое название блюда.</param>
        /// <param name="slug">URL-friendly идентификатор, сгенерированный Application-слоем.</param>
        /// <param name="difficultyLevel">Уровень сложности приготовления.</param>
        /// <param name="costEstimate">Грубая оценка стоимости.</param>
        /// <param name="ownerType">Тип владельца — денормализуется из ролей автора.</param>
        /// <param name="utcNow">Текущее время UTC (передаётся из <c>IDateTimeProvider</c> в Handler).</param>
        /// <returns>Новый <see cref="Dish"/> с зарегистрированным событием <see cref="DishCreatedEvent"/>.</returns>
        public static Dish Create(
            Guid authorUserId,
            string name,
            string slug,
            DifficultyLevel difficultyLevel,
            CostEstimate costEstimate,
            OwnerType ownerType,
            DateTimeOffset utcNow)
        {
            var dish = new Dish(
                authorUserId,
                name,
                slug,
                difficultyLevel,
                costEstimate,
                ownerType,
                utcNow);

            dish.Recipe = Recipe.CreateForDish(dish.Id);
            dish.RaiseDomainEvent(new DishCreatedEvent(dish.Id, dish.AuthorUserId));
            return dish;
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Обновляет основные поля карточки блюда. Поднимает событие <see cref="DishUpdatedEvent"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Изменение опубликованного блюда НЕ обновляет автоматически
        /// <see cref="PublishedVersionData"/> — требуется явный вызов <see cref="Publish"/>
        /// для перепубликации. Это позволяет автору готовить правки в основной таблице,
        /// не затрагивая публичную версию.
        /// </para>
        /// <para>
        /// <see cref="DietLabelsMask"/> и <see cref="MainImageId"/> НЕ являются частью
        /// карточки и редактируются отдельными методами: <see cref="SetDietLabels"/>
        /// (декларация автора с будущей валидацией по составу ингредиентов) и
        /// <see cref="ChangeMainImage"/> (отдельная транзакционная семантика с модулем
        /// Media). Это намеренное разделение операций с разной семантикой инвариантов.
        /// </para>
        /// </remarks>
        /// <param name="name">Новое название.</param>
        /// <param name="shortDescription">Краткая подводка. <see langword="null"/> — очистить.</param>
        /// <param name="description">Полное описание (markdown). <see langword="null"/> — очистить.</param>
        /// <param name="difficultyLevel">Новый уровень сложности.</param>
        /// <param name="costEstimate">Новая оценка стоимости.</param>
        /// <param name="ownerType">Новый тип владельца.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        public void UpdateCard(
            string name,
            string? shortDescription,
            string? description,
            DifficultyLevel difficultyLevel,
            CostEstimate costEstimate,
            OwnerType ownerType,
            DateTimeOffset utcNow)
        {
            Name = name;
            ShortDescription = shortDescription;
            Description = description;
            DifficultyLevel = difficultyLevel;
            CostEstimate = costEstimate;
            OwnerType = ownerType;
            UpdatedAt = utcNow;

            RaiseDomainEvent(new DishUpdatedEvent(Id, AuthorUserId));
        }

        /// <summary>
        /// Меняет главное фото блюда. Поднимает событие <see cref="DishUpdatedEvent"/>.
        /// Attach/detach медиафайла к Dish выполняется Application-слоем через <c>IMediaService</c>.
        /// </summary>
        /// <param name="mainImageId">
        /// Новый идентификатор главного фото. <see langword="null"/> — удалить ссылку
        /// (например, если фото было удалено в Media).
        /// </param>
        /// <param name="utcNow">Текущее время UTC.</param>
        public void ChangeMainImage(Guid? mainImageId, DateTimeOffset utcNow)
        {
            MainImageId = mainImageId;
            UpdatedAt = utcNow;

            RaiseDomainEvent(new DishUpdatedEvent(Id, AuthorUserId));
        }

        /// <summary>
        /// Устанавливает битовую маску диетических меток блюда (например,
        /// <c>Vegan | GlutenFree</c>). Поднимает событие <see cref="DishUpdatedEvent"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Сейчас метод работает как простая запись значения — присваивает маску
        /// и фиксирует <see cref="UpdatedAt"/>, без проверок на согласованность
        /// с составом ингредиентов рецепта.
        /// </para>
        /// <para>
        /// <b>TODO (после реализации справочника совместимости):</b> добавить
        /// валидацию входной маски по текущему составу <see cref="Recipe"/>.
        /// Например, попытка установить <c>DietLabels.Vegan</c> при наличии
        /// в рецепте ингредиента, конфликтующего с этой меткой (мясо, рыба,
        /// молочное и т.п.), должна возвращать ошибку. Справочник конфликтов
        /// (<c>Ingredient.DietConflictsMask</c>) и автокоррекция
        /// <see cref="DietLabelsMask"/> при изменении состава ингредиентов
        /// (<c>Recipe.*Ingredient*</c> методы) — кандидат на отдельный ADR.
        /// </para>
        /// </remarks>
        /// <param name="dietLabelsMask">Новая битовая маска диетических меток.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        public void SetDietLabels(DietLabels dietLabelsMask, DateTimeOffset utcNow)
        {
            DietLabelsMask = dietLabelsMask;
            UpdatedAt = utcNow;

            RaiseDomainEvent(new DishUpdatedEvent(Id, AuthorUserId));
        }

        /// <summary>
        /// Обновляет историко-культурный контекст блюда.
        /// Поднимает событие <see cref="DishUpdatedEvent"/>.
        /// </summary>
        /// <param name="historyText">Текст истории блюда. <see langword="null"/> — очистить.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        public void UpdateHistory(string? historyText, DateTimeOffset utcNow)
        {
            HistoryText = historyText;
            UpdatedAt = utcNow;

            RaiseDomainEvent(new DishUpdatedEvent(Id, AuthorUserId));
        }

        /// <summary>
        /// Атомарно обновляет простые поля <see cref="Recipe"/> (UC-DSH-003): вводный текст,
        /// количество порций по умолчанию, признак алкоголя, советы автора, рекомендации
        /// по сервировке, заметки. Поднимает одно событие <see cref="DishUpdatedEvent"/>
        /// независимо от количества фактически изменённых полей.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Шаги, ингредиенты, тайминг, выход и КБЖУ — это отдельные UC и отдельные методы
        /// (<see cref="AddRecipeStep"/>, <see cref="UpdateTiming"/>, <see cref="UpdateYield"/>,
        /// <see cref="UpdateNutrition"/> и т.п.).
        /// </para>
        /// <para>
        /// Правка не трогает <see cref="PublishedVersionData"/>. Для отражения изменений
        /// в публичной версии необходим явный <see cref="Publish"/>.
        /// </para>
        /// <para>
        /// Узкие методы (<see cref="UpdateRecipeIntroduction"/>, <see cref="SetRecipeIsAlcoholic"/>
        /// и аналоги) остаются доступны и могут использоваться, когда нужно изменить ровно одно
        /// поле без бандла.
        /// </para>
        /// </remarks>
        /// <param name="introductionText">Новый вводный текст. <see langword="null"/> — очистить.</param>
        /// <param name="servingsDefault">Новое количество порций по умолчанию (не меньше 1).</param>
        /// <param name="isAlcoholic">Признак содержания алкоголя.</param>
        /// <param name="authorTips">Советы автора. <see langword="null"/> — очистить.</param>
        /// <param name="servingSuggestions">Рекомендации по сервировке. <see langword="null"/> — очистить.</param>
        /// <param name="notes">Дополнительные заметки. <see langword="null"/> — очистить.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или <see cref="Result.Failure(Error)"/> с
        /// <see cref="DishesErrors.InvalidServingsDefault"/>, если
        /// <paramref name="servingsDefault"/> меньше 1.
        /// </returns>
        public Result UpdateRecipe(
            string? introductionText,
            int servingsDefault,
            bool isAlcoholic,
            string? authorTips,
            string? servingSuggestions,
            string? notes,
            DateTimeOffset utcNow)
        {
            // Проверяем servings первым делом — он единственный с инвариантом. При неуспехе
            // ни одно поле не должно быть применено (атомарность).
            var servingsResult = Recipe.SetServingsDefault(servingsDefault);
            if (servingsResult.IsFailure)
            {
                return servingsResult;
            }

            Recipe.UpdateIntroduction(introductionText);
            Recipe.SetIsAlcoholic(isAlcoholic);
            Recipe.UpdateAuthorTips(authorTips);
            Recipe.UpdateServingSuggestions(servingSuggestions);
            Recipe.UpdateNotes(notes);

            MarkAsUpdated(utcNow);
            return Result.Success();
        }

        /// <summary>
        /// Обновляет вводный текст рецепта.
        /// </summary>
        /// <param name="introductionText">Новый вводный текст. <see langword="null"/> — очистить.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        public void UpdateRecipeIntroduction(string? introductionText, DateTimeOffset utcNow)
        {
            Recipe.UpdateIntroduction(introductionText);
            MarkAsUpdated(utcNow);
        }

        /// <summary>
        /// Устанавливает признак содержания алкоголя в рецепте.
        /// </summary>
        /// <param name="isAlcoholic">Новое значение признака.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        public void SetRecipeIsAlcoholic(bool isAlcoholic, DateTimeOffset utcNow)
        {
            Recipe.SetIsAlcoholic(isAlcoholic);
            MarkAsUpdated(utcNow);
        }

        /// <summary>
        /// Обновляет советы автора в рецепте.
        /// </summary>
        /// <param name="authorTips">Новый текст советов. <see langword="null"/> — очистить.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        public void UpdateRecipeAuthorTips(string? authorTips, DateTimeOffset utcNow)
        {
            Recipe.UpdateAuthorTips(authorTips);
            MarkAsUpdated(utcNow);
        }

        /// <summary>
        /// Обновляет дополнительные заметки в рецепте.
        /// </summary>
        /// <param name="notes">Новый текст заметок. <see langword="null"/> — очистить.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        public void UpdateRecipeNotes(string? notes, DateTimeOffset utcNow)
        {
            Recipe.UpdateNotes(notes);
            MarkAsUpdated(utcNow);
        }

        /// <summary>
        /// Обновляет рекомендации по сервировке.
        /// </summary>
        /// <param name="servingSuggestions">Новый текст рекомендаций. <see langword="null"/> — очистить.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        public void UpdateRecipeServingSuggestions(string? servingSuggestions, DateTimeOffset utcNow)
        {
            Recipe.UpdateServingSuggestions(servingSuggestions);
            MarkAsUpdated(utcNow);
        }

        /// <summary>
        /// Устанавливает количество порций по умолчанию в рецепте.
        /// </summary>
        /// <param name="servingsDefault">Новое количество порций (не меньше 1).</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или
        /// <see cref="Result.Failure(Error)"/> с <see cref="DishesErrors.InvalidServingsDefault"/>,
        /// если значение меньше 1.
        /// </returns>
        public Result SetRecipeServingsDefault(int servingsDefault, DateTimeOffset utcNow)
        {
            var result = Recipe.SetServingsDefault(servingsDefault);
            if (result.IsFailure)
            {
                return result;
            }

            MarkAsUpdated(utcNow);
            return Result.Success();
        }

        /// <summary>
        /// Обновляет времена этапов приготовления. Если <paramref name="isTotalManual"/> =
        /// <see langword="false"/>, общее время вычисляется автоматически как сумма
        /// Prep + Cook + Rest.
        /// </summary>
        /// <param name="prepTimeMinutes">Время подготовки в минутах. <see langword="null"/> — не задано.</param>
        /// <param name="cookTimeMinutes">Время основного приготовления в минутах. <see langword="null"/> — не задано.</param>
        /// <param name="restTimeMinutes">Время отдыха в минутах. <see langword="null"/> — не задано.</param>
        /// <param name="activeTimeMinutes">Время активной работы повара в минутах. <see langword="null"/> — не задано.</param>
        /// <param name="totalTimeMinutes">Общее время в минутах. Используется, только если <paramref name="isTotalManual"/> = <see langword="true"/>.</param>
        /// <param name="isTotalManual">
        /// <see langword="true"/> — общее время задано вручную; <see langword="false"/> — вычисляется
        /// автоматически.
        /// </param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или
        /// <see cref="Result.Failure(Error)"/> с <see cref="DishesErrors.InvalidTiming"/>,
        /// если хотя бы одно значение отрицательно.
        /// </returns>
        public Result UpdateTiming(
            int? prepTimeMinutes,
            int? cookTimeMinutes,
            int? restTimeMinutes,
            int? activeTimeMinutes,
            int totalTimeMinutes,
            bool isTotalManual,
            DateTimeOffset utcNow)
        {
            var result = Recipe.UpdateTiming(
                prepTimeMinutes,
                cookTimeMinutes,
                restTimeMinutes,
                activeTimeMinutes,
                totalTimeMinutes,
                isTotalManual);

            if (result.IsFailure)
            {
                return result;
            }

            MarkAsUpdated(utcNow);
            return Result.Success();
        }

        /// <summary>
        /// Обновляет выход рецепта (общее количество, единица, порции, грамм на порцию).
        /// </summary>
        /// <param name="quantityTotal">Общее количество готового продукта.</param>
        /// <param name="yieldUnit">Единица выхода.</param>
        /// <param name="servingsCount">Количество порций (не меньше 1).</param>
        /// <param name="gramsPerServing">Вес одной порции в граммах. <see langword="null"/> — не задано.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или
        /// <see cref="Result.Failure(Error)"/> с <see cref="DishesErrors.InvalidYield"/>,
        /// если инварианты выхода нарушены.
        /// </returns>
        public Result UpdateYield(
            decimal quantityTotal,
            YieldUnit yieldUnit,
            int servingsCount,
            decimal? gramsPerServing,
            DateTimeOffset utcNow)
        {
            var result = Recipe.UpdateYield(quantityTotal, yieldUnit, servingsCount, gramsPerServing);
            if (result.IsFailure)
            {
                return result;
            }

            MarkAsUpdated(utcNow);
            return Result.Success();
        }

        /// <summary>
        /// Обновляет КБЖУ рецепта. Если у Recipe ещё нет <see cref="Entities.Nutrition"/> —
        /// создаёт новую запись; иначе обновляет существующую. Валидация значений
        /// (неотрицательность, согласованность Sugar/Carbs, SaturatedFats/Fats) ожидается
        /// на уровне команды.
        /// </summary>
        /// <param name="calcMethod">Способ расчёта КБЖУ: на 100 г или на порцию.</param>
        /// <param name="calories">Калорийность, ккал.</param>
        /// <param name="proteins">Белки, г.</param>
        /// <param name="fats">Жиры, г.</param>
        /// <param name="saturatedFats">Насыщенные жиры, г. Опционально.</param>
        /// <param name="carbs">Углеводы, г.</param>
        /// <param name="sugar">Сахара, г. Опционально.</param>
        /// <param name="fiber">Клетчатка, г. Опционально.</param>
        /// <param name="salt">Соль, г. Опционально.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        public void UpdateNutrition(
            NutritionCalcMethod calcMethod,
            decimal calories,
            decimal proteins,
            decimal fats,
            decimal? saturatedFats,
            decimal carbs,
            decimal? sugar,
            decimal? fiber,
            decimal? salt,
            DateTimeOffset utcNow)
        {
            Recipe.UpdateNutrition(
                calcMethod,
                calories,
                proteins,
                fats,
                saturatedFats,
                carbs,
                sugar,
                fiber,
                salt);

            MarkAsUpdated(utcNow);
        }

        /// <summary>
        /// Добавляет новый шаг к рецепту блюда. Порядковый номер назначается автоматически.
        /// </summary>
        /// <param name="description">Основной текст шага.</param>
        /// <param name="title">Короткий заголовок. Опционально.</param>
        /// <param name="imageMediaId">Идентификатор иллюстрации в Media. Опционально.</param>
        /// <param name="videoUrl">URL внешнего видео. Опционально.</param>
        /// <param name="temperatureCelsius">Температура приготовления. Опционально.</param>
        /// <param name="timerMinutes">Время для таймера в минутах. Опционально.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>Идентификатор созданного шага.</returns>
        public Guid AddRecipeStep(
            string description,
            string? title,
            Guid? imageMediaId,
            string? videoUrl,
            int? temperatureCelsius,
            int? timerMinutes,
            DateTimeOffset utcNow)
        {
            var stepId = Recipe.AddStep(
                description,
                title,
                imageMediaId,
                videoUrl,
                temperatureCelsius,
                timerMinutes);

            MarkAsUpdated(utcNow);
            return stepId;
        }

        /// <summary>
        /// Обновляет существующий шаг рецепта.
        /// </summary>
        /// <param name="stepId">Идентификатор шага для обновления.</param>
        /// <param name="description">Основной текст шага.</param>
        /// <param name="title">Короткий заголовок. <see langword="null"/> — очистить.</param>
        /// <param name="imageMediaId">Идентификатор иллюстрации в Media. <see langword="null"/> — очистить.</param>
        /// <param name="videoUrl">URL внешнего видео. <see langword="null"/> — очистить.</param>
        /// <param name="temperatureCelsius">Температура приготовления. <see langword="null"/> — очистить.</param>
        /// <param name="timerMinutes">Время для таймера в минутах. <see langword="null"/> — очистить.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/>, либо <see cref="Result.Failure(Error)"/>
        /// с ошибкой делегирующего вызова.
        /// </returns>
        public Result UpdateRecipeStep(
            Guid stepId,
            string description,
            string? title,
            Guid? imageMediaId,
            string? videoUrl,
            int? temperatureCelsius,
            int? timerMinutes,
            DateTimeOffset utcNow)
        {
            var result = Recipe.UpdateStep(
                stepId,
                description,
                title,
                imageMediaId,
                videoUrl,
                temperatureCelsius,
                timerMinutes);

            if (result.IsFailure)
            {
                return result;
            }

            MarkAsUpdated(utcNow);
            return Result.Success();
        }

        /// <summary>
        /// Удаляет шаг из рецепта и переупорядочивает оставшиеся.
        /// </summary>
        /// <param name="stepId">Идентификатор шага для удаления.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/>, либо <see cref="Result.Failure(Error)"/>
        /// с <see cref="DishesErrors.StepNotFound"/>.
        /// </returns>
        public Result RemoveRecipeStep(Guid stepId, DateTimeOffset utcNow)
        {
            var result = Recipe.RemoveStep(stepId);
            if (result.IsFailure)
            {
                return result;
            }

            MarkAsUpdated(utcNow);
            return Result.Success();
        }

        /// <summary>
        /// Переупорядочивает шаги рецепта. Список должен содержать все Id шагов
        /// без дубликатов.
        /// </summary>
        /// <param name="orderedStepIds">Список Id шагов в желаемом порядке.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/>, либо <see cref="Result.Failure(Error)"/>
        /// с ошибкой делегирующего вызова.
        /// </returns>
        public Result ReorderRecipeSteps(IReadOnlyList<Guid> orderedStepIds, DateTimeOffset utcNow)
        {
            var result = Recipe.ReorderSteps(orderedStepIds);
            if (result.IsFailure)
            {
                return result;
            }

            MarkAsUpdated(utcNow);
            return Result.Success();
        }

        /// <summary>
        /// Добавляет ингредиент из справочника к рецепту блюда.
        /// </summary>
        /// <remarks>
        /// После изменения состава Application Handler должен отдельно вызвать
        /// <see cref="RecalculateAllergens"/> для пересчёта <see cref="AllergensMask"/>
        /// и <see cref="HasUnverifiedAllergens"/>.
        /// </remarks>
        /// <param name="ingredientId">Идентификатор ингредиента из справочника.</param>
        /// <param name="ingredientSpecId">Идентификатор спецификации. Опционально.</param>
        /// <param name="quantity">Количество.</param>
        /// <param name="measureUnitId">Идентификатор единицы измерения.</param>
        /// <param name="isOptional">Признак опциональности.</param>
        /// <param name="preparationNote">Заметка по подготовке. Опционально.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>Идентификатор созданной позиции.</returns>
        public Guid AddRecipeIngredientFromCatalog(
            Guid ingredientId,
            Guid? ingredientSpecId,
            decimal quantity,
            Guid measureUnitId,
            bool isOptional,
            string? preparationNote,
            DateTimeOffset utcNow)
        {
            var riId = Recipe.AddIngredientFromCatalog(
                ingredientId,
                ingredientSpecId,
                quantity,
                measureUnitId,
                isOptional,
                preparationNote);

            MarkAsUpdated(utcNow);
            return riId;
        }

        /// <summary>
        /// Добавляет ингредиент свободным текстом — для случаев, когда нужного
        /// в справочнике нет.
        /// </summary>
        /// <remarks>
        /// Свободный текст устанавливает <see cref="HasUnverifiedAllergens"/> = <see langword="true"/>
        /// при следующем вызове <see cref="RecalculateAllergens"/>.
        /// </remarks>
        /// <param name="freeformText">Свободный текст ингредиента.</param>
        /// <param name="quantity">Количество.</param>
        /// <param name="measureUnitId">Идентификатор единицы измерения.</param>
        /// <param name="isOptional">Признак опциональности.</param>
        /// <param name="preparationNote">Заметка по подготовке. Опционально.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>Идентификатор созданной позиции.</returns>
        public Guid AddRecipeIngredientFreeform(
            string freeformText,
            decimal quantity,
            Guid measureUnitId,
            bool isOptional,
            string? preparationNote,
            DateTimeOffset utcNow)
        {
            var riId = Recipe.AddIngredientFreeform(
                freeformText,
                quantity,
                measureUnitId,
                isOptional,
                preparationNote);

            MarkAsUpdated(utcNow);
            return riId;
        }

        /// <summary>
        /// Обновляет существующий ингредиент рецепта. Допускает смену источника
        /// catalog↔freeform — в этом случае Application Handler должен вызвать
        /// <see cref="RecalculateAllergens"/> после успешного обновления.
        /// </summary>
        /// <param name="recipeIngredientId">Идентификатор позиции для обновления.</param>
        /// <param name="ingredientId">Новый идентификатор ингредиента или <see langword="null"/>.</param>
        /// <param name="ingredientSpecId">Новый идентификатор спецификации или <see langword="null"/>.</param>
        /// <param name="freeformText">Новый свободный текст или <see langword="null"/>.</param>
        /// <param name="quantity">Новое количество.</param>
        /// <param name="measureUnitId">Новый идентификатор единицы измерения.</param>
        /// <param name="isOptional">Признак опциональности.</param>
        /// <param name="preparationNote">Заметка по подготовке.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или <see cref="Result.Failure(Error)"/>
        /// с ошибкой делегирующего вызова.
        /// </returns>
        public Result UpdateRecipeIngredient(
            Guid recipeIngredientId,
            Guid? ingredientId,
            Guid? ingredientSpecId,
            string? freeformText,
            decimal quantity,
            Guid measureUnitId,
            bool isOptional,
            string? preparationNote,
            DateTimeOffset utcNow)
        {
            var result = Recipe.UpdateIngredient(
                recipeIngredientId,
                ingredientId,
                ingredientSpecId,
                freeformText,
                quantity,
                measureUnitId,
                isOptional,
                preparationNote);

            if (result.IsFailure)
            {
                return result;
            }

            MarkAsUpdated(utcNow);
            return Result.Success();
        }

        /// <summary>
        /// Удаляет ингредиент из рецепта и переупорядочивает оставшиеся.
        /// </summary>
        /// <param name="recipeIngredientId">Идентификатор позиции для удаления.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или <see cref="Result.Failure(Error)"/>
        /// с <see cref="DishesErrors.RecipeIngredientNotFound"/>.
        /// </returns>
        public Result RemoveRecipeIngredient(Guid recipeIngredientId, DateTimeOffset utcNow)
        {
            var result = Recipe.RemoveIngredient(recipeIngredientId);
            if (result.IsFailure)
            {
                return result;
            }

            MarkAsUpdated(utcNow);
            return Result.Success();
        }

        /// <summary>
        /// Переупорядочивает ингредиенты рецепта. Список должен содержать все Id
        /// позиций без дубликатов.
        /// </summary>
        /// <param name="orderedIngredientIds">Список Id ингредиентов в желаемом порядке.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или <see cref="Result.Failure(Error)"/>
        /// с ошибкой делегирующего вызова.
        /// </returns>
        public Result ReorderRecipeIngredients(
            IReadOnlyList<Guid> orderedIngredientIds,
            DateTimeOffset utcNow)
        {
            var result = Recipe.ReorderIngredients(orderedIngredientIds);
            if (result.IsFailure)
            {
                return result;
            }

            MarkAsUpdated(utcNow);
            return Result.Success();
        }

        /// <summary>
        /// Полностью пересобирает <see cref="AllergensMask"/> и
        /// <see cref="HasUnverifiedAllergens"/> на основе текущего состава
        /// <see cref="Recipe.Ingredients"/>.
        /// </summary>
        /// <remarks>
        /// Логика: для каждой позиции с <c>IngredientId</c> — OR-им маску, взятую
        /// из <paramref name="ingredientAllergens"/>; для каждой freeform-позиции —
        /// поднимаем флаг <see cref="HasUnverifiedAllergens"/>. Старые значения
        /// перезаписываются полностью.
        /// <para>
        /// Application Handler собирает словарь заранее через
        /// <c>IIngredientRepository.GetAllergensByIdsAsync</c>, передавая список
        /// уникальных <c>IngredientId</c> из текущего <see cref="Recipe.Ingredients"/>.
        /// </para>
        /// </remarks>
        /// <param name="ingredientAllergens">
        /// Словарь IngredientId → маска аллергенов. Ингредиенты без аллергенов попадают
        /// в словарь со значением <see cref="AllergenType.None"/>. Если какой-то Id
        /// отсутствует в словаре — маска для него считается <see cref="AllergenType.None"/>.
        /// </param>
        /// <param name="utcNow">Текущее время UTC.</param>
        public void RecalculateAllergens(
            IReadOnlyDictionary<Guid, AllergenType> ingredientAllergens,
            DateTimeOffset utcNow)
        {
            var combined = AllergenType.None;
            var hasUnverified = false;

            foreach (var ri in Recipe.Ingredients)
            {
                if (ri.IngredientId.HasValue)
                {
                    if (ingredientAllergens.TryGetValue(ri.IngredientId.Value, out var allergens))
                    {
                        combined |= allergens;
                    }
                }
                else
                {
                    hasUnverified = true;
                }
            }

            AllergensMask = combined;
            HasUnverifiedAllergens = hasUnverified;

            MarkAsUpdated(utcNow);
        }

        /// <summary>
        /// Заменяет набор категорий блюда (replace-семантика). Проверяет лимит
        /// в <c>MAX_CATEGORIES</c> категорий и отсутствие дубликатов. Существование
        /// <c>CategoryId</c> в справочнике <see cref="Category"/> Domain не проверяет —
        /// это задача Application Handler перед вызовом.
        /// </summary>
        /// <remarks>
        /// Изменения связующей таблицы не отслеживаются <c>SaveChangesInterceptor</c>,
        /// поэтому метод явно вызывает <see cref="MarkAsUpdated"/> в конце, чтобы
        /// индикатор «есть несохранённые правки» (<see cref="UpdatedAt"/> &gt;
        /// <see cref="PublishedAt"/>) работал корректно. <see cref="CategoriesPublished"/>
        /// при этом не трогается — связи опубликованной версии обновятся только
        /// при следующем <see cref="Publish"/>.
        /// </remarks>
        /// <param name="categoryIds">Новый набор идентификаторов категорий.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или <see cref="Result.Failure(Error)"/> с
        /// <see cref="DishesErrors.CategoryLimitExceeded"/> при превышении лимита
        /// или <see cref="DishesErrors.DuplicateCategoryId"/> при наличии дубликатов.
        /// </returns>
        public Result SetCategories(IReadOnlyCollection<Guid> categoryIds, DateTimeOffset utcNow)
        {
            if (categoryIds.Count > MAX_CATEGORIES)
            {
                return Result.Failure(DishesErrors.CategoryLimitExceeded);
            }

            if (categoryIds.Distinct().Count() != categoryIds.Count)
            {
                return Result.Failure(DishesErrors.DuplicateCategoryId);
            }

            _categories.Clear();
            foreach (var categoryId in categoryIds)
            {
                _categories.Add(new DishCategory(Id, categoryId));
            }

            MarkAsUpdated(utcNow);
            return Result.Success();
        }

        /// <summary>
        /// Заменяет набор тегов блюда (replace-семантика). Проверяет лимит
        /// в <c>MAX_TAGS</c> тегов и отсутствие дубликатов.
        /// </summary>
        /// <remarks>
        /// Существование <c>TagId</c> и пересчёт <see cref="Tag.UsageCount"/> — забота
        /// Application Handler (через <c>ITagRepository.FindOrCreateByNormalizedName</c>
        /// и явный <c>IncrementUsageCount</c>/<c>DecrementUsageCount</c>).
        /// Семантика обновления <see cref="UpdatedAt"/> и <see cref="TagsPublished"/>
        /// аналогична <see cref="SetCategories"/>.
        /// </remarks>
        /// <param name="tagIds">Новый набор идентификаторов тегов.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или <see cref="Result.Failure(Error)"/> с
        /// <see cref="DishesErrors.TagLimitExceeded"/> или
        /// <see cref="DishesErrors.DuplicateTagId"/>.
        /// </returns>
        public Result SetTags(IReadOnlyCollection<Guid> tagIds, DateTimeOffset utcNow)
        {
            if (tagIds.Count > MAX_TAGS)
            {
                return Result.Failure(DishesErrors.TagLimitExceeded);
            }

            if (tagIds.Distinct().Count() != tagIds.Count)
            {
                return Result.Failure(DishesErrors.DuplicateTagId);
            }

            _tags.Clear();
            foreach (var tagId in tagIds)
            {
                _tags.Add(new DishTag(Id, tagId));
            }

            MarkAsUpdated(utcNow);
            return Result.Success();
        }

        /// <summary>
        /// Явный сдвиг <see cref="UpdatedAt"/> для операций, которые не меняют поля Dish
        /// напрямую, но логически модифицируют блюдо (изменение состава категорий, тегов,
        /// шагов рецепта). Поднимает событие <see cref="DishUpdatedEvent"/>.
        /// </summary>
        /// <remarks>
        /// Альтернатива — автоматическое обновление <see cref="UpdatedAt"/> через
        /// <c>SaveChangesInterceptor</c> на изменения связанных сущностей. Оба механизма
        /// сосуществуют: явный вызов нужен для операций, где связанной сущности нет
        /// (например, переключение коллекции тегов).
        /// </remarks>
        /// <param name="utcNow">Текущее время UTC.</param>
        public void MarkAsUpdated(DateTimeOffset utcNow)
        {
            UpdatedAt = utcNow;

            RaiseDomainEvent(new DishUpdatedEvent(Id, AuthorUserId));
        }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Публикует блюдо: устанавливает <see cref="Status"/> в
        /// <see cref="DishStatus.Published"/>, сохраняет JSON-снепшот публичной версии,
        /// фиксирует <see cref="PublishedAt"/> и <see cref="PublishedVersionUpdatedAt"/>,
        /// синхронизирует <see cref="CategoriesPublished"/> и <see cref="TagsPublished"/>
        /// с рабочими коллекциями <see cref="Categories"/> и <see cref="Tags"/>.
        /// Поднимает событие <see cref="DishPublishedEvent"/>.
        /// </summary>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <param name="snapshot">JSON-снепшот, собранный Application-слоем.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или <see cref="Result.Failure(Error)"/> с одной из
        /// ошибок инвариантов публикации:
        /// <see cref="DishesErrors.CannotPublishArchivedDish"/>,
        /// <see cref="DishesErrors.DishAlreadyPublished"/>,
        /// <see cref="DishesErrors.MainImageRequiredForPublish"/>,
        /// <see cref="DishesErrors.StepsRequiredForPublish"/>,
        /// <see cref="DishesErrors.IngredientsRequiredForPublish"/>,
        /// <see cref="DishesErrors.TimingRequiredForPublish"/>.
        /// </returns>
        public Result Publish(DateTimeOffset utcNow, string snapshot)
        {
            if (Status == DishStatus.Archived)
            {
                return Result.Failure(DishesErrors.CannotPublishArchivedDish);
            }

            // Защита от спама DishPublishedEvent: если блюдо уже опубликовано
            // и в нём нет несохранённых правок относительно публичной версии,
            // повторная публикация не имеет смысла. Принудительная пересборка
            // снепшота (для каскадных операций админа) — отдельный механизм
            // через RebuildPublishedSnapshot (Этап 8+).
            if (Status == DishStatus.Published
                && PublishedAt.HasValue
                && UpdatedAt <= PublishedAt.Value)
            {
                return Result.Failure(DishesErrors.DishAlreadyPublished);
            }

            if (MainImageId is null)
            {
                return Result.Failure(DishesErrors.MainImageRequiredForPublish);
            }

            if (Recipe.Steps.Count == 0)
            {
                return Result.Failure(DishesErrors.StepsRequiredForPublish);
            }

            if (Recipe.Ingredients.Count == 0)
            {
                return Result.Failure(DishesErrors.IngredientsRequiredForPublish);
            }

            if (Recipe.Timing.TotalTimeMinutes <= 0)
            {
                return Result.Failure(DishesErrors.TimingRequiredForPublish);
            }

            Status = DishStatus.Published;
            PublishedAt = utcNow;
            PublishedVersionData = snapshot;
            PublishedVersionUpdatedAt = utcNow;
            UpdatedAt = utcNow;

            // Снепшот связующих таблиц: полная замена *Published из текущих рабочих.
            _categoriesPublished.Clear();
            foreach (var dc in _categories)
            {
                _categoriesPublished.Add(new DishCategoryPublished(dc.DishId, dc.CategoryId));
            }

            _tagsPublished.Clear();
            foreach (var dt in _tags)
            {
                _tagsPublished.Add(new DishTagPublished(dt.DishId, dt.TagId));
            }

            RaiseDomainEvent(new DishPublishedEvent(Id, AuthorUserId));
            return Result.Success();
        }

        /// <summary>
        /// Снимает блюдо с публикации: переводит в <see cref="DishStatus.Unpublished"/>,
        /// обнуляет снепшот, временные метки публикации и очищает
        /// <see cref="CategoriesPublished"/> / <see cref="TagsPublished"/>.
        /// Поднимает событие <see cref="DishUnpublishedEvent"/>.
        /// </summary>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или <see cref="Result.Failure(Error)"/> с
        /// <see cref="DishesErrors.DishNotPublished"/>, если блюдо не находится
        /// в статусе <see cref="DishStatus.Published"/>.
        /// </returns>
        public Result Unpublish(DateTimeOffset utcNow)
        {
            if (Status != DishStatus.Published)
            {
                return Result.Failure(DishesErrors.DishNotPublished);
            }

            Status = DishStatus.Unpublished;
            PublishedAt = null;
            PublishedVersionData = null;
            PublishedVersionUpdatedAt = null;
            UpdatedAt = utcNow;

            _categoriesPublished.Clear();
            _tagsPublished.Clear();

            RaiseDomainEvent(new DishUnpublishedEvent(Id, AuthorUserId));
            return Result.Success();
        }

        /// <summary>
        /// Архивирует блюдо (мягкое удаление): переводит в <see cref="DishStatus.Archived"/>,
        /// обнуляет снепшот и временные метки публикации, очищает <see cref="CategoriesPublished"/>
        /// и <see cref="TagsPublished"/>. Из <see cref="DishStatus.Archived"/> блюдо
        /// опубликовать обратно нельзя. Поднимает событие <see cref="DishArchivedEvent"/>.
        /// </summary>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или <see cref="Result.Failure(Error)"/> с
        /// <see cref="DishesErrors.DishAlreadyArchived"/>, если блюдо уже архивировано.
        /// </returns>
        public Result Archive(DateTimeOffset utcNow)
        {
            if (Status == DishStatus.Archived)
            {
                return Result.Failure(DishesErrors.DishAlreadyArchived);
            }

            Status = DishStatus.Archived;
            PublishedAt = null;
            PublishedVersionData = null;
            PublishedVersionUpdatedAt = null;
            UpdatedAt = utcNow;

            _categoriesPublished.Clear();
            _tagsPublished.Clear();

            RaiseDomainEvent(new DishArchivedEvent(Id, AuthorUserId));
            return Result.Success();
        }

        #endregion
    }
}
