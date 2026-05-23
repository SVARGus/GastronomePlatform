using GastronomePlatform.Modules.Dishes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для связующей таблицы <see cref="DishCategoryPublished"/>
    /// — опубликованной версии M:M-связки блюда и категории. Используется каталожным
    /// фильтром «блюда категории X» (UC-DSH-054).
    /// </summary>
    internal sealed class DishCategoryPublishedConfiguration
        : IEntityTypeConfiguration<DishCategoryPublished>
    {
        /// <summary>
        /// Применяет конфигурацию <see cref="DishCategoryPublished"/>: composite-ключ,
        /// FK с <see cref="DeleteBehavior.Cascade"/> на обе стороны и дополнительный
        /// композитный индекс (<c>CategoryId</c>, <c>DishId</c>) для каталожного фильтра.
        /// </summary>
        /// <param name="builder">Билдер EF Core для типа сущности.</param>
        public void Configure(EntityTypeBuilder<DishCategoryPublished> builder)
        {
            builder.ToTable("DishCategoriesPublished");

            builder.HasKey(dcp => new { dcp.DishId, dcp.CategoryId });

            builder.Property(dcp => dcp.DishId).IsRequired();
            builder.Property(dcp => dcp.CategoryId).IsRequired();

            builder.HasOne<Dish>()
                .WithMany(d => d.CategoriesPublished)
                .HasForeignKey(dcp => dcp.DishId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Category>()
                .WithMany()
                .HasForeignKey(dcp => dcp.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Композитный индекс для каталожного запроса:
            // SELECT DishId FROM DishCategoriesPublished WHERE CategoryId = @id.
            builder.HasIndex(dcp => new { dcp.CategoryId, dcp.DishId });
        }
    }
}
