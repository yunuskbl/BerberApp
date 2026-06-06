using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BerberApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentCompletionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ActualTotalPrice",
                table: "Appointments",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionNotes",
                table: "Appointments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppointmentActualServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentActualServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentActualServices_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentActualServices_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentActualServices_AppointmentId",
                table: "AppointmentActualServices",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentActualServices_ServiceId",
                table: "AppointmentActualServices",
                column: "ServiceId");

            migrationBuilder.CreateTable(
                name: "PriceDifferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ActualPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Difference = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceDifferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceDifferences_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PriceDifferences_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceDifferences_TenantId",
                table: "PriceDifferences",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceDifferences_AppointmentId",
                table: "PriceDifferences",
                column: "AppointmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PriceDifferences");
            migrationBuilder.DropTable(name: "AppointmentActualServices");

            migrationBuilder.DropColumn(name: "ActualTotalPrice", table: "Appointments");
            migrationBuilder.DropColumn(name: "CompletedAt",      table: "Appointments");
            migrationBuilder.DropColumn(name: "CompletionNotes",  table: "Appointments");
        }
    }
}
