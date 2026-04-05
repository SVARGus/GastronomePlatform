using GastronomePlatform.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Users.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Конфигурация маппинга сущности <see cref="UserProfile"/> на таблицу БД.
    /// </summary>
    public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            // Таблица
            builder.ToTable("UserProfiles");

            // Первичный ключ
            builder.HasKey(x => x.Id);

            // Индексы
            builder.HasIndex(x => x.UserId).IsUnique();
            builder.HasIndex(x => x.Email).IsUnique();
            builder.HasIndex(x => x.UserName).IsUnique();

            // Свойства с конфигурацией
            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.Email)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(x => x.UserName)
                .HasMaxLength(100)
                .IsRequired();

            // Опциональные строковые поля
            builder.Property(x => x.Phone)
                .HasMaxLength(50);

            builder.Property(x => x.FirstName)
                .HasMaxLength(100);

            builder.Property(x => x.LastName)
                .HasMaxLength(100);

            builder.Property(x => x.MiddleName)
                .HasMaxLength(100);

            builder.Property(x => x.DisplayName)
                .HasMaxLength(100);

            builder.Property(x => x.Bio)
                .HasMaxLength(2000);

            builder.Property(x => x.Country)
                .HasMaxLength(100);

            builder.Property(x => x.Region)
                .HasMaxLength(100);

            builder.Property(x => x.City)
                .HasMaxLength(100);

            // Gender — enum хранится как int
            builder.Property(x => x.Gender)
                .HasConversion<int?>();

            // DateOnly — Npgsql 8 поддерживает нативно
            builder.Property(x => x.DateOfBirth)
                .HasColumnType("date");

            // AvatarMediaId — ссылка на медиафайл из модуля Media, опционально
            builder.Property(x => x.AvatarMediaId);

            // Видимость профиля — дефолт на уровне БД
            builder.Property(x => x.IsPublic)
                .HasDefaultValue(true)
                .IsRequired();

            // Временны́е метки
            builder.Property(x => x.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
        }
    }
}
