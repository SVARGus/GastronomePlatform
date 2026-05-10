using GastronomePlatform.Modules.Dishes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для сущности <see cref="Nutrition"/>.
    /// Описывает таблицу <c>dishes.Nutritions</c>: имена колонок, типы и точность decimal-полей.
    /// </summary>
    internal sealed class NutritionConfiguration : IEntityTypeConfiguration<Nutrition>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<Nutrition> builder)
        {
            builder.ToTable("Nutritions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CalcMethod)
                .IsRequired();

            builder.Property(x => x.Calories)
                .IsRequired()
                .HasPrecision(7, 2);

            builder.Property(x => x.Proteins)
                .IsRequired()
                .HasPrecision(6, 2);

            builder.Property(x => x.Fats)
                .IsRequired()
                .HasPrecision(6, 2);

            builder.Property(x => x.SaturatedFats)
                .HasPrecision(6, 2);

            builder.Property(x => x.Carbs)
                .IsRequired()
                .HasPrecision(6, 2);

            builder.Property(x => x.Sugar)
                .HasPrecision(6, 2);

            builder.Property(x => x.Fiber)
                .HasPrecision(6, 2);

            builder.Property(x => x.Salt)
                .HasPrecision(6, 2);
        }
    }
}
