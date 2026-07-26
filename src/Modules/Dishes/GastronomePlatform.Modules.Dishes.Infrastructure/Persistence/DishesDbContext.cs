using GastronomePlatform.Modules.Dishes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence
{
    /// <summary>
    /// DbContext модуля Dishes.
    /// Работает со схемой <c>dishes</c> базы данных PostgreSQL.
    /// </summary>
    public sealed class DishesDbContext : DbContext
    {
        /// <summary>
        /// Таблица справочника единиц измерения.
        /// </summary>
        public DbSet<MeasureUnit> MeasureUnits => Set<MeasureUnit>();

        /// <summary>
        /// Таблица пользовательских тегов.
        /// </summary>
        public DbSet<Tag> Tags => Set<Tag>();

        /// <summary>
        /// Таблица записей пищевой ценности (КБЖУ).
        /// </summary>
        public DbSet<Nutrition> Nutritions => Set<Nutrition>();

        /// <summary>
        /// Таблица категорий каталога блюд.
        /// </summary>
        public DbSet<Category> Categories => Set<Category>();

        /// <summary>
        /// Таблица справочника ингредиентов.
        /// </summary>
        public DbSet<Ingredient> Ingredients => Set<Ingredient>();

        /// <summary>
        /// Таблица сортов/видов ингредиентов.
        /// </summary>
        public DbSet<IngredientSpec> IngredientSpecs => Set<IngredientSpec>();

        /// <summary>
        /// Таблица блюд — корней агрегата каталога.
        /// </summary>
        public DbSet<Dish> Dishes => Set<Dish>();

        /// <summary>
        /// Рецепты блюд — часть агрегата <c>Dish</c>, 1:1.
        /// </summary>
        public DbSet<Recipe> Recipes => Set<Recipe>();

        /// <summary>
        /// Времена этапов приготовления — часть агрегата <c>Dish</c>, 1:1 с <c>Recipe</c>.
        /// </summary>
        public DbSet<Timing> Timings => Set<Timing>();

        /// <summary>
        /// Выход готового продукта — часть агрегата <c>Dish</c>, 1:1 с <c>Recipe</c>.
        /// </summary>
        public DbSet<Yield> Yields => Set<Yield>();

        /// <summary>
        /// Шаги рецептов — часть агрегата <c>Dish</c>, 1:M с <c>Recipe</c>.
        /// </summary>
        public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();

        /// <summary>
        /// Ингредиенты рецептов — часть агрегата <c>Dish</c>, 1:M с <c>Recipe</c>.
        /// </summary>
        public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();

        /// <summary>
        /// Связи блюд с категориями (рабочая версия) — M:M.
        /// </summary>
        public DbSet<DishCategory> DishCategories => Set<DishCategory>();

        /// <summary>
        /// Связи блюд с тегами (рабочая версия) — M:M.
        /// </summary>
        public DbSet<DishTag> DishTags => Set<DishTag>();

        /// <summary>
        /// Связи блюд с категориями (опубликованная версия). Заполняются при
        /// <c>Dish.Publish(...)</c>, очищаются при <c>Unpublish</c> / <c>Archive</c>.
        /// Используются каталожным фильтром UC-DSH-054.
        /// </summary>
        public DbSet<DishCategoryPublished> DishCategoriesPublished => Set<DishCategoryPublished>();

        /// <summary>
        /// Связи блюд с тегами (опубликованная версия).
        /// </summary>
        public DbSet<DishTagPublished> DishTagsPublished => Set<DishTagPublished>();

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DishesDbContext"/>.
        /// </summary>
        /// <param name="options">Параметры конфигурации DbContext.</param>
        public DishesDbContext(DbContextOptions<DishesDbContext> options) : base(options)
        {
        }

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DishesDbContext).Assembly);

            modelBuilder.HasDefaultSchema("dishes");

            // Id всех сущностей модуля назначается доменом (Guid.NewGuid() в конструкторах),
            // а не БД. Без явного ValueGenerated.Never EF считает Guid-ключи генерируемыми
            // и при обнаружении новой дочерней сущности в коллекции уже отслеживаемого
            // агрегата (DetectChanges через навигацию) трактует заполненный ключ как признак
            // существующей записи — сущность помечается Modified вместо Added, и SaveChanges
            // выполняет UPDATE несуществующей строки (DbUpdateConcurrencyException).
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var primaryKey = entityType.FindPrimaryKey();
                if (primaryKey is null)
                {
                    continue;
                }

                foreach (var property in primaryKey.Properties.Where(p => p.ClrType == typeof(Guid)))
                {
                    property.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
                }
            }
        }
    }
}
