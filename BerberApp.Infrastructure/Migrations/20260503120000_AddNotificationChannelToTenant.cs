using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BerberApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationChannelToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreferredNotificationChannel",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredNotificationChannel",
                table: "Tenants");
        }
    }
}
