using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastronomePlatform.Modules.Media.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMediaSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "media");

            migrationBuilder.CreateTable(
                name: "MediaFiles",
                schema: "media",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    DataCategory = table.Column<int>(type: "integer", nullable: false),
                    MediaType = table.Column<int>(type: "integer", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StorageProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AttachedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaFiles", x => x.Id);
                    table.CheckConstraint("CK_MediaFiles_DimensionsBothOrNone", "(\"Width\" IS NULL AND \"Height\" IS NULL) OR (\"Width\" IS NOT NULL AND \"Height\" IS NOT NULL)");
                    table.CheckConstraint("CK_MediaFiles_EntityRefsMatchNullity", "(\"EntityType\" IS NULL AND \"EntityId\" IS NULL) OR (\"EntityType\" IS NOT NULL AND \"EntityId\" IS NOT NULL)");
                    table.CheckConstraint("CK_MediaFiles_PersonalRequiresOwner", "\"DataCategory\" <> 1 OR \"OwnerUserId\" IS NOT NULL");
                    table.CheckConstraint("CK_MediaFiles_SizeBytesPositive", "\"SizeBytes\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "MediaThumbnails",
                schema: "media",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: false),
                    Format = table.Column<int>(type: "integer", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaThumbnails", x => x.Id);
                    table.CheckConstraint("CK_MediaThumbnails_DimensionsPositive", "\"Width\" > 0 AND \"Height\" > 0");
                    table.CheckConstraint("CK_MediaThumbnails_SizeBytesPositive", "\"SizeBytes\" > 0");
                    table.ForeignKey(
                        name: "FK_MediaThumbnails_MediaFiles_MediaFileId",
                        column: x => x.MediaFileId,
                        principalSchema: "media",
                        principalTable: "MediaFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_EntityType_EntityId",
                schema: "media",
                table: "MediaFiles",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_HardDeleteCandidates",
                schema: "media",
                table: "MediaFiles",
                column: "DeletedAt",
                filter: "\"Status\" = 4");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_OrphanCleanup",
                schema: "media",
                table: "MediaFiles",
                columns: new[] { "Status", "ExpiresAt" },
                filter: "\"EntityType\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_OwnerUserId",
                schema: "media",
                table: "MediaFiles",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "UX_MediaThumbnails_File_Size_Format",
                schema: "media",
                table: "MediaThumbnails",
                columns: new[] { "MediaFileId", "Size", "Format" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaThumbnails",
                schema: "media");

            migrationBuilder.DropTable(
                name: "MediaFiles",
                schema: "media");
        }
    }
}
