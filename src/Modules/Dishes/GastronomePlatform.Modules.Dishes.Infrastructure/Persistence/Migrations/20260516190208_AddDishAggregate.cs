using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDishAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dishes",
                schema: "dishes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    HistoryText = table.Column<string>(type: "text", nullable: true),
                    MainImageId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ModerationStatus = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DifficultyLevel = table.Column<int>(type: "integer", nullable: false),
                    CostEstimate = table.Column<int>(type: "integer", nullable: false),
                    OwnerType = table.Column<int>(type: "integer", nullable: false),
                    DietLabelsMask = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    AllergensMask = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    HasUnverifiedAllergens = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RatingAvg = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false, defaultValue: 0m),
                    RatingCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ViewsCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    FavoritesCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PublishedVersionData = table.Column<string>(type: "jsonb", nullable: true),
                    PublishedVersionUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dishes", x => x.Id);
                    table.CheckConstraint("CK_Dishes_FavoritesCountNonNegative", "\"FavoritesCount\" >= 0");
                    table.CheckConstraint("CK_Dishes_RatingAvgRange", "\"RatingAvg\" BETWEEN 0 AND 5");
                    table.CheckConstraint("CK_Dishes_RatingCountNonNegative", "\"RatingCount\" >= 0");
                    table.CheckConstraint("CK_Dishes_ViewsCountNonNegative", "\"ViewsCount\" >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dishes_AuthorUserId",
                schema: "dishes",
                table: "Dishes",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Dishes_Slug",
                schema: "dishes",
                table: "Dishes",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dishes_Status",
                schema: "dishes",
                table: "Dishes",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dishes",
                schema: "dishes");
        }
    }
}
