using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BerberApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceNameTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThemeColor",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "Services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameRu",
                table: "Services",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TenantPhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantPhotos_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantPhotos_TenantId",
                table: "TenantPhotos",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantPhotos");

            migrationBuilder.DropColumn(
                name: "ThemeColor",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "NameRu",
                table: "Services");
        }
    }
}
