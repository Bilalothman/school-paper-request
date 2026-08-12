using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestResultImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "ResultImage",
                table: "Requests",
                type: "longblob",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultImageContentType",
                table: "Requests",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ResultImageFileName",
                table: "Requests",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResultImage",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ResultImageContentType",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ResultImageFileName",
                table: "Requests");
        }
    }
}
