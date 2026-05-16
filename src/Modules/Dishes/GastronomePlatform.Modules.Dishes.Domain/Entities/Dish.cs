using GastronomePlatform.Common.Domain.Primitives;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Enums;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Events;

namespace GastronomePlatform.Modules.Dishes.Domain.Entities
{
    /// <summary>
    /// Блюдо — корень агрегата каталога. Публичная карточка блюда, которую видят все
    /// пользователи (включая гостей). Содержит ссылку на <c>Recipe</c> и внутреннее
    /// состояние: статус, модерацию, рейтинг, опубликованный снепшот.
    /// </summary>
    /// <remarks>
    /// Двухслойная модель: основные поля карточки хранятся плоско, плюс jsonb-снепшот
    /// <see cref="PublishedVersionData"/> с публичной версией для быстрой отдачи посетителям.
    /// Снепшот заполняется при <see cref="Publish"/> и обнуляется при <see cref="Unpublish"/>
    /// и <see cref="Archive"/>.
    /// </remarks>
    public sealed class Dish : AggregateRoot<Guid>
    {
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
        /// По умолчанию — <see cref="DietLabels.None"/>. Устанавливается автором в
        /// <see cref="UpdateCard"/>.
        /// </summary>
        public DietLabels DietLabelsMask { get; private set; }

        // AllergensMask и HasUnverifiedAllergens — денормализованные публичные маркеры.
        // Источник правды — состав Recipe.RecipeIngredients (Ingredient.AllergenType для
        // ссылочных позиций, флаг unverified для freeform). Хранятся в корне агрегата
        // для быстрого чтения в каталожных запросах.
        //
        // TODO: метод RecalculateAllergens(...) реализуется после добавления Recipe
        // и RecipeIngredient. Вызывается из Application-handler'ов модификации состава
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

        #endregion

        #region Constructors

        /// <summary>
        /// Конструктор без параметров для EF Core.
        /// EF Core использует его при материализации объектов из БД.
        /// </summary>
        private Dish() : base() { }

        /// <summary>
        /// Создаёт новый экземпляр <see cref="Dish"/>. Используется только из фабричного
        /// метода <see cref="Create"/>. Slug ожидается уже сгенерированным и проверенным
        /// на уникальность на уровне Application.
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
        /// Создаёт новое блюдо в статусе <see cref="DishStatus.Draft"/>.
        /// Поднимает доменное событие <see cref="DishCreatedEvent"/>.
        /// Валидация параметров (длина строк, формат slug) ожидается на уровне команды
        /// через FluentValidation — в фабрике проверки не выполняются.
        /// </summary>
        /// <param name="authorUserId">Идентификатор автора (пользователя из модуля Users).</param>
        /// <param name="name">Отображаемое название блюда.</param>
        /// <param name="slug">URL-friendly идентификатор, сгенерированный Application-слоем.</param>
        /// <param name="difficultyLevel">Уровень сложности приготовления.</param>
        /// <param name="costEstimate">Грубая оценка стоимости.</param>
        /// <param name="ownerType">Тип владельца — денормализуется из ролей автора.</param>
        /// <param name="utcNow">Текущее время UTC (передаётся из <c>IDateTimeProvider</c> в Handler).</param>
        /// <returns>Новый экземпляр <see cref="Dish"/> с зарегистрированным событием <see cref="DishCreatedEvent"/>.</returns>
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

            dish.RaiseDomainEvent(new DishCreatedEvent(dish.Id, dish.AuthorUserId));
            return dish;
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Обновляет основные поля карточки блюда. Поднимает событие <see cref="DishUpdatedEvent"/>.
        /// </summary>
        /// <remarks>
        /// Изменение опубликованного блюда НЕ обновляет автоматически
        /// <see cref="PublishedVersionData"/> — требуется явный вызов <see cref="Publish"/>
        /// для перепубликации. Это позволяет автору готовить правки в основной таблице,
        /// не затрагивая публичную версию.
        /// </remarks>
        /// <param name="name">Новое название.</param>
        /// <param name="shortDescription">Краткая подводка. <see langword="null"/> — очистить.</param>
        /// <param name="description">Полное описание (markdown). <see langword="null"/> — очистить.</param>
        /// <param name="difficultyLevel">Новый уровень сложности.</param>
        /// <param name="costEstimate">Новая оценка стоимости.</param>
        /// <param name="ownerType">Новый тип владельца.</param>
        /// <param name="dietLabelsMask">Новая маска диетических меток.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        public void UpdateCard(
            string name,
            string? shortDescription,
            string? description,
            DifficultyLevel difficultyLevel,
            CostEstimate costEstimate,
            OwnerType ownerType,
            DietLabels dietLabelsMask,
            DateTimeOffset utcNow)
        {
            Name = name;
            ShortDescription = shortDescription;
            Description = description;
            DifficultyLevel = difficultyLevel;
            CostEstimate = costEstimate;
            OwnerType = ownerType;
            DietLabelsMask = dietLabelsMask;
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
        /// фиксирует <see cref="PublishedAt"/> и <see cref="PublishedVersionUpdatedAt"/>.
        /// Поднимает событие <see cref="DishPublishedEvent"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Допустимы переходы: <see cref="DishStatus.Draft"/> → <see cref="DishStatus.Published"/>,
        /// <see cref="DishStatus.Unpublished"/> → <see cref="DishStatus.Published"/>,
        /// <see cref="DishStatus.Published"/> → <see cref="DishStatus.Published"/> (перепубликация).
        /// Из <see cref="DishStatus.Archived"/> публикация запрещена.
        /// </para>
        /// <para>
        /// Сравнение «есть ли изменения с момента последней публикации» (для ошибки
        /// <see cref="DishesErrors.DishAlreadyPublished"/>) выполняется на уровне Application
        /// Handler через сравнение <see cref="UpdatedAt"/> и <see cref="PublishedAt"/>.
        /// </para>
        /// </remarks>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <param name="snapshot">
        /// Готовый JSON-снепшот, собранный Application-слоем перед вызовом метода.
        /// </param>
        /// <returns>
        /// <see cref="Result.Success()"/> при успешной публикации;
        /// <see cref="Result.Failure(Error)"/> с ошибкой
        /// <see cref="DishesErrors.CannotPublishArchivedDish"/> или
        /// <see cref="DishesErrors.MainImageRequiredForPublish"/>, если инварианты нарушены.
        /// </returns>
        public Result Publish(DateTimeOffset utcNow, string snapshot)
        {
            if (Status == DishStatus.Archived)
            {
                return Result.Failure(DishesErrors.CannotPublishArchivedDish);
            }

            if (MainImageId is null)
            {
                return Result.Failure(DishesErrors.MainImageRequiredForPublish);
            }

            // TODO: после добавления Recipe/RecipeStep/RecipeIngredient — расширить проверки:
            //   - StepsRequiredForPublish       — Recipe.Steps.Count > 0
            //   - IngredientsRequiredForPublish — Recipe.RecipeIngredients.Count > 0
            //   - TimingRequiredForPublish      — Recipe.Timing.TotalTimeMinutes > 0

            Status = DishStatus.Published;
            PublishedAt = utcNow;
            PublishedVersionData = snapshot;
            PublishedVersionUpdatedAt = utcNow;
            UpdatedAt = utcNow;

            RaiseDomainEvent(new DishPublishedEvent(Id, AuthorUserId));
            return Result.Success();
        }

        /// <summary>
        /// Снимает блюдо с публикации: устанавливает <see cref="Status"/> в
        /// <see cref="DishStatus.Unpublished"/>, обнуляет снепшот и связанные временные метки.
        /// Поднимает событие <see cref="DishUnpublishedEvent"/>.
        /// </summary>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> при успешном переходе;
        /// <see cref="Result.Failure(Error)"/> с ошибкой <see cref="DishesErrors.DishNotPublished"/>,
        /// если блюдо не в статусе <see cref="DishStatus.Published"/>.
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

            RaiseDomainEvent(new DishUnpublishedEvent(Id, AuthorUserId));
            return Result.Success();
        }

        /// <summary>
        /// Архивирует блюдо (мягкое удаление): устанавливает <see cref="Status"/> в
        /// <see cref="DishStatus.Archived"/>, обнуляет снепшот и связанные временные метки.
        /// Поднимает событие <see cref="DishArchivedEvent"/>. Из <see cref="DishStatus.Archived"/>
        /// блюдо опубликовать обратно нельзя.
        /// </summary>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> при успешной архивации;
        /// <see cref="Result.Failure(Error)"/> с ошибкой <see cref="DishesErrors.DishAlreadyArchived"/>,
        /// если блюдо уже архивировано.
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

            RaiseDomainEvent(new DishArchivedEvent(Id, AuthorUserId));
            return Result.Success();
        }

        #endregion
    }
}
