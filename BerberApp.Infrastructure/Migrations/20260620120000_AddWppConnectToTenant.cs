using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BerberApp.Infrastructure.Migrations
{
    public partial class AddWppConnectToTenant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WppConnectSession",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WppConnectToken",
                table: "Tenants",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WppConnectSession",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "WppConnectToken",
                table: "Tenants");
        }
    }
}
