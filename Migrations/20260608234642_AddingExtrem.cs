using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringServiceCore.Migrations
{
    /// <inheritdoc />
    public partial class AddingExtrem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "Resources",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Resources",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateTable(
                name: "ExtremistCheckResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    CheckTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HasExtremistMaterials = table.Column<bool>(type: "boolean", nullable: false),
                    FoundMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtremistCheckResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FoundMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    MatchedKeyword = table.Column<string>(type: "text", nullable: true),
                    MatchType = table.Column<string>(type: "text", nullable: true),
                    Context = table.Column<string>(type: "text", nullable: true),
                    DecisionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckResultId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoundMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoundMaterials_ExtremistCheckResults_CheckResultId",
                        column: x => x.CheckResultId,
                        principalTable: "ExtremistCheckResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoundMaterials_CheckResultId",
                table: "FoundMaterials",
                column: "CheckResultId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FoundMaterials");

            migrationBuilder.DropTable(
                name: "ExtremistCheckResults");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "Resources",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Resources",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
