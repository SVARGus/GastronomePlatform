using GastronomePlatform.Modules.Dishes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для связующей таблицы <see cref="DishCategory"/>
    /// — рабочей версии M:M-связки блюда и категории.
    /// </summary>
    internal sealed class DishCategoryConfiguration : IEntityTypeConfiguration<DishCategory>
    {
        /// <summary>
        /// Применяет конфигурацию <see cref="DishCategory"/>: composite-ключ
        /// (<c>DishId</c>, <c>CategoryId</c>) и FK с <see cref="DeleteBehavior.Cascade"/>
        /// на обе стороны.
        /// </summary>
        /// <param name="builder">Билдер EF Core для типа сущности.</param>
        public void Configure(EntityTypeBuilder<DishCategory> builder)
        {
            builder.ToTable("DishCategories");

            builder.HasKey(dc => new { dc.DishId, dc.CategoryId });

            builder.Property(dc => dc.DishId).IsRequired();
            builder.Property(dc => dc.CategoryId).IsRequired();

            // DishCategory → Dish (M:1, Cascade). Навигация Categories на Dish.
            builder.HasOne<Dish>()
                .WithMany(d => d.Categories)
                .HasForeignKey(dc => dc.DishId)
                .OnDelete(DeleteBehavior.Cascade);

            // DishCategory → Category (M:1, Cascade). Без обратной навигации
            // (на Category коллекция не нужна — каталожные запросы идут через индекс).
            builder.HasOne<Category>()
                .WithMany()
                .HasForeignKey(dc => dc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
