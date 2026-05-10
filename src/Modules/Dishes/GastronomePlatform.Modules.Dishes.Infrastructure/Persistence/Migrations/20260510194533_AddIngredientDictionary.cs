using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientDictionary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ingredients",
                schema: "dishes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PluralName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ImageMediaId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsLiquid = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DensityApprox = table.Column<decimal>(type: "numeric(5,3)", precision: 5, scale: 3, nullable: true),
                    IsAllergen = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AllergenType = table.Column<int>(type: "integer", nullable: true),
                    BaseMeasureUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefaultNutritionId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingredients", x => x.Id);
                    table.CheckConstraint("CK_Ingredients_AllergenType", "\"IsAllergen\" = false OR \"AllergenType\" IS NOT NULL");
                    table.CheckConstraint("CK_Ingredients_LiquidDensity", "\"IsLiquid\" = false OR \"DensityApprox\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_Ingredients_MeasureUnits_BaseMeasureUnitId",
                        column: x => x.BaseMeasureUnitId,
                        principalSchema: "dishes",
                        principalTable: "MeasureUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ingredients_Nutritions_DefaultNutritionId",
                        column: x => x.DefaultNutritionId,
                        principalSchema: "dishes",
                        principalTable: "Nutritions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "IngredientSpecs",
                schema: "dishes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NutritionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientSpecs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IngredientSpecs_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalSchema: "dishes",
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IngredientSpecs_Nutritions_NutritionId",
                        column: x => x.NutritionId,
                        principalSchema: "dishes",
                        principalTable: "Nutritions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_BaseMeasureUnitId",
                schema: "dishes",
                table: "Ingredients",
                column: "BaseMeasureUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_DefaultNutritionId",
                schema: "dishes",
                table: "Ingredients",
                column: "DefaultNutritionId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_Name",
                schema: "dishes",
                table: "Ingredients",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IngredientSpecs_IngredientId",
                schema: "dishes",
                table: "IngredientSpecs",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientSpecs_NutritionId",
                schema: "dishes",
                table: "IngredientSpecs",
                column: "NutritionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IngredientSpecs",
                schema: "dishes");

            migrationBuilder.DropTable(
                name: "Ingredients",
                schema: "dishes");
        }
    }
}
