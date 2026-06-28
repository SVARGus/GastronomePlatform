using GastronomePlatform.Modules.Dishes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для сущности <see cref="Category"/>.
    /// Описывает таблицу <c>dishes.Categories</c>: имена колонок, типы,
    /// ограничения, индексы и self-reference через <see cref="Category.ParentId"/>.
    /// </summary>
    internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("Categories");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(Category.MAX_NAME_LENGTH);

            builder.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(Category.MAX_SLUG_LENGTH);

            builder.Property(x => x.ParentId);

            builder.Property(x => x.Order)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.IconMediaId);

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasIndex(x => x.Slug)
                .IsUnique();

            // Self-reference: каждая Category может иметь родителя через ParentId.
            // Без навигационных свойств (Parent / Children) — они не нужны на Этапе 2,
            // дерево собираем в памяти из плоского ListActiveAsync.
            builder.HasOne<Category>()
                .WithMany()
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
