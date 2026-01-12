using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringServiceCore.Migrations
{
    /// <inheritdoc />
    public partial class AddingSiteAnalMigraion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SiteAnalyses_AnalyzedDate",
                table: "SiteAnalyses");

            migrationBuilder.DropIndex(
                name: "IX_SiteAnalyses_Url",
                table: "SiteAnalyses");

            migrationBuilder.DropColumn(
                name: "AspNetCount",
                table: "SiteAnalyses");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "SiteAnalyses");

            migrationBuilder.DropColumn(
                name: "DotNetCount",
                table: "SiteAnalyses");

            migrationBuilder.DropColumn(
                name: "NetCount",
                table: "SiteAnalyses");

            migrationBuilder.RenameColumn(
                name: "TotalCharacters",
                table: "SiteAnalyses",
                newName: "CountOfViolations");

            migrationBuilder.AddColumn<string>(
                name: "DomainUrl",
                table: "SiteAnalyses",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DomainUrl",
                table: "SiteAnalyses");

            migrationBuilder.RenameColumn(
                name: "CountOfViolations",
                table: "SiteAnalyses",
                newName: "TotalCharacters");

            migrationBuilder.AddColumn<int>(
                name: "AspNetCount",
                table: "SiteAnalyses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "SiteAnalyses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DotNetCount",
                table: "SiteAnalyses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NetCount",
                table: "SiteAnalyses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SiteAnalyses_AnalyzedDate",
                table: "SiteAnalyses",
                column: "AnalyzedDate");

            migrationBuilder.CreateIndex(
                name: "IX_SiteAnalyses_Url",
                table: "SiteAnalyses",
                column: "Url");
        }
    }
}
