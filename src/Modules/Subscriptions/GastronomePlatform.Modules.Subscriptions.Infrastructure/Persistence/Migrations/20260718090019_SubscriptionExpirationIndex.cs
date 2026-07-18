using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastronomePlatform.Modules.Subscriptions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SubscriptionExpirationIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_Status_CurrentPeriodEnd",
                schema: "subscriptions",
                table: "UserSubscriptions",
                columns: new[] { "Status", "CurrentPeriodEnd" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_Status_CurrentPeriodEnd",
                schema: "subscriptions",
                table: "UserSubscriptions");
        }
    }
}
