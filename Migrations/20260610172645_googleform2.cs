using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringServiceCore.Migrations
{
    /// <inheritdoc />
    public partial class googleform2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormTypes",
                table: "GoogleFormsDetectionResults");

            migrationBuilder.DropColumn(
                name: "FormUrls",
                table: "GoogleFormsDetectionResults");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "FormTypes",
                table: "GoogleFormsDetectionResults",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "FormUrls",
                table: "GoogleFormsDetectionResults",
                type: "text[]",
                nullable: false);
        }
    }
}
