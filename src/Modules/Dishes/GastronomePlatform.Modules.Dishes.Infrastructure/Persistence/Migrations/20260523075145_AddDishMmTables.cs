using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDishMmTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DishCategories",
                schema: "dishes",
                columns: table => new
                {
                    DishId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DishCategories", x => new { x.DishId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_DishCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "dishes",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DishCategories_Dishes_DishId",
                        column: x => x.DishId,
                        principalSchema: "dishes",
                        principalTable: "Dishes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DishCategoriesPublished",
                schema: "dishes",
                columns: table => new
                {
                    DishId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DishCategoriesPublished", x => new { x.DishId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_DishCategoriesPublished_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "dishes",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DishCategoriesPublished_Dishes_DishId",
                        column: x => x.DishId,
                        principalSchema: "dishes",
                        principalTable: "Dishes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DishTags",
                schema: "dishes",
                columns: table => new
                {
                    DishId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DishTags", x => new { x.DishId, x.TagId });
                    table.ForeignKey(
                        name: "FK_DishTags_Dishes_DishId",
                        column: x => x.DishId,
                        principalSchema: "dishes",
                        principalTable: "Dishes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DishTags_Tags_TagId",
                        column: x => x.TagId,
                        principalSchema: "dishes",
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DishTagsPublished",
                schema: "dishes",
                columns: table => new
                {
                    DishId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DishTagsPublished", x => new { x.DishId, x.TagId });
                    table.ForeignKey(
                        name: "FK_DishTagsPublished_Dishes_DishId",
                        column: x => x.DishId,
                        principalSchema: "dishes",
                        principalTable: "Dishes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DishTagsPublished_Tags_TagId",
                        column: x => x.TagId,
                        principalSchema: "dishes",
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DishCategories_CategoryId",
                schema: "dishes",
                table: "DishCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DishCategoriesPublished_CategoryId_DishId",
                schema: "dishes",
                table: "DishCategoriesPublished",
                columns: new[] { "CategoryId", "DishId" });

            migrationBuilder.CreateIndex(
                name: "IX_DishTags_TagId",
                schema: "dishes",
                table: "DishTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_DishTagsPublished_TagId_DishId",
                schema: "dishes",
                table: "DishTagsPublished",
                columns: new[] { "TagId", "DishId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DishCategories",
                schema: "dishes");

            migrationBuilder.DropTable(
                name: "DishCategoriesPublished",
                schema: "dishes");

            migrationBuilder.DropTable(
                name: "DishTags",
                schema: "dishes");

            migrationBuilder.DropTable(
                name: "DishTagsPublished",
                schema: "dishes");
        }
    }
}
