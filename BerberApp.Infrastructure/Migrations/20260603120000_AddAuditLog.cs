using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BerberApp.Infrastructure.Migrations
{
    public partial class AddAuditLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id          = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType   = table.Column<string>(type: "character varying(50)",  maxLength: 50,  nullable: false),
                    Severity    = table.Column<string>(type: "character varying(20)",  maxLength: 20,  nullable: false),
                    UserId      = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId    = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IpAddress   = table.Column<string>(type: "character varying(50)",  maxLength: 50,  nullable: false),
                    UserAgent   = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Path        = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Method      = table.Column<string>(type: "character varying(10)",  maxLength: 10,  nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt   = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Severity",
                table: "AuditLogs",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EventType",
                table: "AuditLogs",
                column: "EventType");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AuditLogs");
        }
    }
}
