using GastronomePlatform.Modules.Dishes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для <see cref="Yield"/> — части агрегата <see cref="Dish"/>.
    /// </summary>
    /// <remarks>
    /// Yield ↔ Recipe — 1:1, Cascade при удалении Recipe. UNIQUE-индекс на
    /// <c>RecipeId</c> создаётся EF Core как часть 1:1-связки.
    /// </remarks>
    internal sealed class YieldConfiguration : IEntityTypeConfiguration<Yield>
    {
        /// <summary>
        /// Настраивает таблицу <c>dishes.Yields</c> и связку с Recipe.
        /// </summary>
        /// <param name="builder">Билдер конфигурации сущности.</param>
        public void Configure(EntityTypeBuilder<Yield> builder)
        {
            builder.ToTable("Yields");

            builder.HasKey(y => y.Id);

            builder.Property(y => y.RecipeId)
                .IsRequired();

            builder.Property(y => y.QuantityTotal)
                .HasPrecision(8, 2)
                .IsRequired();

            builder.Property(y => y.YieldUnit)
                .IsRequired();

            builder.Property(y => y.ServingsCount)
                .IsRequired();

            builder.Property(y => y.GramsPerServing)
                .HasPrecision(6, 1);

            // Yield ↔ Recipe (1:1, Cascade)
            builder.HasOne<Recipe>()
                .WithOne(r => r.Yield)
                .HasForeignKey<Yield>(y => y.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
