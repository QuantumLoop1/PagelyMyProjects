using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pagely.Migrations
{
    public partial class AddPageScheduling : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledFor",
                table: "Pages",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Pages",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Todo");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduledFor",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Pages");
        }
    }
}
