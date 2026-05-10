using GastronomePlatform.Modules.Dishes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для сущности <see cref="IngredientSpec"/>.
    /// Описывает таблицу <c>dishes.IngredientSpecs</c>: колонки, FK на <c>Ingredient</c> и <c>Nutrition</c>,
    /// уникальный индекс на <see cref="IngredientSpec.NutritionId"/> (1:1 связь с КБЖУ).
    /// </summary>
    internal sealed class IngredientSpecConfiguration : IEntityTypeConfiguration<IngredientSpec>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<IngredientSpec> builder)
        {
            builder.ToTable("IngredientSpecs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.IngredientId)
                .IsRequired();

            builder.Property(x => x.SpecName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.NutritionId)
                .IsRequired();

            // 1:1 связь Spec → Nutrition: каждая КБЖУ-запись принадлежит максимум одной spec.
            builder.HasIndex(x => x.NutritionId)
                .IsUnique();

            // FK на Ingredient: spec умирает вместе с родительским ингредиентом.
            builder.HasOne<Ingredient>()
                .WithMany()
                .HasForeignKey(x => x.IngredientId)
                .OnDelete(DeleteBehavior.Cascade);

            // FK на Nutrition: КБЖУ-запись нельзя удалить, пока spec на неё ссылается (NOT NULL FK).
            builder.HasOne<Nutrition>()
                .WithMany()
                .HasForeignKey(x => x.NutritionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
