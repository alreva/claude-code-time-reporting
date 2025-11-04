using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeReportingApi.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEmailAndName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserEmail",
                table: "time_entries",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "time_entries",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserEmail",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "time_entries");
        }
    }
}
