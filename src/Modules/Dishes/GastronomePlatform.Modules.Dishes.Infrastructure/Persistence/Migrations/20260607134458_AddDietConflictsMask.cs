using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDietConflictsMask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DietConflictsMask",
                schema: "dishes",
                table: "Ingredients",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DietConflictsMask",
                schema: "dishes",
                table: "Ingredients");
        }
    }
}
