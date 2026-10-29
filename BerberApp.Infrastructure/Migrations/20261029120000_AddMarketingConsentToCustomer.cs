using System;
using BerberApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BerberApp.Infrastructure.Migrations
{
    // Attribute'lar olmadan EF migration'ı görmez: ne uygular ne hata verir.
    // Bu depoda 20260620 sonrası üç migration tam bu yüzden ölü kaldı.
    [DbContext(typeof(AppDbContext))]
    [Migration("20261029120000_AddMarketingConsentToCustomer")]
    public partial class AddMarketingConsentToCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mevcut müşterilerde null kalır: geçmişte rıza alınmadığı için
            // "rıza var" varsaymak yanlış olurdu.
            migrationBuilder.AddColumn<DateTime>(
                name: "MarketingConsentAt",
                table: "Customers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarketingConsentAt",
                table: "Customers");
        }
    }
}
