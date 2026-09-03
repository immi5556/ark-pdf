using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ark.Document.Scan.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScanJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalContentType = table.Column<string>(type: "TEXT", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ImageWidth = table.Column<int>(type: "INTEGER", nullable: false),
                    ImageHeight = table.Column<int>(type: "INTEGER", nullable: false),
                    Corners = table.Column<string>(type: "TEXT", nullable: false),
                    WasAutoDetected = table.Column<bool>(type: "INTEGER", nullable: false),
                    RotationDegrees = table.Column<int>(type: "INTEGER", nullable: false),
                    FilterMode = table.Column<string>(type: "TEXT", nullable: false),
                    Brightness = table.Column<double>(type: "REAL", nullable: false),
                    Contrast = table.Column<double>(type: "REAL", nullable: false),
                    OriginalImagePath = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessedImagePath = table.Column<string>(type: "TEXT", nullable: true),
                    ShareToken = table.Column<string>(type: "TEXT", nullable: true),
                    ShareCreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanJobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScanJobs_ShareToken",
                table: "ScanJobs",
                column: "ShareToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScanJobs");
        }
    }
}
