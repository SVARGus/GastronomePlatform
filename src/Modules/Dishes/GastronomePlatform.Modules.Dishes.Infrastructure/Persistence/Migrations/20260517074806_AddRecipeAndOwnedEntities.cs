using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeAndOwnedEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Recipes",
                schema: "dishes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DishId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntroductionText = table.Column<string>(type: "text", nullable: true),
                    ServingsDefault = table.Column<int>(type: "integer", nullable: false),
                    IsAlcoholic = table.Column<bool>(type: "boolean", nullable: false),
                    AuthorTips = table.Column<string>(type: "text", nullable: true),
                    ServingSuggestions = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    NutritionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recipes_Dishes_DishId",
                        column: x => x.DishId,
                        principalSchema: "dishes",
                        principalTable: "Dishes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Recipes_Nutritions_NutritionId",
                        column: x => x.NutritionId,
                        principalSchema: "dishes",
                        principalTable: "Nutritions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Timings",
                schema: "dishes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrepTimeMinutes = table.Column<int>(type: "integer", nullable: true),
                    CookTimeMinutes = table.Column<int>(type: "integer", nullable: true),
                    RestTimeMinutes = table.Column<int>(type: "integer", nullable: true),
                    ActiveTimeMinutes = table.Column<int>(type: "integer", nullable: true),
                    TotalTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsTotalManual = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timings_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalSchema: "dishes",
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Yields",
                schema: "dishes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityTotal = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    YieldUnit = table.Column<int>(type: "integer", nullable: false),
                    ServingsCount = table.Column<int>(type: "integer", nullable: false),
                    GramsPerServing = table.Column<decimal>(type: "numeric(6,1)", precision: 6, scale: 1, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Yields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Yields_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalSchema: "dishes",
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_DishId",
                schema: "dishes",
                table: "Recipes",
                column: "DishId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_NutritionId",
                schema: "dishes",
                table: "Recipes",
                column: "NutritionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Timings_RecipeId",
                schema: "dishes",
                table: "Timings",
                column: "RecipeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Yields_RecipeId",
                schema: "dishes",
                table: "Yields",
                column: "RecipeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Timings",
                schema: "dishes");

            migrationBuilder.DropTable(
                name: "Yields",
                schema: "dishes");

            migrationBuilder.DropTable(
                name: "Recipes",
                schema: "dishes");
        }
    }
}
