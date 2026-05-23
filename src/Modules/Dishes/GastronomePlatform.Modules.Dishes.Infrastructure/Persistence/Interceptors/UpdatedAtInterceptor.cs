using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Interceptors
{
    /// <summary>
    /// SaveChanges-интерсептор, автоматически обновляющий <see cref="Dish.UpdatedAt"/>
    /// при изменении любой сущности, входящей в агрегат Dish:
    /// <see cref="Recipe"/>, <see cref="RecipeStep"/>, <see cref="RecipeIngredient"/>,
    /// <see cref="Timing"/>, <see cref="Yield"/>, <see cref="Nutrition"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Defensive-слой. Основной путь — wrapper-методы на <see cref="Dish"/>, явно
    /// вызывающие <see cref="Dish.MarkAsUpdated"/>. Интерсептор покрывает крайние
    /// случаи, когда модификация дочерней сущности произошла без вызова wrapper'а
    /// (например, прямой <c>EntityState.Modified</c> на <see cref="RecipeStep"/>
    /// из тестов или сценариев каскадного обновления).
    /// </para>
    /// <para>
    /// Поиск родительского <see cref="Dish"/> идёт через <c>ChangeTracker</c>, без
    /// обращения к БД. Если родительская сущность не загружена в трекер — обновление
    /// <see cref="Dish.UpdatedAt"/> не выполняется (нет данных для разрешения связи).
    /// </para>
    /// <para>
    /// Не триггерит обновление при изменении денормализованных счётчиков
    /// (<see cref="Dish.RatingAvg"/>, <see cref="Dish.RatingCount"/>,
    /// <see cref="Dish.ViewsCount"/>, <see cref="Dish.FavoritesCount"/>) и при
    /// обновлении JSON-снепшота (<see cref="Dish.PublishedVersionData"/>,
    /// <see cref="Dish.PublishedVersionUpdatedAt"/>).
    /// </para>
    /// </remarks>
    internal sealed class UpdatedAtInterceptor : SaveChangesInterceptor
    {
        // Свойства Dish, изменение которых НЕ должно сдвигать UpdatedAt.
        // Денормализованные счётчики, снепшот публичной версии и сам UpdatedAt
        // (чтобы изменение поля не триггерило само себя).
        private static readonly HashSet<string> _excludedDishProperties = new()
        {
            nameof(Dish.PublishedVersionData),
            nameof(Dish.PublishedVersionUpdatedAt),
            nameof(Dish.RatingAvg),
            nameof(Dish.RatingCount),
            nameof(Dish.ViewsCount),
            nameof(Dish.FavoritesCount),
            nameof(Dish.UpdatedAt),
        };

        private readonly IDateTimeProvider _dateTimeProvider;

        /// <summary>
        /// Создаёт новый <see cref="UpdatedAtInterceptor"/>.
        /// </summary>
        /// <param name="dateTimeProvider">Провайдер системного времени.</param>
        public UpdatedAtInterceptor(IDateTimeProvider dateTimeProvider)
        {
            _dateTimeProvider = dateTimeProvider;
        }

        /// <summary>
        /// Перехватывает синхронный <c>SaveChanges</c>, применяет логику обновления
        /// <see cref="Dish.UpdatedAt"/> до фиксации изменений.
        /// </summary>
        /// <param name="eventData">Данные события EF Core.</param>
        /// <param name="result">Результат, передаваемый по цепочке интерсепторов.</param>
        /// <returns>Результат базового вызова <see cref="SaveChangesInterceptor.SavingChanges"/>.</returns>
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            ApplyUpdatedAt(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        /// <summary>
        /// Перехватывает асинхронный <c>SaveChangesAsync</c>, применяет логику обновления
        /// <see cref="Dish.UpdatedAt"/> до фиксации изменений.
        /// </summary>
        /// <param name="eventData">Данные события EF Core.</param>
        /// <param name="result">Результат, передаваемый по цепочке интерсепторов.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Результат базового вызова <see cref="SaveChangesInterceptor.SavingChangesAsync"/>.</returns>
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ApplyUpdatedAt(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        /// <summary>
        /// Основная логика интерсептора: собирает идентификаторы затронутых Dish
        /// в два прохода по <c>ChangeTracker</c> и устанавливает им <c>UpdatedAt</c>.
        /// </summary>
        /// <param name="context">DbContext текущей операции. Может быть <see langword="null"/>.</param>
        private void ApplyUpdatedAt(DbContext? context)
        {
            if (context is null)
            {
                return;
            }

            var utcNow = _dateTimeProvider.UtcNow;
            var affectedDishIds = new HashSet<Guid>();

            // Проход 1: собираем затронутые Dish по всем релевантным entry трекера.
            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                {
                    continue;
                }

                switch (entry.Entity)
                {
                    case Dish dish when IsMeaningfulDishChange(entry):
                        affectedDishIds.Add(dish.Id);
                        break;

                    case Recipe recipe:
                        affectedDishIds.Add(recipe.DishId);
                        break;

                    case RecipeStep step:
                        TryAddDishIdFromRecipeId(context, step.RecipeId, affectedDishIds);
                        break;

                    case RecipeIngredient ri:
                        TryAddDishIdFromRecipeId(context, ri.RecipeId, affectedDishIds);
                        break;

                    case Timing timing:
                        TryAddDishIdFromRecipeId(context, timing.RecipeId, affectedDishIds);
                        break;

                    case Yield yield:
                        TryAddDishIdFromRecipeId(context, yield.RecipeId, affectedDishIds);
                        break;

                    case Nutrition nutrition:
                        TryAddDishIdFromNutritionOwner(context, nutrition.Id, affectedDishIds);
                        break;
                }
            }

            if (affectedDishIds.Count == 0)
            {
                return;
            }

            // Проход 2: устанавливаем UpdatedAt на каждом затронутом Dish.
            // Пропускаем Added (UpdatedAt уже задан конструктором) и Deleted
            // (нет смысла обновлять то, что удаляется).
            foreach (var dishId in affectedDishIds)
            {
                var dishEntry = context.ChangeTracker.Entries<Dish>()
                    .FirstOrDefault(e => e.Entity.Id == dishId);

                if (dishEntry is null)
                {
                    continue;
                }

                if (dishEntry.State is EntityState.Added or EntityState.Deleted)
                {
                    continue;
                }

                // Прямая запись через Property, без вызова Dish.MarkAsUpdated —
                // чтобы не поднимать дубль DishUpdatedEvent (его поднимают wrapper-методы
                // на Dish, через которые и пришло основное изменение).
                dishEntry.Property(nameof(Dish.UpdatedAt)).CurrentValue = utcNow;
            }
        }

        /// <summary>
        /// Определяет, нужно ли сдвигать <see cref="Dish.UpdatedAt"/> на основании
        /// текущего состояния entry. Для <see cref="EntityState.Added"/> и
        /// <see cref="EntityState.Deleted"/> возвращает <see langword="false"/>.
        /// Для <see cref="EntityState.Modified"/> — <see langword="true"/>, если изменено
        /// хотя бы одно свойство, не входящее в <see cref="_excludedDishProperties"/>.
        /// </summary>
        /// <param name="entry">Entry трекера для <see cref="Dish"/>.</param>
        /// <returns>
        /// <see langword="true"/>, если изменение должно сдвинуть <see cref="Dish.UpdatedAt"/>.
        /// </returns>
        private static bool IsMeaningfulDishChange(EntityEntry entry)
        {
            if (entry.State != EntityState.Modified)
            {
                return false;
            }

            foreach (var property in entry.Properties)
            {
                if (!property.IsModified)
                {
                    continue;
                }

                if (_excludedDishProperties.Contains(property.Metadata.Name))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Находит <see cref="Recipe"/> в трекере по его Id и, если найден, добавляет
        /// его <see cref="Recipe.DishId"/> в множество затронутых блюд.
        /// </summary>
        /// <param name="context">DbContext с активным ChangeTracker.</param>
        /// <param name="recipeId">Идентификатор Recipe, владеющего изменённой дочерней сущностью.</param>
        /// <param name="affected">Множество, в которое добавляется DishId.</param>
        private static void TryAddDishIdFromRecipeId(
            DbContext context,
            Guid recipeId,
            HashSet<Guid> affected)
        {
            var recipeEntry = context.ChangeTracker.Entries<Recipe>()
                .FirstOrDefault(e => e.Entity.Id == recipeId);

            if (recipeEntry is not null)
            {
                affected.Add(recipeEntry.Entity.DishId);
            }
        }

        /// <summary>
        /// Находит <see cref="Recipe"/>, владеющий заданной <see cref="Nutrition"/>,
        /// по совпадению <see cref="Recipe.NutritionId"/>. Если <see cref="Nutrition"/>
        /// принадлежит другой сущности (например, <c>Ingredient.DefaultNutrition</c>)
        /// — Recipe не находится, и обновление не выполняется.
        /// </summary>
        /// <param name="context">DbContext с активным ChangeTracker.</param>
        /// <param name="nutritionId">Идентификатор изменённой <see cref="Nutrition"/>.</param>
        /// <param name="affected">Множество, в которое добавляется DishId.</param>
        private static void TryAddDishIdFromNutritionOwner(
            DbContext context,
            Guid nutritionId,
            HashSet<Guid> affected)
        {
            var ownerRecipe = context.ChangeTracker.Entries<Recipe>()
                .FirstOrDefault(e => e.Entity.NutritionId == nutritionId);

            if (ownerRecipe is not null)
            {
                affected.Add(ownerRecipe.Entity.DishId);
            }
        }
    }
}
