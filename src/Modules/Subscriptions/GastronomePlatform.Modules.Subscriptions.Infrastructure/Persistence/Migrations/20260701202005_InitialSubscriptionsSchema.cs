using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastronomePlatform.Modules.Subscriptions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSubscriptionsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "subscriptions");

            migrationBuilder.CreateTable(
                name: "Promotions",
                schema: "subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    From = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    To = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    InternalNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotions", x => x.Id);
                    table.CheckConstraint("CK_Promotions_PeriodOrdered", "\"From\" < \"To\"");
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                schema: "subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanKind = table.Column<int>(type: "integer", nullable: false),
                    PublicName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TechnicalName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RequiredRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AvailableFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AvailableUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InternalNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromotionGrants",
                schema: "subscriptions",
                columns: table => new
                {
                    PromotionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Grant = table.Column<int>(type: "integer", nullable: false),
                    IsGrant = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionGrants", x => new { x.PromotionId, x.Grant });
                    table.ForeignKey(
                        name: "FK_PromotionGrants_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalSchema: "subscriptions",
                        principalTable: "Promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromotionTargets",
                schema: "subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<int>(type: "integer", nullable: false),
                    TargetValue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionTargets_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalSchema: "subscriptions",
                        principalTable: "Promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanGrants",
                schema: "subscriptions",
                columns: table => new
                {
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Grant = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanGrants", x => new { x.PlanId, x.Grant });
                    table.ForeignKey(
                        name: "FK_PlanGrants_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "subscriptions",
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanPrices",
                schema: "subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    PublicName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DurationDays = table.Column<int>(type: "integer", nullable: true),
                    Currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CompareAtAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    DiscountPercent = table.Column<int>(type: "integer", nullable: true),
                    TrialDays = table.Column<int>(type: "integer", nullable: true),
                    IsRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    IsPurchasable = table.Column<bool>(type: "boolean", nullable: false),
                    RenewsAsPriceId = table.Column<Guid>(type: "uuid", nullable: true),
                    FallbackPriceId = table.Column<Guid>(type: "uuid", nullable: true),
                    AvailableFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AvailableUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    InternalNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanPrices", x => x.Id);
                    table.CheckConstraint("CK_PlanPrices_AmountNonNegative", "\"Amount\" >= 0");
                    table.CheckConstraint("CK_PlanPrices_AvailabilityWindow", "\"AvailableFrom\" IS NULL OR \"AvailableUntil\" IS NULL OR \"AvailableFrom\" < \"AvailableUntil\"");
                    table.CheckConstraint("CK_PlanPrices_TrialRequiresFreeWithDays", "\"Kind\" <> 0 OR (\"Amount\" = 0 AND \"TrialDays\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_PlanPrices_PlanPrices_FallbackPriceId",
                        column: x => x.FallbackPriceId,
                        principalSchema: "subscriptions",
                        principalTable: "PlanPrices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanPrices_PlanPrices_RenewsAsPriceId",
                        column: x => x.RenewsAsPriceId,
                        principalSchema: "subscriptions",
                        principalTable: "PlanPrices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanPrices_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "subscriptions",
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                schema: "subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SnapshotAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SnapshotCurrency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CurrentPeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CurrentPeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TrialEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AutoRenew = table.Column<bool>(type: "boolean", nullable: false),
                    CancelAtPeriodEnd = table.Column<bool>(type: "boolean", nullable: false),
                    FailedAttempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    NextBillingAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    GatewayPaymentMethodId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RecurringDisabledReason = table.Column<int>(type: "integer", nullable: true),
                    CanceledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                    table.CheckConstraint("CK_UserSubscriptions_CurrentPeriodOrdered", "\"CurrentPeriodStart\" < \"CurrentPeriodEnd\"");
                    table.CheckConstraint("CK_UserSubscriptions_FailedAttemptsNonNegative", "\"FailedAttempts\" >= 0");
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_PlanPrices_CurrentPriceId",
                        column: x => x.CurrentPriceId,
                        principalSchema: "subscriptions",
                        principalTable: "PlanPrices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "subscriptions",
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionAgreements",
                schema: "subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ChangeType = table.Column<int>(type: "integer", nullable: false),
                    TermsSnapshot = table.Column<string>(type: "jsonb", nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContentHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EffectiveAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionAgreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionAgreements_UserSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalSchema: "subscriptions",
                        principalTable: "UserSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPayments",
                schema: "subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    GatewayTransactionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GatewayPayload = table.Column<string>(type: "jsonb", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPayments", x => x.Id);
                    table.CheckConstraint("CK_SubscriptionPayments_AmountNonNegative", "\"Amount\" >= 0");
                    table.ForeignKey(
                        name: "FK_SubscriptionPayments_PlanPrices_PriceId",
                        column: x => x.PriceId,
                        principalSchema: "subscriptions",
                        principalTable: "PlanPrices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionPayments_UserSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalSchema: "subscriptions",
                        principalTable: "UserSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanPrices_FallbackPriceId",
                schema: "subscriptions",
                table: "PlanPrices",
                column: "FallbackPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanPrices_PlanId_IsActive",
                schema: "subscriptions",
                table: "PlanPrices",
                columns: new[] { "PlanId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanPrices_RenewsAsPriceId",
                schema: "subscriptions",
                table: "PlanPrices",
                column: "RenewsAsPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionTargets_PromotionId_TargetType_TargetValue",
                schema: "subscriptions",
                table: "PromotionTargets",
                columns: new[] { "PromotionId", "TargetType", "TargetValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionAgreements_SubscriptionId_Version",
                schema: "subscriptions",
                table: "SubscriptionAgreements",
                columns: new[] { "SubscriptionId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_GatewayTransactionId",
                schema: "subscriptions",
                table: "SubscriptionPayments",
                column: "GatewayTransactionId",
                unique: true,
                filter: "\"GatewayTransactionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_PriceId",
                schema: "subscriptions",
                table: "SubscriptionPayments",
                column: "PriceId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_SubscriptionId_OccurredAt",
                schema: "subscriptions",
                table: "SubscriptionPayments",
                columns: new[] { "SubscriptionId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_TechnicalName",
                schema: "subscriptions",
                table: "SubscriptionPlans",
                column: "TechnicalName",
                unique: true,
                filter: "\"TechnicalName\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_CurrentPriceId",
                schema: "subscriptions",
                table: "UserSubscriptions",
                column: "CurrentPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_NextBillingAt",
                schema: "subscriptions",
                table: "UserSubscriptions",
                column: "NextBillingAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_PlanId",
                schema: "subscriptions",
                table: "UserSubscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId_Status",
                schema: "subscriptions",
                table: "UserSubscriptions",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanGrants",
                schema: "subscriptions");

            migrationBuilder.DropTable(
                name: "PromotionGrants",
                schema: "subscriptions");

            migrationBuilder.DropTable(
                name: "PromotionTargets",
                schema: "subscriptions");

            migrationBuilder.DropTable(
                name: "SubscriptionAgreements",
                schema: "subscriptions");

            migrationBuilder.DropTable(
                name: "SubscriptionPayments",
                schema: "subscriptions");

            migrationBuilder.DropTable(
                name: "Promotions",
                schema: "subscriptions");

            migrationBuilder.DropTable(
                name: "UserSubscriptions",
                schema: "subscriptions");

            migrationBuilder.DropTable(
                name: "PlanPrices",
                schema: "subscriptions");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans",
                schema: "subscriptions");
        }
    }
}
