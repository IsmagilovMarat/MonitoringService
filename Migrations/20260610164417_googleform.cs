using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringServiceCore.Migrations
{
    /// <inheritdoc />
    public partial class googleform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoogleFormsDetectionResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    DetectionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HasGoogleForms = table.Column<bool>(type: "boolean", nullable: false),
                    HtmlLoaded = table.Column<bool>(type: "boolean", nullable: false),
                    HtmlLength = table.Column<int>(type: "integer", nullable: false),
                    FormUrls = table.Column<List<string>>(type: "text[]", nullable: false),
                    FormTypes = table.Column<List<string>>(type: "text[]", nullable: false),
                    IsPotentiallyMalicious = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleFormsDetectionResults", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoogleFormsDetectionResults");
        }
    }
}
