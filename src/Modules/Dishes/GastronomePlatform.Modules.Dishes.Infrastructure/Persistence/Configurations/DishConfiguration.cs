using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для агрегата <see cref="Dish"/>.
    /// Описывает таблицу <c>dishes.Dishes</c>: колонки, ограничения, дефолты
    /// денормализованных полей, индексы для каталожных фильтров и jsonb-маппинг
    /// снепшота публичной версии.
    /// </summary>
    internal sealed class DishConfiguration : IEntityTypeConfiguration<Dish>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<Dish> builder)
        {
            builder.ToTable("Dishes", t =>
            {
                // Денормализованные счётчики не должны опускаться ниже нуля
                // (защита от рассинхрона event-handler'ов на Этапе 5).
                t.HasCheckConstraint(
                    "CK_Dishes_RatingAvgRange",
                    "\"RatingAvg\" BETWEEN 0 AND 5");

                t.HasCheckConstraint(
                    "CK_Dishes_RatingCountNonNegative",
                    "\"RatingCount\" >= 0");

                t.HasCheckConstraint(
                    "CK_Dishes_ViewsCountNonNegative",
                    "\"ViewsCount\" >= 0");

                t.HasCheckConstraint(
                    "CK_Dishes_FavoritesCountNonNegative",
                    "\"FavoritesCount\" >= 0");
            });

            builder.HasKey(x => x.Id);

            // Кросс-модульные идентификаторы: без FK на уровне БД, только колонки.
            builder.Property(x => x.AuthorUserId)
                .IsRequired();

            builder.Property(x => x.MainImageId);

            // Текстовые поля карточки.
            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(220);

            builder.Property(x => x.ShortDescription)
                .HasMaxLength(500);

            // Description и HistoryText — без HasMaxLength: маппятся в PG-тип text.
            builder.Property(x => x.Description);

            builder.Property(x => x.HistoryText);

            // Статусные enums.
            builder.Property(x => x.Status)
                .IsRequired()
                .HasDefaultValue(DishStatus.Draft);

            builder.Property(x => x.ModerationStatus)
                .IsRequired()
                .HasDefaultValue(ModerationStatus.Approved);

            builder.Property(x => x.DifficultyLevel)
                .IsRequired();

            builder.Property(x => x.CostEstimate)
                .IsRequired();

            builder.Property(x => x.OwnerType)
                .IsRequired();

            // Битовые маски — публичные маркеры для каталожных фильтров.
            builder.Property(x => x.DietLabelsMask)
                .IsRequired()
                .HasDefaultValue(DietLabels.None);

            builder.Property(x => x.AllergensMask)
                .IsRequired()
                .HasDefaultValue(AllergenType.None);

            builder.Property(x => x.HasUnverifiedAllergens)
                .IsRequired()
                .HasDefaultValue(false);

            // Денормализованные счётчики и рейтинг.
            builder.Property(x => x.RatingAvg)
                .IsRequired()
                .HasPrecision(3, 2)
                .HasDefaultValue(0m);

            builder.Property(x => x.RatingCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.ViewsCount)
                .IsRequired()
                .HasDefaultValue(0L);

            builder.Property(x => x.FavoritesCount)
                .IsRequired()
                .HasDefaultValue(0);

            // Снепшот публичной версии — jsonb (структура задаётся Application-слоем).
            builder.Property(x => x.PublishedVersionData)
                .HasColumnType("jsonb");

            builder.Property(x => x.PublishedVersionUpdatedAt);

            builder.Property(x => x.PublishedAt);

            // Временны́е метки агрегата.
            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired();

            // Уникальный slug — публичный URL.
            builder.HasIndex(x => x.Slug)
                .IsUnique();

            // Запросы «мои блюда» по автору.
            builder.HasIndex(x => x.AuthorUserId);

            // Каталожные фильтры по статусу.
            builder.HasIndex(x => x.Status);

            // Backing fields для M:M-навигаций — read-only коллекции IReadOnlyList
            // обслуживаются приватными полями _categories / _tags / _categoriesPublished /
            // _tagsPublished, EF Core читает и пишет напрямую в поля.
            builder.Navigation(d => d.Categories)
                .HasField("_categories")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.Navigation(d => d.Tags)
                .HasField("_tags")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.Navigation(d => d.CategoriesPublished)
                .HasField("_categoriesPublished")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.Navigation(d => d.TagsPublished)
                .HasField("_tagsPublished")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
