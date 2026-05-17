using GastronomePlatform.Common.Domain.Primitives;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Enums;
using GastronomePlatform.Modules.Dishes.Domain.Errors;

namespace GastronomePlatform.Modules.Dishes.Domain.Entities
{
    /// <summary>
    /// Рецепт блюда — часть агрегата <see cref="Dish"/>. Содержит текстовые поля рецепта
    /// и 1:1-связки с <see cref="Timing"/>, <see cref="Yield"/>, <see cref="Nutrition"/>.
    /// Создаётся вместе с <see cref="Dish"/>, отдельно от него не существует.
    /// </summary>
    /// <remarks>
    /// Все методы изменения состояния — <see langword="internal"/>. Внешний код управляет
    /// рецептом исключительно через wrapper-методы на корне агрегата <see cref="Dish"/>.
    /// </remarks>
    public sealed class Recipe : Entity<Guid>
    {
        // Вспомогательное поле для навигации по шагам. Настраивается в RecipeConfiguration
        // через HasField("_steps") + PropertyAccessMode.Field.
        private readonly List<RecipeStep> _steps = new();
        private readonly List<RecipeIngredient> _ingredients = new();

        #region Properties

        /// <summary>
        /// Идентификатор блюда, к которому относится рецепт. FK на <c>dishes.Dishes</c>.
        /// </summary>
        public Guid DishId { get; private set; }

        /// <summary>
        /// Вводный текст автора перед инструкцией приготовления.
        /// </summary>
        public string? IntroductionText { get; private set; }

        /// <summary>
        /// Количество порций по умолчанию при отображении рецепта. Точка отсчёта
        /// для функции пересчёта количества ингредиентов. По умолчанию — 1.
        /// </summary>
        public int ServingsDefault { get; private set; }

        /// <summary>
        /// Признак, что рецепт содержит алкоголь. Используется для фильтрации
        /// в каталоге и для предупреждений в UI.
        /// </summary>
        public bool IsAlcoholic { get; private set; }

        /// <summary>
        /// Советы автора по приготовлению. Опционально.
        /// </summary>
        public string? AuthorTips { get; private set; }

        /// <summary>
        /// Рекомендации по сервировке блюда (с чем подавать, как украшать). Опционально.
        /// </summary>
        public string? ServingSuggestions { get; private set; }

        /// <summary>
        /// Дополнительные заметки автора, не входящие в основной текст рецепта. Опционально.
        /// </summary>
        public string? Notes { get; private set; }

        /// <summary>
        /// Идентификатор связанной записи КБЖУ. <see langword="null"/>, пока значения
        /// пищевой ценности не заданы автором.
        /// </summary>
        public Guid? NutritionId { get; private set; }

        /// <summary>
        /// Времена этапов приготовления (1:1). Создаётся вместе с Recipe пустым.
        /// </summary>
        public Timing Timing { get; private set; } = null!;

        /// <summary>
        /// Выход готового продукта и размер порции (1:1). Создаётся вместе с Recipe
        /// с дефолтными значениями.
        /// </summary>
        public Yield Yield { get; private set; } = null!;

        /// <summary>
        /// Пищевая ценность блюда (1:1). <see langword="null"/>, пока автор не задал
        /// значения через <c>Dish.UpdateNutrition(...)</c>.
        /// </summary>
        public Nutrition? Nutrition { get; private set; }

        /// <summary>
        /// Шаги приготовления рецепта (1:M). Read-only коллекция; добавление,
        /// обновление, удаление и переупорядочивание — только через wrapper-методы
        /// на <see cref="Dish"/>.
        /// </summary>
        public IReadOnlyList<RecipeStep> Steps => _steps;

        /// <summary>
        /// Ингредиенты рецепта (1:M). Read-only коллекция; добавление, обновление,
        /// удаление и переупорядочивание — только через wrapper-методы на <see cref="Dish"/>.
        /// </summary>
        public IReadOnlyList<RecipeIngredient> Ingredients => _ingredients;

        #endregion

        #region Constructors

        /// <summary>
        /// Конструктор без параметров для EF Core.
        /// </summary>
        private Recipe() : base() { }

        /// <summary>
        /// Приватный конструктор, используется только из <see cref="CreateForDish"/>.
        /// </summary>
        /// <param name="dishId">Идентификатор блюда, к которому привязан рецепт.</param>
        private Recipe(Guid dishId) : base(Guid.NewGuid())
        {
            DishId = dishId;
            ServingsDefault = 1;
            IsAlcoholic = false;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Создаёт новый Recipe вместе с пустым <see cref="Timing"/> и дефолтным
        /// <see cref="Yield"/>. Вызывается только из <c>Dish.Create(...)</c>.
        /// </summary>
        /// <param name="dishId">Идентификатор блюда, к которому привязан рецепт.</param>
        /// <returns>Новый <see cref="Recipe"/> с инициализированными Timing и Yield.</returns>
        internal static Recipe CreateForDish(Guid dishId)
        {
            var recipe = new Recipe(dishId);
            recipe.Timing = Timing.CreateForRecipe(recipe.Id);
            recipe.Yield = Yield.CreateForRecipe(recipe.Id);
            return recipe;
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Обновляет вводный текст рецепта.
        /// </summary>
        /// <param name="introductionText">Новый вводный текст. <see langword="null"/> — очистить.</param>
        internal void UpdateIntroduction(string? introductionText)
        {
            IntroductionText = introductionText;
        }

        /// <summary>
        /// Устанавливает признак содержания алкоголя в рецепте.
        /// </summary>
        /// <param name="isAlcoholic">Новое значение признака.</param>
        internal void SetIsAlcoholic(bool isAlcoholic)
        {
            IsAlcoholic = isAlcoholic;
        }

        /// <summary>
        /// Обновляет советы автора в рецепте.
        /// </summary>
        /// <param name="authorTips">Новый текст советов. <see langword="null"/> — очистить.</param>
        internal void UpdateAuthorTips(string? authorTips)
        {
            AuthorTips = authorTips;
        }

        /// <summary>
        /// Обновляет дополнительные заметки в рецепте.
        /// </summary>
        /// <param name="notes">Новый текст заметок. <see langword="null"/> — очистить.</param>
        internal void UpdateNotes(string? notes)
        {
            Notes = notes;
        }

        /// <summary>
        /// Обновляет рекомендации по сервировке.
        /// </summary>
        /// <param name="servingSuggestions">Новый текст рекомендаций. <see langword="null"/> — очистить.</param>
        internal void UpdateServingSuggestions(string? servingSuggestions)
        {
            ServingSuggestions = servingSuggestions;
        }

        /// <summary>
        /// Устанавливает количество порций по умолчанию. Должно быть не меньше 1.
        /// </summary>
        /// <param name="servingsDefault">Новое количество порций.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или
        /// <see cref="Result.Failure(Error)"/> с <see cref="DishesErrors.InvalidServingsDefault"/>.
        /// </returns>
        internal Result SetServingsDefault(int servingsDefault)
        {
            if (servingsDefault < 1)
            {
                return Result.Failure(DishesErrors.InvalidServingsDefault);
            }

            ServingsDefault = servingsDefault;
            return Result.Success();
        }

        /// <summary>
        /// Делегирует обновление времён приготовления внутреннему <see cref="Timing"/>.
        /// </summary>
        /// <param name="prepTimeMinutes">Время подготовки в минутах. <see langword="null"/> — не задано.</param>
        /// <param name="cookTimeMinutes">Время основного приготовления в минутах. <see langword="null"/> — не задано.</param>
        /// <param name="restTimeMinutes">Время отдыха в минутах. <see langword="null"/> — не задано.</param>
        /// <param name="activeTimeMinutes">Время активной работы повара в минутах. <see langword="null"/> — не задано.</param>
        /// <param name="totalTimeMinutes">Общее время в минутах. Используется, если <paramref name="isTotalManual"/> = <see langword="true"/>.</param>
        /// <param name="isTotalManual">
        /// <see langword="true"/> — общее время задано вручную; <see langword="false"/> — вычисляется
        /// как сумма prep + cook + rest.
        /// </param>
        /// <returns>
        /// Результат делегирующего вызова <see cref="Timing.UpdateTimes"/>.
        /// </returns>
        internal Result UpdateTiming(
            int? prepTimeMinutes,
            int? cookTimeMinutes,
            int? restTimeMinutes,
            int? activeTimeMinutes,
            int totalTimeMinutes,
            bool isTotalManual)
        {
            return Timing.UpdateTimes(
                prepTimeMinutes,
                cookTimeMinutes,
                restTimeMinutes,
                activeTimeMinutes,
                totalTimeMinutes,
                isTotalManual);
        }

        /// <summary>
        /// Делегирует обновление выхода внутреннему <see cref="Yield"/>.
        /// </summary>
        /// <param name="quantityTotal">Общее количество готового продукта.</param>
        /// <param name="yieldUnit">Единица выхода.</param>
        /// <param name="servingsCount">Количество порций.</param>
        /// <param name="gramsPerServing">Вес одной порции в граммах. <see langword="null"/> — не задано.</param>
        /// <returns>
        /// Результат делегирующего вызова <see cref="Yield.Update"/>.
        /// </returns>
        internal Result UpdateYield(
            decimal quantityTotal,
            YieldUnit yieldUnit,
            int servingsCount,
            decimal? gramsPerServing)
        {
            return Yield.Update(
                quantityTotal,
                yieldUnit,
                servingsCount,
                gramsPerServing);
        }

        /// <summary>
        /// Создаёт <see cref="Nutrition"/>, если её ещё нет, иначе обновляет существующую.
        /// Валидация значений (неотрицательность, согласованность Sugar/Carbs и SaturatedFats/Fats)
        /// ожидается на уровне команды через FluentValidation.
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
        internal void UpdateNutrition(
            NutritionCalcMethod calcMethod,
            decimal calories,
            decimal proteins,
            decimal fats,
            decimal? saturatedFats,
            decimal carbs,
            decimal? sugar,
            decimal? fiber,
            decimal? salt)
        {
            if (Nutrition is null)
            {
                Nutrition = Entities.Nutrition.Create(
                    calcMethod,
                    calories,
                    proteins,
                    fats,
                    saturatedFats,
                    carbs,
                    sugar,
                    fiber,
                    salt);
                NutritionId = Nutrition.Id;
            }
            else
            {
                Nutrition.Update(
                    calcMethod,
                    calories,
                    proteins,
                    fats,
                    saturatedFats,
                    carbs,
                    sugar,
                    fiber,
                    salt);
            }
        }

        /// <summary>
        /// Добавляет новый шаг к рецепту. Порядковый номер назначается автоматически
        /// как <c>Steps.Count + 1</c>.
        /// </summary>
        /// <param name="description">Основной текст шага.</param>
        /// <param name="title">Короткий заголовок. Опционально.</param>
        /// <param name="imageMediaId">Идентификатор иллюстрации в Media. Опционально.</param>
        /// <param name="videoUrl">URL внешнего видео. Опционально.</param>
        /// <param name="temperatureCelsius">Температура приготовления. Опционально.</param>
        /// <param name="timerMinutes">Время для таймера в минутах. Опционально.</param>
        /// <returns>Идентификатор созданного <see cref="RecipeStep"/>.</returns>
        internal Guid AddStep(
            string description,
            string? title,
            Guid? imageMediaId,
            string? videoUrl,
            int? temperatureCelsius,
            int? timerMinutes)
        {
            var order = _steps.Count + 1;
            var step = RecipeStep.CreateForRecipe(
                Id,
                order,
                description,
                title,
                imageMediaId,
                videoUrl,
                temperatureCelsius,
                timerMinutes);

            _steps.Add(step);
            return step.Id;
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
        /// <returns>
        /// <see cref="Result.Success()"/>, либо <see cref="Result.Failure(Error)"/>
        /// с <see cref="DishesErrors.StepNotFound"/> при отсутствии шага, либо с
        /// ошибкой валидации температуры/таймера.
        /// </returns>
        internal Result UpdateStep(
            Guid stepId,
            string description,
            string? title,
            Guid? imageMediaId,
            string? videoUrl,
            int? temperatureCelsius,
            int? timerMinutes)
        {
            var step = _steps.FirstOrDefault(s => s.Id == stepId);
            if (step is null)
            {
                return Result.Failure(DishesErrors.StepNotFound);
            }

            return step.Update(
                description,
                title,
                imageMediaId,
                videoUrl,
                temperatureCelsius,
                timerMinutes);
        }

        /// <summary>
        /// Удаляет шаг из рецепта и переупорядочивает оставшиеся: Order пересчитывается
        /// с 1 до N в порядке текущего размещения в коллекции.
        /// </summary>
        /// <param name="stepId">Идентификатор шага для удаления.</param>
        /// <returns>
        /// <see cref="Result.Success()"/>, либо <see cref="Result.Failure(Error)"/>
        /// с <see cref="DishesErrors.StepNotFound"/>.
        /// </returns>
        internal Result RemoveStep(Guid stepId)
        {
            var step = _steps.FirstOrDefault(s => s.Id == stepId);
            if (step is null)
            {
                return Result.Failure(DishesErrors.StepNotFound);
            }

            _steps.Remove(step);

            for (var i = 0; i < _steps.Count; i++)
            {
                _steps[i].SetOrder(i + 1);
            }

            return Result.Success();
        }

        /// <summary>
        /// Назначает новый порядок шагов. Принимает упорядоченный список Id шагов:
        /// первый получает Order = 1, второй — 2, и так далее. Список должен содержать
        /// все шаги рецепта без дубликатов.
        /// </summary>
        /// <param name="orderedStepIds">Список Id шагов в желаемом порядке.</param>
        /// <returns>
        /// <see cref="Result.Success()"/>, либо <see cref="Result.Failure(Error)"/>
        /// с <see cref="DishesErrors.InvalidStepOrder"/> (несовпадение размера или дубликаты)
        /// или <see cref="DishesErrors.StepNotFound"/> (Id не принадлежит рецепту).
        /// </returns>
        internal Result ReorderSteps(IReadOnlyList<Guid> orderedStepIds)
        {
            if (orderedStepIds.Count != _steps.Count
                || orderedStepIds.Distinct().Count() != orderedStepIds.Count)
            {
                return Result.Failure(DishesErrors.InvalidStepOrder);
            }

            var existingIds = _steps.Select(s => s.Id).ToHashSet();
            if (orderedStepIds.Any(id => !existingIds.Contains(id)))
            {
                return Result.Failure(DishesErrors.StepNotFound);
            }

            var reordered = new List<RecipeStep>(_steps.Count);
            for (var i = 0; i < orderedStepIds.Count; i++)
            {
                var step = _steps.First(s => s.Id == orderedStepIds[i]);
                step.SetOrder(i + 1);
                reordered.Add(step);
            }

            _steps.Clear();
            _steps.AddRange(reordered);

            return Result.Success();
        }

        /// <summary>
        /// Добавляет ингредиент из справочника к рецепту. Порядковый номер
        /// назначается автоматически как <c>Ingredients.Count + 1</c>.
        /// </summary>
        /// <param name="ingredientId">Идентификатор ингредиента из справочника.</param>
        /// <param name="ingredientSpecId">Идентификатор спецификации. Опционально.</param>
        /// <param name="quantity">Количество.</param>
        /// <param name="measureUnitId">Идентификатор единицы измерения.</param>
        /// <param name="isOptional">Признак опциональности.</param>
        /// <param name="preparationNote">Заметка по подготовке. Опционально.</param>
        /// <returns>Идентификатор созданного <see cref="RecipeIngredient"/>.</returns>
        internal Guid AddIngredientFromCatalog(
            Guid ingredientId,
            Guid? ingredientSpecId,
            decimal quantity,
            Guid measureUnitId,
            bool isOptional,
            string? preparationNote)
        {
            var order = _ingredients.Count + 1;
            var ri = RecipeIngredient.CreateFromCatalog(
                Id,
                ingredientId,
                ingredientSpecId,
                quantity,
                measureUnitId,
                order,
                isOptional,
                preparationNote);

            _ingredients.Add(ri);
            return ri.Id;
        }

        /// <summary>
        /// Добавляет ингредиент свободным текстом. Порядковый номер
        /// назначается автоматически.
        /// </summary>
        /// <param name="freeformText">Свободный текст ингредиента.</param>
        /// <param name="quantity">Количество.</param>
        /// <param name="measureUnitId">Идентификатор единицы измерения.</param>
        /// <param name="isOptional">Признак опциональности.</param>
        /// <param name="preparationNote">Заметка по подготовке. Опционально.</param>
        /// <returns>Идентификатор созданного <see cref="RecipeIngredient"/>.</returns>
        internal Guid AddIngredientFreeform(
            string freeformText,
            decimal quantity,
            Guid measureUnitId,
            bool isOptional,
            string? preparationNote)
        {
            var order = _ingredients.Count + 1;
            var ri = RecipeIngredient.CreateFreeform(
                Id,
                freeformText,
                quantity,
                measureUnitId,
                order,
                isOptional,
                preparationNote);

            _ingredients.Add(ri);
            return ri.Id;
        }

        /// <summary>
        /// Обновляет существующий ингредиент. Допускается смена источника catalog↔freeform.
        /// </summary>
        /// <param name="recipeIngredientId">Идентификатор позиции для обновления.</param>
        /// <param name="ingredientId">Новый идентификатор ингредиента или <see langword="null"/>.</param>
        /// <param name="ingredientSpecId">Новый идентификатор спецификации или <see langword="null"/>.</param>
        /// <param name="freeformText">Новый свободный текст или <see langword="null"/>.</param>
        /// <param name="quantity">Новое количество.</param>
        /// <param name="measureUnitId">Новый идентификатор единицы измерения.</param>
        /// <param name="isOptional">Признак опциональности.</param>
        /// <param name="preparationNote">Заметка по подготовке.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или <see cref="Result.Failure(Error)"/>
        /// с <see cref="DishesErrors.RecipeIngredientNotFound"/> при отсутствии позиции,
        /// или с ошибкой валидации (XOR / Quantity).
        /// </returns>
        internal Result UpdateIngredient(
            Guid recipeIngredientId,
            Guid? ingredientId,
            Guid? ingredientSpecId,
            string? freeformText,
            decimal quantity,
            Guid measureUnitId,
            bool isOptional,
            string? preparationNote)
        {
            var ri = _ingredients.FirstOrDefault(i => i.Id == recipeIngredientId);
            if (ri is null)
            {
                return Result.Failure(DishesErrors.RecipeIngredientNotFound);
            }

            return ri.Update(
                ingredientId,
                ingredientSpecId,
                freeformText,
                quantity,
                measureUnitId,
                isOptional,
                preparationNote);
        }

        /// <summary>
        /// Удаляет ингредиент из рецепта и переупорядочивает оставшиеся
        /// (Order пересчитывается с 1 по N).
        /// </summary>
        /// <param name="recipeIngredientId">Идентификатор позиции для удаления.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или <see cref="Result.Failure(Error)"/>
        /// с <see cref="DishesErrors.RecipeIngredientNotFound"/>.
        /// </returns>
        internal Result RemoveIngredient(Guid recipeIngredientId)
        {
            var ri = _ingredients.FirstOrDefault(i => i.Id == recipeIngredientId);
            if (ri is null)
            {
                return Result.Failure(DishesErrors.RecipeIngredientNotFound);
            }

            _ingredients.Remove(ri);

            for (var i = 0; i < _ingredients.Count; i++)
            {
                _ingredients[i].SetOrder(i + 1);
            }

            return Result.Success();
        }

        /// <summary>
        /// Назначает новый порядок ингредиентов. Принимает упорядоченный список Id:
        /// первый получает Order = 1, второй — 2, и так далее. Список должен содержать
        /// все позиции рецепта без дубликатов.
        /// </summary>
        /// <param name="orderedIngredientIds">Список Id ингредиентов в желаемом порядке.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или <see cref="Result.Failure(Error)"/>
        /// с <see cref="DishesErrors.InvalidIngredientOrder"/> (несовпадение размера
        /// или дубликаты) или <see cref="DishesErrors.RecipeIngredientNotFound"/> (Id
        /// не принадлежит рецепту).
        /// </returns>
        internal Result ReorderIngredients(IReadOnlyList<Guid> orderedIngredientIds)
        {
            if (orderedIngredientIds.Count != _ingredients.Count
                || orderedIngredientIds.Distinct().Count() != orderedIngredientIds.Count)
            {
                return Result.Failure(DishesErrors.InvalidIngredientOrder);
            }

            var existingIds = _ingredients.Select(i => i.Id).ToHashSet();
            if (orderedIngredientIds.Any(id => !existingIds.Contains(id)))
            {
                return Result.Failure(DishesErrors.RecipeIngredientNotFound);
            }

            var reordered = new List<RecipeIngredient>(_ingredients.Count);
            for (var i = 0; i < orderedIngredientIds.Count; i++)
            {
                var ri = _ingredients.First(x => x.Id == orderedIngredientIds[i]);
                ri.SetOrder(i + 1);
                reordered.Add(ri);
            }

            _ingredients.Clear();
            _ingredients.AddRange(reordered);

            return Result.Success();
        }

        #endregion
    }
}
