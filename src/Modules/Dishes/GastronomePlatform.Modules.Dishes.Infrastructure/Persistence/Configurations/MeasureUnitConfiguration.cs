using GastronomePlatform.Modules.Dishes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для сущности <see cref="MeasureUnit"/>.
    /// Описывает таблицу <c>dishes.MeasureUnits</c>: имена колонок, типы,
    /// ограничения и индексы.
    /// </summary>
    internal sealed class MeasureUnitConfiguration : IEntityTypeConfiguration<MeasureUnit>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<MeasureUnit> builder)
        {
            builder.ToTable("MeasureUnits");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.NameRu)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Type)
                .IsRequired();

            builder.Property(x => x.ConversionToBase)
                .IsRequired()
                .HasPrecision(10, 5);

            builder.Property(x => x.IsBase)
                .IsRequired();

            builder.HasIndex(x => x.Code)
                .IsUnique();
        }
    }
}
