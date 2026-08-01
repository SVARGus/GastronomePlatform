using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecipeOrderDeferrableUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecipeSteps_RecipeId_Order",
                schema: "dishes",
                table: "RecipeSteps");

            migrationBuilder.DropIndex(
                name: "IX_RecipeIngredients_RecipeId_Order",
                schema: "dishes",
                table: "RecipeIngredients");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeSteps_RecipeId",
                schema: "dishes",
                table: "RecipeSteps",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_RecipeId",
                schema: "dishes",
                table: "RecipeIngredients",
                column: "RecipeId");

            // Уникальность (RecipeId, Order) переезжает из обычных UNIQUE-индексов
            // в constraint-ы DEFERRABLE INITIALLY DEFERRED: PostgreSQL проверяет их
            // в конце транзакции, что позволяет переставлять Order местами одним
            // SaveChanges (UC-DSH-023/033). EF Core не моделирует DEFERRABLE —
            // constraint-ы добавляются raw SQL-ом и невидимы для модели (иначе EF
            // снова строил бы циклические зависимости UPDATE-ов при swap).
            migrationBuilder.Sql(
                """
                ALTER TABLE dishes."RecipeSteps"
                    ADD CONSTRAINT "UQ_RecipeSteps_RecipeId_Order"
                    UNIQUE ("RecipeId", "Order") DEFERRABLE INITIALLY DEFERRED;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE dishes."RecipeIngredients"
                    ADD CONSTRAINT "UQ_RecipeIngredients_RecipeId_Order"
                    UNIQUE ("RecipeId", "Order") DEFERRABLE INITIALLY DEFERRED;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE dishes."RecipeSteps"
                    DROP CONSTRAINT "UQ_RecipeSteps_RecipeId_Order";
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE dishes."RecipeIngredients"
                    DROP CONSTRAINT "UQ_RecipeIngredients_RecipeId_Order";
                """);

            migrationBuilder.DropIndex(
                name: "IX_RecipeSteps_RecipeId",
                schema: "dishes",
                table: "RecipeSteps");

            migrationBuilder.DropIndex(
                name: "IX_RecipeIngredients_RecipeId",
                schema: "dishes",
                table: "RecipeIngredients");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeSteps_RecipeId_Order",
                schema: "dishes",
                table: "RecipeSteps",
                columns: new[] { "RecipeId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_RecipeId_Order",
                schema: "dishes",
                table: "RecipeIngredients",
                columns: new[] { "RecipeId", "Order" },
                unique: true);
        }
    }
}
