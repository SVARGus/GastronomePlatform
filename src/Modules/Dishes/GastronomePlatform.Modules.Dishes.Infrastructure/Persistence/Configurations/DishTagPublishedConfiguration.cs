using GastronomePlatform.Modules.Dishes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для связующей таблицы <see cref="DishTagPublished"/>
    /// — опубликованной версии M:M-связки блюда и тега.
    /// </summary>
    internal sealed class DishTagPublishedConfiguration
        : IEntityTypeConfiguration<DishTagPublished>
    {
        /// <summary>
        /// Применяет конфигурацию <see cref="DishTagPublished"/>: composite-ключ,
        /// FK с <see cref="DeleteBehavior.Cascade"/> на обе стороны и дополнительный
        /// композитный индекс (<c>TagId</c>, <c>DishId</c>) для каталожного фильтра
        /// «блюда с тегом X».
        /// </summary>
        /// <param name="builder">Билдер EF Core для типа сущности.</param>
        public void Configure(EntityTypeBuilder<DishTagPublished> builder)
        {
            builder.ToTable("DishTagsPublished");

            builder.HasKey(dtp => new { dtp.DishId, dtp.TagId });

            builder.Property(dtp => dtp.DishId).IsRequired();
            builder.Property(dtp => dtp.TagId).IsRequired();

            builder.HasOne<Dish>()
                .WithMany(d => d.TagsPublished)
                .HasForeignKey(dtp => dtp.DishId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Tag>()
                .WithMany()
                .HasForeignKey(dtp => dtp.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(dtp => new { dtp.TagId, dtp.DishId });
        }
    }
}
