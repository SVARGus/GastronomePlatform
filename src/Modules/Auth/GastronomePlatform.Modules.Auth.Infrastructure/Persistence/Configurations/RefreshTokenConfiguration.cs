using GastronomePlatform.Modules.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Auth.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Конфигурация маппинга сущности <see cref="RefreshToken"/> на таблицу БД.
    /// </summary>
    public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            // Таблица
            builder.ToTable("RefreshTokens");

            // Первичный ключ
            builder.HasKey(rt => rt.Id);

            // Поля
            builder.Property(rt => rt.Token)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(rt => rt.UserId)
                .IsRequired();

            builder.Property(rt => rt.ExpiresAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(rt => rt.RevokedAt)
                .HasColumnType("timestamp with time zone");

            // IsActive — вычисляемое свойство, не хранится в БД
            builder.Ignore(rt => rt.IsActive);

            // Индекс по Token — используется в GetByTokenAsync
            builder.HasIndex(rt => rt.Token)
                .IsUnique();

            // Индекс по UserId — для выборки всех токенов пользователя
            builder.HasIndex(rt => rt.UserId);
        }
    }
}
