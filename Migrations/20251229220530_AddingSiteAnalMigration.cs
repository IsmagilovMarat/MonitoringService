using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringServiceCore.Migrations
{
    /// <inheritdoc />
    public partial class AddingSiteAnalMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AnalyzedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NetCount = table.Column<int>(type: "integer", nullable: false),
                    DotNetCount = table.Column<int>(type: "integer", nullable: false),
                    AspNetCount = table.Column<int>(type: "integer", nullable: false),
                    TotalCharacters = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteAnalyses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteAnalyses_AnalyzedDate",
                table: "SiteAnalyses",
                column: "AnalyzedDate");

            migrationBuilder.CreateIndex(
                name: "IX_SiteAnalyses_Url",
                table: "SiteAnalyses",
                column: "Url");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteAnalyses");
        }
    }
}
