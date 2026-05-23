using GastronomePlatform.Modules.Dishes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для связующей таблицы <see cref="DishTag"/>
    /// — рабочей версии M:M-связки блюда и тега.
    /// </summary>
    internal sealed class DishTagConfiguration : IEntityTypeConfiguration<DishTag>
    {
        /// <summary>
        /// Применяет конфигурацию <see cref="DishTag"/>: composite-ключ
        /// (<c>DishId</c>, <c>TagId</c>) и FK с <see cref="DeleteBehavior.Cascade"/>
        /// на обе стороны.
        /// </summary>
        /// <param name="builder">Билдер EF Core для типа сущности.</param>
        public void Configure(EntityTypeBuilder<DishTag> builder)
        {
            builder.ToTable("DishTags");

            builder.HasKey(dt => new { dt.DishId, dt.TagId });

            builder.Property(dt => dt.DishId).IsRequired();
            builder.Property(dt => dt.TagId).IsRequired();

            builder.HasOne<Dish>()
                .WithMany(d => d.Tags)
                .HasForeignKey(dt => dt.DishId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Tag>()
                .WithMany()
                .HasForeignKey(dt => dt.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
