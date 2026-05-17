using GastronomePlatform.Modules.Dishes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для <see cref="RecipeIngredient"/> — части агрегата <see cref="Dish"/>.
    /// </summary>
    /// <remarks>
    /// Связки:
    /// <list type="bullet">
    ///   <item>RecipeIngredient → Recipe — M:1, Cascade.</item>
    ///   <item>RecipeIngredient → Ingredient — M:1, Restrict (защита от потери ингредиентов в рецептах).</item>
    ///   <item>RecipeIngredient → IngredientSpec — M:1, SetNull (спецификация — необязательное уточнение).</item>
    ///   <item>RecipeIngredient → MeasureUnit — M:1, Restrict (единицы редко меняются/удаляются).</item>
    /// </list>
    /// CHECK-constraints дублируют доменные инварианты: XOR на
    /// <c>(IngredientId, FreeformText)</c>, требование <c>IngredientSpec → IngredientId</c>,
    /// положительность Quantity и Order. Индекс на <c>IngredientId</c> — для запросов
    /// «в каких рецептах используется этот ингредиент».
    /// </remarks>
    internal sealed class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
    {
        /// <summary>
        /// Настраивает таблицу <c>dishes.RecipeIngredients</c>, связки, индексы и CHECK-constraints.
        /// </summary>
        /// <param name="builder">Билдер конфигурации сущности.</param>
        public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
        {
            builder.ToTable("RecipeIngredients", t =>
            {
                // XOR: ровно одно из (IngredientId, FreeformText) — не оба и не ни одного.
                t.HasCheckConstraint(
                    "CK_RecipeIngredients_IngredientXorFreeform",
                    "(\"IngredientId\" IS NOT NULL AND \"FreeformText\" IS NULL) " +
                    "OR (\"IngredientId\" IS NULL AND \"FreeformText\" IS NOT NULL)");

                // IngredientSpec допустим только при заполненном IngredientId.
                t.HasCheckConstraint(
                    "CK_RecipeIngredients_SpecRequiresIngredient",
                    "\"IngredientSpecId\" IS NULL OR \"IngredientId\" IS NOT NULL");

                // Положительное количество.
                t.HasCheckConstraint(
                    "CK_RecipeIngredients_QuantityPositive",
                    "\"Quantity\" > 0");

                // Положительный Order.
                t.HasCheckConstraint(
                    "CK_RecipeIngredients_OrderPositive",
                    "\"Order\" > 0");
            });

            builder.HasKey(ri => ri.Id);

            builder.Property(ri => ri.RecipeId)
                .IsRequired();

            builder.Property(ri => ri.IngredientId);

            builder.Property(ri => ri.IngredientSpecId);

            builder.Property(ri => ri.FreeformText)
                .HasMaxLength(200);

            builder.Property(ri => ri.Quantity)
                .HasPrecision(10, 3)
                .IsRequired();

            builder.Property(ri => ri.MeasureUnitId)
                .IsRequired();

            builder.Property(ri => ri.Order)
                .IsRequired();

            builder.Property(ri => ri.IsOptional)
                .IsRequired();

            builder.Property(ri => ri.PreparationNote)
                .HasMaxLength(200);

            // RecipeIngredient → Recipe (M:1, Cascade). Навигация Ingredients на Recipe.
            builder.HasOne<Recipe>()
                .WithMany(r => r.Ingredients)
                .HasForeignKey(ri => ri.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            // RecipeIngredient → Ingredient (M:1, Restrict). Без навигации на Ingredient.
            builder.HasOne<Ingredient>()
                .WithMany()
                .HasForeignKey(ri => ri.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);

            // RecipeIngredient → IngredientSpec (M:1, SetNull). Без навигации.
            builder.HasOne<IngredientSpec>()
                .WithMany()
                .HasForeignKey(ri => ri.IngredientSpecId)
                .OnDelete(DeleteBehavior.SetNull);

            // RecipeIngredient → MeasureUnit (M:1, Restrict). Без навигации.
            builder.HasOne<MeasureUnit>()
                .WithMany()
                .HasForeignKey(ri => ri.MeasureUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // UNIQUE (RecipeId, Order).
            builder.HasIndex(ri => new { ri.RecipeId, ri.Order })
                .IsUnique();

            // Индекс по IngredientId — для запросов «в каких рецептах используется ингредиент».
            builder.HasIndex(ri => ri.IngredientId);
        }
    }
}
