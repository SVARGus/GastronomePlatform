using GastronomePlatform.Modules.Media.Domain.Entities;
using GastronomePlatform.Modules.Media.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Media.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для агрегата <see cref="MediaFile"/>.
    /// Описывает таблицу <c>media.MediaFiles</c>: колонки, индексы, частичные индексы
    /// для фоновых задач очистки orphan/soft-deleted записей, и связку с миниатюрами.
    /// </summary>
    internal sealed class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<MediaFile> builder)
        {
            builder.ToTable("MediaFiles", t =>
            {
                // Размер файла — строго положительный (пустой файл не имеет смысла).
                t.HasCheckConstraint(
                    "CK_MediaFiles_SizeBytesPositive",
                    "\"SizeBytes\" > 0");

                // Если изображение — Width/Height должны быть либо оба заданы, либо оба NULL.
                t.HasCheckConstraint(
                    "CK_MediaFiles_DimensionsBothOrNone",
                    "(\"Width\" IS NULL AND \"Height\" IS NULL) " +
                    "OR (\"Width\" IS NOT NULL AND \"Height\" IS NOT NULL)");

                // Парность EntityType + EntityId (доменный инвариант, дублируется в БД).
                t.HasCheckConstraint(
                    "CK_MediaFiles_EntityRefsMatchNullity",
                    "(\"EntityType\" IS NULL AND \"EntityId\" IS NULL) " +
                    "OR (\"EntityType\" IS NOT NULL AND \"EntityId\" IS NOT NULL)");

                // Personal-файл обязан иметь владельца (доменный инвариант, дублируется в БД).
                // DataCategory.Personal = 1.
                t.HasCheckConstraint(
                    "CK_MediaFiles_PersonalRequiresOwner",
                    "\"DataCategory\" <> 1 OR \"OwnerUserId\" IS NOT NULL");
            });

            builder.HasKey(x => x.Id);

            // Кросс-модульная ссылка на пользователя — без FK на уровне БД.
            builder.Property(x => x.OwnerUserId);

            // Привязка к сущности-владельцу — мягкая (без FK).
            builder.Property(x => x.EntityType)
                .HasMaxLength(50);

            builder.Property(x => x.EntityId);

            // Классификации и статус — int в БД, дефолт через enum-значение.
            builder.Property(x => x.DataCategory)
                .IsRequired();

            builder.Property(x => x.MediaType)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired()
                .HasDefaultValue(MediaStatus.Uploaded);

            // Технические поля файла.
            builder.Property(x => x.ContentType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.OriginalFileName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.StorageProvider)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.StorageKey)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.SizeBytes)
                .IsRequired();

            builder.Property(x => x.Width);

            builder.Property(x => x.Height);

            builder.Property(x => x.DurationSeconds);

            // Временны́е метки жизненного цикла.
            builder.Property(x => x.ExpiresAt);

            builder.Property(x => x.AttachedAt);

            builder.Property(x => x.DeletedAt);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired();

            // Индексы для типичных запросов.

            // Каскадная очистка медиа сущности (DeleteByEntityAsync) и поиск по привязке.
            builder.HasIndex(x => new { x.EntityType, x.EntityId })
                .HasDatabaseName("IX_MediaFiles_EntityType_EntityId");

            // Запросы «мои файлы» / экспорт PII по 152-ФЗ.
            builder.HasIndex(x => x.OwnerUserId)
                .HasDatabaseName("IX_MediaFiles_OwnerUserId");

            // Частичный индекс для фоновой очистки orphan-файлов
            // (UC-MED-210, Этап 8+). Узкий — попадают только не-привязанные.
            builder.HasIndex(x => new { x.Status, x.ExpiresAt })
                .HasDatabaseName("IX_MediaFiles_OrphanCleanup")
                .HasFilter("\"EntityType\" IS NULL");

            // Частичный индекс для фоновой задачи физического удаления
            // soft-deleted записей (UC-MED-211, Этап 8+). MediaStatus.Deleted = 4.
            builder.HasIndex(x => x.DeletedAt)
                .HasDatabaseName("IX_MediaFiles_HardDeleteCandidates")
                .HasFilter("\"Status\" = 4");

            // Backing field для коллекции миниатюр — read-only IReadOnlyList,
            // EF Core читает и пишет напрямую в _thumbnails.
            builder.Navigation(x => x.Thumbnails)
                .HasField("_thumbnails")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
