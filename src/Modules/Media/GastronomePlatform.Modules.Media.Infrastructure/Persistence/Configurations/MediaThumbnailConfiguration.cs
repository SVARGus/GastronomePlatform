using GastronomePlatform.Modules.Media.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Media.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для <see cref="MediaThumbnail"/> — части агрегата
    /// <see cref="MediaFile"/>.
    /// </summary>
    /// <remarks>
    /// Связка с <see cref="MediaFile"/> — M:1, Cascade при удалении файла-владельца.
    /// UNIQUE на тройке <c>(MediaFileId, Size, Format)</c> гарантирует, что одна
    /// и та же пара (размер, формат) не может встретиться у одного файла дважды
    /// (это же проверяется доменом в <see cref="MediaFile.AddThumbnail"/>).
    /// </remarks>
    internal sealed class MediaThumbnailConfiguration : IEntityTypeConfiguration<MediaThumbnail>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<MediaThumbnail> builder)
        {
            builder.ToTable("MediaThumbnails", t =>
            {
                t.HasCheckConstraint(
                    "CK_MediaThumbnails_DimensionsPositive",
                    "\"Width\" > 0 AND \"Height\" > 0");

                t.HasCheckConstraint(
                    "CK_MediaThumbnails_SizeBytesPositive",
                    "\"SizeBytes\" > 0");
            });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.MediaFileId)
                .IsRequired();

            builder.Property(x => x.Size)
                .IsRequired();

            builder.Property(x => x.Format)
                .IsRequired();

            builder.Property(x => x.StorageKey)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.Width)
                .IsRequired();

            builder.Property(x => x.Height)
                .IsRequired();

            builder.Property(x => x.SizeBytes)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            // MediaThumbnail → MediaFile (M:1, Cascade). Навигация Thumbnails на MediaFile.
            builder.HasOne<MediaFile>()
                .WithMany(x => x.Thumbnails)
                .HasForeignKey(x => x.MediaFileId)
                .OnDelete(DeleteBehavior.Cascade);

            // UNIQUE (MediaFileId, Size, Format) — одна пара размер/формат на файл.
            builder.HasIndex(x => new { x.MediaFileId, x.Size, x.Format })
                .IsUnique()
                .HasDatabaseName("UX_MediaThumbnails_File_Size_Format");
        }
    }
}
