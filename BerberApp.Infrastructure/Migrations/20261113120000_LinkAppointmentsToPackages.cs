using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BerberApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkAppointmentsToPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentId",
                table: "PackageSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerPackageId",
                table: "Appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PackageSessions_AppointmentId",
                table: "PackageSessions",
                column: "AppointmentId",
                unique: true,
                filter: "\"AppointmentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CustomerPackageId",
                table: "Appointments",
                column: "CustomerPackageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_CustomerPackages_CustomerPackageId",
                table: "Appointments",
                column: "CustomerPackageId",
                principalTable: "CustomerPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PackageSessions_Appointments_AppointmentId",
                table: "PackageSessions",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_CustomerPackages_CustomerPackageId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_PackageSessions_Appointments_AppointmentId",
                table: "PackageSessions");

            migrationBuilder.DropIndex(
                name: "IX_PackageSessions_AppointmentId",
                table: "PackageSessions");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_CustomerPackageId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "PackageSessions");

            migrationBuilder.DropColumn(
                name: "CustomerPackageId",
                table: "Appointments");
        }
    }
}
