using GastronomePlatform.Modules.Dishes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для <see cref="Recipe"/> — части агрегата <see cref="Dish"/>.
    /// </summary>
    /// <remarks>
    /// Связки:
    /// <list type="bullet">
    ///   <item>Recipe ↔ Dish — 1:1, Cascade при удалении Dish.</item>
    ///   <item>Recipe ↔ Nutrition — 1:1 (UNIQUE на <c>NutritionId</c>),
    ///   SetNull при удалении Nutrition (рецепт остаётся без КБЖУ).</item>
    /// </list>
    /// </remarks>
    internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
    {
        /// <summary>
        /// Настраивает таблицу <c>dishes.Recipes</c>, связки и индексы.
        /// </summary>
        /// <param name="builder">Билдер конфигурации сущности.</param>
        public void Configure(EntityTypeBuilder<Recipe> builder)
        {
            builder.ToTable("Recipes");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.DishId)
                .IsRequired();

            builder.Property(r => r.IntroductionText);

            builder.Property(r => r.ServingsDefault)
                .IsRequired();

            builder.Property(r => r.IsAlcoholic)
                .IsRequired();

            builder.Property(r => r.AuthorTips);

            builder.Property(r => r.ServingSuggestions);

            builder.Property(r => r.Notes);

            builder.Property(r => r.NutritionId);

            // Recipe ↔ Dish (1:1, Cascade)
            // На стороне Dish — навигация Recipe; на стороне Recipe — только FK DishId.
            // EF Core сам создаст UNIQUE-индекс на DishId как часть 1:1-связки.
            builder.HasOne<Dish>()
                .WithOne(d => d.Recipe)
                .HasForeignKey<Recipe>(r => r.DishId)
                .OnDelete(DeleteBehavior.Cascade);

            // Recipe → Nutrition (1:1, SetNull)
            // Nutrition не имеет обратной навигации, FK nullable. UNIQUE-индекс
            // на NutritionId добавлен явно ниже (множественные NULL допустимы — PG-default).
            builder.HasOne(r => r.Nutrition)
                .WithMany()
                .HasForeignKey(r => r.NutritionId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(r => r.NutritionId)
                .IsUnique();

            // Вспомогательные поля для навигации по коллекциям.
            // Steps/Ingredients — read-only IReadOnlyList<T> без сеттера,
            // EF Core пишет в _steps / _ingredients напрямую.
            builder.Navigation(r => r.Steps)
                .HasField("_steps")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.Navigation(r => r.Ingredients)
                .HasField("_ingredients")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
