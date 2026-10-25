using System;
using BerberApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BerberApp.Infrastructure.Migrations
{
    // EF Core migration'ları bu iki attribute ile keşfeder; normalde
    // "dotnet ef migrations add" bunları .Designer.cs dosyasına yazar.
    // Attribute'suz bir migration sessizce yok sayılır — ne uygulanır ne
    // hata verir. Bu depoda 20260620 sonrası üç migration bu yüzden hiç
    // çalışmamış (tabloları Program.cs'teki ham SQL blokları oluşturmuş).
    [DbContext(typeof(AppDbContext))]
    [Migration("20261018120000_AddBroadcastJobs")]
    public partial class AddBroadcastJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BroadcastJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false, defaultValue: ""),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Total = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Sent = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Failed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Skipped = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BroadcastJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BroadcastJobs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BroadcastRecipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BroadcastJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BroadcastRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BroadcastRecipients_BroadcastJobs_BroadcastJobId",
                        column: x => x.BroadcastJobId,
                        principalTable: "BroadcastJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BroadcastJobs_TenantId_CreatedAt",
                table: "BroadcastJobs",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BroadcastRecipients_BroadcastJobId",
                table: "BroadcastRecipients",
                column: "BroadcastJobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BroadcastRecipients");
            migrationBuilder.DropTable(name: "BroadcastJobs");
        }
    }
}
