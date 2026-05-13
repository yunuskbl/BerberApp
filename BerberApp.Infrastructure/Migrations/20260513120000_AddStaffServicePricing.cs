using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BerberApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffServicePricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mevcut verileri yedekle
            migrationBuilder.Sql(@"
                CREATE TEMP TABLE ""_StaffServicesBackup"" AS
                SELECT ""ServicesId"" AS ""ServiceId"", ""StaffId"" FROM ""StaffServices"";
            ");

            // Eski junction tablosunu düşür
            migrationBuilder.DropTable(name: "StaffServices");

            // Yeni entity tablosunu oluştur
            migrationBuilder.CreateTable(
                name: "StaffServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CustomDurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffServices_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StaffServices_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffServices_ServiceId",
                table: "StaffServices",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffServices_StaffId",
                table: "StaffServices",
                column: "StaffId");

            // Yedekten mevcut atamaları geri yükle
            migrationBuilder.Sql(@"
                INSERT INTO ""StaffServices"" (""Id"", ""StaffId"", ""ServiceId"", ""CustomPrice"", ""CustomDurationMinutes"", ""CreatedAt"", ""IsDeleted"")
                SELECT gen_random_uuid(), ""StaffId"", ""ServiceId"", NULL, NULL, NOW(), FALSE
                FROM ""_StaffServicesBackup"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "StaffServices");

            migrationBuilder.CreateTable(
                name: "StaffServices",
                columns: table => new
                {
                    ServicesId = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffServices", x => new { x.ServicesId, x.StaffId });
                    table.ForeignKey(
                        name: "FK_StaffServices_Services_ServicesId",
                        column: x => x.ServicesId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StaffServices_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffServices_StaffId",
                table: "StaffServices",
                column: "StaffId");
        }
    }
}
