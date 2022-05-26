using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CheckersWeb.Migrations
{
    public partial class AddBot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BlackPlayerBotDepth",
                table: "Games",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WhitePlayerBotDepth",
                table: "Games",
                type: "integer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlackPlayerBotDepth",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "WhitePlayerBotDepth",
                table: "Games");
        }
    }
}
