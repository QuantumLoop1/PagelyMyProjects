using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pagely.Migrations
{
    public partial class RemoveIsDeleted : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Pages");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Pages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
