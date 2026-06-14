using GastronomePlatform.Modules.Dishes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для сущности <see cref="Tag"/>.
    /// Описывает таблицу <c>dishes.Tags</c>: имена колонок, типы,
    /// ограничения, индексы и значения по умолчанию.
    /// </summary>
    internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            builder.ToTable("Tags");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(Tag.MAX_NAME_LENGTH);

            builder.Property(x => x.NormalizedName)
                .IsRequired()
                .HasMaxLength(Tag.MAX_NAME_LENGTH);

            builder.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(Tag.MAX_SLUG_LENGTH);

            builder.Property(x => x.UsageCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.IsVerified)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.CreatedByUserId);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasIndex(x => x.NormalizedName)
                .IsUnique();

            builder.HasIndex(x => x.Slug)
                .IsUnique();
        }
    }
}
