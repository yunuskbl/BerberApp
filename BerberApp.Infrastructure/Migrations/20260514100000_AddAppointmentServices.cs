using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BerberApp.Infrastructure.Migrations;

public partial class AddAppointmentServices : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AppointmentServices",
            columns: table => new
            {
                Id          = table.Column<Guid>(nullable: false, defaultValueSql: "gen_random_uuid()"),
                AppointmentId = table.Column<Guid>(nullable: false),
                ServiceId   = table.Column<Guid>(nullable: false),
                CreatedAt   = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                UpdatedAt   = table.Column<DateTime>(nullable: true),
                IsDeleted   = table.Column<bool>(nullable: false, defaultValue: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AppointmentServices", x => x.Id);
                table.ForeignKey(
                    name: "FK_AppointmentServices_Appointments_AppointmentId",
                    column: x => x.AppointmentId,
                    principalTable: "Appointments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_AppointmentServices_Services_ServiceId",
                    column: x => x.ServiceId,
                    principalTable: "Services",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_AppointmentServices_AppointmentId",
            table: "AppointmentServices",
            column: "AppointmentId");

        migrationBuilder.CreateIndex(
            name: "IX_AppointmentServices_ServiceId",
            table: "AppointmentServices",
            column: "ServiceId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AppointmentServices");
    }
}
