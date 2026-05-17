using GastronomePlatform.Modules.Dishes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для <see cref="RecipeStep"/> — части агрегата <see cref="Dish"/>.
    /// </summary>
    /// <remarks>
    /// Связка с Recipe — M:1, Cascade при удалении Recipe. UNIQUE на паре
    /// <c>(RecipeId, Order)</c> гарантирует уникальность порядка шагов в рамках рецепта.
    /// CHECK-constraints дублируют доменную валидацию диапазонов температуры и таймера
    /// (defense-in-depth).
    /// </remarks>
    internal sealed class RecipeStepConfiguration : IEntityTypeConfiguration<RecipeStep>
    {
        /// <summary>
        /// Настраивает таблицу <c>dishes.RecipeSteps</c>, связку с Recipe,
        /// индексы и CHECK-constraints.
        /// </summary>
        /// <param name="builder">Билдер конфигурации сущности.</param>
        public void Configure(EntityTypeBuilder<RecipeStep> builder)
        {
            builder.ToTable("RecipeSteps", t =>
            {
                t.HasCheckConstraint(
                    "CK_RecipeSteps_OrderPositive",
                    "\"Order\" > 0");

                t.HasCheckConstraint(
                    "CK_RecipeSteps_TemperatureRange",
                    "\"TemperatureCelsius\" IS NULL OR (\"TemperatureCelsius\" BETWEEN -30 AND 300)");

                t.HasCheckConstraint(
                    "CK_RecipeSteps_TimerRange",
                    "\"TimerMinutes\" IS NULL OR (\"TimerMinutes\" BETWEEN 1 AND 1440)");
            });

            builder.HasKey(s => s.Id);

            builder.Property(s => s.RecipeId)
                .IsRequired();

            builder.Property(s => s.Order)
                .IsRequired();

            builder.Property(s => s.Title)
                .HasMaxLength(200);

            builder.Property(s => s.Description)
                .IsRequired();

            builder.Property(s => s.ImageMediaId);

            builder.Property(s => s.VideoUrl)
                .HasMaxLength(500);

            builder.Property(s => s.TemperatureCelsius);

            builder.Property(s => s.TimerMinutes);

            // RecipeStep → Recipe (M:1, Cascade). Навигация Steps на Recipe.
            builder.HasOne<Recipe>()
                .WithMany(r => r.Steps)
                .HasForeignKey(s => s.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            // UNIQUE (RecipeId, Order) — порядок уникален в рамках рецепта.
            builder.HasIndex(s => new { s.RecipeId, s.Order })
                .IsUnique();
        }
    }
}
