using GastronomePlatform.Modules.Dishes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для <see cref="Timing"/> — части агрегата <see cref="Dish"/>.
    /// </summary>
    /// <remarks>
    /// Timing ↔ Recipe — 1:1, Cascade при удалении Recipe. UNIQUE-индекс на
    /// <c>RecipeId</c> создаётся EF Core как часть 1:1-связки.
    /// </remarks>
    internal sealed class TimingConfiguration : IEntityTypeConfiguration<Timing>
    {
        /// <summary>
        /// Настраивает таблицу <c>dishes.Timings</c> и связку с Recipe.
        /// </summary>
        /// <param name="builder">Билдер конфигурации сущности.</param>
        public void Configure(EntityTypeBuilder<Timing> builder)
        {
            builder.ToTable("Timings");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.RecipeId)
                .IsRequired();

            builder.Property(t => t.PrepTimeMinutes);
            builder.Property(t => t.CookTimeMinutes);
            builder.Property(t => t.RestTimeMinutes);
            builder.Property(t => t.ActiveTimeMinutes);

            builder.Property(t => t.TotalTimeMinutes)
                .IsRequired();

            builder.Property(t => t.IsTotalManual)
                .IsRequired();

            // Timing ↔ Recipe (1:1, Cascade)
            builder.HasOne<Recipe>()
                .WithOne(r => r.Timing)
                .HasForeignKey<Timing>(t => t.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
