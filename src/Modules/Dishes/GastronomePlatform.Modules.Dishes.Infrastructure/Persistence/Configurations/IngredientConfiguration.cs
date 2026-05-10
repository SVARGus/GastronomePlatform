using GastronomePlatform.Modules.Dishes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для сущности <see cref="Ingredient"/>.
    /// Описывает таблицу <c>dishes.Ingredients</c>: колонки, ограничения,
    /// FK на <c>MeasureUnit</c> и <c>Nutrition</c>, индексы, CHECK-constraints.
    /// </summary>
    internal sealed class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<Ingredient> builder)
        {
            builder.ToTable("Ingredients", t =>
            {
                // Условный инвариант: если IsLiquid = true, то DensityApprox обязателен.
                t.HasCheckConstraint(
                    "CK_Ingredients_LiquidDensity",
                    "\"IsLiquid\" = false OR \"DensityApprox\" IS NOT NULL");

                // Условный инвариант: если IsAllergen = true, то AllergenType обязателен.
                t.HasCheckConstraint(
                    "CK_Ingredients_AllergenType",
                    "\"IsAllergen\" = false OR \"AllergenType\" IS NOT NULL");
            });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.PluralName)
                .HasMaxLength(200);

            builder.Property(x => x.Description);

            builder.Property(x => x.ImageMediaId);

            builder.Property(x => x.IsLiquid)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.DensityApprox)
                .HasPrecision(5, 3);

            builder.Property(x => x.IsAllergen)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.AllergenType);

            builder.Property(x => x.BaseMeasureUnitId)
                .IsRequired();

            builder.Property(x => x.DefaultNutritionId);

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasIndex(x => x.Name)
                .IsUnique();

            // FK на MeasureUnit: единицу измерения нельзя удалить, пока есть ингредиенты с ней.
            builder.HasOne<MeasureUnit>()
                .WithMany()
                .HasForeignKey(x => x.BaseMeasureUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // FK на Nutrition (default КБЖУ): при удалении Nutrition обнуляется ссылка,
            // ингредиент остаётся живым.
            builder.HasOne<Nutrition>()
                .WithMany()
                .HasForeignKey(x => x.DefaultNutritionId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
