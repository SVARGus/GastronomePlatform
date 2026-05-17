using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeStepsAndIngredients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                schema: "dishes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: true),
                    IngredientSpecId = table.Column<Guid>(type: "uuid", nullable: true),
                    FreeformText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    MeasureUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsOptional = table.Column<bool>(type: "boolean", nullable: false),
                    PreparationNote = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredients", x => x.Id);
                    table.CheckConstraint("CK_RecipeIngredients_IngredientXorFreeform", "(\"IngredientId\" IS NOT NULL AND \"FreeformText\" IS NULL) OR (\"IngredientId\" IS NULL AND \"FreeformText\" IS NOT NULL)");
                    table.CheckConstraint("CK_RecipeIngredients_OrderPositive", "\"Order\" > 0");
                    table.CheckConstraint("CK_RecipeIngredients_QuantityPositive", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_RecipeIngredients_SpecRequiresIngredient", "\"IngredientSpecId\" IS NULL OR \"IngredientId\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_IngredientSpecs_IngredientSpecId",
                        column: x => x.IngredientSpecId,
                        principalSchema: "dishes",
                        principalTable: "IngredientSpecs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalSchema: "dishes",
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_MeasureUnits_MeasureUnitId",
                        column: x => x.MeasureUnitId,
                        principalSchema: "dishes",
                        principalTable: "MeasureUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalSchema: "dishes",
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeSteps",
                schema: "dishes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ImageMediaId = table.Column<Guid>(type: "uuid", nullable: true),
                    VideoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TemperatureCelsius = table.Column<int>(type: "integer", nullable: true),
                    TimerMinutes = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeSteps", x => x.Id);
                    table.CheckConstraint("CK_RecipeSteps_OrderPositive", "\"Order\" > 0");
                    table.CheckConstraint("CK_RecipeSteps_TemperatureRange", "\"TemperatureCelsius\" IS NULL OR (\"TemperatureCelsius\" BETWEEN -30 AND 300)");
                    table.CheckConstraint("CK_RecipeSteps_TimerRange", "\"TimerMinutes\" IS NULL OR (\"TimerMinutes\" BETWEEN 1 AND 1440)");
                    table.ForeignKey(
                        name: "FK_RecipeSteps_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalSchema: "dishes",
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_IngredientId",
                schema: "dishes",
                table: "RecipeIngredients",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_IngredientSpecId",
                schema: "dishes",
                table: "RecipeIngredients",
                column: "IngredientSpecId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_MeasureUnitId",
                schema: "dishes",
                table: "RecipeIngredients",
                column: "MeasureUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_RecipeId_Order",
                schema: "dishes",
                table: "RecipeIngredients",
                columns: new[] { "RecipeId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeSteps_RecipeId_Order",
                schema: "dishes",
                table: "RecipeSteps",
                columns: new[] { "RecipeId", "Order" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipeIngredients",
                schema: "dishes");

            migrationBuilder.DropTable(
                name: "RecipeSteps",
                schema: "dishes");
        }
    }
}
